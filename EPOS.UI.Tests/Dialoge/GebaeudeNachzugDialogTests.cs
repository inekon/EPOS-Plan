using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// GD-N am Gebäudedialog: Der Sprung „Kühlung“ wählt das zuletzt gewählte Projektgebäude (über das Öffnen hinaus gemerkt),
/// die Zusammenfassung zeigt den Jahreswärmebedarf des letzten Laufs mit Datum bzw. „noch nicht gerechnet“; der
/// Maskenschlüssel der Projektgebäude ist ein Seitenschlüssel.
/// </summary>
public class GebaeudeNachzugDialogTests : EposBunitContext
{
    public GebaeudeNachzugDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static List<GebaeudeProjektZeile> Zwei() => new()
    {
        new GebaeudeProjektZeile { IdZ = 1, IdGebaeude = 11, Name = "Haus A", HatProjektkopie = true },
        new GebaeudeProjektZeile { IdZ = 2, IdGebaeude = 12, Name = "Haus B", HatProjektkopie = true },
    };

    [Fact]
    public void Sprung_Kuehlung_waehlt_das_zuletzt_gewaehlte_Gebaeude_und_merkt_die_Wahl()
    {
        var gemerkt = new List<int>();
        GebaeudeProjektZeile? geoeffnet = null;
        Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, Zwei())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ZuletztGewaehlt, () => 2)
            .Add(x => x.GewaehltMerken, id => gemerkt.Add(id))
            .Add(x => x.ProjektGaben, z => { geoeffnet = z; return null!; })
            .Add(x => x.Sprungziel, Sprungziele.Kuehlung));
        Assert.Equal("Haus B", geoeffnet?.Name);
        Assert.Equal(new[] { 2 }, gemerkt);

        // Ein gemerktes Gebäude, das es nicht mehr gibt: das erste.
        geoeffnet = null;
        Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, Zwei())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ZuletztGewaehlt, () => 99)
            .Add(x => x.ProjektGaben, z => { geoeffnet = z; return null!; })
            .Add(x => x.Sprungziel, Sprungziele.Kuehlung));
        Assert.Equal("Haus A", geoeffnet?.Name);
    }

    private IRenderedComponent<GebaeudeDialog> Gewaehlt(Func<GebaeudeProjektZeile, GebaeudeJahreswaermebedarf?>? jwb)
        => Render<GebaeudeDialog>(p =>
        {
            p.Add(x => x.Zeilen, Zwei())
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.ZuletztGewaehlt, () => 1)
             .Add(x => x.Sprungziel, Sprungziele.Kuehlung);
            if (jwb is not null) p.Add(x => x.Jahreswaermebedarf, jwb);
        });

    private static string? Zeile(IRenderedComponent<GebaeudeDialog> cut)
        => cut.Instance.Zusammenfassung().FirstOrDefault(a => a.Beschriftung == "Jahreswärmebedarf")?.Wert;

    [Fact]
    public void Die_Zusammenfassung_zeigt_den_Jahreswaermebedarf_mit_Datum_oder_noch_nicht_gerechnet()
    {
        var cut = Gewaehlt(z => z.IdGebaeude == 11 ? new GebaeudeJahreswaermebedarf(12.34, new DateTime(2026, 8, 30, 6, 11, 59), false) : null);
        Assert.Equal("12,3 MWh/a (Lauf vom 30.08.2026)", Zeile(cut));

        var projekt = Gewaehlt(_ => new GebaeudeJahreswaermebedarf(389.73, new DateTime(2026, 8, 30), true));
        Assert.StartsWith("389,7 MWh/a, Projekt gesamt (Lauf vom 30.08.2026", Zeile(projekt));

        Assert.Equal("noch nicht gerechnet", Zeile(Gewaehlt(_ => null)));
        Assert.Null(Zeile(Gewaehlt(null)));
    }

    [Fact]
    public void Der_Maskenschluessel_der_Projektgebaeude_ist_ein_Seitenschluessel_und_die_Wurzel_fuehrt_ihn()
    {
        Assert.Equal(Masken.Projektgebaeude, EPOS.UI.Seiten.Seitenschluessel.Projektgebaeude);
        Assert.Contains(Masken.Projektgebaeude, EPOS.UI.Seiten.Seitenschluessel.Alle);
        Assert.True(EPOS.UI.Seiten.AppWurzel.FuehrtZiel(Masken.Projektgebaeude));
    }
}
