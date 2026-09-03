using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Die Umstellungstabelle: welcher WinForms-Typ zu welcher Komponente aus
/// EPOS.UI wird. Sie steht in Werkzeuge/Formularkarte/LIESMICH.md und ist die
/// Vorgabe fuer iU9 - deshalb hier Zeile fuer Zeile festgehalten.
/// </summary>
public sealed class ZieltabelleTests
{
    private static Steuerelement Element(string typ, string name = "feld", string? zahlArt = null,
                                         string text = "")
    {
        var element = new Steuerelement { Name = name, Typ = typ, Art = Typtabelle.Einordnen(typ), ZahlArt = zahlArt };
        if (text.Length > 0) element.Eigenschaften["Text"] = text;
        return element;
    }

    [Theory]
    [InlineData("ComboBox", "Auswahl", "Auswahlfeld")]
    [InlineData("ListBox", "Auswahl", "Auswahlfeld")]
    [InlineData("NumericUpDown", "Zahl", "Zahlenfeld")]
    [InlineData("CheckBox", "Schalter", "Schalter")]
    [InlineData("DateTimePicker", "Datum", "Datumsfeld")]
    [InlineData("DataGridView", "Raster", "Raster")]
    [InlineData("ListView", "Raster", "Raster")]
    [InlineData("Chart", "Diagramm", "ChartBild")]
    [InlineData("GroupBox", "Sektion", "Gruppenkopf")]
    [InlineData("TabPage", "Sektion", "Gruppenkopf")]
    [InlineData("TabControl", "Sektion", "Aufteilung")]
    [InlineData("Label", "Text", "Text")]
    [InlineData("PictureBox", "-", "pruefen")]
    [InlineData("ProgressBar", "-", "pruefen")]
    [InlineData("AktionsKarte", "-", "pruefen")]
    public void TypFuehrtZurKomponente(string typ, string feldtyp, string komponente)
    {
        var ziel = Kartenbau.Ziel(Element(typ));

        Assert.Equal(feldtyp, ziel.Feldtyp);
        Assert.Equal(komponente, ziel.Komponente);
    }

    [Theory]
    [InlineData(null, "Text", "Textfeld")]
    [InlineData("Zahl", "Zahl", "Zahlenfeld")]
    [InlineData("Ganzzahl", "Ganzzahl", "Ganzzahlfeld")]
    public void TextBoxHaengtAnDerPruefungInDerFormCs(string? zahlArt, string feldtyp, string komponente)
    {
        var ziel = Kartenbau.Ziel(Element("TextBox", zahlArt: zahlArt));

        Assert.Equal(feldtyp, ziel.Feldtyp);
        Assert.Equal(komponente, ziel.Komponente);
    }

    [Theory]
    [InlineData("btn_OK", "", "SpeichernLeiste")]
    [InlineData("btn_Abbrechen", "", "SpeichernLeiste")]
    [InlineData("btnSpeichern", "", "SpeichernLeiste")]
    [InlineData("btn_Help", "", "InfoKnopf")]
    [InlineData("btn_Import", "", "Knopf (pruefen)")]
    [InlineData("btn_Irgendwas", "Abbrechen", "SpeichernLeiste")]
    public void KnopfHaengtAnNamenUndAufschrift(string name, string text, string komponente)
    {
        Assert.Equal(komponente, Kartenbau.Ziel(Element("Button", name, text: text)).Komponente);
    }

    [Fact]
    public void UnbekannteTypenWerdenNichtGeraten()
    {
        Assert.False(Typtabelle.Bekannt("KlimazonenKarte"));
        Assert.Equal("pruefen", Kartenbau.Ziel(Element("KlimazonenKarte")).Komponente);
    }

    [Theory]
    [InlineData("textBox_Wert", "Wert")]
    [InlineData("TextBox_Variante", "Variante")]
    [InlineData("cmbBrennstoffArt", "BrennstoffArt")]
    [InlineData("comboBox_Gruppe", "Gruppe")]
    [InlineData("btn_OK", "OK")]
    [InlineData("lbTag", "Tag")]
    [InlineData("comboBox1", "ComboBox1")]
    [InlineData("tabs", "Tabs")]
    public void KernnameStreiftDieVorsilbeAb(string name, string kern)
    {
        Assert.Equal(kern, Typtabelle.Kernname(name));
    }
}
