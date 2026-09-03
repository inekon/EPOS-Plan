using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Das Razor-Skelett. Geprueft wird, dass es die Bausteine aus EPOS.UI
/// verwendet, an ein Werte-Record bindet und jeden Ereignishandler des
/// Vorbilds mit Fundstelle nennt. Dass es auch UEBERSETZT, steht in
/// LIESMICH.md unter "Nachweis" - dafuer wird es in eine Kopie von EPOS.UI
/// gelegt und gebaut; ein Razor-Uebersetzer gehoert nicht in diese Tests.
/// </summary>
public sealed class RazorSchreiberTests
{
    private static string Skelett(string relativ) =>
        RazorSchreiber.Schreiben(Kartenbau.Vollstaendig(Repowurzel.Designer(relativ)));

    [Fact]
    public void NennteZielnamensraumUndBausteine()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("@namespace EPOS.UI.Dialoge.Kosten", razor, StringComparison.Ordinal);
        Assert.Contains("<Auswahlfeld ", razor, StringComparison.Ordinal);
        Assert.Contains("<Textfeld ", razor, StringComparison.Ordinal);
        Assert.Contains("<SpeichernLeiste ", razor, StringComparison.Ordinal);
        Assert.Contains("<Warnbanner Stufe=\"WarnStufe.Warnung\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void BindetAnDasWerteRecord()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("@bind-Auswahl=\"Werte.BrennstoffArt\"", razor, StringComparison.Ordinal);
        Assert.Contains("@bind-Wert=\"Werte.Variante\"", razor, StringComparison.Ordinal);
        Assert.Contains("public int? BrennstoffArt { get; set; }", razor, StringComparison.Ordinal);
        Assert.Contains("public string Variante { get; set; } = \"\";", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void MeldetDasErgebnisUeberEinenEventCallback()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("EventCallback<Form_Kosten_AuswahlErgebnis?> Geschlossen", razor, StringComparison.Ordinal);
        Assert.Contains("public sealed record Form_Kosten_AuswahlErgebnis(Form_Kosten_AuswahlWerte Werte);",
                        razor, StringComparison.Ordinal);
    }

    [Fact]
    public void TexteStehenDeutschMitHinweisAufDenRessourcenschluessel()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("public string TitelText { get; set; } = \"Energieträger Variante\"; // TODO Ressourcenschluessel",
                        razor, StringComparison.Ordinal);
        Assert.Contains("= \"Energieträger:\"; // TODO Ressourcenschluessel", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void BeantwortetEnterUndEscSelbst()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("tabindex=\"-1\"", razor, StringComparison.Ordinal);
        Assert.Contains("@onkeydown=\"BeiTaste\"", razor, StringComparison.Ordinal);
        Assert.Contains("\"Escape\" => BeiErgebnis(false)", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void NenntJedenHandlerMitFundstelleUndUmfang()
    {
        var razor = Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        // Die Zeilennummer wird nicht festgeschrieben - der Bestand bewegt sich.
        Assert.Matches(@"// TODO: btn_OK\.Click -> btnOk_Click aus Form_Kosten_Auswahl\.cs:\d+ \(\d+ Zeilen\)", razor);
        Assert.Matches(@"// TODO: Fenster\.Load -> Form_Kosten_Auswahl_Load aus Form_Kosten_Auswahl\.cs:\d+ \(\d+ Zeilen\)", razor);
    }

    [Fact]
    public void OhneHilfeknopfImDesignerKeinInfoKnopf()
    {
        Assert.DoesNotContain("<InfoKnopf", Skelett("Kosten/Form_Kosten_Auswahl.Designer.cs"), StringComparison.Ordinal);
        Assert.Contains("<InfoKnopf Schluessel=\"@HilfeSchluessel\" />",
                        Skelett("Klimadaten/Form_Klimadaten.Designer.cs"), StringComparison.Ordinal);
        Assert.Contains("HilfeSchluessel { get; set; } = \"Form_Klimadaten.btn_Help\"",
                        Skelett("Klimadaten/Form_Klimadaten.Designer.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void AbschnitteWerdenZuGruppenkoepfen()
    {
        var razor = Skelett("Kosten/Form_Kostenprofil.Designer.cs");

        Assert.Contains("<Gruppenkopf Titel=\"@MonatTitel\">", razor, StringComparison.Ordinal);
        // Gruppenkopf nimmt seinen Inhalt nicht als ChildContent, sondern als
        // benannten Parameter KindInhalt - ohne das Element uebersetzt es nicht.
        Assert.Contains("<KindInhalt>", razor, StringComparison.Ordinal);
        Assert.Contains("<ChartBild Png=\"@Werte.Chart\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void KleingeschriebeneMaskenBekommenEinenGrossenAnfangsbuchstaben()
    {
        // Razor laesst kleingeschriebene Komponentennamen nicht zu (RZ10011).
        var maske = Kartenbau.Vollstaendig(Repowurzel.Designer("Kosten/ucKostenItem.Designer.cs"));

        Assert.Equal("UcKostenItem.razor", RazorSchreiber.Dateiname(maske));
        Assert.Contains("public sealed record UcKostenItemWerte", RazorSchreiber.Schreiben(maske),
                        StringComparison.Ordinal);
    }

    [Fact]
    public void UmlauteImOrdnernamenWerdenUmschrieben()
    {
        var maske = Kartenbau.Vollstaendig(Repowurzel.Designer("Wärmepumpe/Form_WPFilterAuswahl.Designer.cs"));

        Assert.Equal("EPOS.UI.Dialoge.Waermepumpe", RazorSchreiber.Namensraum(maske));
    }

    [Fact]
    public void MaskenAusserhalbEinesFachordnersLandenInAllgemein()
    {
        // MDIMainForm liegt in der Projektwurzel; hiesse der Namensraum
        // "EPOS.UI.Dialoge.WindowsFormsApplication1", verdeckte er das
        // @using WindowsFormsApplication1.MyResource aus _Imports.razor.
        var maske = Kartenbau.Vollstaendig(Repowurzel.Datei("WindowsFormsApplication1/MDIMainForm.Designer.cs"));

        Assert.Equal("EPOS.UI.Dialoge.Allgemein", RazorSchreiber.Namensraum(maske));
    }
}
