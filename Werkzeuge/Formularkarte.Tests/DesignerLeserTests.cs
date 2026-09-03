using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Der Leser gegen die beiden Masken, deren Feldkarte im Umsetzungsplan iU8
/// von Hand steht - Abschnitt D. Stimmen sie, stimmt die Grundmechanik:
/// beide Schreibweisen des Designers, die Zeilenregel fuer Beschriftungen und
/// die Zuordnung der Zielkomponenten.
/// </summary>
public sealed class DesignerLeserTests
{
    private const string KostenAuswahl = "Kosten/Form_Kosten_Auswahl.Designer.cs";
    private const string KostenfaktorItem = "Kosten/Form_KostenfaktorItem.Designer.cs";

    private static Maske Lesen(string relativ) =>
        Kartenbau.Vollstaendig(Repowurzel.Designer(relativ));

    // Die Zeilenregel fuer Beschriftungen kommt erst mit iU8-12b; bis dahin
    // steht jedes Label als eigene Zeile in der Karte.

    // ---- Form_Kosten_Auswahl: die Handkarte aus dem Plan --------------------

    [Fact]
    public void KostenAuswahl_HatSechsZeilen()
    {
        var abschnitte = Kartenbau.Abschnitte(Lesen(KostenAuswahl));

        var abschnitt = Assert.Single(abschnitte);
        Assert.Equal("Fenster", abschnitt.Titel);
        Assert.Equal(6, abschnitt.Zeilen.Count);
    }

    [Fact]
    public void KostenAuswahl_ZeilenStehenInTabIndexReihenfolge()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenAuswahl))[0].Zeilen;

        Assert.Equal(
            new[] { "cmbBrennstoffArt", "TextBox_Variante", "btn_Abbrechen", "btn_OK", "label1", "label_Variante" },
            zeilen.Select(z => z.Element.Name).ToArray());
    }

    [Fact]
    public void KostenAuswahl_LiestDieTexteDerLabel()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenAuswahl))[0].Zeilen;

        Assert.Equal("Energieträger:", zeilen[4].TextDe);
        Assert.Equal("Energieträger Varianten Bezeichnung:", zeilen[5].TextDe);
        Assert.Equal(new Paar(13, 29), zeilen[4].Element.Ort);
    }

    [Fact]
    public void KostenAuswahl_ZielkomponentenStimmen()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenAuswahl))[0].Zeilen;

        Assert.Equal("Auswahlfeld", zeilen[0].Komponente);
        Assert.Equal("Textfeld", zeilen[1].Komponente);
        Assert.Equal("SpeichernLeiste", zeilen[2].Komponente);
        Assert.Equal("SpeichernLeiste", zeilen[3].Komponente);
    }

    [Fact]
    public void KostenAuswahl_BeideKnoepfeHabenEinenClickHandler()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenAuswahl))[0].Zeilen;
        var knoepfe = zeilen.Where(z => z.Element.Typ == "Button").Select(z => z.Element).ToList();

        Assert.Equal(2, knoepfe.Count);
        Assert.All(knoepfe, k => Assert.Equal("Click", Assert.Single(k.Ereignisse).Ereignis));
        Assert.Equal("btn_Abbrechen_Click", knoepfe[0].Ereignisse[0].Handler);
        Assert.Equal("btnOk_Click", knoepfe[1].Ereignisse[0].Handler);
    }

    [Fact]
    public void KostenAuswahl_LiestFormulareigenschaftenOhneThis()
    {
        var maske = Lesen(KostenAuswahl);

        Assert.Equal("Energieträger Variante", maske.Titel);
        Assert.Equal(new Paar(356, 185), maske.Fenstergroesse);
        Assert.False(maske.Lokalisiert);
        Assert.Equal("Form_Kosten_Auswahl_Load", Assert.Single(maske.FormularEreignisse).Handler);
    }

    [Fact]
    public void KostenAuswahl_KenntMeldungUndAufrufer()
    {
        var maske = Lesen(KostenAuswahl);

        Assert.True(maske.QuelltextGefunden);
        Assert.Equal(1, maske.Meldungen);
        Assert.Equal("WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs:2092", Assert.Single(maske.Aufrufer));
    }

    [Fact]
    public void KostenAuswahl_NenntZeileUndUmfangDerHandler()
    {
        var maske = Lesen(KostenAuswahl);

        Assert.True(maske.Handler.TryGetValue("btnOk_Click", out var stelle));
        Assert.Equal(42, stelle.Zeile);
        Assert.Equal(14, stelle.Zeilen);
    }

    // ---- Form_KostenfaktorItem: 7 Zeilen, 5 Zuordnungen --------------------

    [Fact]
    public void Kostenfaktor_HatZwoelfZeilen()
    {
        var abschnitt = Assert.Single(Kartenbau.Abschnitte(Lesen(KostenfaktorItem)));
        Assert.Equal(12, abschnitt.Zeilen.Count);
    }

    [Fact]
    public void Kostenfaktor_LiestKoordinatenBeiderSpalten()
    {
        var maske = Lesen(KostenfaktorItem);

        // Label links (x = 16), Feld rechts (x = 114) - die Rohdaten der
        // Zeilenregel, die iU8-12b daraus macht.
        Assert.Equal(new Paar(16, 163), maske.Finden("label2")!.Ort);
        Assert.Equal(new Paar(114, 160), maske.Finden("textBox_Wert")!.Ort);
    }

    [Fact]
    public void Kostenfaktor_LiestDieSchreibweiseMitThis()
    {
        var maske = Lesen(KostenfaktorItem);

        // Der Designer schreibt hier durchgaengig "this.x = new ...".
        Assert.Equal(12, maske.Steuerelemente.Count(s => s.Art != Art.Beiwerk));
        Assert.Equal(new Paar(370, 255), maske.Fenstergroesse);
        Assert.Equal(new Paar(114, 32), maske.Finden("comboBox1")!.Ort);
        Assert.Equal("DropDownList", maske.Finden("comboBox1")!.Wert("DropDownStyle"));
    }

    [Fact]
    public void Kostenfaktor_HatKeineMeldungUndEinenAufrufer()
    {
        var maske = Lesen(KostenfaktorItem);

        Assert.Equal(0, maske.Meldungen);
        Assert.Equal("WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs:1482", Assert.Single(maske.Aufrufer));
    }
}
