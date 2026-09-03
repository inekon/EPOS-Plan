using System.Collections.Concurrent;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Formularkarte;

/// <summary>
/// Liest die <c>Form_X.cs</c> neben dem Designer - alles, was die Karte ueber
/// das Verhalten der Maske sagt:
///
/// <list type="bullet">
///   <item>welche Textfelder ueber <c>Program.ZahlPruefen</c> /
///         <c>ZahlFaerben</c> / <c>ZahlParsen</c> (bzw. die Ganzzahlfassungen)
///         laufen - daraus wird der Feldtyp "Zahl" bzw. "Ganzzahl" und damit
///         das Ziel <c>Zahlenfeld</c> / <c>Ganzzahlfeld</c>,</item>
///   <item>wie viele <c>MessageBox.Show</c> die Maske hat (jede davon wird in
///         Blazor ein <c>Warnbanner</c>),</item>
///   <item>Zeile und Umfang jedes Ereignishandlers,</item>
///   <item>wer die Maske mit <c>ShowDialog</c> oeffnet.</item>
/// </list>
/// </summary>
public static class QuelltextLeser
{
    private static readonly HashSet<string> ZahlHelfer = new(StringComparer.Ordinal)
    {
        "ZahlPruefen", "ZahlFaerben", "ZahlParsen"
    };

    private static readonly HashSet<string> GanzzahlHelfer = new(StringComparer.Ordinal)
    {
        "GanzzahlPruefen", "GanzzahlFaerben", "GanzzahlParsen", "checkInt"
    };

    /// <summary>Zwischenspeicher fuer die Dateien eines Projektbaums (Stapellauf).</summary>
    private static readonly ConcurrentDictionary<string, List<(string Pfad, string Text)>> Baeume = new();

    /// <summary>Liest die Form_X.cs zur Maske, wenn es sie gibt.</summary>
    public static void Anwenden(Maske maske, string? suchwurzel)
    {
        var pfad = Quelltextpfad(maske.Datei);
        if (pfad is not null && File.Exists(pfad))
        {
            maske.QuelltextGefunden = true;
            Auswerten(maske, File.ReadAllText(pfad, Encoding.UTF8));
        }

        suchwurzel ??= Projektwurzel(maske.Datei);
        if (suchwurzel is not null) Aufrufer(maske, suchwurzel);
    }

    /// <summary>Die <c>Form_X.cs</c> zur Designer-Datei.</summary>
    public static string? Quelltextpfad(string designerPfad)
    {
        var ordner = Path.GetDirectoryName(Path.GetFullPath(designerPfad));
        if (ordner is null) return null;
        return Path.Combine(ordner, DesignerLeser.Dateibezeichner(designerPfad) + ".cs");
    }

