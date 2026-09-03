using System.Globalization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Formularkarte;

/// <summary>Wie steht es um den Einstieg in eine Maske?</summary>
public enum Erreichbar
{
    /// <summary>Es gibt einen Weg von einer Wurzel (MDIMainForm, Form_Start) bis zur Maske.</summary>
    Ja,
    /// <summary>Oeffner stehen im Quelltext, sind aber selbst nicht zu erreichen.</summary>
    Nein,
    /// <summary>Gar kein Oeffner im Quelltext - die Maske wird nirgends erzeugt.</summary>
    Verwaist,
    /// <summary>Nur ueber einen zweifelhaften Weg zu erreichen (verborgener oder gesperrter Knopf).</summary>
    Unklar
}

/// <summary>
/// Der Befund zu einer Maske: Zustand, Weg von der Wurzel und - wo es keinen
/// gibt - wer die Maske im Quelltext trotzdem erzeugt.
/// </summary>
public sealed class Maskenknoten
{
    /// <summary>Klassenname der Maske, z. B. <c>Form_Kosten</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Datei der Klasse, relativ zum Elternordner der Suchwurzel.</summary>
    public string Datei { get; set; } = "";

    /// <summary>Ist die Maske selbst ein Einstieg (MDIMainForm, Form_Start)?</summary>
    public bool Wurzel { get; set; }

    /// <summary>Wird die Datei ueberhaupt uebersetzt (kein <c>Compile Remove</c> in der .csproj)?</summary>
    public bool Uebersetzt { get; set; } = true;

    /// <summary>Der Befund.</summary>
    public Erreichbar Status { get; set; } = Erreichbar.Verwaist;

    /// <summary>Der Weg von der Wurzel, z. B. <c>Form_Start → btnTraeger → Form_Energietraeger</c>.</summary>
    public string Pfad { get; set; } = "";

    /// <summary>Wer die Maske im Quelltext erzeugt - auch dann, wenn er selbst unerreichbar ist.</summary>
    public List<string> Oeffner { get; } = new();

    /// <summary>Befunde am Rand: gesperrte Handler, nicht uebersetzte Dateien, Pruefmuster.</summary>
    public List<string> Hinweise { get; } = new();

    /// <summary>"ja", "nein", "verwaist" oder "unklar" - so steht es in Karte und Uebersicht.</summary>
    public string StatusText => Status switch
    {
        Erreichbar.Ja => "ja",
        Erreichbar.Nein => "nein",
        Erreichbar.Verwaist => "verwaist",
        _ => "unklar"
    };

    /// <summary>
    /// Die eine Zeile fuer den Kopf der Feldkarte und die Spalte der Uebersicht:
    /// Zustand, dann der Weg bzw. - wo es keinen gibt - die Oeffner und der Grund.
    /// </summary>
    public string Zusammenfassung
    {
        get
        {
            var teile = new List<string>();
            if (Status is Erreichbar.Ja or Erreichbar.Unklar && !string.IsNullOrEmpty(Pfad)) teile.Add(Pfad);

            if (Oeffner.Count > 0 && Status is Erreichbar.Nein or Erreichbar.Unklar)
            {
                teile.Add("Öffner: " + string.Join("; ", Oeffner.Take(4)) +
                          (Oeffner.Count > 4 ? " ... (" + Oeffner.Count.ToString(CultureInfo.InvariantCulture) + ")" : ""));
            }
            teile.AddRange(Hinweise);

            return teile.Count == 0 ? StatusText : StatusText + " — " + string.Join(" — ", teile);
        }
    }
}

/// <summary>
/// Der fertig gerechnete Erreichbarkeitsgraph eines Projektbaums: je Maske ein
/// <see cref="Maskenknoten"/>.
/// </summary>
public sealed class Erreichbarkeitsgraph
{
    private readonly Dictionary<string, Maskenknoten> _knoten;

    internal Erreichbarkeitsgraph(string wurzel, Dictionary<string, Maskenknoten> knoten)
    {
        Wurzel = wurzel;
        _knoten = knoten;
    }

    /// <summary>Der Projektordner, ueber dem gerechnet wurde.</summary>
    public string Wurzel { get; }

    /// <summary>Alle gefundenen Masken, nach Klassennamen.</summary>
    public IReadOnlyDictionary<string, Maskenknoten> Knoten => _knoten;

    /// <summary>Der Knoten zu einer Klasse; <c>null</c>, wenn der Baum sie nicht kennt.</summary>
    public Maskenknoten? Fuer(string klasse) =>
        _knoten.TryGetValue(klasse, out var knoten) ? knoten : null;

    /// <summary>Anzahl der Masken in einem Zustand.</summary>
    public int Zaehlen(Erreichbar status) => _knoten.Values.Count(k => k.Status == status);
}

