using System.Globalization;
using System.Text;

namespace Formularkarte;

/// <summary>
/// Schreibt das Razor-Skelett einer Maske - den Rohbau des Dialogs in
/// <c>EPOS.UI/Dialoge/</c>.
///
/// <para>
/// Vorbild ist der erste fertige Dialog
/// <c>EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor</c>: Wurzel-div
/// mit <c>tabindex="-1"</c> und <c>@onkeydown</c> (Enter/Esc), Kopfzeile mit
/// Titel und <c>InfoKnopf</c>, die Felder als Standardkomponenten, ein
/// <c>Warnbanner</c> fuer die Pruefung und eine <c>SpeichernLeiste</c>, die das
/// Ergebnis ueber einen <c>EventCallback</c> meldet.
/// </para>
/// <para>
/// Das Skelett ist absichtlich leer im Fach: Es bindet Felder an ein
/// Werte-Record, benennt jeden Ereignishandler des Bestands als TODO mit
/// Fundstelle und Umfang und laesst alles Uebrige offen. Es soll uebersetzen,
/// nicht rechnen.
/// </para>
/// </summary>
public static class RazorSchreiber
{
    /// <summary>Namen, die im erzeugten Bauteil schon vergeben sind.</summary>
    private static readonly string[] Belegt =
    {
        "Werte", "Ergebnis", "Geschlossen", "TitelText", "OkText", "AbbrechenText",
        "SpeichernText", "HilfeSchluessel", "BeiErgebnis", "BeiTaste", "Meldung"
    };

    public static string Schreiben(Maske maske)
    {
        var abschnitte = Kartenbau.Abschnitte(maske);
        var bauteil = Bauteilname(maske.Bezeichner);
        var namen = Namen(maske, abschnitte, bauteil);

        var werkzeug = new StringBuilder();
        Kopfkommentar(werkzeug, maske);
        werkzeug.Append("@namespace ").Append(Namensraum(maske)).Append("\n\n");

        var hilfe = Knopf(abschnitte, "InfoKnopf");
        var schliesser = abschnitte.SelectMany(a => a.Zeilen)
            .Where(z => z.Komponente == "SpeichernLeiste").Select(z => z.Element).ToList();

        // ---- Ansicht --------------------------------------------------------
        werkzeug.Append("<div class=\"epos-dialog\" tabindex=\"-1\" @ref=\"_wurzel\" @onkeydown=\"BeiTaste\">\n\n");
        werkzeug.Append("    <div class=\"epos-dialog-kopf\">\n");
        werkzeug.Append("        <h1 class=\"epos-dialog-titel\">@TitelText</h1>\n");
        if (hilfe is not null)
        {
            werkzeug.Append("        <InfoKnopf Schluessel=\"@HilfeSchluessel\" />\n");
        }
        werkzeug.Append("    </div>\n\n");

        werkzeug.Append("    @if (!string.IsNullOrEmpty(_meldung))\n    {\n");
        werkzeug.Append("        <Warnbanner Stufe=\"WarnStufe.Warnung\" Text=\"@_meldung\" />\n    }\n\n");

        foreach (var abschnitt in abschnitte)
        {
            var zeilen = abschnitt.Zeilen.Where(SollInDenKoerper).ToList();
            if (zeilen.Count == 0) continue;

            if (abschnitt.Traeger is null)
            {
                foreach (var zeile in zeilen) Feld(werkzeug, zeile, namen, "    ");
                werkzeug.Append('\n');
                continue;
            }

            werkzeug.Append("    @* Abschnitt `").Append(abschnitt.Traeger.Name)
                    .Append("` (").Append(abschnitt.Traeger.Typ).Append(") *@\n");
            werkzeug.Append("    <Gruppenkopf Titel=\"@").Append(namen[abschnitt.Traeger.Name]).Append("Titel\">\n");
            werkzeug.Append("        <KindInhalt>\n");
            foreach (var zeile in zeilen) Feld(werkzeug, zeile, namen, "            ");
            werkzeug.Append("        </KindInhalt>\n");
            werkzeug.Append("    </Gruppenkopf>\n\n");
        }

        var mitSpeichern = schliesser.Any(k => Typtabelle.Kernname(k.Name)
            .StartsWith("Speich", StringComparison.OrdinalIgnoreCase));
        werkzeug.Append("    <SpeichernLeiste OkText=\"@OkText\" AbbrechenText=\"@AbbrechenText\"");
        if (mitSpeichern) werkzeug.Append(" MitSpeichern=\"true\"");
        werkzeug.Append(" Ergebnis=\"BeiErgebnis\" />\n");
        werkzeug.Append("</div>\n\n");

        // ---- Code -----------------------------------------------------------
        werkzeug.Append("@code {\n");
        Bausteine(werkzeug, maske, abschnitte, namen, bauteil, hilfe, schliesser);
        werkzeug.Append("}\n");
        return werkzeug.ToString();
    }