    /// <summary>Der naechste Ordner oberhalb, in dem eine .csproj liegt.</summary>
    public static string? Projektwurzel(string pfad)
    {
        var ordner = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(pfad))!);
        while (ordner is not null)
        {
            if (ordner.EnumerateFiles("*.csproj").Any()) return ordner.FullName;
            ordner = ordner.Parent;
        }
        return null;
    }

    private static void Auswerten(Maske maske, string quelle)
    {
        var baum = CSharpSyntaxTree.ParseText(quelle);
        var wurzel = baum.GetCompilationUnitRoot();

        var textfelder = maske.Steuerelemente
            .Where(s => s.Typ is "TextBox" or "RichTextBox" or "MaskedTextBox")
            .ToDictionary(s => s.Name, StringComparer.Ordinal);

        foreach (var aufruf in wurzel.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methode = Methodenname(aufruf);
            if (methode is null) continue;

            if (methode == "Show" && aufruf.Expression is MemberAccessExpressionSyntax
                { Expression: IdentifierNameSyntax { Identifier.ValueText: "MessageBox" } })
            {
                maske.Meldungen++;
                continue;
            }

            var art = ZahlHelfer.Contains(methode) ? "Zahl"
                    : GanzzahlHelfer.Contains(methode) ? "Ganzzahl"
                    : null;
            if (art is null) continue;

            // Die Feldnamen stehen in den Argumenten - als "txtWert" oder als
            // "txtWert.Text". Beides liefert denselben Bezeichner.
            foreach (var bezeichner in aufruf.ArgumentList.DescendantNodesAndSelf()
                                             .OfType<IdentifierNameSyntax>())
            {
                if (textfelder.TryGetValue(bezeichner.Identifier.ValueText, out var feld)) feld.ZahlArt = art;
            }

            // Muster "feld.TextChanged += (s, e) => Program.ZahlFaerben(s);" -
            // dort steht der Feldname links vom +=, nicht im Argument.
            var anmeldung = aufruf.Ancestors().OfType<AssignmentExpressionSyntax>()
                .FirstOrDefault(a => a.IsKind(SyntaxKind.AddAssignmentExpression));
            if (anmeldung?.Left is MemberAccessExpressionSyntax { Expression: { } traeger })
            {
                var name = traeger switch
                {
                    IdentifierNameSyntax id => id.Identifier.ValueText,
                    MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } zugriff
                        => zugriff.Name.Identifier.ValueText,
                    _ => null
                };
                if (name is not null && textfelder.TryGetValue(name, out var feld2)) feld2.ZahlArt = art;
            }
        }

        // Zeile und Umfang der Ereignishandler.
        var gesucht = maske.Steuerelemente.SelectMany(s => s.Ereignisse)
            .Concat(maske.FormularEreignisse)
            .Select(e => e.Handler)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var methode in wurzel.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var name = methode.Identifier.ValueText;
            if (!gesucht.Contains(name) || maske.Handler.ContainsKey(name)) continue;

            var spanne = methode.GetLocation().GetLineSpan();
            var anfang = spanne.StartLinePosition.Line + 1;
            var ende = spanne.EndLinePosition.Line + 1;
            maske.Handler[name] = (anfang, ende - anfang + 1);
        }
    }

    private static string? Methodenname(InvocationExpressionSyntax aufruf) => aufruf.Expression switch
    {
        MemberAccessExpressionSyntax zugriff => zugriff.Name.Identifier.ValueText,
        IdentifierNameSyntax bezeichner => bezeichner.Identifier.ValueText,
        _ => null
    };

    private static void Aufrufer(Maske maske, string wurzel)
    {
        var eigene = Path.GetFullPath(maske.Datei);
        var eigeneQuelle = Quelltextpfad(maske.Datei);

        foreach (var (pfad, text) in Dateien(wurzel))
        {
            if (string.Equals(pfad, eigene, StringComparison.Ordinal)) continue;
            if (eigeneQuelle is not null && string.Equals(pfad, Path.GetFullPath(eigeneQuelle), StringComparison.Ordinal)) continue;
            if (!text.Contains(maske.Klasse, StringComparison.Ordinal)) continue;
            if (!text.Contains("ShowDialog", StringComparison.Ordinal)) continue;

            var syntax = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();

            foreach (var erzeugung in syntax.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var typ = erzeugung.Type.ToString();
                if (typ.Substring(typ.LastIndexOf('.') + 1) != maske.Klasse) continue;

                if (!OeffnetModal(erzeugung)) continue;
                var zeile = erzeugung.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                maske.Aufrufer.Add(Relativ(pfad, wurzel) + ":" + zeile);
            }
        }
        maske.Aufrufer.Sort(StringComparer.Ordinal);
    }

    /// <summary>
    /// Wird das eben erzeugte Fenster auch modal geoeffnet? Gesucht wird nur im
    /// Geltungsbereich dieser einen Erzeugung - im Bestand traegt derselbe Name
    /// ("dlg", "frm") in einer Datei nacheinander verschiedene Masken, ein
    /// blosser Namensvergleich wuerde jede davon als Aufrufer melden.
    /// </summary>
    private static bool OeffnetModal(ObjectCreationExpressionSyntax erzeugung)
    {
        // a) Direkt: new Form_X().ShowDialog()
        if (erzeugung.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ShowDialog" }) return true;

        var name = Traegername(erzeugung);
        if (name is null) return false;

        // b) Ueber eine Variable. Der Geltungsbereich ist die using-Anweisung
        //    bzw. der umgebende Block; er endet spaetestens dort, wo derselbe
        //    Name das naechste Mal etwas Neues bekommt.
        SyntaxNode? bereich = erzeugung.Ancestors()
            .FirstOrDefault(a => a is UsingStatementSyntax or BlockSyntax or SwitchSectionSyntax);
        if (bereich is null) return false;

        var ab = erzeugung.Span.End;
        var bis = int.MaxValue;
        foreach (var weitere in bereich.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (weitere == erzeugung || weitere.Span.End <= ab) continue;
            if (!string.Equals(Traegername(weitere), name, StringComparison.Ordinal)) continue;
            bis = Math.Min(bis, weitere.SpanStart);
        }

        foreach (var aufruf in bereich.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (aufruf.SpanStart < ab || aufruf.SpanStart >= bis) continue;
            if (aufruf.Expression is not MemberAccessExpressionSyntax zugriff) continue;
            if (zugriff.Name.Identifier.ValueText != "ShowDialog") continue;

            var empfaenger = zugriff.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax innen => innen.Name.Identifier.ValueText,
                _ => null
            };
            if (string.Equals(empfaenger, name, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>Der Name, unter dem die Erzeugung abgelegt wird.</summary>
    private static string? Traegername(ObjectCreationExpressionSyntax erzeugung) => erzeugung.Parent switch
    {
        EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax deklarator } => deklarator.Identifier.ValueText,
        AssignmentExpressionSyntax { Left: IdentifierNameSyntax id } => id.Identifier.ValueText,
        AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax zugriff } => zugriff.Name.Identifier.ValueText,
        _ => null
    };

    private static string Relativ(string pfad, string wurzel)
    {
        var oberhalb = Path.GetDirectoryName(wurzel.TrimEnd(Path.DirectorySeparatorChar));
        var bezug = oberhalb ?? wurzel;
        return Path.GetRelativePath(bezug, pfad).Replace('\\', '/');
    }

    /// <summary>
    /// Alle .cs-Dateien eines Projektbaums, einmal gelesen und danach
    /// zwischengespeichert. Der Erreichbarkeitsgraph liest denselben Baum -
    /// er soll ihn nicht ein zweites Mal von der Platte holen.
    /// </summary>
    public static IReadOnlyList<(string Pfad, string Text)> Baumdateien(string wurzel) => Dateien(wurzel);

    private static List<(string Pfad, string Text)> Dateien(string wurzel) =>
        Baeume.GetOrAdd(Path.GetFullPath(wurzel), pfad =>
        {
            var liste = new List<(string, string)>();
            foreach (var datei in Directory.EnumerateFiles(pfad, "*.cs", SearchOption.AllDirectories))
            {
                var teil = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
                var teilBin = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
                if (datei.Contains(teil, StringComparison.Ordinal)) continue;
                if (datei.Contains(teilBin, StringComparison.Ordinal)) continue;
                liste.Add((Path.GetFullPath(datei), File.ReadAllText(datei, Encoding.UTF8)));
            }
            return liste;
        });
}