/// <summary>
/// Der Erreichbarkeitsgraph: Beantwortet die Frage, die die Feldkarte bis iU8-12
/// nicht beantworten konnte - <b>ist der Oeffner dieser Maske selbst noch zu
/// erreichen?</b>
///
/// <para>
/// Anlass ist der Befund vom 03.09.2026: Der erste Blazor-Dialog wurde an
/// <c>Form_Kosten</c> gehaengt, weil die Karte dort einen Aufrufer auswies -
/// <c>Form_Kosten</c> selbst hat aber seit KD6a keinen Einstieg mehr
/// (<c>Form_Start.EntferneAltknopf(btn_Kosten)</c>). Die Karte nannte den
/// Aufrufer, nicht den Weg dorthin.
/// </para>
///
/// <para><b>Knoten</b> sind die Masken: Klassen, die von <c>Form</c>,
/// <c>UserControl</c> oder <c>BaseForm</c> abstammen. <b>Kanten</b> sind
/// "A oeffnet B": <c>new B(...)</c>, <c>B.Zeigen(...)</c>, <c>ShowDialog</c>,
/// <c>Show</c> sowie <c>Dienste.Navigation.OeffneMaske(Masken.X)</c> - der
/// Schluessel wird ueber die Sprungtabelle in
/// <c>Dienste/WinFormsNavigation.cs</c> aufgeloest. Wer die Maske oeffnet, ist
/// haeufig kein Formular, sondern ein Vermittler (<c>MenueCtrl</c>,
/// <c>AssistentSeiten</c>, die <c>*KontextMenuCtrl</c>); solche Klassen sind
/// deshalb Zwischenknoten: Wer einen von ihnen erzeugt, erbt seine Kanten.</para>
///
/// <para><b>Wurzeln</b> sind <c>MDIMainForm</c> und <c>Form_Start</c>, dazu die
/// Einsprungklasse <c>Program</c> (sie zeigt vor dem Hauptfenster den
/// Erststart-Dialog). Abgezogen werden die Wege, die es zur Laufzeit nicht mehr
/// gibt: Handler entfernter Steuerelemente
/// (<c>EntferneAltknopf</c>/<c>Controls.Remove</c>) und Handler, die nirgends
/// angemeldet sind. Steuerelemente, die auf <c>Visible</c>/<c>Enabled = false</c>
/// stehen und nie wieder eingeschaltet werden, machen den Weg nicht ungueltig,
/// sondern "unklar" - im Zweifel wird nicht behauptet, die Maske sei erreichbar.</para>
/// </summary>
public static class Erreichbarkeit
{
    /// <summary>Die Masken, an denen die Suche beginnt.</summary>
    public static readonly string[] Wurzelmasken = { "MDIMainForm", "Form_Start" };

    /// <summary>
    /// Die Klasse mit dem Programmeinsprung. <c>Program.Main</c> laeuft vor jedem
    /// Fenster und zeigt den Erststart-Dialog; ohne sie waere der verwaist.
    /// </summary>
    public const string Wurzelklasse = "Program";

    private static readonly Dictionary<string, Erreichbarkeitsgraph> Graphen = new(StringComparer.Ordinal);
    private static readonly Lock Riegel = new();

    /// <summary>Der Graph ueber einem Projektbaum; je Baum nur einmal gerechnet.</summary>
    public static Erreichbarkeitsgraph Bauen(string projektwurzel)
    {
        var voll = Path.GetFullPath(projektwurzel);
        lock (Riegel)
        {
            if (Graphen.TryGetValue(voll, out var vorhanden)) return vorhanden;
            var graph = new Graphbau(voll).Rechnen();
            Graphen[voll] = graph;
            return graph;
        }
    }

    /// <summary>
    /// Haengt der Maske ihren Befund an. <paramref name="suchwurzel"/> ist derselbe
    /// Projektordner, den auch <see cref="QuelltextLeser"/> nutzt.
    /// </summary>
    public static void Anwenden(Maske maske, string? suchwurzel)
    {
        suchwurzel ??= QuelltextLeser.Projektwurzel(maske.Datei);
        if (suchwurzel is null) return;

        var graph = Bauen(suchwurzel);
        var gefunden = graph.Fuer(maske.Klasse);

        if (gefunden is null)
        {
            maske.Erreichbarkeit = new Maskenknoten { Name = maske.Klasse, Datei = maske.Datei, Status = Erreichbar.Unklar };
            maske.Erreichbarkeit.Hinweise.Add("Klasse im Projektbaum nicht gefunden");
            return;
        }

        // Ein Pruefmuster ist der eingefrorene letzte Stand einer Maske, die es im
        // Bestand nicht mehr gibt. Es hat im Graph zwangslaeufig keinen Einstieg -
        // der Grund dafuer steht aber nicht im Quelltext, sondern in der Historie.
        if (Istpruefmuster(maske.Datei) && !gefunden.Hinweise.Any(h => h.Contains("Blazor-Nachfolge", StringComparison.Ordinal)))
        {
            gefunden.Hinweise.Add("Prüfmuster: die WinForms-Fassung ist gelöscht, Blazor-Nachfolge in EPOS.UI");
        }

        maske.Erreichbarkeit = gefunden;
    }

    /// <summary>Liegt die Datei unter einem Ordner <c>Pruefmuster</c>?</summary>
    private static bool Istpruefmuster(string datei) =>
        datei.Replace('\\', '/').Contains("/Pruefmuster/", StringComparison.Ordinal) ||
        datei.Replace('\\', '/').StartsWith("Pruefmuster/", StringComparison.Ordinal);

    /// <summary>Leert den Zwischenspeicher - nur fuer Tests, die den Baum wechseln.</summary>
    public static void Vergessen()
    {
        lock (Riegel) Graphen.Clear();
    }
}

// ======================================================================
//  Innenleben
// ======================================================================

/// <summary>Art eines Kantenziels.</summary>
internal enum Zielart
{
    /// <summary>Eine Maske wird erzeugt oder gezeigt.</summary>
    Maske,
    /// <summary>Ein Vermittler wird erzeugt - alle seine Mitglieder werden wirksam.</summary>
    Klasse,
    /// <summary>Ein einzelnes Mitglied eines Vermittlers wird gerufen.</summary>
    Mitglied,
    /// <summary>Ein Maskenschluessel aus <c>Masken.*</c>.</summary>
    Schluessel
}

/// <summary>Eine Kante mit ihrer Fundstelle.</summary>
internal readonly record struct Ziel(Zielart Art, string Name, string Fundstelle);

/// <summary>Ein Mitglied einer Klasse - die kleinste Einheit, die der Graph an- oder abschaltet.</summary>
internal sealed class Mitgliedknoten
{
    public required string Klasse { get; init; }
    public required string Name { get; init; }
    public List<Ziel> Ziele { get; } = new();

    /// <summary>Signatur <c>(object sender, EventArgs e)</c> - also ein Ereignishandler.</summary>
    public bool Handlerform { get; set; }

    /// <summary>Steuerelement, an dem der Handler angemeldet ist.</summary>
    public string? Steuerelement { get; set; }

