using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Baustein <c>WaermepumpeStammFelder</c> — das Feldraster eines Wärmepumpen-
/// STAMMSATZES, am 16.09.2026 aus <c>WaermepumpeStammDialog</c> herausgelöst.
///
/// <para>Er steht seither an ZWEI Stellen: in der Stammdatenpflege und — neu — im
/// Kenndatenblock der Detailansicht, wo der Anwender die Parameter des Stammgeräts
/// unmittelbar bearbeitet, statt den Umweg „Parameter Bearbeiten…" zu nehmen.</para>
/// </summary>
public class WaermepumpeStammFelderTests : EposBunitContext
{
    public WaermepumpeStammFelderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static WaermepumpeStammDaten Satz() => new()
    {
        Id = 1,
        Name = "WP Alpha",
        Firma = "Alpha",
        Beschreibung = "Testgerät",
        Typ = "Luft-Wasser",
        Baujahr = 2023,
        Aufstellung = "Innenaufstellung",
        Nennleistung = 12,
        Heizstab = 6,
        Regelung = "stetig",
        Kuehlleistung = 8.5,
        Modulkosten = 4000
    };

    private IRenderedComponent<WaermepumpeStammFelder> Aufbauen(
        WaermepumpeStammDaten? daten = null, Action? geaendert = null, bool aktiv = true,
        bool bezeichnerAenderbar = true)
        => Render<WaermepumpeStammFelder>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.BezeichnerAenderbar, bezeichnerAenderbar)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    /// <summary>
    /// Derselbe Feldbestand, den die Stammdatenpflege zeigte: fünf Eingabefelder, ein
    /// mehrzeiliges Feld und vier Klapplisten.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        // Name, Hersteller (Text), Nennleistung, Heizstab (Ganzzahl), Kuehlleistung (Zahl).
        Assert.Equal(5, cut.FindAll("input").Count);
        Assert.Single(cut.FindAll("textarea"));
        // Vier Klapplisten: Typ, Leistungsstufen, Aufstellung, Baujahr.
        Assert.Equal(4, cut.FindAll("select").Count);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (string soll in new[]
                 {
                     "Name", "Hersteller", "Beschreibung", "Wärmepumpentyp", "Leistungsstufen",
                     "Aufstellung", "Baujahr", "Nennleistung", "Heizstab", "Kühlleistung",
                     "Modulkosten"
                 })
            Assert.Contains(soll, texte);
    }

    /// <summary>
    /// <b>Zwei Werte stehen NUR LESEND da.</b> Die Kühlleistung kommt aus den
    /// Kühl-Kenndaten; die Modulkosten sind seit W14a‑O‑1 ein Lesewert mit
    /// Herleitungszeile — gepflegt werden Gerätekosten in der Kostenverwaltung (Ä19).
    /// </summary>
    [Fact]
    public void Kuehlleistung_und_Modulkosten_sind_nur_lesend()
    {
        var cut = Aufbauen();

        // Die Kuehlleistung ist das EINZIGE gesperrte Eingabefeld.
        Assert.Single(cut.FindAll("input[disabled]"));

        // Die Modulkosten sind gar kein Feld, sondern ein Lesewert samt Einheit. Die
        // Einheit steht in DERSELBEN Zeile wie der Lesewert - die erste Einheit der
        // Maske waere sonst das "kW" der Nennleistung.
        IElement lesewert = cut.Find(".epos-lesewert");
        Assert.Equal("4000", lesewert.TextContent.Trim());
        Assert.Contains("€", lesewert.ParentElement!.QuerySelector(".epos-einheit")!.TextContent);
        Assert.Contains(cut.FindAll(".epos-herleitung").Select(e => e.TextContent),
                        t => t.Contains("Kostenverwaltung"));
    }

    /// <summary>Ohne Planwert: der Halbgeviertstrich und die zweite, leise Zeile.</summary>
    [Fact]
    public void Ohne_Planwert_stehen_Strich_und_Hinweis()
    {
        var daten = Satz();
        daten.Modulkosten = 0;
        var cut = Aufbauen(daten);

        Assert.Equal("–", cut.Find(".epos-lesewert").TextContent.Trim());
        Assert.Contains(cut.FindAll(".epos-herleitung").Select(e => e.TextContent),
                        t => t.Contains("kein Planwert im Datenbestand"));
    }

    [Fact]
    public void Jede_Eingabe_schreibt_in_den_Satz_und_meldet()
    {
        int gemeldet = 0;
        var daten = Satz();
        var cut = Aufbauen(daten, geaendert: () => gemeldet++);

        cut.FindAll("input[type=text]")[0].Input("WP Alpha neu");
        Assert.Equal("WP Alpha neu", daten.Name);

        cut.FindAll("textarea")[0].Input("Anderer Text");
        Assert.Equal("Anderer Text", daten.Beschreibung);

        cut.FindAll("select")[0].Change("1");                 // Wasser-Wasser
        Assert.Equal("Wasser-Wasser", daten.Typ);

        Assert.Equal(3, gemeldet);
    }

    /// <summary>
    /// A-16: Ein Bestandswert außerhalb der festen Liste wird VORANGESTELLT — ein
    /// <c>select</c> würde ihn sonst still verwerfen.
    /// </summary>
    [Fact]
    public void Ein_Bestandswert_ausserhalb_der_Liste_bleibt_stehen()
    {
        var daten = Satz();
        daten.Typ = "Erdreich-Wasser";
        var cut = Aufbauen(daten);

        var eintraege = cut.FindAll("select")[0].QuerySelectorAll("option")
                           .Select(o => o.TextContent).ToList();
        Assert.Equal("Erdreich-Wasser", eintraege[0]);
    }

    /// <summary>Ohne Gaben zeichnet er — leerer Satz, Texte aus dem Bündel.</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Baustein()
    {
        var cut = Render<WaermepumpeStammFelder>();

        Assert.NotEmpty(cut.FindAll(".epos-formularraster"));
        Assert.Contains("Name", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Equal("–", cut.Find(".epos-lesewert").TextContent.Trim());
    }

    /// <summary>
    /// <b><c>BezeichnerAenderbar="false"</c> stellt NUR das Namensfeld nur lesend</b>
    /// (Anwenderentscheid 16.09.2026) — der Weg des Anlagendialogs: Dort bearbeitet der
    /// Baustein die Projektkopie, und deren einzige Klammer zum Katalogsatz ist der Name.
    /// Alle übrigen Felder bleiben bedienbar.
    /// </summary>
    [Fact]
    public void Ohne_BezeichnerAenderbar_ist_nur_das_Namensfeld_nur_lesend()
    {
        var daten = Satz();
        var cut = Aufbauen(daten, bezeichnerAenderbar: false);

        IElement name = cut.FindAll("input[type=text]")[0];
        Assert.True(name.HasAttribute("readonly"));

        // Hersteller, die vier Klapplisten und die beiden Ganzzahlfelder bleiben offen.
        Assert.False(cut.FindAll("input[type=text]")[1].HasAttribute("readonly"));
        Assert.False(cut.Find("textarea").HasAttribute("readonly"));
        Assert.All(cut.FindAll("select"), s => Assert.False(s.HasAttribute("disabled")));

        // Und der Baustein nimmt die Eingabe im Namensfeld nicht entgegen — er zeichnet
        // ihn nur lesend; der Satz bleibt, wie er war.
        Assert.Equal("WP Alpha", daten.Name);
    }

    /// <summary>Vorgabe ist AENDERBAR — der Weg der Katalogverwaltung.</summary>
    [Fact]
    public void Der_Bezeichner_ist_vorgabegemaess_aenderbar()
    {
        var cut = Aufbauen();
        Assert.False(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    /// <summary>Nur ansehen: Jedes Feld ist gesperrt bzw. schreibgeschützt.</summary>
    [Fact]
    public void Ohne_Aktiv_ist_jedes_Feld_gesperrt()
    {
        var cut = Aufbauen(aktiv: false);

        Assert.All(cut.FindAll("select"), s => Assert.True(s.HasAttribute("disabled")));
        Assert.All(cut.FindAll("input"),
                   f => Assert.True(f.HasAttribute("disabled") || f.HasAttribute("readonly")));
        Assert.True(cut.Find("textarea").HasAttribute("readonly"));
    }
}
