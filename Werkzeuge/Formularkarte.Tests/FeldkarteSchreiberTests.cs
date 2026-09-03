using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Die Feldkarte selbst - Kopfzeilen, Tabellenkopf und Abnahme-Checkbox.
/// Sie ist die Checkliste, an der eine umgestellte Maske abgenommen wird;
/// ihre Form muss deshalb verlaesslich sein.
/// </summary>
public sealed class FeldkarteSchreiberTests
{
    private static string Karte(string relativ) =>
        FeldkarteSchreiber.Schreiben(Kartenbau.Vollstaendig(Repowurzel.Designer(relativ)));

    [Fact]
    public void KopfNenntMaskeTitelGroesseUndAufrufer()
    {
        var karte = Karte("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("# Feldkarte Form_Kosten_Auswahl", karte, StringComparison.Ordinal);
        Assert.Contains("| Titel de | Energieträger Variante |", karte, StringComparison.Ordinal);
        Assert.Contains("| ClientSize | 356 x 185 |", karte, StringComparison.Ordinal);
        Assert.Contains("| MessageBox | 1 |", karte, StringComparison.Ordinal);
        Assert.Contains("Form_Kosten.cs:2092", karte, StringComparison.Ordinal);
    }

    [Fact]
    public void TabellenkopfStehtWieVereinbart()
    {
        var karte = Karte("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains(
            "| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | " +
            "Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |",
            karte, StringComparison.Ordinal);
    }

    [Fact]
    public void JedeZeileEndetMitDerAbnahmeCheckbox()
    {
        var karte = Karte("Kosten/Form_Kosten_Auswahl.Designer.cs");
        var zeilen = karte.Split('\n').Where(z => z.StartsWith("| 1 | `", StringComparison.Ordinal)
                                              || z.StartsWith("| 2 | `", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(zeilen);
        Assert.All(zeilen, z => Assert.EndsWith("| ☐ |", z.TrimEnd('\r'), StringComparison.Ordinal));
    }

    [Fact]
    public void SenkrechteImTextBrichtDieTabelleNicht()
    {
        var element = new Steuerelement { Name = "lbl", Typ = "Label", Art = Art.Beschriftung };
        element.Eigenschaften["Text"] = "a | b";
        element.Eigenschaften["Location"] = "10, 10";

        var maske = new Maske { Bezeichner = "Probe", Klasse = "Probe" };
        maske.Steuerelemente.Add(element);

        Assert.Contains("a \\| b", FeldkarteSchreiber.Schreiben(maske), StringComparison.Ordinal);
    }

    [Fact]
    public void AbschnitteBekommenEigeneUeberschriftUndTabelle()
    {
        var karte = Karte("Kosten/Form_Kostenprofil.Designer.cs");

        Assert.Contains("#### Monatswerte (`tpMonat`, TabPage)", karte, StringComparison.Ordinal);
        Assert.Contains("#### Grafik (`tpGrafik`, TabPage)", karte, StringComparison.Ordinal);
    }

    [Fact]
    public void EreignishandlerStehenMitZeileUndUmfangImFuss()
    {
        var karte = Karte("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("## Ereignishandler in `Form_Kosten_Auswahl.cs`", karte, StringComparison.Ordinal);
        Assert.Contains("| `btnOk_Click` | 42 | 14 Zeilen |", karte, StringComparison.Ordinal);
    }

    [Fact]
    public void LokalisierteMaskeNenntIhreRessourcendateien()
    {
        var karte = Karte("Stromspeicher/Form_StromspeicherItemNeu.Designer.cs");

        Assert.Contains("| Lokalisiert | ja (ApplyResources) |", karte, StringComparison.Ordinal);
        Assert.Contains("`Form_StromspeicherItemNeu.en-US.resx`", karte, StringComparison.Ordinal);
        Assert.Contains("| Titel en | Enter identifier |", karte, StringComparison.Ordinal);
    }

    [Fact]
    public void GrenzenEinerNumericUpDownStehenInDerSpalteBereich()
    {
        // Der Designer schreibt Minimum/Maximum als new decimal(new int[]{...}) -
        // in der Karte muss wieder eine Zahl stehen.
        var maske = Kartenbau.Vollstaendig(Repowurzel.Designer("Kosten/Form_CaseEingabe.Designer.cs"));
        var zeilen = Kartenbau.Abschnitte(maske).SelectMany(a => a.Zeilen)
            .Where(z => z.Element.Typ == "NumericUpDown").ToList();

        Assert.NotEmpty(zeilen);
        Assert.All(zeilen, z => Assert.Contains("Max=", z.Bereich, StringComparison.Ordinal));
        Assert.All(zeilen, z => Assert.DoesNotContain("decimal", z.Bereich, StringComparison.Ordinal));
    }
}