    /// <summary>Der Weg ueber dieses Mitglied gibt es zur Laufzeit nicht mehr.</summary>
    public bool Gesperrt { get; set; }

    /// <summary>Der Weg ist zweifelhaft (verborgenes oder gesperrtes Steuerelement).</summary>
    public bool Zweifelhaft { get; set; }

    /// <summary>Grund fuer Sperre bzw. Zweifel.</summary>
    public string Grund { get; set; } = "";

    /// <summary>Die Beschriftung im Pfad: der Knopf, sonst der Methodenname.</summary>
    public string Beschriftung => Steuerelement ?? Name;
}

/// <summary>Eine Klasse des Projektbaums mit allem, was der Graph ueber sie weiss.</summary>
internal sealed class Klassenknoten
{
    public required string Name { get; init; }
    public HashSet<string> Basis { get; } = new(StringComparer.Ordinal);
    public List<string> Dateien { get; } = new();
    public bool IstMaske { get; set; }
    public bool Uebersetzt { get; set; }
    public Dictionary<string, Mitgliedknoten> Mitglieder { get; } = new(StringComparer.Ordinal);

    /// <summary>Feldname -&gt; einfacher Typname (fuer <c>ctrl.Zeigen()</c> ueber ein Feld).</summary>
    public Dictionary<string, string> Felder { get; } = new(StringComparer.Ordinal);

    /// <summary>Handlername -&gt; Steuerelement, an dem er angemeldet ist.</summary>
    public Dictionary<string, string> HandlerSteuerelement { get; } = new(StringComparer.Ordinal);

    /// <summary>Steuerelemente, die zur Laufzeit aus der Maske genommen werden, mit dem Weg dorthin.</summary>
    public Dictionary<string, string> Entfernte { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Ist die Klasse die Sprungtabelle <c>WinFormsNavigation</c>? Ihre Zweige
    /// sind nur ueber einen Maskenschluessel zu betreten - wer sie bloss erzeugt
    /// (<c>Program.Main</c> legt sie als Dienst ein), oeffnet damit keine Maske.
    /// </summary>
    public bool IstSprungtabelle { get; set; }

    /// <summary>Steuerelemente, die auf false stehen und nie wieder eingeschaltet werden.</summary>
    public HashSet<string> Zweifelhafte { get; } = new(StringComparer.Ordinal);

    /// <summary>Alle Bezeichner im Klassenrumpf - daran haengt "Handler nirgends angemeldet".</summary>
    public HashSet<string> Bezeichner { get; } = new(StringComparer.Ordinal);
}

/// <summary>Woher eine Einheit im Durchlauf erreicht wurde - fuer den Pfad.</summary>
internal readonly record struct Spur(string? Vorgaenger, string Beschriftung);

/// <summary>
/// Baut den Graphen: Klassen einlesen, Kanten ziehen, tote Wege abschneiden,
/// von den Wurzeln aus durchlaufen.
/// </summary>
internal sealed class Graphbau
{
    /// <summary>Der Sammelname fuer Feldinitialisierer und alles ohne eigenen Namen.</summary>
    private const string Felderglied = "<Felder>";

    /// <summary>
    /// Trennt Klasse und Mitglied in einer Kennung. Bewusst kein Punkt: Der
    /// Erzeuger heisst <c>.ctor</c>, "Form_X..ctor" liesse sich nicht mehr
    /// eindeutig zerlegen.
    /// </summary>
    private const char Trenner = '#';

    /// <summary>Die Kennung eines Mitglieds: <c>Klasse#Mitglied</c>.</summary>
    private static string Kennung(string klasse, string mitglied) => klasse + Trenner + mitglied;

    /// <summary>Die Kennung fuer den Menschen: <c>Klasse.Mitglied</c>.</summary>
    private static string Beschriftung(string kennung) => kennung.Replace(Trenner, '.');

    /// <summary>Basisklassen, ab denen eine Klasse als Maske gilt.</summary>
    private static readonly HashSet<string> Maskenbasis = new(StringComparer.Ordinal)
    {
        "Form", "UserControl", "BaseForm"
    };

    private readonly string _wurzel;
    private readonly Dictionary<string, Klassenknoten> _klassen = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Ziel>> _schluessel = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _oeffner = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nichtUebersetzt = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Pfad, CompilationUnitSyntax Baum)> _baeume = new();

    public Graphbau(string wurzel) => _wurzel = wurzel;

    public Erreichbarkeitsgraph Rechnen()
    {
        Bauausschluesse();
        Einlesen();
        Kantenziehen();
        Wegesperren();

        var sicher = Durchlaufen(mitZweifel: false);
        var alle = Durchlaufen(mitZweifel: true);

        var knoten = new Dictionary<string, Maskenknoten>(StringComparer.Ordinal);
        foreach (var klasse in _klassen.Values.Where(k => k.IstMaske))
        {
            var eintrag = new Maskenknoten
            {
                Name = klasse.Name,
                Datei = klasse.Dateien.Count > 0 ? Relativ(klasse.Dateien[0]) : "",
                Wurzel = Erreichbarkeit.Wurzelmasken.Contains(klasse.Name, StringComparer.Ordinal),
                Uebersetzt = klasse.Uebersetzt
            };

            if (_oeffner.TryGetValue(klasse.Name, out var wer)) eintrag.Oeffner.AddRange(wer);

            var einheit = "M:" + klasse.Name;
            if (sicher.ContainsKey(einheit))
            {
                eintrag.Status = Erreichbar.Ja;
                eintrag.Pfad = Pfadtext(sicher, einheit);
            }
            else if (alle.ContainsKey(einheit))
            {
                eintrag.Status = Erreichbar.Unklar;
                eintrag.Pfad = Pfadtext(alle, einheit);
            }
            else
            {
                eintrag.Status = eintrag.Oeffner.Count == 0 ? Erreichbar.Verwaist : Erreichbar.Nein;
            }

            if (!klasse.Uebersetzt) eintrag.Hinweise.Add("nicht übersetzt (Compile Remove in der .csproj)");

            knoten[klasse.Name] = eintrag;
        }
        return new Erreichbarkeitsgraph(_wurzel, knoten);
    }

