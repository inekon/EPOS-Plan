using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Der Leser gegen die beiden Masken, deren Feldkarte im Umsetzungsplan iU8
/// von Hand steht - Abschnitt D. Stimmen sie, stimmt die Grundmechanik:
/// beide Schreibweisen des Designers, die Zeilenregel fuer Beschriftungen und
/// die Zuordnung der Zielkomponenten.
///
/// <para>
/// Form_Kosten_Auswahl gibt es im Bestand seit iU8-9 (Stichtag iZ5) nicht mehr;
/// sie laeuft als EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor. Die
/// Handkarte aus dem Plan wird deshalb gegen das eingefrorene Pruefmuster unter
/// Pruefmuster/Kosten/ geprueft - Zeile fuer Zeile dieselbe Aussage wie vorher,
/// nur an einer Vorlage, die sich nicht mehr bewegt.
/// </para>
/// </summary>
public sealed class DesignerLeserTests
{
    private const string KostenAuswahl = "Kosten/Form_Kosten_Auswahl.Designer.cs";
    private const string KostenfaktorItem = "Kosten/Form_KostenfaktorItem.Designer.cs";

    private static Maske Lesen(string relativ) =>
        Kartenbau.Vollstaendig(Repowurzel.Designer(relativ));

    /// <summary>
    /// Eine Maske aus dem Pruefmuster-Ordner. Die Suche nach dem ShowDialog-
    /// Aufrufer laeuft ueber genau diesen Ordner, nicht ueber den Bestand.
    /// </summary>
    private static Maske Muster(string relativ) =>
        Kartenbau.Vollstaendig(Repowurzel.Pruefmuster(relativ), null, Repowurzel.PruefmusterWurzel);

    // ---- Form_Kosten_Auswahl: die Handkarte aus dem Plan --------------------

    [Fact]
    public void KostenAuswahl_HatVierZeilen()
    {
        var abschnitte = Kartenbau.Abschnitte(Muster(KostenAuswahl));

        var abschnitt = Assert.Single(abschnitte);
        Assert.Equal("Fenster", abschnitt.Titel);
        Assert.Equal(4, abschnitt.Zeilen.Count);
    }

    [Fact]
    public void KostenAuswahl_ZeilenStehenInTabIndexReihenfolge()
    {
        var zeilen = Kartenbau.Abschnitte(Muster(KostenAuswahl))[0].Zeilen;

        Assert.Equal(
            new[] { "cmbBrennstoffArt", "TextBox_Variante", "btn_Abbrechen", "btn_OK" },
            zeilen.Select(z => z.Element.Name).ToArray());
    }

    [Fact]
    public void KostenAuswahl_BeschriftungenStehenLinksInDerselbenZeile()
    {
        var zeilen = Kartenbau.Abschnitte(Muster(KostenAuswahl))[0].Zeilen;

        Assert.Equal("Energieträger:", zeilen[0].TextDe);
        Assert.Equal("Energieträger Varianten Bezeichnung:", zeilen[1].TextDe);
    }

    [Fact]
    public void KostenAuswahl_ZielkomponentenStimmen()
    {
        var zeilen = Kartenbau.Abschnitte(Muster(KostenAuswahl))[0].Zeilen;

        Assert.Equal("Auswahlfeld", zeilen[0].Komponente);
        Assert.Equal("Textfeld", zeilen[1].Komponente);
        Assert.Equal("SpeichernLeiste", zeilen[2].Komponente);
        Assert.Equal("SpeichernLeiste", zeilen[3].Komponente);
    }

    [Fact]
    public void KostenAuswahl_BeideKnoepfeHabenEinenClickHandler()
    {
        var zeilen = Kartenbau.Abschnitte(Muster(KostenAuswahl))[0].Zeilen;
        var knoepfe = zeilen.Where(z => z.Element.Typ == "Button").Select(z => z.Element).ToList();

        Assert.Equal(2, knoepfe.Count);
        Assert.All(knoepfe, k => Assert.Equal("Click", Assert.Single(k.Ereignisse).Ereignis));
        Assert.Equal("btn_Abbrechen_Click", knoepfe[0].Ereignisse[0].Handler);
        Assert.Equal("btnOk_Click", knoepfe[1].Ereignisse[0].Handler);
    }

