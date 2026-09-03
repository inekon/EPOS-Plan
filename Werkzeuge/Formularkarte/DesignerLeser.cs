using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Formularkarte;

/// <summary>
/// Liest eine <c>Form_X.Designer.cs</c> mit Roslyn.
///
/// <para>
/// Gelesen wird ausschliesslich <c>InitializeComponent</c> und die
/// Felddeklarationen der partiellen Klasse. Beide Schreibweisen des
/// Bestands sind abgedeckt: die alte mit <c>this.</c> (Form_KostenfaktorItem)
/// und die neue ohne (Form_Kosten_Auswahl).
/// </para>
/// <para>
/// Die Werte werden beim Lesen normalisiert - Zeichenketten ohne
/// Anfuehrungszeichen, <c>new Point(159, 26)</c> als "159, 26",
/// Aufzaehlungen auf ihr letztes Glied gekuerzt. Damit liefert der
/// .resx-Leser (<see cref="ResxLeser"/>) dieselbe Form und kann seine Werte
/// einfach daruebersetzen.
/// </para>
/// </summary>
public static class DesignerLeser
{
    /// <summary>Liest die Designer-Datei; wirft, wenn kein <c>InitializeComponent</c> darin steht.</summary>
    public static Maske Lesen(string pfad)
    {
        var maske = Versuchen(pfad);
        if (maske is null)
        {
            throw new InvalidOperationException(
                "In '" + pfad + "' steht keine Methode InitializeComponent - das ist keine Designer-Datei einer Maske.");
        }
        return maske;
    }

    /// <summary>
    /// Wie <see cref="Lesen"/>, liefert aber <c>null</c> statt einer Ausnahme,
    /// wenn die Datei kein <c>InitializeComponent</c> enthaelt (Resource.Designer.cs,
    /// Settings.Designer.cs). Das braucht der Stapellauf.
    /// </summary>
    public static Maske? Versuchen(string pfad)
    {
        var quelle = File.ReadAllText(pfad, Encoding.UTF8);
        var wurzel = CSharpSyntaxTree.ParseText(quelle).GetCompilationUnitRoot();

        var init = wurzel.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "InitializeComponent" && m.Body is not null);
        if (init is null) return null;

        var klasse = init.Ancestors().OfType<ClassDeclarationSyntax>().First();

        var maske = new Maske
        {
            Bezeichner = Dateibezeichner(pfad),
            Klasse = klasse.Identifier.Text,
            Datei = pfad.Replace('\\', '/'),
            Ordner = Fachbereich(pfad)
        };

        var nachName = new Dictionary<string, Steuerelement>(StringComparer.Ordinal);
        var reihenfolge = 0;

        // 1. Felddeklarationen: sie nennen den Typ verbindlich, auch dort, wo
        //    der Designer unqualifiziert "new Button()" schreibt.
        foreach (var feld in klasse.Members.OfType<FieldDeclarationSyntax>())
        {
            var typ = feld.Declaration.Type.ToString();
            foreach (var variable in feld.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                if (nachName.ContainsKey(name)) continue;

                var element = new Steuerelement { Name = name, Reihenfolge = reihenfolge++ };
                TypSetzen(element, typ);
                nachName[name] = element;
                maske.Steuerelemente.Add(element);
            }
        }

        // 2. InitializeComponent auswerten.
        foreach (var knoten in init.Body!.DescendantNodes())
        {
            switch (knoten)
            {
                case AssignmentExpressionSyntax zuweisung:
                    Zuweisung(maske, nachName, zuweisung, ref reihenfolge);
                    break;
                case InvocationExpressionSyntax aufruf:
                    Aufruf(maske, nachName, aufruf);
                    break;
            }
        }