    /// <summary>
    /// Der Klassenname des Bauteils ist der Dateiname der Maske - mit grossem
    /// Anfangsbuchstaben, denn Razor laesst kleingeschriebene Komponentennamen
    /// nicht zu (RZ10011). Aus <c>ucKostenItem</c> wird deshalb
    /// <c>UcKostenItem</c>.
    /// </summary>
    public static string Bauteilname(string bezeichner)
    {
        var werkzeug = new StringBuilder();
        foreach (var zeichen in bezeichner)
        {
            werkzeug.Append(char.IsLetterOrDigit(zeichen) || zeichen == '_' ? zeichen : '_');
        }
        var name = werkzeug.ToString();
        if (name.Length == 0) return "Unbenannt";
        if (char.IsDigit(name[0])) return "_" + name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>Der Dateiname des Skeletts - er bestimmt den Komponentennamen.</summary>
    public static string Dateiname(Maske maske) => Bauteilname(maske.Bezeichner) + ".razor";

    /// <summary>Zielnamensraum: <c>EPOS.UI.Dialoge.&lt;Fachbereich&gt;</c>, Umlaute umschrieben.</summary>
    public static string Namensraum(Maske maske) => "EPOS.UI.Dialoge." + Umschreiben(maske.Ordner);

    /// <summary>Umlaute und Sonderzeichen fuer Bezeichner umschreiben.</summary>
    public static string Umschreiben(string text)
    {
        var werkzeug = new StringBuilder();
        foreach (var zeichen in text)
        {
            switch (zeichen)
            {
                case 'ä': werkzeug.Append("ae"); break;
                case 'ö': werkzeug.Append("oe"); break;
                case 'ü': werkzeug.Append("ue"); break;
                case 'Ä': werkzeug.Append("Ae"); break;
                case 'Ö': werkzeug.Append("Oe"); break;
                case 'Ü': werkzeug.Append("Ue"); break;
                case 'ß': werkzeug.Append("ss"); break;
                default:
                    werkzeug.Append(char.IsLetterOrDigit(zeichen) || zeichen == '_' ? zeichen : '_');
                    break;
            }
        }
        var name = werkzeug.ToString();
        if (name.Length == 0) return "Unbenannt";
        return char.IsDigit(name[0]) ? "_" + name : name;
    }

    /// <summary>
    /// Eindeutige C#-Bezeichner fuer alle Steuerelemente der Maske.
    ///
    /// <para>
    /// Geprueft wird nicht der Bezeichner allein, sondern alles, was aus ihm
    /// entsteht: <c>&lt;Name&gt;Text</c>, <c>&lt;Name&gt;Eintraege</c>,
    /// <c>&lt;Name&gt;Titel</c> und <c>Bei&lt;Name&gt;</c>. Sonst gaebe ein
    /// Label namens "lblTitel" die Eigenschaft "TitelText" - die es im Bauteil
    /// schon gibt.
    /// </para>
    /// </summary>
    private static Dictionary<string, string> Namen(Maske maske, List<Abschnitt> abschnitte, string bauteil)
    {
        var vergeben = new HashSet<string>(Belegt, StringComparer.Ordinal)
        {
            bauteil, bauteil + "Werte", bauteil + "Ergebnis",
            "OnAfterRenderAsync", "_wurzel", "_ersterAufbau", "_meldung"
        };
        var namen = new Dictionary<string, string>(StringComparer.Ordinal);

        var elemente = abschnitte.SelectMany(a => a.Zeilen).Select(z => z.Element)
            .Concat(abschnitte.Where(a => a.Traeger is not null).Select(a => a.Traeger!));

        foreach (var element in elemente)
        {
            if (namen.ContainsKey(element.Name)) continue;

            var vorschlag = Umschreiben(Typtabelle.Kernname(element.Name));
            if (!Frei(vergeben, vorschlag)) vorschlag = Umschreiben(element.Name);
            var kern = vorschlag;
            var zaehler = 2;
            while (!Frei(vergeben, vorschlag))
            {
                vorschlag = kern + zaehler.ToString(CultureInfo.InvariantCulture);
                zaehler++;
            }
            Belegen(vergeben, vorschlag);
            namen[element.Name] = vorschlag;
        }
        return namen;
    }

    private static IEnumerable<string> Abkoemmlinge(string name)
    {
        yield return name;
        yield return name + "Text";
        yield return name + "Eintraege";
        yield return name + "Titel";
        yield return "Bei" + name;
    }

    private static bool Frei(HashSet<string> vergeben, string name) =>
        Abkoemmlinge(name).All(n => !vergeben.Contains(n));

    private static void Belegen(HashSet<string> vergeben, string name)
    {
        foreach (var abkoemmling in Abkoemmlinge(name)) vergeben.Add(abkoemmling);
    }

    private static Steuerelement? Knopf(List<Abschnitt> abschnitte, string komponente) =>
        abschnitte.SelectMany(a => a.Zeilen).FirstOrDefault(z => z.Komponente == komponente)?.Element;

    /// <summary>OK/Abbrechen und der Hilfeknopf stehen im Rahmen, nicht im Koerper.</summary>
    private static bool SollInDenKoerper(Zeile zeile) =>
        zeile.Komponente is not ("SpeichernLeiste" or "InfoKnopf");

    private static void Feld(StringBuilder werkzeug, Zeile zeile, Dictionary<string, string> namen, string einzug)
    {
        var element = zeile.Element;
        var name = namen[element.Name];

        switch (zeile.Komponente)
        {
            case "Zahlenfeld":
                werkzeug.Append(einzug).Append("<Zahlenfeld Bezeichnung=\"@").Append(name).Append("Text\"")
                        .Append(Grenze(element, "Minimum", "Min"))
                        .Append(Grenze(element, "Maximum", "Max"))
                        .Append(Grenze(element, "DecimalPlaces", "Nachkommastellen"))
                        .Append(" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Ganzzahlfeld":
                werkzeug.Append(einzug).Append("<Ganzzahlfeld Bezeichnung=\"@").Append(name).Append("Text\"")
                        .Append(Grenze(element, "Minimum", "Min"))
                        .Append(Grenze(element, "Maximum", "Max"))
                        .Append(" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Textfeld":
                werkzeug.Append(einzug).Append("<Textfeld Bezeichnung=\"@").Append(name).Append("Text\"")
                        .Append(Grenze(element, "MaxLength", "Hoechstlaenge"))
                        .Append(" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Auswahlfeld":
            case "Auswahlfeld (Gruppe pruefen)":
                if (element.Typ == "RadioButton")
                {
                    werkzeug.Append(einzug).Append("@* TODO: `").Append(element.Name)
                            .Append("` gehoert zu einer Optionsgruppe - die Gruppe zu EINEM Auswahlfeld zusammenfassen. *@\n");
                    werkzeug.Append(einzug).Append("<Schalter Bezeichnung=\"@").Append(name)
                            .Append("Text\" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                    break;
                }
                werkzeug.Append(einzug).Append("<Auswahlfeld Bezeichnung=\"@").Append(name).Append("Text\"")
                        .Append(" Eintraege=\"@").Append(name).Append("Eintraege\"")
                        .Append(" @bind-Auswahl=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Schalter":
                werkzeug.Append(einzug).Append("<Schalter Bezeichnung=\"@").Append(name)
                        .Append("Text\" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Datumsfeld":
                werkzeug.Append(einzug).Append("<Datumsfeld Bezeichnung=\"@").Append(name)
                        .Append("Text\" @bind-Wert=\"Werte.").Append(name).Append("\" />\n");
                break;

            case "Raster":
                werkzeug.Append(einzug).Append("<Raster TZeile=\"object\" Zeilen=\"@Werte.").Append(name).Append("\">\n");
                werkzeug.Append(einzug).Append("    <KindInhalt>\n");
                werkzeug.Append(einzug).Append("        @* TODO: Spalten aus `").Append(element.Name)
                        .Append("` als PropertyColumn nachtragen. *@\n");
                werkzeug.Append(einzug).Append("    </KindInhalt>\n");
                werkzeug.Append(einzug).Append("</Raster>\n");
                break;

            case "ChartBild":
                werkzeug.Append(einzug).Append("<ChartBild Png=\"@Werte.").Append(name)
                        .Append("\" Alt=\"@").Append(name).Append("Text\" />\n");
                break;

            case "Text":
                werkzeug.Append(einzug).Append("<p class=\"epos-feld-text\">@").Append(name).Append("Text</p>\n");
                break;

            case "Knopf (pruefen)":
                werkzeug.Append(einzug).Append("<button type=\"button\" class=\"epos-knopf\" @onclick=\"Bei")
                        .Append(name).Append("\">@").Append(name).Append("Text</button>\n");
                break;

            default:
                werkzeug.Append(einzug).Append("@* TODO pruefen: `").Append(element.Name).Append("` (")
                        .Append(element.Typ).Append(") hat keine Entsprechung in EPOS.UI. *@\n");
                break;
        }
    }

    /// <summary>Zahlengrenze als Razor-Attribut, wenn der Designer eine nennt.</summary>
    private static string Grenze(Steuerelement element, string eigenschaft, string attribut)
    {
        var wert = element.Wert(eigenschaft);
        if (string.IsNullOrWhiteSpace(wert)) return "";
        if (!decimal.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out var zahl)) return "";
        return " " + attribut + "=\"" + zahl.ToString(CultureInfo.InvariantCulture) + "\"";
    }

    private static void Kopfkommentar(StringBuilder werkzeug, Maske maske)
    {
        werkzeug.Append("@* Skelett zu ").Append(maske.Bezeichner).Append(" - erzeugt von Werkzeuge/Formularkarte.\n\n");
        werkzeug.Append("   Vorbild: ").Append(maske.Datei).Append("\n");
        werkzeug.Append("   Feldkarte: ").Append(maske.Bezeichner).Append(".karte.md\n\n");
        werkzeug.Append("   Der Rohbau bindet die Felder an ein Werte-Record und nennt jeden\n");
        werkzeug.Append("   Ereignishandler des Vorbilds als TODO mit Fundstelle und Umfang. Fach,\n");
        werkzeug.Append("   Pruefungen und Datenwege gehoeren in die Hand des Menschen - der\n");
        werkzeug.Append("   Generator sichert nur die Vollstaendigkeit.\n\n");
        werkzeug.Append("   TEXTE stehen als deutsche Literale in [Parameter] string-Vorgaben, wie\n");
        werkzeug.Append("   im ersten Dialog EnergietraegerVarianteDialog.razor. Sobald die\n");
        werkzeug.Append("   Ressourcenschluessel liegen, setzt die Huelle sie beim Erzeugen. *@\n\n");
    }

    private static void Bausteine(StringBuilder werkzeug, Maske maske, List<Abschnitt> abschnitte,
                                  Dictionary<string, string> namen, string bauteil,
                                  Steuerelement? hilfe, List<Steuerelement> schliesser)
    {
        var zeilen = abschnitte.SelectMany(a => a.Zeilen).ToList();
        var werte = bauteil + "Werte";
        var ergebnis = bauteil + "Ergebnis";

        werkzeug.Append("    /// <summary>Die Eingabewerte des Dialogs. Die Huelle fuellt sie und liest sie zurueck.</summary>\n");
        werkzeug.Append("    [Parameter] public ").Append(werte).Append(" Werte { get; set; } = new();\n\n");

        werkzeug.Append("    /// <summary>Der Dialog ist zu Ende: das Ergebnis bei OK, <c>null</c> bei Abbrechen.</summary>\n");
        werkzeug.Append("    [Parameter] public EventCallback<").Append(ergebnis).Append("?> Geschlossen { get; set; }\n\n");

        Text(werkzeug, "TitelText", maske.Titel, "Fenstertitel");
        Text(werkzeug, "OkText", Aufschrift(schliesser, "ok", "OK"), "Beschriftung des OK-Knopfes");
        Text(werkzeug, "AbbrechenText", Aufschrift(schliesser, "abbrechen", "Abbrechen"), "Beschriftung des Abbrechen-Knopfes");

        if (hilfe is not null)
        {
            werkzeug.Append("    /// <summary>Schluessel des Hilfeknopfes - die Zeile in help_mapping.txt.</summary>\n");
            werkzeug.Append("    [Parameter] public string HilfeSchluessel { get; set; } = \"")
                    .Append(maske.Bezeichner).Append('.').Append(hilfe.Name).Append("\";\n\n");
        }

        // Abschnittsueberschriften.
        foreach (var abschnitt in abschnitte.Where(a => a.Traeger is not null))
        {
            if (!abschnitt.Zeilen.Any(SollInDenKoerper)) continue;
            var name = namen[abschnitt.Traeger!.Name];
            Text(werkzeug, name + "Titel", abschnitt.Titel,
                 "Ueberschrift des Abschnitts `" + abschnitt.Traeger.Name + "`");
        }

        // Beschriftungen und Auswahllisten.
        foreach (var zeile in zeilen.Where(SollInDenKoerper))
        {
            var name = namen[zeile.Element.Name];
            Text(werkzeug, name + "Text", Kartenbau.TextDe(zeile.Element),
                 "Beschriftung zu `" + zeile.Element.Name + "` (" + zeile.Element.Typ + ")");

            if (zeile.Komponente == "Auswahlfeld" && zeile.Element.Typ != "RadioButton")
            {
                Auswahlliste(werkzeug, name, zeile.Element);
            }
        }

        werkzeug.Append("    private ElementReference _wurzel;\n");
        werkzeug.Append("    private bool _ersterAufbau = true;\n");
        werkzeug.Append("    private string _meldung = \"\";\n\n");

        werkzeug.Append("    /// <summary>Wie im ersten Dialog: der Erstfokus liegt auf dem Fenster, nicht im ersten Feld.</summary>\n");
        werkzeug.Append("    protected override async Task OnAfterRenderAsync(bool firstRender)\n    {\n");
        werkzeug.Append("        if (!firstRender || !_ersterAufbau) return;\n");
        werkzeug.Append("        _ersterAufbau = false;\n");
        werkzeug.Append("        await _wurzel.FocusAsync();\n    }\n\n");

        werkzeug.Append("    private async Task BeiErgebnis(bool ok)\n    {\n");
        werkzeug.Append("        if (!ok)\n        {\n");
        werkzeug.Append("            await Geschlossen.InvokeAsync(null);\n            return;\n        }\n\n");
        werkzeug.Append("        // TODO: Pruefungen des Vorbilds hier beantworten - jede MessageBox\n");
        werkzeug.Append("        // wird ein Warnbanner ueber _meldung, der Dialog bleibt dann offen.\n");
        werkzeug.Append("        await Geschlossen.InvokeAsync(new ").Append(ergebnis).Append("(Werte));\n    }\n\n");

        werkzeug.Append("    /// <summary>Enter und Esc: Eine BlazorWebView sieht AcceptButton und CancelButton nicht.</summary>\n");
        werkzeug.Append("    private Task BeiTaste(KeyboardEventArgs e) => e.Key switch\n    {\n");
        werkzeug.Append("        \"Enter\" => BeiErgebnis(true),\n");
        werkzeug.Append("        \"Escape\" => BeiErgebnis(false),\n");
        werkzeug.Append("        _ => Task.CompletedTask\n    };\n\n");

        // Knoepfe ausserhalb der SpeichernLeiste.
        foreach (var zeile in zeilen.Where(z => z.Komponente == "Knopf (pruefen)"))
        {
            var name = namen[zeile.Element.Name];
            werkzeug.Append("    private void Bei").Append(name).Append("()\n    {\n");
            werkzeug.Append("        ").Append(Todo(maske, zeile.Element)).Append("\n    }\n\n");
        }

        // Die restlichen Ereignisse als TODO-Zeilen.
        var offen = zeilen.Select(z => z.Element)
            .Where(e => e.Ereignisse.Count > 0 && Kartenbau.Ziel(e).Komponente != "Knopf (pruefen)")
            .ToList();
        if (offen.Count > 0 || maske.FormularEreignisse.Count > 0)
        {
            werkzeug.Append("    // Ereignisse des Vorbilds, die noch zu beantworten sind:\n");
            foreach (var element in offen)
            {
                foreach (var anmeldung in element.Ereignisse)
                {
                    werkzeug.Append("    // TODO: ").Append(element.Name).Append('.').Append(anmeldung.Ereignis)
                            .Append(" -> ").Append(Stelle(maske, anmeldung.Handler)).Append('\n');
                }
            }
            foreach (var anmeldung in maske.FormularEreignisse)
            {
                werkzeug.Append("    // TODO: Fenster.").Append(anmeldung.Ereignis)
                        .Append(" -> ").Append(Stelle(maske, anmeldung.Handler)).Append('\n');
            }
            werkzeug.Append('\n');
        }

        Werterecord(werkzeug, werte, zeilen, namen);
        werkzeug.Append("    /// <summary>Was der Dialog bei OK zurueckgibt.</summary>\n");
        werkzeug.Append("    public sealed record ").Append(ergebnis).Append('(').Append(werte).Append(" Werte);\n");
    }

    private static void Werterecord(StringBuilder werkzeug, string werte, List<Zeile> zeilen,
                                    Dictionary<string, string> namen)
    {
        werkzeug.Append("    /// <summary>\n");
        werkzeug.Append("    /// Ein Feld je Eingabe des Vorbilds. Der Satz gehoert spaeter in eine\n");
        werkzeug.Append("    /// eigene Datei neben den Dialog - so wie EnergietraegerVarianteErgebnis.cs.\n");
        werkzeug.Append("    /// </summary>\n");
        werkzeug.Append("    public sealed record ").Append(werte).Append("\n    {\n");

        var leer = true;
        foreach (var zeile in zeilen.Where(SollInDenKoerper))
        {
            var typ = Wertetyp(zeile);
            if (typ is null) continue;
            leer = false;
            var name = namen[zeile.Element.Name];
            werkzeug.Append("        /// <summary>`").Append(zeile.Element.Name).Append("` (")
                    .Append(zeile.Element.Typ).Append(").</summary>\n");
            werkzeug.Append("        public ").Append(typ.Value.Typ).Append(' ').Append(name)
                    .Append(" { get; set; }").Append(typ.Value.Vorgabe).Append('\n');
        }
        if (leer)
        {
            werkzeug.Append("        // Die Maske hat kein Eingabefeld - der Satz bleibt vorerst leer.\n");
        }
        werkzeug.Append("    }\n\n");
    }

    /// <summary>Typ und Vorgabewert der Eigenschaft im Werte-Record.</summary>
    private static (string Typ, string Vorgabe)? Wertetyp(Zeile zeile) => zeile.Komponente switch
    {
        "Zahlenfeld" => ("double?", ""),
        "Ganzzahlfeld" => ("int?", ""),
        "Textfeld" => ("string", " = \"\";"),
        "Auswahlfeld" when zeile.Element.Typ == "RadioButton" => ("bool", ""),
        "Auswahlfeld (Gruppe pruefen)" => ("bool", ""),
        "Auswahlfeld" => ("int?", ""),
        "Schalter" => ("bool", ""),
        "Datumsfeld" => ("DateOnly?", ""),
        "Raster" => ("IQueryable<object>?", ""),
        "ChartBild" => ("byte[]?", ""),
        _ => null
    };

    private static void Auswahlliste(StringBuilder werkzeug, string name, Steuerelement element)
    {
        werkzeug.Append("    /// <summary>Die waehlbaren Eintraege zu `").Append(element.Name).Append("`.</summary>\n");
        werkzeug.Append("    [Parameter] public IReadOnlyList<(int Id, string Text)> ").Append(name)
                .Append("Eintraege { get; set; } = ");

        if (element.Eintraege.Count == 0)
        {
            werkzeug.Append("Array.Empty<(int, string)>();\n\n");
            return;
        }

        werkzeug.Append("new (int, string)[]\n    {\n");
        for (var i = 0; i < element.Eintraege.Count; i++)
        {
            werkzeug.Append("        (").Append(i.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(Literal(element.Eintraege[i])).Append("),\n");
        }
        werkzeug.Append("    };\n\n");
    }

    private static void Text(StringBuilder werkzeug, string name, string wert, string zweck)
    {
        werkzeug.Append("    /// <summary>").Append(Kommentar(zweck)).Append(".</summary>\n");
        werkzeug.Append("    [Parameter] public string ").Append(name).Append(" { get; set; } = ")
                .Append(Literal(wert)).Append("; // TODO Ressourcenschluessel\n\n");
    }

    private static string Aufschrift(List<Steuerelement> schliesser, string kern, string vorgabe)
    {
        var knopf = schliesser.FirstOrDefault(k =>
            Typtabelle.Kernname(k.Name).StartsWith(kern, StringComparison.OrdinalIgnoreCase));
        var text = knopf?.Text;
        return string.IsNullOrWhiteSpace(text) ? vorgabe : text!;
    }

    private static string Todo(Maske maske, Steuerelement element)
    {
        if (element.Ereignisse.Count == 0) return "// TODO: Wirkung von `" + element.Name + "` nachtragen.";
        var erste = element.Ereignisse[0];
        return "// TODO: " + Stelle(maske, erste.Handler);
    }

    private static string Stelle(Maske maske, string handler)
    {
        if (maske.Handler.TryGetValue(handler, out var stelle))
        {
            return handler + " aus " + maske.Bezeichner + ".cs:" +
                   stelle.Zeile.ToString(CultureInfo.InvariantCulture) +
                   " (" + stelle.Zeilen.ToString(CultureInfo.InvariantCulture) + " Zeilen)";
        }
        return handler + " aus " + maske.Bezeichner + ".cs (nicht gefunden)";
    }

    /// <summary>Zeichenkettenliteral fuer C# - Anfuehrungszeichen und Umbrueche entschaerfen.</summary>
    private static string Literal(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "\"\"";
        var werkzeug = new StringBuilder("\"");
        foreach (var zeichen in text)
        {
            switch (zeichen)
            {
                case '"': werkzeug.Append("\\\""); break;
                case '\\': werkzeug.Append("\\\\"); break;
                case '\r': werkzeug.Append("\\r"); break;
                case '\n': werkzeug.Append("\\n"); break;
                case '\t': werkzeug.Append("\\t"); break;
                default: werkzeug.Append(zeichen); break;
            }
        }
        return werkzeug.Append('"').ToString();
    }

    /// <summary>Kommentartext entschaerfen: XML-Kommentare vertragen keine spitzen Klammern.</summary>
    private static string Kommentar(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\r", " ")
        .Replace("\n", " ");
}