    // ------------------------------------------------------------------
    //  Einlesen
    // ------------------------------------------------------------------

    /// <summary>
    /// Die <c>Compile Remove</c>-Zeilen der Projektdateien. Eine Maske, deren
    /// Datei gar nicht uebersetzt wird, kann niemand oeffnen - das ist der
    /// haerteste Befund fuer die Stilllegungsliste.
    /// </summary>
    private void Bauausschluesse()
    {
        foreach (var projekt in Directory.EnumerateFiles(_wurzel, "*.csproj", SearchOption.AllDirectories))
        {
            XDocument blatt;
            try { blatt = XDocument.Load(projekt); }
            catch (Exception) { continue; }

            var ordner = Path.GetDirectoryName(Path.GetFullPath(projekt))!;
            foreach (var eintrag in blatt.Descendants().Where(e => e.Name.LocalName == "Compile"))
            {
                var muster = (string?)eintrag.Attribute("Remove");
                if (string.IsNullOrWhiteSpace(muster)) continue;

                var pfad = Path.Combine(ordner, muster.Replace('\\', Path.DirectorySeparatorChar));
                var stern = pfad.IndexOf('*');
                if (stern >= 0) pfad = pfad.Substring(0, stern);
                _nichtUebersetzt.Add(Path.GetFullPath(pfad.TrimEnd(Path.DirectorySeparatorChar)));
            }
        }
    }

