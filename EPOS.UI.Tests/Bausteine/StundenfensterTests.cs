using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Stundenfenster</b> (Stufe KP2, Welle U1; Teilkonzept Konditionierungsprofile 3.3, 7.2) — das
/// Nachtfenster einer Spalte der Vorgabe-Matrix als EIN Feld „22–6".
///
/// <para><b>Was geprüft wird:</b> Lesen und Anzeigen (Trenner, „h"/„Uhr", über Mitternacht); was kein
/// Fenster ist (Stunde außerhalb 0 … 23, Kommazahl, eine Stunde, gleiche Stunden, fremde Ziffern) färbt
/// und meldet nichts; leer meldet (null, null); die Anzeige folgt dem Wert, eine Fehleingabe bleibt
/// stehen; der Fehlerzustand geht mit dem Feldnamen hinaus; der Fall ohne Gaben.</para>
/// </summary>
public class StundenfensterTests : EposBunitContext
{
    private static IElement Eingabe(IRenderedComponent<Stundenfenster> cut) => cut.Find("input");

    [Theory]
    [InlineData("22-6", 22, 6)]
    [InlineData("22–6", 22, 6)]
    [InlineData("22 — 6", 22, 6)]
    [InlineData("22/6", 22, 6)]
    [InlineData("22 6", 22, 6)]
    [InlineData(" 22h - 6h ", 22, 6)]
    [InlineData("22 Uhr - 6 Uhr", 22, 6)]
    [InlineData("0-23", 0, 23)]
    [InlineData("08-17", 8, 17)]
    public void Zwei_Stunden_ergeben_ein_Fenster(string text, int von, int bis)
    {
        Assert.True(Stundenfenster.Lesen(text, out int v, out int b), text);
        Assert.Equal(von, v);
        Assert.Equal(bis, b);
    }

    [Theory]
    [InlineData("24-6")]
    [InlineData("-1-6")]
    [InlineData("22,5-6")]
    [InlineData("22")]
    [InlineData("5-5")]
    [InlineData("22-6-7")]
    [InlineData("abc")]
    [InlineData("٢٢-٦")]   // keine fremden Ziffern
    [InlineData("")]
    public void Was_kein_Fenster_ist_wird_nicht_gelesen(string text)
        => Assert.False(Stundenfenster.Lesen(text, out _, out _), text);

    [Fact]
    public void Die_Anzeige_hat_den_Halbgeviertstrich_und_ist_ohne_Stunde_leer()
    {
        Assert.Equal("22–6", Stundenfenster.Anzeigetext(22, 6));
        Assert.Equal("0–23", Stundenfenster.Anzeigetext(0, 23));
        Assert.Equal("", Stundenfenster.Anzeigetext(22, null));
        Assert.Equal("", Stundenfenster.Anzeigetext(null, null));
    }

    [Fact]
    public void Eine_Eingabe_meldet_beide_Stunden_zusammen()
    {
        (int? Von, int? Bis)? gemeldet = null;
        var cut = Render<Stundenfenster>(p => p
            .Add(x => x.Bezeichnung, "Heizen · Nachtfenster")
            .Add(x => x.Platzhalter, "22–6")
            .Add(x => x.FensterChanged, f => gemeldet = f));

        Assert.Equal("numeric", Eingabe(cut).GetAttribute("inputmode"));
        Assert.Equal("22–6", Eingabe(cut).GetAttribute("placeholder"));
        Assert.Equal("Heizen · Nachtfenster", cut.Find(".epos-feld-text").TextContent);

        Eingabe(cut).Input("23-5");
        Assert.Equal((23, 5), gemeldet!.Value);
        Assert.False(cut.Instance.Fehlerhaft);

        Eingabe(cut).Input("");
        Assert.Equal(((int?)null, (int?)null), gemeldet!.Value);
    }

    [Fact]
    public void Eine_Fehleingabe_faerbt_meldet_nichts_und_bleibt_stehen()
    {
        int meldungen = 0;
        var zustaende = new List<(string, bool)>();
        var cut = Render<Stundenfenster>(p => p
            .Add(x => x.Von, 22)
            .Add(x => x.Bis, 6)
            .Add(x => x.Feldname, "Heizen · Nachtfenster")
            .Add(x => x.FehlerZustand, z => zustaende.Add(z))
            .Add(x => x.FensterChanged, _ => meldungen++));

        Assert.Equal("22–6", Eingabe(cut).GetAttribute("value"));

        Eingabe(cut).Input("24-6");
        Assert.Contains("epos-fehleingabe", Eingabe(cut).ClassName ?? "");
        Assert.Equal("true", Eingabe(cut).GetAttribute("aria-invalid"));
        Assert.Equal(0, meldungen);
        Assert.Equal(new[] { ("Heizen · Nachtfenster", true) }, zustaende);

        // Ein neuer Wert von außen überschreibt die laufende Fehleingabe nicht.
        cut.Render(p => p.Add(x => x.Von, 21).Add(x => x.Bis, 7));
        Assert.Equal("24-6", Eingabe(cut).GetAttribute("value"));

        Eingabe(cut).Input("21-7");
        Assert.DoesNotContain("epos-fehleingabe", Eingabe(cut).ClassName ?? "");
        Assert.Equal(("Heizen · Nachtfenster", false), zustaende[^1]);
        Assert.Equal(1, meldungen);
    }

    [Fact]
    public void Die_Anzeige_folgt_dem_Wert_ein_Text_der_ihn_meint_bleibt()
    {
        var cut = Render<Stundenfenster>(p => p.Add(x => x.Von, 22).Add(x => x.Bis, 6));
        Eingabe(cut).Input("22 - 6");
        cut.Render(p => p.Add(x => x.Von, 22).Add(x => x.Bis, 6));
        Assert.Equal("22 - 6", Eingabe(cut).GetAttribute("value"));

        cut.Render(p => p.Add(x => x.Von, 20).Add(x => x.Bis, 5));
        Assert.Equal("20–5", Eingabe(cut).GetAttribute("value"));
    }

    [Fact]
    public void Gesperrt_und_ohne_Gaben()
    {
        var cut = Render<Stundenfenster>(p => p.Add(x => x.Aktiv, false));
        Assert.True(Eingabe(cut).HasAttribute("disabled"));

        var leer = Render<Stundenfenster>();
        Assert.Equal("", Eingabe(leer).GetAttribute("value") ?? "");
        Assert.Empty(leer.FindAll(".epos-feld-text"));
        Assert.Null(Eingabe(leer).GetAttribute("placeholder"));
    }
}
