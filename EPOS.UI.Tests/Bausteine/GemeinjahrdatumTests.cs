using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Datum im Gemeinjahr</b> (Stufe KP2, Welle U0b; Teilkonzept Konditionierungsprofile 3.2,
/// Befund B13, Entwurf KP2 Festlegung 12) — „TT.MM." ↔ Jahrestag 1 … 365 über
/// <c>Feiertage.Gemeinjahrestag</c> und <c>Feiertage.Datum</c>.
///
/// <para><b>Was geprüft wird:</b> Lesen und Anzeigen samt Rundlauf über alle 365 Tage und gegen
/// den Kern; der 29.02. und unmögliche Tage sind eine Fehleingabe mit dem Text aus dem Bündel, die
/// nichts meldet; ein leeres Feld meldet <c>null</c>; die Anzeige folgt dem Wert, eine Fehleingabe
/// bleibt stehen; gesperrt; der Fall ohne Gaben.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (der deutsche Rückfalltext der Meldung).</para>
/// </summary>
public class GemeinjahrdatumTests : EposBunitContext
{
    private static IElement Eingabe(IRenderedComponent<Gemeinjahrdatum> cut) => cut.Find("input");

    // =================================================================================
    // Lesen und Anzeigen
    // =================================================================================

    [Theory]
    [InlineData("01.01.", 1)]
    [InlineData("1.1", 1)]
    [InlineData("28.02.", 59)]
    [InlineData("01.03.", 60)]
    [InlineData("1,10", 274)]
    [InlineData(" 30.4. ", 120)]
    [InlineData("24.12.", 358)]
    [InlineData("31.12", 365)]
    public void Tag_und_Monat_ergeben_den_Jahrestag_im_Gemeinjahr(string text, int jahrestag)
    {
        Assert.True(Gemeinjahrdatum.Lesen(text, out int t), text);
        Assert.Equal(jahrestag, t);
    }

    [Theory]
    [InlineData("29.02.")]
    [InlineData("29.2")]
    [InlineData("31.04.")]
    [InlineData("00.05.")]
    [InlineData("12.13.")]
    [InlineData("32.01.")]
    [InlineData("1.2.3")]
    [InlineData("24/12")]
    [InlineData("abc")]
    [InlineData("24")]
    [InlineData("١.١")]   // keine fremden Ziffern
    public void Unmoegliche_Tage_und_fremde_Formen_sind_kein_Datum(string text)
        => Assert.False(Gemeinjahrdatum.Lesen(text, out _), text);

    [Fact]
    public void Anzeige_und_Lesen_laufen_ueber_alle_365_Tage_rund_und_folgen_dem_Kern()
    {
        for (int t = 1; t <= 365; t++)
        {
            string text = Gemeinjahrdatum.Anzeigetext(t);
            Assert.Matches(@"^[0-9]{2}\.[0-9]{2}\.$", text);
            Assert.True(Gemeinjahrdatum.Lesen(text, out int zurueck), text);
            Assert.Equal(t, zurueck);

            Assert.True(Feiertage.Datum(t, out int monat, out int tag));
            Assert.Equal(t, Feiertage.Gemeinjahrestag(monat, tag));
            Assert.Equal($"{tag:00}.{monat:00}.", text);
        }

        Assert.Equal("", Gemeinjahrdatum.Anzeigetext(null));
        Assert.Equal("", Gemeinjahrdatum.Anzeigetext(0));
        Assert.Equal("", Gemeinjahrdatum.Anzeigetext(366));
    }

    [Fact]
    public void Das_Feld_zeigt_den_Wert_als_Tag_und_Monat()
    {
        var cut = Render<Gemeinjahrdatum>(p => p.Add(x => x.Wert, 274));

        Assert.Equal("01.10.", Eingabe(cut).GetAttribute("value"));

        cut.Render(p => p.Add(x => x.Wert, 120));
        Assert.Equal("30.04.", Eingabe(cut).GetAttribute("value"));
    }

    // =================================================================================
    // Eingabe
    // =================================================================================

    [Fact]
    public void Eine_gueltige_Eingabe_meldet_den_Jahrestag()
    {
        int? gemeldet = null;
        var cut = Render<Gemeinjahrdatum>(p => p.Add(x => x.WertChanged, (int? w) => gemeldet = w));

        Eingabe(cut).Input("24.12.");

        Assert.Equal(358, gemeldet);
        Assert.False(cut.Instance.Fehlerhaft);
        Assert.Empty(cut.FindAll(".epos-gemeinjahrdatum-meldung"));
    }

    [Fact]
    public void Der_29_Februar_ist_eine_Fehleingabe_mit_dem_Text_aus_dem_Buendel()
    {
        int meldungen = 0;
        var fehler = new List<(string Feld, bool Fehlerhaft)>();
        var cut = Render<Gemeinjahrdatum>(p => p
            .Add(x => x.Wert, 60)
            .Add(x => x.Feldname, "Start der Saison")
            .Add(x => x.WertChanged, (int? _) => meldungen++)
            .Add(x => x.FehlerZustand, ((string Feld, bool Fehlerhaft) e) => fehler.Add(e)));

        Eingabe(cut).Input("29.02.");

        Assert.Equal(0, meldungen);
        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Contains("epos-fehleingabe", Eingabe(cut).ClassName);
        Assert.Equal("true", Eingabe(cut).GetAttribute("aria-invalid"));
        Assert.Equal("Ungültiges Datum: TT.MM. im Gemeinjahr; den 29.02. gibt es nicht.",
                     cut.Find(".epos-gemeinjahrdatum-meldung").TextContent.Trim());
        Assert.Equal(new[] { ("Start der Saison", true) }, fehler);

        // Die Fehleingabe bleibt stehen - der Anwender sieht, was er tippte.
        Assert.Equal("29.02.", Eingabe(cut).GetAttribute("value"));

        // Ein gültiger Tag hebt sie auf.
        Eingabe(cut).Input("01.03.");
        Assert.Equal(1, meldungen);
        Assert.False(cut.Instance.Fehlerhaft);
        Assert.Empty(cut.FindAll(".epos-gemeinjahrdatum-meldung"));
        Assert.Equal(("Start der Saison", false), fehler[^1]);
    }

    [Fact]
    public void Die_Meldung_ist_der_Text_des_Wirts()
    {
        var cut = Render<Gemeinjahrdatum>(p => p
            .Add(x => x.Meldung, "Invalid date: DD.MM. in a common year; there is no 29.02."));

        Eingabe(cut).Input("31.04.");

        Assert.Equal("Invalid date: DD.MM. in a common year; there is no 29.02.",
                     cut.Find(".epos-gemeinjahrdatum-meldung").TextContent.Trim());
    }

    [Fact]
    public void Ein_geleertes_Feld_meldet_null_und_ist_neutral()
    {
        int? gemeldet = 5;
        bool aufgerufen = false;
        var cut = Render<Gemeinjahrdatum>(p => p
            .Add(x => x.Wert, 5)
            .Add(x => x.Platzhalter, "ganzjährig")
            .Add(x => x.WertChanged, (int? w) => { gemeldet = w; aufgerufen = true; }));

        Eingabe(cut).Input("");

        Assert.True(aufgerufen);
        Assert.Null(gemeldet);
        Assert.False(cut.Instance.Fehlerhaft);
        Assert.Equal("ganzjährig", Eingabe(cut).GetAttribute("placeholder"));
    }

    [Fact]
    public void Gesperrt_zeigt_es_den_Wert_ohne_Eingabe()
    {
        var cut = Render<Gemeinjahrdatum>(p => p
            .Add(x => x.Wert, 1)
            .Add(x => x.Aktiv, false)
            .Add(x => x.Bezeichnung, "Start der Saison"));

        Assert.True(Eingabe(cut).HasAttribute("disabled"));
        Assert.Equal("01.01.", Eingabe(cut).GetAttribute("value"));
        Assert.Equal("Start der Saison", cut.Find(".epos-feld-text").TextContent);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_es_ein_leeres_Feld()
    {
        var cut = Render<Gemeinjahrdatum>();

        Assert.Equal("", Eingabe(cut).GetAttribute("value") ?? "");
        Assert.False(Eingabe(cut).HasAttribute("placeholder"));
        Assert.False(Eingabe(cut).HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-feld-text"));
        Assert.Empty(cut.FindAll(".epos-gemeinjahrdatum-meldung"));
        Assert.Contains("epos-gemeinjahrdatum", cut.Find("label").ClassName);
    }
}