    private bool Uebersetzt(string datei)
    {
        var voll = Path.GetFullPath(datei);
        foreach (var aus in _nichtUebersetzt)
        {
            if (string.Equals(voll, aus, StringComparison.OrdinalIgnoreCase)) return false;
            if (voll.StartsWith(aus, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    /// <summary>Erster Gang: alle Klassen, ihre Basis, ihre Felder, ihre Dateien.</summary>
    private void Einlesen()
    {
        foreach (var (pfad, text) in QuelltextLeser.Baumdateien(_wurzel))
        {
            var baum = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
            _baeume.Add((pfad, baum));

            foreach (var klasse in baum.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var knoten = Knoten(klasse.Identifier.ValueText);
                if (!knoten.Dateien.Contains(pfad, StringComparer.Ordinal)) knoten.Dateien.Add(pfad);
                if (Uebersetzt(pfad)) knoten.Uebersetzt = true;

                foreach (var basis in klasse.BaseList?.Types ?? default)
                {
                    knoten.Basis.Add(Einfach(basis.Type.ToString()));
                }

                foreach (var feld in klasse.Members.OfType<FieldDeclarationSyntax>())
                {
                    var typ = Einfach(feld.Declaration.Type.ToString());
                    foreach (var name in feld.Declaration.Variables) knoten.Felder[name.Identifier.ValueText] = typ;
                }
            }
        }
        Maskenerben();
    }

    /// <summary>Maske ist, wer von Form/UserControl/BaseForm abstammt - ueber beliebig viele Stufen.</summary>
    private void Maskenerben()
    {
        var geaendert = true;
        while (geaendert)
        {
            geaendert = false;
            foreach (var knoten in _klassen.Values)
            {
                if (knoten.IstMaske) continue;
                if (!knoten.Basis.Any(b => Maskenbasis.Contains(b) ||
                                           (_klassen.TryGetValue(b, out var eltern) && eltern.IstMaske))) continue;
                knoten.IstMaske = true;
                geaendert = true;
            }
        }
    }

    private Klassenknoten Knoten(string name)
    {
        if (_klassen.TryGetValue(name, out var vorhanden)) return vorhanden;
        var neu = new Klassenknoten { Name = name };
        _klassen[name] = neu;
        return neu;
    }

    // ------------------------------------------------------------------
    //  Kanten
    // ------------------------------------------------------------------

    /// <summary>Zweiter Gang: Mitglieder, Kanten, Handleranmeldungen, entfernte Steuerelemente.</summary>
    private void Kantenziehen()
    {
        foreach (var (pfad, baum) in _baeume)
        {
            var uebersetzt = Uebersetzt(pfad);
            foreach (var klasse in baum.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var knoten = Knoten(klasse.Identifier.ValueText);
                Anmeldungen(knoten, klasse);
                Steuerelementzustand(knoten, klasse);

                foreach (var bezeichner in klasse.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    knoten.Bezeichner.Add(bezeichner.Identifier.ValueText);
                }

                // Eine Datei, die nicht uebersetzt wird, kann nichts oeffnen.
                if (!uebersetzt) continue;

                var tabelle = klasse.Identifier.ValueText == "WinFormsNavigation";
                if (tabelle)
                {
                    knoten.IstSprungtabelle = true;
                    Sprungtabelle(knoten, klasse, pfad);
                }

                foreach (var glied in klasse.Members)
                {
                    if (glied is BaseTypeDeclarationSyntax) continue;   // geschachtelte Typen sind eigene Knoten

                    var name = glied switch
                    {
                        MethodDeclarationSyntax methode => methode.Identifier.ValueText,
                        ConstructorDeclarationSyntax => ".ctor",
                        PropertyDeclarationSyntax eigenschaft => eigenschaft.Identifier.ValueText,
                        _ => Felderglied
                    };

                    var mitglied = Mitglied(knoten, name);
                    if (glied is MethodDeclarationSyntax handler && Handlerform(handler)) mitglied.Handlerform = true;
                    Kanten(knoten, mitglied, glied, pfad);
                }

                // Der Rumpf von OeffneMaske IST die Sprungtabelle - er wird ueber
                // die Schluessel betreten, nicht als gewoehnliche Kante.
                if (tabelle && knoten.Mitglieder.TryGetValue("OeffneMaske", out var rumpf)) rumpf.Ziele.Clear();
            }
        }
    }

    private static bool Handlerform(MethodDeclarationSyntax methode)
    {
        var parameter = methode.ParameterList.Parameters;
        if (parameter.Count != 2) return false;
        return Einfach(parameter[1].Type?.ToString() ?? "").EndsWith("EventArgs", StringComparison.Ordinal);
    }

    private static Mitgliedknoten Mitglied(Klassenknoten knoten, string name)
    {
        if (knoten.Mitglieder.TryGetValue(name, out var vorhanden)) return vorhanden;
        var neu = new Mitgliedknoten { Klasse = knoten.Name, Name = name };
        knoten.Mitglieder[name] = neu;
        return neu;
    }

    /// <summary>Alle Kanten, die aus einem Mitglied herausfuehren.</summary>
    private void Kanten(Klassenknoten knoten, Mitgliedknoten mitglied, SyntaxNode rumpf, string pfad)
    {
        // Lokale Variablen mit ihrem Typ - damit "ctrl.Machwas()" den Vermittler trifft.
        var lokal = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var erklaerung in rumpf.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            var typ = Einfach(erklaerung.Type.ToString());
            foreach (var name in erklaerung.Variables) lokal[name.Identifier.ValueText] = typ;
        }

        foreach (var stelle in rumpf.DescendantNodes())
        {
            switch (stelle)
            {
                case ObjectCreationExpressionSyntax erzeugung:
                    Kante(mitglied, Einfach(erzeugung.Type.ToString()), Fundstelle(pfad, erzeugung));
                    break;

                case InvocationExpressionSyntax aufruf:
                    Aufrufkante(knoten, mitglied, aufruf, lokal, pfad);
                    break;

                case MemberAccessExpressionSyntax zugriff
                    when zugriff.Expression is IdentifierNameSyntax { Identifier.ValueText: "Masken" }:
                    mitglied.Ziele.Add(new Ziel(Zielart.Schluessel, zugriff.Name.Identifier.ValueText,
                                                Fundstelle(pfad, zugriff)));
                    break;
            }
        }
    }

    /// <summary>Eine Kante auf einen Typnamen: Maske oder Vermittler.</summary>
    private void Kante(Mitgliedknoten mitglied, string typ, string fundstelle)
    {
        if (!_klassen.TryGetValue(typ, out var ziel)) return;
        mitglied.Ziele.Add(new Ziel(ziel.IstMaske ? Zielart.Maske : Zielart.Klasse, typ, fundstelle));
    }

    private void Aufrufkante(Klassenknoten knoten, Mitgliedknoten mitglied, InvocationExpressionSyntax aufruf,
                             Dictionary<string, string> lokal, string pfad)
    {
        var fundstelle = Fundstelle(pfad, aufruf);

        // a) Aufruf einer eigenen Methode: "AssistentZeigen(...)".
        if (aufruf.Expression is IdentifierNameSyntax eigen)
        {
            if (knoten.Mitglieder.ContainsKey(eigen.Identifier.ValueText) ||
                !_klassen.ContainsKey(eigen.Identifier.ValueText))
            {
                mitglied.Ziele.Add(new Ziel(Zielart.Mitglied, Kennung(knoten.Name, eigen.Identifier.ValueText), fundstelle));
            }
            return;
        }

        if (aufruf.Expression is not MemberAccessExpressionSyntax zugriff) return;
        var methode = zugriff.Name.Identifier.ValueText;

        // b) OeffneMaske("Form_X") - der Schluessel als Zeichenkette.
        if (methode == "OeffneMaske" && aufruf.ArgumentList.Arguments.Count > 0 &&
            aufruf.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: string wert })
        {
            mitglied.Ziele.Add(new Ziel(Zielart.Schluessel, wert, fundstelle));
        }

        if (zugriff.Expression is not IdentifierNameSyntax traeger) return;
        var name = traeger.Identifier.ValueText;

        // c) Statischer Aufruf ueber den Klassennamen: "Form_ImportKonflikte.Zeigen(...)",
        //    "Form_KiChat.Oeffnen(this)", "Form_Erststart.Zeigen(...)" - aber auch
        //    "Form_Kosten.LiesAnlagenSummen(...)", das nur eine Tabelle liest.
        //    Deshalb wird das MITGLIED angesteuert, nicht die Maske: Eine
        //    Schaufabrik erzeugt ihre Maske im Rumpf, und genau daran erkennt sie
        //    der Graph. Eine statische Datenhilfe tut es nicht - sie macht ihre
        //    Maske also auch nicht erreichbar.
        if (_klassen.ContainsKey(name))
        {
            mitglied.Ziele.Add(new Ziel(Zielart.Mitglied, Kennung(name, methode), fundstelle));
            return;
        }

        // d) Aufruf ueber eine Variable oder ein Feld bekannten Typs.
        if (!lokal.TryGetValue(name, out var typ) && !knoten.Felder.TryGetValue(name, out typ)) return;
        if (!_klassen.ContainsKey(typ)) return;

        mitglied.Ziele.Add(new Ziel(Zielart.Mitglied, Kennung(typ, methode), fundstelle));
    }

    /// <summary>
    /// Die Sprungtabelle aus <c>WinFormsNavigation.OeffneMaske</c>: je
    /// <c>case Masken.X:</c> die Masken und Methoden, die dieser Zweig anfaesst.
    /// Die Tabelle selbst ist danach keine Kante mehr - sonst oeffnete jeder
    /// Aufruf von <c>OeffneMaske</c> saemtliche Masken auf einmal.
    /// </summary>
    private void Sprungtabelle(Klassenknoten knoten, ClassDeclarationSyntax klasse, string pfad)
    {
        var oeffne = klasse.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == "OeffneMaske");
        if (oeffne is null) return;

        foreach (var schalter in oeffne.DescendantNodes().OfType<SwitchStatementSyntax>())
        {
            foreach (var abschnitt in schalter.Sections)
            {
                var schluessel = abschnitt.Labels.OfType<CaseSwitchLabelSyntax>()
                    .Select(l => l.Value).OfType<MemberAccessExpressionSyntax>()
                    .Where(m => Einfach(m.Expression.ToString()) == "Masken")
                    .Select(m => m.Name.Identifier.ValueText)
                    .ToList();
                if (schluessel.Count == 0) continue;

                var zweig = new Mitgliedknoten { Klasse = knoten.Name, Name = "OeffneMaske" };
                foreach (var anweisung in abschnitt.Statements) Kanten(knoten, zweig, anweisung, pfad);

                foreach (var eines in schluessel)
                {
                    if (!_schluessel.TryGetValue(eines, out var liste))
                    {
                        liste = new List<Ziel>();
                        _schluessel[eines] = liste;
                    }
                    liste.AddRange(zweig.Ziele);
                }
            }
        }

    }

    // ------------------------------------------------------------------
    //  Tote Wege
    // ------------------------------------------------------------------

    /// <summary>Wer haengt an welchem Steuerelement, und welcher Handler ist ueberhaupt angemeldet?</summary>
    private static void Anmeldungen(Klassenknoten knoten, ClassDeclarationSyntax klasse)
    {
        foreach (var zuweisung in klasse.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!zuweisung.IsKind(SyntaxKind.AddAssignmentExpression)) continue;
            if (zuweisung.Left is not MemberAccessExpressionSyntax links) continue;

            var handler = Handlername(zuweisung.Right);
            if (handler is null) continue;

            var steuerelement = Traegername(links.Expression);
            if (steuerelement is not null && !knoten.HandlerSteuerelement.ContainsKey(handler))
            {
                knoten.HandlerSteuerelement[handler] = steuerelement;
            }
        }
    }