        return maske;
    }

    /// <summary>Dateiname ohne <c>.Designer.cs</c> bzw. <c>.designer.cs</c>.</summary>
    public static string Dateibezeichner(string pfad)
    {
        var name = Path.GetFileName(pfad);
        var punkt = name.LastIndexOf(".Designer.cs", StringComparison.OrdinalIgnoreCase);
        return punkt >= 0 ? name.Substring(0, punkt) : Path.GetFileNameWithoutExtension(name);
    }

    /// <summary>
    /// Der Fachbereich einer Maske - der Ordner unter <c>Views/</c>, aus dem
    /// spaeter <c>EPOS.UI/Dialoge/&lt;Fachbereich&gt;</c> wird.
    ///
    /// <para>
    /// Nicht jede Designer-Datei liegt in einem Fachordner: <c>MDIMainForm</c>
    /// liegt in der Projektwurzel, <c>Form_StromTest</c> unmittelbar in
    /// <c>Views/</c>. Deren Ordnername ("WindowsFormsApplication1", "Views")
    /// waere als Namensraum falsch - er wuerde sogar das
    /// <c>@using WindowsFormsApplication1.MyResource</c> aus _Imports.razor
    /// verdecken. Solche Masken bekommen deshalb "Allgemein".
    /// </para>
    /// </summary>
    public static string Fachbereich(string pfad)
    {
        var ordner = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(pfad))!);
        if (ordner.Name is "Views" or "Properties") return "Allgemein";
        if (ordner.EnumerateFiles("*.csproj").Any()) return "Allgemein";
        return ordner.Name;
    }

    private static void TypSetzen(Steuerelement element, string vollerTyp)
    {
        element.VollerTyp = vollerTyp;
        var einfach = vollerTyp;
        var punkt = einfach.LastIndexOf('.');
        if (punkt >= 0) einfach = einfach.Substring(punkt + 1);
        element.Typ = einfach;
        element.Art = Typtabelle.Einordnen(einfach);
    }

    private static void Zuweisung(Maske maske, Dictionary<string, Steuerelement> nachName,
                                  AssignmentExpressionSyntax zuweisung, ref int reihenfolge)
    {
        var links = zuweisung.Left;

        // a) Erzeugung eines Steuerelements: x = new T() bzw. this.x = new T().
        if (zuweisung.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
            zuweisung.Right is ObjectCreationExpressionSyntax erzeugung &&
            EinfacherName(links) is { } zielName)
        {
            if (nachName.TryGetValue(zielName, out var vorhanden))
            {
                // Der Feldtyp gewinnt; die Erzeugung ergaenzt ihn nur, wenn
                // die Deklaration fehlte.
                if (string.IsNullOrEmpty(vorhanden.VollerTyp)) TypSetzen(vorhanden, erzeugung.Type.ToString());
                return;
            }

            // Kein Feld gleichen Namens: das ist eine Formulareigenschaft wie
            // Font = new Font(...) oder Margin = new Padding(4).
            if (IstFormulareigenschaft(links))
            {
                maske.Formular[zielName] = Wert(zuweisung.Right);
                return;
            }

            var neu = new Steuerelement { Name = zielName, Reihenfolge = reihenfolge++ };
            TypSetzen(neu, erzeugung.Type.ToString());
            nachName[zielName] = neu;
            maske.Steuerelemente.Add(neu);
            return;
        }

        // b) Eigenschaft oder Ereignis eines Steuerelements: x.Prop = ... / x.Ev += ...
        if (links is MemberAccessExpressionSyntax zugriff && zugriff.Expression is not ThisExpressionSyntax)
        {
            var traeger = EinfacherName(zugriff.Expression);
            if (traeger is not null && nachName.TryGetValue(traeger, out var element))
            {
                var eigenschaft = zugriff.Name.Identifier.Text;
                if (zuweisung.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    element.Ereignisse.Add(new Anmeldung(eigenschaft, Handlername(zuweisung.Right)));
                }
                else
                {
                    element.Eigenschaften[eigenschaft] = Wert(zuweisung.Right);
                }
                return;
            }
        }

        // c) Formular selbst: this.Prop = ... / Prop = ... / this.Ev += ...
        if (IstFormulareigenschaft(links) && EinfacherName(links) is { } eigenschaftName)
        {
            if (zuweisung.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                maske.FormularEreignisse.Add(new Anmeldung(eigenschaftName, Handlername(zuweisung.Right)));
            }
            else
            {
                maske.Formular[eigenschaftName] = Wert(zuweisung.Right);
            }
        }
    }

    private static void Aufruf(Maske maske, Dictionary<string, Steuerelement> nachName,
                               InvocationExpressionSyntax aufruf)
    {
        if (aufruf.Expression is not MemberAccessExpressionSyntax zugriff) return;
        var methode = zugriff.Name.Identifier.Text;

        // resources.ApplyResources(ctrl, "name") - die lokalisierten Masken.
        if (methode == "ApplyResources" && aufruf.ArgumentList.Arguments.Count >= 2)
        {
            var ziel = aufruf.ArgumentList.Arguments[0].Expression;
            var schluessel = Wert(aufruf.ArgumentList.Arguments[1].Expression);

            if (ziel is ThisExpressionSyntax || schluessel == "$this")
            {
                maske.Lokalisiert = true;
                return;
            }
            var name = EinfacherName(ziel);
            if (name is not null && nachName.TryGetValue(name, out var element))
            {
                element.AusRessourcen = true;
                maske.Lokalisiert = true;
            }
            return;
        }

        if (methode != "Add" && methode != "AddRange") return;
        if (zugriff.Expression is not MemberAccessExpressionSyntax sammlungZugriff)
        {
            // Controls.Add(x) ohne "this." - die Sammlung steht dann als
            // schlichter Bezeichner da.
            if (zugriff.Expression is IdentifierNameSyntax bezeichner && bezeichner.Identifier.Text == "Controls")
            {
                foreach (var kind in Argumente(aufruf))
                {
                    if (!nachName.TryGetValue(kind, out var element)) continue;
                    element.Elter = null;
                    element.Eingehaengt = true;
                }
            }
            return;
        }

        var sammlung = sammlungZugriff.Name.Identifier.Text;
        var besitzer = sammlungZugriff.Expression is ThisExpressionSyntax
            ? null
            : EinfacherName(sammlungZugriff.Expression);

        if (sammlung == "Controls")
        {
            foreach (var kind in Argumente(aufruf))
            {
                if (!nachName.TryGetValue(kind, out var element)) continue;
                element.Elter = besitzer;
                element.Eingehaengt = true;
            }
            return;
        }

        if (sammlung == "Items" && besitzer is not null && nachName.TryGetValue(besitzer, out var liste))
        {
            foreach (var eintrag in Textargumente(aufruf))
            {
                liste.Eintraege.Add(eintrag);
            }
        }
    }

    /// <summary>Die Namen der uebergebenen Steuerelemente - einzeln oder als Feldliteral.</summary>
    private static IEnumerable<string> Argumente(InvocationExpressionSyntax aufruf)
    {
        foreach (var argument in aufruf.ArgumentList.Arguments)
        {
            switch (argument.Expression)
            {
                case ArrayCreationExpressionSyntax { Initializer: { } feld }:
                    foreach (var element in feld.Expressions)
                    {
                        if (EinfacherName(element) is { } name) yield return name;
                    }
                    break;
                case ImplicitArrayCreationExpressionSyntax implizit:
                    foreach (var element in implizit.Initializer.Expressions)
                    {
                        if (EinfacherName(element) is { } name) yield return name;
                    }
                    break;
                default:
                    if (EinfacherName(argument.Expression) is { } einzeln) yield return einzeln;
                    break;
            }
        }
    }

    /// <summary>Die uebergebenen Zeichenketten - fuer Items.Add / Items.AddRange.</summary>
    private static IEnumerable<string> Textargumente(InvocationExpressionSyntax aufruf)
    {
        foreach (var argument in aufruf.ArgumentList.Arguments)
        {
            switch (argument.Expression)
            {
                case ArrayCreationExpressionSyntax { Initializer: { } feld }:
                    foreach (var element in feld.Expressions) yield return Wert(element);
                    break;
                case ImplicitArrayCreationExpressionSyntax implizit:
                    foreach (var element in implizit.Initializer.Expressions) yield return Wert(element);
                    break;
                default:
                    yield return Wert(argument.Expression);
                    break;
            }
        }
    }

    /// <summary>
    /// Bezeichner hinter <c>this.</c> oder ohne alles - <c>this.btn_OK</c> und
    /// <c>btn_OK</c> liefern beide "btn_OK".
    /// </summary>
    private static string? EinfacherName(ExpressionSyntax ausdruck) => ausdruck switch
    {
        IdentifierNameSyntax bezeichner => bezeichner.Identifier.Text,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } zugriff => zugriff.Name.Identifier.Text,
        _ => null
    };

    /// <summary>Gehoert die linke Seite dem Formular selbst?</summary>
    private static bool IstFormulareigenschaft(ExpressionSyntax links) =>
        links is IdentifierNameSyntax ||
        links is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax };

    /// <summary>
    /// Handlername aus <c>new System.EventHandler(this.btn_OK_Click)</c>,
    /// <c>btnOk_Click</c> oder einer Lambdaform.
    /// </summary>
    private static string Handlername(ExpressionSyntax ausdruck)
    {
        switch (ausdruck)
        {
            case ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } erzeugung:
                return Handlername(erzeugung.ArgumentList!.Arguments[0].Expression);
            case MemberAccessExpressionSyntax zugriff:
                return zugriff.Name.Identifier.Text;
            case IdentifierNameSyntax bezeichner:
                return bezeichner.Identifier.Text;
            case ParenthesizedLambdaExpressionSyntax:
            case SimpleLambdaExpressionSyntax:
            case AnonymousMethodExpressionSyntax:
                return "(anonym)";
            default:
                return ausdruck.ToString();
        }
    }

    /// <summary>
    /// Normalisierter Wert einer Zuweisung: Zeichenkette ohne
    /// Anfuehrungszeichen, <c>new Point(159, 26)</c> als "159, 26",
    /// Aufzaehlung auf das letzte Glied gekuerzt.
    /// </summary>
    private static string Wert(ExpressionSyntax ausdruck)
    {
        switch (ausdruck)
        {
            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                return literal.Token.ValueText;
            case LiteralExpressionSyntax literal:
            {
                var text = literal.Token.Text;
                // Zahlensuffixe (9.75F, 100M) stoeren beim Vergleichen.
                if (text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-' || text[0] == '.'))
                {
                    text = text.TrimEnd('F', 'f', 'D', 'd', 'M', 'm', 'L', 'l', 'U', 'u');
                }
                return text;
            }
            case PrefixUnaryExpressionSyntax vorzeichen when vorzeichen.IsKind(SyntaxKind.UnaryMinusExpression):
                return "-" + Wert(vorzeichen.Operand);
            case ParenthesizedExpressionSyntax klammer:
                return Wert(klammer.Expression);
            case CastExpressionSyntax umwandlung:
                return Wert(umwandlung.Expression);
            case ObjectCreationExpressionSyntax erzeugung:
            {
                var typ = erzeugung.Type.ToString();
                var einfach = typ.Substring(typ.LastIndexOf('.') + 1);
                var argumente = erzeugung.ArgumentList?.Arguments ?? default;
                if ((einfach is "Point" or "Size" or "SizeF") && argumente.Count == 2)
                {
                    return Wert(argumente[0].Expression) + ", " + Wert(argumente[1].Expression);
                }
                if (einfach == "Font" && argumente.Count >= 2)
                {
                    return Wert(argumente[0].Expression) + ", " + Wert(argumente[1].Expression);
                }
                if (einfach == "decimal" && Dezimalzahl(erzeugung) is { } zahl)
                {
                    return zahl.ToString(CultureInfo.InvariantCulture);
                }
                return Kuerzen(erzeugung.ToString());
            }
            case MemberAccessExpressionSyntax zugriff:
                // System.Windows.Forms.ComboBoxStyle.DropDownList -> DropDownList
                return zugriff.Name.Identifier.Text;
            default:
                return Kuerzen(ausdruck.ToString());
        }
    }

    /// <summary>
    /// Der Designer schreibt die Grenzen einer NumericUpDown als
    /// <c>new decimal(new int[] { lo, mid, hi, flags })</c>. Hier wird daraus
    /// wieder eine Zahl.
    /// </summary>
    private static decimal? Dezimalzahl(ObjectCreationExpressionSyntax erzeugung)
    {
        var argumente = erzeugung.ArgumentList?.Arguments;
        if (argumente is not { Count: 1 }) return null;

        var teile = argumente.Value[0].Expression switch
        {
            ArrayCreationExpressionSyntax { Initializer: { } feld } => feld.Expressions,
            ImplicitArrayCreationExpressionSyntax implizit => implizit.Initializer.Expressions,
            _ => default
        };
        if (teile.Count != 4) return null;

        var bits = new int[4];
        for (var i = 0; i < 4; i++)
        {
            if (!int.TryParse(Wert(teile[i]), NumberStyles.Integer, CultureInfo.InvariantCulture, out bits[i]))
            {
                return null;
            }
        }
        try
        {
            return new decimal(bits);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string Kuerzen(string text) => text
        .Replace("System.Windows.Forms.", "")
        .Replace("System.Drawing.", "")
        .Replace("System.ComponentModel.", "")
        .Replace(Environment.NewLine, " ")
        .Replace("\n", " ")
        .Replace("\r", " ");
}