    [Fact]
    public void KostenAuswahl_LiestFormulareigenschaftenOhneThis()
    {
        var maske = Muster(KostenAuswahl);

        Assert.Equal("Energieträger Variante", maske.Titel);
        Assert.Equal(new Paar(356, 185), maske.Fenstergroesse);
        Assert.False(maske.Lokalisiert);
        Assert.Equal("Form_Kosten_Auswahl_Load", Assert.Single(maske.FormularEreignisse).Handler);
    }

    [Fact]
    public void KostenAuswahl_KenntMeldungUndAufrufer()
    {
        var maske = Muster(KostenAuswahl);

        Assert.True(maske.QuelltextGefunden);
        Assert.Equal(1, maske.Meldungen);

        // Der Aufrufer steht als Auszug im Pruefmuster - genau einer, wie im
        // Bestand vor iU8-9 auch.
        var aufrufer = Assert.Single(maske.Aufrufer);
        Assert.StartsWith("Pruefmuster/Kosten/Form_Kosten.Auszug.cs:", aufrufer, StringComparison.Ordinal);
        Fundstelle.Enthaelt(Repowurzel.PruefmusterBezug, aufrufer, "new Form_Kosten_Auswahl");
    }

    [Fact]
    public void KostenAuswahl_NenntZeileUndUmfangDerHandler()
    {
        var maske = Muster(KostenAuswahl);

        Assert.Equal(4, maske.Handler.Count);
        foreach (var handler in new[] { "btnOk_Click", "btn_Abbrechen_Click",
                                        "cmbBrennstoffArt_SelectedIndexChanged", "Form_Kosten_Auswahl_Load" })
        {
            Fundstelle.HandlerStimmt(maske, handler);
        }
    }

    // ---- Form_KostenfaktorItem: 7 Zeilen, 5 Zuordnungen --------------------

    [Fact]
    public void Kostenfaktor_HatSiebenZeilen()
    {
        var abschnitt = Assert.Single(Kartenbau.Abschnitte(Lesen(KostenfaktorItem)));
        Assert.Equal(7, abschnitt.Zeilen.Count);
    }

    [Fact]
    public void Kostenfaktor_OrdnetAlleFuenfLabelZu()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenfaktorItem))[0].Zeilen
            .ToDictionary(z => z.Element.Name, z => z.TextDe, StringComparer.Ordinal);

        Assert.Equal("Kostenfaktor", zeilen["comboBox1"]);
        Assert.Equal("Gruppe", zeilen["comboBox_Gruppe"]);
        Assert.Equal("Nutzungsdauer", zeilen["textBox_Nutzungsdauer"]);
        Assert.Equal("Einheit", zeilen["textBox_Einheit"]);
        Assert.Equal("Wert", zeilen["textBox_Wert"]);
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
    public void Kostenfaktor_KeinVerbrauchtesLabelStehtNochAlsZeile()
    {
        var zeilen = Kartenbau.Abschnitte(Lesen(KostenfaktorItem))[0].Zeilen;
        Assert.DoesNotContain(zeilen, z => z.Element.Art == Art.Beschriftung);
    }

    [Fact]
    public void Kostenfaktor_HatKeineMeldungUndEinenAufrufer()
    {
        var maske = Lesen(KostenfaktorItem);

        Assert.Equal(0, maske.Meldungen);

        var aufrufer = Assert.Single(maske.Aufrufer);
        Assert.StartsWith("WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs:", aufrufer, StringComparison.Ordinal);
        Fundstelle.Enthaelt(aufrufer, "new Form_KostenfaktorItem");
    }
}
