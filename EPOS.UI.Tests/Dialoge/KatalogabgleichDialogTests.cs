using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „Katalog aktualisieren…" (Entscheidungsvorlage Modellgrenzen KU1 Stufe 1): Feldbestand, „Nur
/// prüfen", „Abgleichen" nach Rückfrage, „Wiederherstellen" je behaltenem Satz, Schließen und der Fall
/// ohne Gaben. Die Kultur ist auf de-DE gepinnt (Hausvorrichtung).
/// </summary>
public class KatalogabgleichDialogTests : EposBunitContext
{
    private static readonly KatalogabgleichStand STAND =
        new("—", "3", "/app/Vorlage/Katalogpaket.json", true, new[] { "2026-10-03 · 3 · Abgleich" });

    private static readonly KatalogabgleichZeile NEU =
        new("Tab_Prozesswaerme_STAMM", "Prozesswärmeprofile", "PW:EINSCHICHT_5_TAGE", "Einschicht 5 Tage", "neu", "", false);

    private static readonly KatalogabgleichZeile BEHALTEN =
        new("Tab_Prozesswaerme_STAMM", "Prozesswärmeprofile", "PW:DREISCHICHT_5_TAGE", "Dreischicht 5 Tage", "behalten",
            "Ihre Anpassung bleibt; der neue Auslieferungsstand liegt als Vergleich vor.", true);

    public KatalogabgleichDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KatalogabgleichDialog> Zeige(
        Func<Task<KatalogabgleichAntwort>>? pruefen = null,
        Func<Task<KatalogabgleichAntwort>>? abgleichen = null,
        Func<string, string, Task<KatalogabgleichAntwort>>? wiederherstellen = null,
        Action<bool>? geschlossen = null,
        KatalogabgleichStand? stand = null)
        => Render<KatalogabgleichDialog>(p => p
            .Add(x => x.Stand, stand ?? STAND)
            .Add(x => x.Pruefen, pruefen)
            .Add(x => x.Abgleichen, abgleichen)
            .Add(x => x.Wiederherstellen, wiederherstellen)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static Func<Task<KatalogabgleichAntwort>> Antwort(params KatalogabgleichZeile[] zeilen)
        => () => Task.FromResult(new KatalogabgleichAntwort(true, "", "1 neu, 0 aktualisiert, 1 behalten, 0 ausgelaufen", zeilen));

    [Fact]
    public void Feldbestand_Fassungen_Paketort_Knoepfe_und_Protokoll()
    {
        var cut = Zeige(Antwort(), Antwort());

        Assert.Equal("Katalog aktualisieren", cut.Find("h1").TextContent.Trim());
        Assert.Equal("—", cut.Find(".epos-kabg-fassung-db").TextContent.Trim());
        Assert.Equal("3", cut.Find(".epos-kabg-fassung-paket").TextContent.Trim());
        Assert.Equal("/app/Vorlage/Katalogpaket.json", cut.Find(".epos-kabg-paketort").TextContent.Trim());
        Assert.Equal("Nur prüfen", cut.Find(".epos-kabg-pruefen").TextContent.Trim());
        Assert.Equal("Abgleichen…", cut.Find(".epos-kabg-abgleichen").TextContent.Trim());
        Assert.Equal("Noch nicht geprüft.", cut.Find(".epos-leerzustand").TextContent.Trim());
        Assert.Single(cut.FindAll(".epos-kabg-protokoll li"));
        Assert.Single(cut.FindComponents<Schliesskreuz>());
    }

    [Fact]
    public void Nur_pruefen_zeigt_die_Saetze_und_schreibt_nichts()
    {
        bool? zu = null;
        var cut = Zeige(Antwort(NEU, BEHALTEN), Antwort(),
                        (_, _) => Task.FromResult(new KatalogabgleichAntwort(true, "", "", Array.Empty<KatalogabgleichZeile>())),
                        geschlossen: b => zu = b);

        cut.Find(".epos-kabg-pruefen").Click();

        Assert.Equal(2, cut.FindAll(".epos-kabg-liste tbody tr").Count);
        Assert.Equal("1 neu, 0 aktualisiert, 1 behalten, 0 ausgelaufen", cut.Find(".epos-status").TextContent.Trim());
        Assert.Contains("Nur geprüft", cut.Markup);
        Assert.Single(cut.FindAll(".epos-kabg-wiederherstellen"));     // nur der behaltene Satz
        Assert.False(cut.Instance.Geschrieben);

        cut.Find(".epos-knopf--primaer").Click();                       // Schließen
        Assert.False(zu);
    }

    [Fact]
    public void Abgleichen_fragt_mit_Vorgabe_Nein_und_schreibt_erst_nach_Ja()
    {
        int laeufe = 0;
        bool? zu = null;
        var cut = Zeige(Antwort(), () => { laeufe++; return Antwort(NEU)(); }, geschlossen: b => zu = b);

        cut.Find(".epos-kabg-abgleichen").Click();
        var frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Equal(0, laeufe);

        cut.Find(".epos-kabg-abgleichen").Click();
        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(1, laeufe);
        Assert.True(cut.Instance.Geschrieben);
        Assert.Single(cut.Instance.Zeilen);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.True(zu);
    }

    [Fact]
    public void Wiederherstellen_eines_behaltenen_Satzes_nach_Rueckfrage()
    {
        (string, string)? gerufen = null;
        var cut = Zeige(Antwort(NEU, BEHALTEN), Antwort(),
            (t, s) => { gerufen = (t, s); return Task.FromResult(new KatalogabgleichAntwort(true, "Der Auslieferungsstand wurde wiederhergestellt.", "", Array.Empty<KatalogabgleichZeile>())); });

        cut.Find(".epos-kabg-pruefen").Click();
        cut.Find(".epos-kabg-wiederherstellen").Click();
        var frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.Contains("Dreischicht 5 Tage", frage.Instance.Frage);
        frage.FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(("Tab_Prozesswaerme_STAMM", "PW:DREISCHICHT_5_TAGE"), gerufen);
        Assert.Single(cut.Instance.Zeilen);
        Assert.True(cut.Instance.Geschrieben);
        Assert.Contains("wiederhergestellt", cut.Markup);
    }

    [Fact]
    public void Esc_schliesst_ohne_geschrieben()
    {
        bool? zu = null;
        var cut = Zeige(Antwort(), Antwort(), geschlossen: b => zu = b);
        cut.Find(".epos-katalogabgleich").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zu);
    }

    [Fact]
    public void Ohne_Paket_sind_die_Knoepfe_gesperrt()
    {
        var cut = Zeige(Antwort(), Antwort(), stand: new KatalogabgleichStand("2", "kein Paket", "", false, Array.Empty<string>()));
        Assert.True(cut.Find(".epos-kabg-pruefen").HasAttribute("disabled"));
        Assert.True(cut.Find(".epos-kabg-abgleichen").HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_den_leeren_Stand()
    {
        var cut = Render<KatalogabgleichDialog>();
        Assert.Empty(cut.FindAll(".epos-kabg-pruefen"));
        Assert.Empty(cut.FindAll(".epos-kabg-abgleichen"));
        Assert.Contains("Noch nicht geprüft.", cut.Markup);
    }
}
