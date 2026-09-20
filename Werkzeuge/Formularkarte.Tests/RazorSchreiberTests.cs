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

    /// <summary>
    /// Dasselbe Skelett aus dem eingefrorenen Pruefmuster. Vorbild des Schreibers
    /// ist EnergietraegerVarianteDialog.razor - also genau die Komponente, die
    /// Form_Kosten_Auswahl seit iU8-9 (Stichtag iZ5) ersetzt. Geprueft wird
    /// deshalb weiter an deren letzter WinForms-Fassung, die dazu unter
    /// Pruefmuster/Kosten/ liegt. Der Ordnername ist der Fachbereich und damit
    /// der Namensraum - "Kosten" muss er heissen, nicht anders.
    /// </summary>
    private static string Musterskelett(string relativ) =>
        RazorSchreiber.Schreiben(
            Kartenbau.Vollstaendig(Repowurzel.Pruefmuster(relativ), null, Repowurzel.PruefmusterWurzel));

    [Fact]
    public void NennteZielnamensraumUndBausteine()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("@namespace EPOS.UI.Dialoge.Kosten", razor, StringComparison.Ordinal);
        Assert.Contains("<Auswahlfeld ", razor, StringComparison.Ordinal);
        Assert.Contains("<Textfeld ", razor, StringComparison.Ordinal);
        Assert.Contains("<SpeichernLeiste ", razor, StringComparison.Ordinal);
        Assert.Contains("<Warnbanner Stufe=\"WarnStufe.Warnung\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void BindetAnDasWerteRecord()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("@bind-Auswahl=\"Werte.BrennstoffArt\"", razor, StringComparison.Ordinal);
        Assert.Contains("@bind-Wert=\"Werte.Variante\"", razor, StringComparison.Ordinal);
        Assert.Contains("public int? BrennstoffArt { get; set; }", razor, StringComparison.Ordinal);
        Assert.Contains("public string Variante { get; set; } = \"\";", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void MeldetDasErgebnisUeberEinenEventCallback()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("EventCallback<Form_Kosten_AuswahlErgebnis?> Geschlossen", razor, StringComparison.Ordinal);
        Assert.Contains("public sealed record Form_Kosten_AuswahlErgebnis(Form_Kosten_AuswahlWerte Werte);",
                        razor, StringComparison.Ordinal);
    }

    [Fact]
    public void TexteStehenDeutschMitHinweisAufDenRessourcenschluessel()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("public string TitelText { get; set; } = \"Energieträger Variante\"; // TODO Ressourcenschluessel",
                        razor, StringComparison.Ordinal);
        Assert.Contains("= \"Energieträger:\"; // TODO Ressourcenschluessel", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void BeantwortetEnterUndEscSelbst()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        Assert.Contains("tabindex=\"-1\"", razor, StringComparison.Ordinal);
        Assert.Contains("@onkeydown=\"BeiTaste\"", razor, StringComparison.Ordinal);
        Assert.Contains("\"Escape\" => BeiErgebnis(false)", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void NenntJedenHandlerMitFundstelleUndUmfang()
    {
        var razor = Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs");

        // Die Zeilennummer wird nicht festgeschrieben - der Bestand bewegt sich.
        Assert.Matches(@"// TODO: btn_OK\.Click -> btnOk_Click aus Form_Kosten_Auswahl\.cs:\d+ \(\d+ Zeilen\)", razor);
        Assert.Matches(@"// TODO: Fenster\.Load -> Form_Kosten_Auswahl_Load aus Form_Kosten_Auswahl\.cs:\d+ \(\d+ Zeilen\)", razor);
    }

    [Fact]
    public void OhneHilfeknopfImDesignerKeinInfoKnopf()
    {
        Assert.DoesNotContain("<InfoKnopf", Musterskelett("Kosten/Form_Kosten_Auswahl.Designer.cs"), StringComparison.Ordinal);

        // iU9-W14c.9: Form_Klimadaten ist mit Welle 14c gefallen und liegt als
        // PRUEFMUSTER - sie war die einzige Maske, deren btn_Help im Designer stand.
        Assert.Contains("<InfoKnopf Schluessel=\"@HilfeSchluessel\" />",
                        Musterskelett("Klimadaten/Form_Klimadaten.Designer.cs"), StringComparison.Ordinal);
        Assert.Contains("HilfeSchluessel { get; set; } = \"Form_Klimadaten.btn_Help\"",
                        Musterskelett("Klimadaten/Form_Klimadaten.Designer.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void AbschnitteWerdenZuGruppenkoepfen()
    {
        var razor = Musterskelett("Kosten/Form_Kostenprofil.Designer.cs");

        Assert.Contains("<Gruppenkopf Titel=\"@MonatTitel\">", razor, StringComparison.Ordinal);
        // Gruppenkopf nimmt seinen Inhalt nicht als ChildContent, sondern als
        // benannten Parameter KindInhalt - ohne das Element uebersetzt es nicht.
        Assert.Contains("<KindInhalt>", razor, StringComparison.Ordinal);
        Assert.Contains("<DiagrammSvg Modell=\"@Werte.Chart\"", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void KleingeschriebeneMaskenBekommenEinenGrossenAnfangsbuchstaben()
    {
        // Razor laesst kleingeschriebene Komponentennamen nicht zu (RZ10011).
        //
        // iU9-W0 (03.09.2026): Bis dahin stand hier ucKostenItem. Die Maske haengt am
        // einstiegslosen Form_Kosten und ist mit ihm stillgelegt (Anwenderentscheid
        // iF29); der Zeuge wurde ucVorlagenZeile.
        //
        // iU9-W4.2: Auch ucVorlagenZeile ist umgestellt und geloescht (Regel M1).
        // Sie bleibt der Zeuge - als eingefrorenes Pruefmuster, denn sie ist die
        // EINZIGE kleingeschriebene Maske, die der Bestand je gefuehrt hat.
        var maske = Kartenbau.Vollstaendig(Repowurzel.Pruefmuster("Kosten/ucVorlagenZeile.Designer.cs"));

        Assert.Equal("UcVorlagenZeile.razor", RazorSchreiber.Dateiname(maske));
        Assert.Contains("public sealed record UcVorlagenZeileWerte", RazorSchreiber.Schreiben(maske),
                        StringComparison.Ordinal);
    }

    [Fact]
    public void UmlauteImOrdnernamenWerdenUmschrieben()
    {
        // Der Zeuge stand bis iU9-W7.10 auf Form_WPFilterAuswahl, danach auf
        // Form_WP_einlesen im selben Ordner mit Umlaut.
        //
        // iU9-W13.6: Auch Form_WP_einlesen ist umgestellt und geloescht (Regel M1).
        // Ihr Designer bleibt der Zeuge - als eingefrorenes PRUEFMUSTER im Ordner
        // Pruefmuster/Wärmepumpe/, genau wie W7 es mit Wizard_WPItem und W4 mit
        // ucVorlagenZeile gemacht hat. Das ist die stabile Loesung: Der
        // Umlaut-Ordner IST die Pruefsache, nicht die Maske - ein eingefrorenes
        // Muster kann keine Welle mehr wegnehmen. Die Umschreibregel (ä -> ae)
        // bleibt in Kraft, solange EPOS.UI/Dialoge/Waermepumpe/ so heisst.
        var maske = Kartenbau.Vollstaendig(
            Repowurzel.Pruefmuster("Wärmepumpe/Form_WP_einlesen.designer.cs"),
            null, Repowurzel.PruefmusterWurzel);

        Assert.Equal("EPOS.UI.Dialoge.Waermepumpe", RazorSchreiber.Namensraum(maske));
    }

    [Fact]
    public void MaskenAusserhalbEinesFachordnersLandenInAllgemein()
    {
        // Hauptfensterrahmen lag in der PROJEKTWURZEL; hiesse der Namensraum
        // "EPOS.UI.Dialoge.WindowsFormsApplication1", verdeckte er das
        // @using WindowsFormsApplication1.MyResource aus _Imports.razor.
        //
        // iU9-W16c.5: Hauptfensterrahmen ist mit dem Rueckbau der Huelle ohne Designer
        // (er steht eingefroren unter Pruefmuster/Hauptformular/, Entscheid E-9),
        // und im PROJEKTORDNER selbst liegt danach keine Designer-Datei mehr. Der
        // Zeuge wandert deshalb auf den ZWEITEN Zweig derselben Regel
        // (DesignerLeser.Fachbereich:122-128): Ein Ordner namens "Views" oder
        // "Properties" ist ebenfalls kein Fachbereich. Properties/Resources.Designer.cs
        // ist die erzeugte Ressourcendatei jedes WinForms-Projekts - sie kann
        // keine Welle mehr wegnehmen.
        // Beide Zweige der Regel, unmittelbar an ihrer Quelle:
        Assert.Equal("Allgemein", DesignerLeser.Fachbereich(
            Repowurzel.Datei("WindowsFormsApplication1/Properties/Resources.Designer.cs")));

        // ... und der PROJEKTORDNER selbst, in dem MDIMainForm.Designer.cs bis
        // iU9-W16c.3 lag - erkannt an der .csproj daneben.
        Assert.Equal("Allgemein", DesignerLeser.Fachbereich(
            Repowurzel.Datei("WindowsFormsApplication1/Program.cs")));

        // Und der Gegenbeweis am eingefrorenen Muster: Dieselbe Maske in einem
        // Ordner MIT Namen bekommt diesen Namen - samt der Umsetzung in den
        // Namensraum, um die es hier geht.
        var imMuster = Kartenbau.Vollstaendig(
            Repowurzel.Pruefmuster("Hauptformular/MDIMainForm.Designer.cs"),
            null, Repowurzel.PruefmusterWurzel);

        Assert.Equal("EPOS.UI.Dialoge.Hauptformular", RazorSchreiber.Namensraum(imMuster));
    }

    [Fact]
    public void ChartsWerdenZuDiagrammSvgMitEigenerKennung()
    {
        // DG-E3: Die Bausteine ChartBild (PNG-Bild) und Diagramm sind gefallen -
        // jedes Diagramm der Oberflaeche ist ein Zeichenmodell im Baustein
        // DiagrammSvg (EPOS.UI/CLAUDE.md, "Anordnung"). Ein Skelett, das noch
        // <ChartBild Png=...> schriebe, zeigte auf eine Komponente, die es nicht
        // mehr gibt, und baute nicht.
        //
        // Zeuge ist Form_Klimadaten, das einzige Pruefmuster mit ZWEI Charts in
        // EINEM Dialog (chart1 und chart2, je ein Reiter). DiagrammSvg bildet aus
        // der Kennung seine clipPath-Namen; tragen zwei Bilder eines Blattes
        // dieselbe, schneidet das eine am Rechteck des anderen. Die Kennung muss
        // deshalb je Bild verschieden sein - der Feldname ist es, denn er ist im
        // Werte-Record schon eindeutig.
        var razor = Musterskelett("Klimadaten/Form_Klimadaten.Designer.cs");

        Assert.Contains("<DiagrammSvg Modell=\"@Werte.Chart1\" Kennung=\"chart1\" Bezeichnung=\"@Chart1Text\" />",
                        razor, StringComparison.Ordinal);
        Assert.Contains("<DiagrammSvg Modell=\"@Werte.Chart2\" Kennung=\"chart2\" Bezeichnung=\"@Chart2Text\" />",
                        razor, StringComparison.Ordinal);

        // Das Feld im Werte-Record traegt das Modell, keine PNG-Bytes mehr.
        Assert.Contains("public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Chart1 { get; set; }",
                        razor, StringComparison.Ordinal);
        Assert.Contains("public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Chart2 { get; set; }",
                        razor, StringComparison.Ordinal);
        Assert.DoesNotContain("byte[]", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("<ChartBild", razor, StringComparison.Ordinal);

        // Und die Kennungen aller Bilder des Blattes sind paarweise verschieden.
        var kennungen = System.Text.RegularExpressions.Regex
            .Matches(razor, "<DiagrammSvg [^>]*Kennung=\"([^\"]*)\"")
            .Select(treffer => treffer.Groups[1].Value)
            .ToList();
        Assert.Equal(2, kennungen.Count);
        Assert.Equal(kennungen.Count, kennungen.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void JederGenannteBausteinLiegtInEposUi()
    {
        // Der Schreiber kennt seine Bausteine nur dem Namen nach, und EPOS.UI
        // weiss nichts von ihm. Faellt dort ein Baustein - ChartBild mit DG-E3 -,
        // merkt der Schreiber es nicht, und wer die Karte fuer eine neue Maske
        // zieht, bekommt Quelltext, der nicht baut. Diese Wache haelt jeden
        // Bausteinnamen aus den Skeletten ALLER Pruefmuster gegen die
        // .razor-Dateien in EPOS.UI - so faellt die naechste Umbenennung hier
        // auf, nicht erst beim Ziehen der Karte.
        //
        // Gemessen wird nur der Markup-Teil vor "@code {": Im Code stehen
        // generische Typen wie EventCallback<UcVorlagenZeileErgebnis?>, deren
        // Argument wie ein Bausteinname aussieht. HTML-Elemente schreiben sich
        // klein und fallen durch das Muster; KindInhalt ist kein Baustein, sondern
        // der benannte Inhaltsparameter von Gruppenkopf und Raster.
        var vorhanden = Directory
            .EnumerateFiles(Repowurzel.Datei("EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .Where(pfad => !pfad.Split(Path.DirectorySeparatorChar).Any(teil => teil is "bin" or "obj"))
            .Select(pfad => Path.GetFileNameWithoutExtension(pfad))
            .ToHashSet(StringComparer.Ordinal);
        var parameter = new HashSet<string>(StringComparer.Ordinal) { "KindInhalt" };

        var fehlend = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var designer in Stapel.Dateien(Repowurzel.PruefmusterWurzel))
        {
            var maske = Kartenbau.Vollstaendig(designer, null, Repowurzel.PruefmusterWurzel, erreichbarkeit: false);
            var razor = RazorSchreiber.Schreiben(maske);
            var markup = razor.Substring(0, razor.IndexOf("@code {", StringComparison.Ordinal));

            foreach (System.Text.RegularExpressions.Match treffer in
                     System.Text.RegularExpressions.Regex.Matches(markup, "<([A-Z][A-Za-z0-9]*)"))
            {
                var baustein = treffer.Groups[1].Value;
                if (parameter.Contains(baustein) || vorhanden.Contains(baustein)) continue;
                fehlend.Add(baustein + " (" + Path.GetFileName(designer) + ")");
            }
        }

        Assert.True(fehlend.Count == 0,
                    "Skelette nennen Bausteine, die EPOS.UI nicht hat: " + string.Join(", ", fehlend));
    }
}
