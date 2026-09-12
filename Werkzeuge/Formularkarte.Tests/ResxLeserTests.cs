using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Die lokalisierten Masken: Bei ihnen steht im Designer nur
/// <c>resources.ApplyResources(ctrl, "name")</c> - Koordinaten, Groessen,
/// TabIndex und Texte kommen aus <c>Form_X.resx</c>, die Uebersetzungen aus
/// <c>Form_X.de-DE.resx</c> und <c>Form_X.en-US.resx</c>.
/// Beispielmaske: <c>Form_StromspeicherItemNeu</c> (Klasse
/// <c>Form_Sp_ItemNeu</c> - Datei- und Klassenname weichen ab).
///
/// <para>Sie ist mit <b>iU9-W2.1</b> auf die Razor-Komponente
/// <c>EPOS.UI/Dialoge/Allgemein/NamensDialog.razor</c> umgestellt und im
/// Bestand geloescht (Regel M1); ihr letzter Stand liegt eingefroren als
/// Pruefmuster daneben. Sie bleibt der Beleg fuer den lokalisierten Weg: drei
/// Ressourcendateien, Koordinaten und TabIndex ausschliesslich in der
/// <c>.resx</c>, dazu ein Klassenname, der vom Dateinamen abweicht.</para>
/// </summary>
public sealed class ResxLeserTests
{
    private const string ItemNeu = "Stromspeicher/Form_StromspeicherItemNeu.Designer.cs";

    private static Maske Lesen() =>
        Kartenbau.Vollstaendig(Repowurzel.Pruefmuster(ItemNeu), null, Repowurzel.PruefmusterWurzel);

    [Fact]
    public void ErkenntDieLokalisierungUndLiestAlleDreiDateien()
    {
        var maske = Lesen();

        Assert.True(maske.Lokalisiert);
        Assert.Equal(3, maske.Ressourcendateien.Count);
        Assert.All(new[] { ".resx", ".de-DE.resx", ".en-US.resx" },
                   endung => Assert.Contains(maske.Ressourcendateien, d => d.EndsWith(endung, StringComparison.Ordinal)));
    }

    [Fact]
    public void KlassennameWeichtVomDateinamenAb()
    {
        var maske = Lesen();

        Assert.Equal("Form_StromspeicherItemNeu", maske.Bezeichner);
        Assert.Equal("Form_Sp_ItemNeu", maske.Klasse);
    }

    [Fact]
    public void KoordinatenUndGroessenStehenInDerResx()
    {
        var maske = Lesen();

        // Im Designer steht zu diesen Feldern keine einzige Zahl.
        Assert.Equal(new Paar(15, 24), maske.Finden("label1")!.Ort);
        Assert.Equal(new Paar(203, 70), maske.Finden("btn_OK")!.Ort);
        Assert.Equal(65, maske.Finden("btn_OK")!.TabIndex);
        Assert.Equal(new Paar(325, 119), maske.Fenstergroesse);
    }

    [Fact]
    public void TexteKommenDeutschUndEnglisch()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen())[0].Zeilen
            .ToDictionary(z => z.Element.Name, StringComparer.Ordinal);

        Assert.Equal("Bezeichner", zeilen["textBox_Bezeichner"].TextDe);
        Assert.Equal("Identifier", zeilen["textBox_Bezeichner"].TextEn);
        Assert.Equal("Abbrechen", zeilen["btn_Abbrechen"].TextDe);
        Assert.Equal("Cancel", zeilen["btn_Abbrechen"].TextEn);
    }

    [Fact]
    public void FenstertitelKommtDeutschUndEnglisch()
    {
        var maske = Lesen();

        Assert.Equal("Bezeichner eingeben", maske.Titel);
        Assert.Equal("Enter identifier", maske.TitelEn);
    }

    [Fact]
    public void ZeilenregelGreiftAuchAufResxKoordinaten()
    {
        var abschnitt = Assert.Single(Kartenbau.Abschnitte(Lesen()));

        // label1 (15/24) steht links vom Textfeld - ohne die .resx haette
        // keines der beiden eine Koordinate und die Regel liefe leer.
        Assert.Equal(3, abschnitt.Zeilen.Count);
        Assert.Equal("Bezeichner", abschnitt.Zeilen.Single(z => z.Element.Name == "textBox_Bezeichner").TextDe);
    }

    [Fact]
    public void KommentierteBeispielzeilenDerResxWerdenNichtGelesen()
    {
        // Der Kopf jeder .resx enthaelt in einem XML-Kommentar Beispiele wie
        // <data name="Name1">. Wer die .resx mit einem Suchmuster statt mit
        // einem XML-Leser liest, nimmt sie als echte Eintraege mit.
        var werte = ResxLeser.Lesen(
            Repowurzel.Pruefmuster("Stromspeicher/Form_StromspeicherItemNeu.resx"));

        Assert.DoesNotContain("Name1", werte.Keys);
        Assert.DoesNotContain("Bitmap1", werte.Keys);
        Assert.Contains("$this.Text", werte.Keys);
    }
}
