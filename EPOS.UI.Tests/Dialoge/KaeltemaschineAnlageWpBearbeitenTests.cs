using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KM‑K2 — „Bearbeiten…" einer Wärmepumpe im Kältemaschinen-Dialog</b>: Mit der Naht
/// <c>WaermepumpeAnlageGaben</c> öffnet der Knopf die Wärmepumpen-Überlagerung (Anlagenfelder), ihr OK geht über den
/// Schreibweg der Naht und eine Ablehnung bleibt in der Überlagerung stehen; ohne die Naht bleibt die benannte Sperre.
/// Dazu die Verwendungsmarke je Projektkopie und der genannte fehlende Platz der Wärmekaskade.
/// </summary>
public class KaeltemaschineAnlageWpBearbeitenTests : EposBunitContext
{
    public KaeltemaschineAnlageWpBearbeitenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int ID_WP = 501;
    private const int ID_ANLAGE = 77;

    private readonly List<KuehlWaermepumpeZeile> _wp = new()
    {
        new(ID_WP, 40, "WP Bestand", "Neutral", "Luft-Wasser", 1, 1, 38, 3.4, 7, "Strom", false, "zulässig")
    };

    /// <summary>
    /// Der Feldsatz, in den die Anlagenfelder schreiben — die Naht liefert ihn mit den Gaben. <b>Vollständig</b> wie
    /// aus der Naht (<c>WaermepumpeAnlagenaht.AusModell</c> liest sie aus der Anlagenzeile): Heizstableistung und
    /// Sperrzeiten sind Pflichtfelder der Anlagenfelder — fehlen sie, lehnt deren <c>Pruefen()</c> schon vor dem
    /// Schreibweg ab, genau wie im Wärmepumpendialog.
    /// </summary>
    private readonly WaermepumpeAnlageDaten _felder = new()
    {
        Bezeichner = "WP Bestand", IdWp = ID_WP, Vorlauf = 55, Ruecklauf = 45,
        Heizstab = false, HeizstabLeistung = 6, Sperrung = false, SperrzeitVon = 0, SperrzeitBis = 0
    };

    private int _geschrieben;
    private string? _ablehnungSchreiben;

    /// <summary>Die Ablehnung der ANLAGENFELDER (Temperaturprüfung der Gaben); <c>null</c> = die Felder stehen.</summary>
    private string? _ablehnungFelder;
    private string? _hinweis;
    private bool _nahtLiefert = true;

    private static Katalogfilterzeile WpZeile(int id, string name)
        => new Katalogfilterzeile(id, name) { Schluessel = KuehlerzeugerKatalogCtrl.WpSchluessel(id) }
               .MitText(KuehlerzeugerKatalogCtrl.SpArt, R.KMA_ART_WAERMEPUMPE)
               .MitText(Katalogfilterprofil.SpBezeichner, name);

    private WaermepumpeAnlagegaben? Naht(int idWp)
    {
        if (!_nahtLiefert || idWp != ID_WP) return null;
        var gaben = new Dictionary<string, object>
        {
            ["Daten"] = _felder,
            ["TemperaturenPruefen"] = new Func<int?, int?, string?>((_, _) => _ablehnungFelder)
        };
        return new WaermepumpeAnlagegaben(ID_ANLAGE, "WP Bestand", gaben, () =>
        {
            _geschrieben++;
            return _ablehnungSchreiben;
        });
    }