    /// <summary>Der Methodenname rechts vom <c>+=</c> - auch aus <c>new EventHandler(...)</c>.</summary>
    private static string? Handlername(ExpressionSyntax rechts) => rechts switch
    {
        IdentifierNameSyntax name => name.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } zugriff => zugriff.Name.Identifier.ValueText,
        ObjectCreationExpressionSyntax erzeugung when erzeugung.ArgumentList?.Arguments.Count == 1
            => Handlername(erzeugung.ArgumentList.Arguments[0].Expression),
        _ => null
    };

    /// <summary>Der Name links vom Punkt: <c>btn_Kosten</c> aus <c>this.btn_Kosten.Click</c>.</summary>
    private static string? Traegername(ExpressionSyntax? links) => links switch
    {
        IdentifierNameSyntax name => name.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } zugriff => zugriff.Name.Identifier.ValueText,
        _ => null
    };

    /// <summary>
    /// Steuerelemente, die zur Laufzeit verschwinden oder auf false stehen.
    ///
    /// <para>Entfernt wird ueber <c>Controls.Remove(x)</c> - entweder unmittelbar
    /// oder ueber eine Hilfsmethode, die ihren Parameter entfernt
    /// (<c>Form_Start.EntferneAltknopf</c>). Beides zaehlt.</para>
    /// </summary>
    private static void Steuerelementzustand(Klassenknoten knoten, ClassDeclarationSyntax klasse)
    {
        // a) Hilfsmethoden, die ihren Parameter aus der Maske nehmen.
        var entferner = new HashSet<string>(StringComparer.Ordinal);
        foreach (var methode in klasse.Members.OfType<MethodDeclarationSyntax>())
        {
            var parameter = methode.ParameterList.Parameters.Select(p => p.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
            if (parameter.Count == 0) continue;
            if (Entfernte(methode).Any(parameter.Contains)) entferner.Add(methode.Identifier.ValueText);
        }

        // b) Unmittelbare Entfernungen.
        foreach (var name in Entfernte(klasse)) knoten.Entfernte[name] = "Controls.Remove";

        // c) Aufrufe der Hilfsmethoden.
        foreach (var aufruf in klasse.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (aufruf.Expression is not IdentifierNameSyntax gerufen) continue;
            if (!entferner.Contains(gerufen.Identifier.ValueText)) continue;
            var erstes = aufruf.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            var name = Traegername(erstes);
            if (name is not null) knoten.Entfernte[name] = gerufen.Identifier.ValueText;
        }

        // d) Visible/Enabled = false ohne spaeteres true.
        var aus = new HashSet<string>(StringComparer.Ordinal);
        var an = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zuweisung in klasse.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!zuweisung.IsKind(SyntaxKind.SimpleAssignmentExpression)) continue;
            if (zuweisung.Left is not MemberAccessExpressionSyntax links) continue;
            if (links.Name.Identifier.ValueText is not ("Visible" or "Enabled")) continue;

            var steuerelement = Traegername(links.Expression);
            if (steuerelement is null) continue;

            if (zuweisung.Right.IsKind(SyntaxKind.FalseLiteralExpression)) aus.Add(steuerelement);
            else an.Add(steuerelement);
        }
        aus.ExceptWith(an);
        aus.ExceptWith(knoten.Entfernte.Keys);
        foreach (var name in aus) knoten.Zweifelhafte.Add(name);
    }

    /// <summary>Die Namen aus allen <c>*.Controls.Remove(x)</c> unterhalb eines Knotens.</summary>
    private static IEnumerable<string> Entfernte(SyntaxNode rumpf)
    {
        foreach (var aufruf in rumpf.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (aufruf.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Remove" } zugriff) continue;
            if (!zugriff.Expression.ToString().EndsWith("Controls", StringComparison.Ordinal)) continue;

            var name = Traegername(aufruf.ArgumentList.Arguments.FirstOrDefault()?.Expression);
            if (name is not null) yield return name;
        }
    }

    /// <summary>
    /// Schneidet die Wege ab, die es zur Laufzeit nicht gibt, und sammelt
    /// nebenher die Oeffnerliste je Maske.
    /// </summary>
    private void Wegesperren()
    {
        foreach (var knoten in _klassen.Values)
        {
            foreach (var mitglied in knoten.Mitglieder.Values)
            {
                if (mitglied.Handlerform) Handlerpruefen(knoten, mitglied);

                foreach (var ziel in mitglied.Ziele.Where(z => z.Art == Zielart.Maske))
                {
                    Oeffnereintrag(ziel.Name, knoten.Name + "." + mitglied.Name + " (" + ziel.Fundstelle + ")" +
                                              (mitglied.Gesperrt ? " — gesperrt: " + mitglied.Grund : "") +
                                              (mitglied.Zweifelhaft ? " — zweifelhaft: " + mitglied.Grund : ""));
                }
            }
        }

        // Die Sprungtabelle ist auch ein Oeffner - sie erzeugt die Maske.
        foreach (var (schluessel, ziele) in _schluessel)
        {
            foreach (var ziel in ziele.Where(z => z.Art == Zielart.Maske))
            {
                Oeffnereintrag(ziel.Name, "WinFormsNavigation.OeffneMaske → Masken." + schluessel +
                                          " (" + ziel.Fundstelle + ")");
            }
        }
    }

    /// <summary>
    /// Kann dieser Ereignishandler ueberhaupt noch anspringen?
    ///
    /// <para>Drei Faelle: Das Steuerelement, an dem er haengt, wird zur Laufzeit
    /// entfernt (gesperrt); es steht dauerhaft auf <c>Visible</c>/<c>Enabled =
    /// false</c> (zweifelhaft); oder der Handler ist ueberhaupt nirgends
    /// angemeldet und wird auch sonst nicht erwaehnt (gesperrt). Der letzte Fall
    /// ist der von <c>Form_Start.btn_Kosten_Click</c>: Der Knopf wird in
    /// <c>BaueBerichteKostenSeite</c> mit <c>EntferneAltknopf</c> aus der Maske
    /// genommen, und der Designer meldet den Handler seither gar nicht mehr an.
    /// Damit der Grund den Knopf nennt, wird bei einem unangemeldeten Handler das
    /// Namensmuster <c>&lt;Steuerelement&gt;_&lt;Ereignis&gt;</c> nachgeschlagen.</para>
    /// </summary>
    private static void Handlerpruefen(Klassenknoten knoten, Mitgliedknoten mitglied)
    {
        if (knoten.HandlerSteuerelement.TryGetValue(mitglied.Name, out var steuerelement))
        {
            mitglied.Steuerelement = steuerelement;
            if (knoten.Entfernte.TryGetValue(steuerelement, out var weg))
            {
                mitglied.Gesperrt = true;
                mitglied.Grund = "Steuerelement " + steuerelement + " wird zur Laufzeit entfernt (" + weg + ")";
            }
            else if (knoten.Zweifelhafte.Contains(steuerelement))
            {
                mitglied.Zweifelhaft = true;
                mitglied.Grund = "Steuerelement " + steuerelement + " bleibt auf Visible/Enabled = false";
            }
            return;
        }

        if (knoten.Bezeichner.Contains(mitglied.Name)) return;   // irgendwo erwaehnt - kein Urteil

        mitglied.Gesperrt = true;
        var punkt = mitglied.Name.LastIndexOf('_');
        var vermutet = punkt > 0 ? mitglied.Name.Substring(0, punkt) : null;

        if (vermutet is not null && knoten.Entfernte.TryGetValue(vermutet, out var weise))
        {
            mitglied.Steuerelement = vermutet;
            mitglied.Grund = "Steuerelement " + vermutet + " wird zur Laufzeit entfernt (" + weise +
                             "), der Handler ist nirgends angemeldet";
        }
        else
        {
            mitglied.Grund = "Handler " + mitglied.Name + " ist nirgends angemeldet";
        }
    }

    private void Oeffnereintrag(string maske, string text)
    {
        if (!_oeffner.TryGetValue(maske, out var liste))
        {
            liste = new List<string>();
            _oeffner[maske] = liste;
        }
        if (!liste.Contains(text, StringComparer.Ordinal)) liste.Add(text);
    }

    // ------------------------------------------------------------------
    //  Durchlauf
    // ------------------------------------------------------------------

    /// <summary>
    /// Breitensuche von den Wurzeln aus. Eine Einheit ist entweder eine Maske
    /// ("M:"), ein Mitglied ("m:") oder ein Maskenschluessel ("k:").
    /// </summary>
    private Dictionary<string, Spur> Durchlaufen(bool mitZweifel)
    {
        var erreicht = new Dictionary<string, Spur>(StringComparer.Ordinal);
        var schlange = new Queue<string>();

        void Aufnehmen(string einheit, string? vorgaenger, string beschriftung)
        {
            if (erreicht.ContainsKey(einheit)) return;
            erreicht[einheit] = new Spur(vorgaenger, beschriftung);
            schlange.Enqueue(einheit);
        }

        foreach (var wurzel in Erreichbarkeit.Wurzelmasken)
        {
            if (_klassen.ContainsKey(wurzel)) Aufnehmen("M:" + wurzel, null, wurzel);
        }
        Abarbeiten();

        // Erst wenn von Menue und Startseite aus nichts mehr zu holen ist, kommt der
        // Programmeinsprung dazu. So nennt der Pfad den Weg, den der Anwender geht,
        // und nicht den Umweg ueber Program.Main - der fuehrt zwar auch hin, taugt
        // aber nicht als Wegbeschreibung.
        if (_klassen.TryGetValue(Erreichbarkeit.Wurzelklasse, out var einsprung))
        {
            foreach (var mitglied in einsprung.Mitglieder.Values)
            {
                Aufnehmen("m:" + Kennung(einsprung.Name, mitglied.Name), null, einsprung.Name + "." + mitglied.Name);
            }
        }
        Abarbeiten();
        return erreicht;

        void Abarbeiten()
        {
            while (schlange.Count > 0)
            {
                var einheit = schlange.Dequeue();
                var inhalt = einheit.Substring(2);

                switch (einheit[0])
                {
                    case 'M':
                        if (!_klassen.TryGetValue(inhalt, out var maske)) break;
                        foreach (var mitglied in maske.Mitglieder.Values)
                        {
                            if (mitglied.Gesperrt) continue;
                            if (mitglied.Zweifelhaft && !mitZweifel) continue;
                            Aufnehmen("m:" + Kennung(inhalt, mitglied.Name), einheit, mitglied.Beschriftung);
                        }
                        break;

                    case 'm':
                        Mitgliedschritt(inhalt, einheit, Aufnehmen);
                        break;

                    case 'k':
                        if (!_schluessel.TryGetValue(inhalt, out var ziele)) break;
                        foreach (var ziel in ziele) Zielschritt(ziel, einheit, Aufnehmen);
                        break;
                }
            }
        }
    }

    private void Mitgliedschritt(string inhalt, string einheit, Action<string, string?, string> aufnehmen)
    {
        var trenner = inhalt.IndexOf(Trenner);
        if (trenner <= 0) return;

        var klasse = inhalt.Substring(0, trenner);
        var name = inhalt.Substring(trenner + 1);
        if (!_klassen.TryGetValue(klasse, out var knoten)) return;

        // Feldinitialisierer laufen mit, sobald irgendein Mitglied der Klasse laeuft
        // (AssistentSeiten legt seine dreizehn Seiten in einem statischen Feld ab).
        if (name != Felderglied && knoten.Mitglieder.ContainsKey(Felderglied))
        {
            aufnehmen("m:" + Kennung(klasse, Felderglied), einheit, klasse + " (Felder)");
        }

        if (!knoten.Mitglieder.TryGetValue(name, out var mitglied)) return;
        if (mitglied.Gesperrt) return;

        foreach (var ziel in mitglied.Ziele) Zielschritt(ziel, einheit, aufnehmen);
    }

    private void Zielschritt(Ziel ziel, string einheit, Action<string, string?, string> aufnehmen)
    {
        switch (ziel.Art)
        {
            case Zielart.Maske:
                aufnehmen("M:" + ziel.Name, einheit, ziel.Name);
                break;

            case Zielart.Klasse:
                // Ein erzeugter Vermittler bringt alle seine Mitglieder mit: Ein
                // Kontextmenue-Controller meldet seine Menuepunkte selbst an. Die
                // Sprungtabelle ist die Ausnahme - Program.Main legt sie als Dienst
                // ein, oeffnet damit aber keine einzige Maske.
                if (!_klassen.TryGetValue(ziel.Name, out var vermittler)) break;
                if (vermittler.IstSprungtabelle) break;
                foreach (var mitglied in vermittler.Mitglieder.Values)
                {
                    aufnehmen("m:" + Kennung(ziel.Name, mitglied.Name), einheit, ziel.Name + "." + mitglied.Name);
                }
                break;

            case Zielart.Mitglied:
                var trenner = ziel.Name.IndexOf(Trenner);
                if (trenner <= 0) break;
                var klasse = ziel.Name.Substring(0, trenner);
                if (!_klassen.TryGetValue(klasse, out var traeger)) break;

                if (traeger.Mitglieder.ContainsKey(ziel.Name.Substring(trenner + 1)))
                {
                    aufnehmen("m:" + ziel.Name, einheit, Beschriftung(ziel.Name));
                }
                else if (traeger.IstMaske)
                {
                    // Geerbtes Mitglied einer Maske - das ist "frm.ShowDialog()".
                    aufnehmen("M:" + klasse, einheit, klasse);
                }
                else
                {
                    // Geerbtes Mitglied eines Vermittlers: lieber die ganze Klasse.
                    foreach (var mitglied in traeger.Mitglieder.Values)
                    {
                        aufnehmen("m:" + Kennung(klasse, mitglied.Name), einheit, klasse + "." + mitglied.Name);
                    }
                }
                break;

            case Zielart.Schluessel:
                aufnehmen("k:" + ziel.Name, einheit, "Masken." + ziel.Name);
                break;
        }
    }

    // ------------------------------------------------------------------
    //  Kleinkram
    // ------------------------------------------------------------------

    /// <summary>Der Weg von der Wurzel bis zur Einheit, lange Ketten in der Mitte gekuerzt.</summary>
    private static string Pfadtext(Dictionary<string, Spur> spuren, string einheit)
    {
        var schritte = new List<string>();
        var laufend = einheit;
        while (spuren.TryGetValue(laufend, out var spur))
        {
            schritte.Add(spur.Beschriftung);
            if (spur.Vorgaenger is null) break;
            laufend = spur.Vorgaenger;
            if (schritte.Count > 40) break;
        }
        schritte.Reverse();

        if (schritte.Count > 8)
        {
            var gekuerzt = schritte.Take(3).ToList();
            gekuerzt.Add("…");
            gekuerzt.AddRange(schritte.Skip(schritte.Count - 3));
            schritte = gekuerzt;
        }
        return string.Join(" → ", schritte);
    }

    private static string Fundstelle(string pfad, SyntaxNode knoten) =>
        Path.GetFileName(pfad) + ":" +
        (knoten.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>Der einfache Typname: ohne Namensraum, ohne Typargumente, ohne Fragezeichen.</summary>
    private static string Einfach(string typ)
    {
        var name = typ;
        var spitz = name.IndexOf('<');
        if (spitz >= 0) name = name.Substring(0, spitz);
        var punkt = name.LastIndexOf('.');
        if (punkt >= 0) name = name.Substring(punkt + 1);
        return name.TrimEnd('?', ' ');
    }

    private string Relativ(string pfad)
    {
        var oberhalb = Path.GetDirectoryName(_wurzel.TrimEnd(Path.DirectorySeparatorChar));
        return Path.GetRelativePath(oberhalb ?? _wurzel, pfad).Replace('\\', '/');
    }
}