    private IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(bool mitNaht = true, Func<IReadOnlyList<string>>? kopien = null)
    {
        _nahtLiefert = mitNaht;
        return Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, Array.Empty<KaeltemaschineAnlageDaten>)
            .Add(x => x.Stromtraeger, new[] { (1, "Strom") })
            .Add(x => x.Katalogzeilen, () => (IReadOnlyList<Katalogfilterzeile>)new[] { WpZeile(40, "WP Bestand"), WpZeile(41, "WP Frei") })
            .Add(x => x.Katalogprofil, KuehlerzeugerKatalogCtrl.Profil(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.KuehlWaermepumpen, () => _wp.ToList())
            .Add(x => x.WaermepumpeUebernehmen,
                 stamm => new KuehlWpUebernahme(true, ID_WP, "WP Bestand", true, null, null, _hinweis))
            .Add(x => x.WpKuehlbetriebSetzen, (_, _) => null)
            .Add(x => x.WaermepumpeKopien, kopien)
            .Add(x => x.WaermepumpeAnlageGaben, mitNaht ? Naht : (Func<int, WaermepumpeAnlagegaben?>?)null));
    }

    private static void Projektzeile(IRenderedComponent<KaeltemaschineAnlageDialog> cut, int i)
        => cut.FindAll(".epos-zweispalten-bereich--projekt tbody tr")[i].QuerySelector(".epos-zeilenzelle")!.Click();

    private static void Bearbeiten(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-wp").Click();

    private static string Banner(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => string.Join(" ", cut.FindAll(".epos-warnbanner-text").Select(e => e.TextContent));

    private static void Ueberlagerungsknopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string text)
        => cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == text).Click();

    [Fact]
    public void Mit_der_Naht_oeffnet_Bearbeiten_die_Waermepumpen_Ueberlagerung()
    {
        var cut = Aufbauen();
        Projektzeile(cut, 0);
        Bearbeiten(cut);
        Assert.True(cut.Instance.WpUeberlagerungOffen);
        Assert.Equal(ID_ANLAGE, cut.Instance.WpUeberlagerungAnlage);
        Assert.Contains(R.KMA_WP_TITEL_BEARBEITEN, cut.Markup);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung .epos-wp-anlagefelder"));

        // Abbrechen schließt ohne zu schreiben.
        Ueberlagerungsknopf(cut, new KaeltemaschineAnlageTexte().Abbrechen);
        Assert.False(cut.Instance.WpUeberlagerungOffen);
        Assert.Equal(0, _geschrieben);
    }

    [Fact]
    public void Ohne_die_Naht_bleibt_Bearbeiten_benannt_gesperrt()
    {
        var cut = Aufbauen(mitNaht: false);
        Projektzeile(cut, 0);
        var knopf = cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-wp");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.KMA_WP_BEARBEITEN_GESPERRT, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.WpUeberlagerungOffen);
        Assert.Contains(R.KMA_WP_BEARBEITEN_GESPERRT, Banner(cut));
    }

    [Fact]
    public void OK_schreibt_ueber_die_Naht_und_eine_Ablehnung_bleibt_in_der_Ueberlagerung_stehen()
    {
        var cut = Aufbauen();
        Projektzeile(cut, 0);
        Bearbeiten(cut);

        // Lehnen schon die FELDER ab, bleibt die Überlagerung mit dem Wortlaut stehen — und der Schreibweg
        // der Naht wird gar nicht erst gegangen (keine stille Ablehnung).
        _ablehnungFelder = "Rücklauf über Vorlauf";
        Ueberlagerungsknopf(cut, new KaeltemaschineAnlageTexte().Ok);
        Assert.True(cut.Instance.WpUeberlagerungOffen);
        Assert.Equal(0, _geschrieben);
        Assert.Contains("Rücklauf über Vorlauf", cut.Markup);

        _ablehnungFelder = null;
        _ablehnungSchreiben = "Vorlauf zu hoch";
        Ueberlagerungsknopf(cut, new KaeltemaschineAnlageTexte().Ok);
        Assert.True(cut.Instance.WpUeberlagerungOffen);
        Assert.Equal(1, _geschrieben);
        Assert.Contains("Vorlauf zu hoch", cut.Markup);

        _ablehnungSchreiben = null;
        Ueberlagerungsknopf(cut, new KaeltemaschineAnlageTexte().Ok);
        Assert.False(cut.Instance.WpUeberlagerungOffen);
        Assert.Equal(2, _geschrieben);
    }

    [Fact]
    public void Die_Verwendungsmarke_steht_je_Projektkopie()
    {
        // Ohne die Gabe zählt nur die Projektliste: „WP Frei" ist nicht verwendet.
        var ohne = Aufbauen();
        ohne.FindAll(".epos-katalogliste tbody tr").First(tr => tr.TextContent.Contains("WP Frei"))
            .QuerySelector(".epos-zeilenzelle")!.Click();
        Assert.False(ohne.Instance.Katalogzeile!.ImProjekt);

        // Mit der Gabe zählt jede Projektkopie — auch eine ohne eingeschalteten Kühlbetrieb.
        var mit = Aufbauen(kopien: () => new[] { "WP Bestand", "WP Frei" });
        mit.FindAll(".epos-katalogliste tbody tr").First(tr => tr.TextContent.Contains("WP Frei"))
           .QuerySelector(".epos-zeilenzelle")!.Click();
        Assert.True(mit.Instance.Katalogzeile!.ImProjekt);
    }

    [Fact]
    public void Der_fehlende_Platz_der_Waermekaskade_wird_genannt()
    {
        _hinweis = R.KMA_MSG_WP_OHNE_KASKADE;
        var cut = Aufbauen();
        cut.FindAll(".epos-katalogliste tbody tr").First(tr => tr.TextContent.Contains("WP Bestand"))
           .QuerySelector(".epos-zeilenzelle")!.Click();
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Contains(R.KMA_MSG_WP_OHNE_KASKADE, Banner(cut));
    }
}
