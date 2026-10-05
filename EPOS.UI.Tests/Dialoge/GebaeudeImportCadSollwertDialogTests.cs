using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen"</b> im Kopf des Zuordnungsdialogs: nur mit
/// CAD-Raumtemperatur ohne Norm-Sollwert sichtbar, Vorgabe aus, Schlüssel als <c>data-schluessel</c>; das Umschalten ordnet
/// neu zu und ändert das Sollwertfeld und die Zonenwerte — mit synthetischen Gaben und mit der echten Hülle an der Probe
/// <c>ifc4_z6_cad.ifc</c>; die Texte deutsch und englisch.
/// </summary>
public class GebaeudeImportCadSollwertDialogTests : EposBunitContext
{
    public GebaeudeImportCadSollwertDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const string SCHALTER = "[data-schluessel=\"" + GebaeudeImportSchalter.RAUMTEMPERATUR_ALS_SOLLWERT + "\"]";

    private static readonly GebaeudeImportProfilDaten Profil =
        new("Format Alpha", "(*.alpha)|*.alpha", "25 MB", new[] { "Regel eins – eine Zone" }, "Form_Alpha.btn_Help");

    private static readonly IReadOnlyList<string> Klassen = Enumerable.Range(0, 13).Select(i => "Klasse " + (char)('A' + i)).ToList();

    private static GebaeudeImportStand Stand(GebaeudeZuordnungsanfrage a, bool moeglich) => new()
    {
        Kopftext = "Kopf-Probe",
        Vorschlagsname = "Haus",
        CadSollwertMoeglich = moeglich,
        Zeilen = new[]
        {
            new GebaeudeFeldzeileDaten { Zielfeld = "SOLL", Gruppe = "Sollwerte", Feld = "Heizsollwert Tag",
                                         Wert = a.RaumtemperaturAlsSollwert ? 19.3 : 20, Einheit = "°C",
                                         WertText = a.RaumtemperaturAlsSollwert ? "19,3" : "20",
                                         HerkunftSchluessel = a.RaumtemperaturAlsSollwert ? "DATEI" : "VORGABE", HerkunftText = "x",
                                         Haken = true, Eingebbar = true, HakenSetzbar = true },
        },
    };

    private IRenderedComponent<GebaeudeImportDialog> Bauen(List<GebaeudeZuordnungsanfrage> anfragen, List<GebaeudeImportErgebnis> uebernommen,
                                                           bool moeglich, GebaeudeImportTexte? texte = null)
        => Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, Profil);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((_, _, _) =>
                Task.FromResult(new GebaeudeLesestand(true, new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20 KB", "Regel eins"),
                                                      new[] { "Haus 1" }, Array.Empty<GebaeudeImportMeldung>()))));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { anfragen.Add(a); return Stand(a, moeglich); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e => { uebernommen.Add(e); return Task.FromResult<string?>(null); }));
            if (texte is not null) c.Add(x => x.Texte, texte);
        });

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut, string knopf = "Datei wählen")
    {
        cut.FindAll("button").First(k => k.TextContent.Contains(knopf)).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
    }

    private static IElement Eingabe(IRenderedComponent<GebaeudeImportDialog> cut) => cut.Find(SCHALTER + " input");

    [Fact]
    public void Ohne_CAD_Temperatur_fehlt_der_Schalter()
    {
        var anfragen = new List<GebaeudeZuordnungsanfrage>();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(anfragen, new(), moeglich: false);
        Einlesen(cut);
        Assert.Empty(cut.FindAll(SCHALTER));
        Assert.DoesNotContain("Raumtemperatur der Datei als Heizsollwert", cut.Markup);
        Assert.All(anfragen, a => Assert.False(a.RaumtemperaturAlsSollwert));
    }

    [Fact]
    public void Der_Schalter_steht_aus_und_ordnet_beim_Umschalten_neu_zu()
    {
        var anfragen = new List<GebaeudeZuordnungsanfrage>();
        var uebernommen = new List<GebaeudeImportErgebnis>();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(anfragen, uebernommen, moeglich: true);
        Einlesen(cut);

        IElement schalter = cut.Find(SCHALTER);
        Assert.Contains("Raumtemperatur der Datei als Heizsollwert übernehmen", schalter.TextContent);
        Assert.False(Eingabe(cut).HasAttribute("checked"));
        Assert.False(cut.Instance.CadSollwertWirksam);
        Assert.Contains("Vorgabe: Normtemperatur; der Wert bleibt änderbar.", cut.Markup);
        Assert.Equal(20.0, cut.Instance.Zeilen.Single(z => z.Zielfeld == "SOLL").Wert);

        int vorher = anfragen.Count;
        Eingabe(cut).Change(true);
        Assert.Equal(vorher + 1, anfragen.Count);
        Assert.True(anfragen.Last().RaumtemperaturAlsSollwert);
        Assert.True(cut.Instance.CadSollwertWirksam);
        Assert.Equal(19.3, cut.Instance.Zeilen.Single(z => z.Zielfeld == "SOLL").Wert);

        cut.Find(".epos-leiste button.epos-knopf--primaer").Click();
        Assert.True(Assert.Single(uebernommen).RaumtemperaturAlsSollwert);

        Eingabe(cut).Change(false);
        Assert.False(anfragen.Last().RaumtemperaturAlsSollwert);
        Assert.Equal(20.0, cut.Instance.Zeilen.Single(z => z.Zielfeld == "SOLL").Wert);
    }

    [Fact]
    public void Die_Texte_stehen_auch_englisch()
    {
        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Use the room temperature of the file as heating setpoint", englisch.CadSollwert);
        Assert.StartsWith("Default: standard temperature; the value stays editable.", englisch.CadSollwertHinweis);
        Assert.Equal("Heating setpoint day", englisch.SpalteSollwert);
        var deutsch = new GebaeudeImportTexte();
        Assert.Equal("Raumtemperatur der Datei als Heizsollwert übernehmen", deutsch.CadSollwert);
        Assert.Equal("Heizsollwert Tag", deutsch.SpalteSollwert);

        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(new(), new(), moeglich: true, englisch);
        Einlesen(cut, englisch.DateiKnopf.TrimEnd('…', '.'));
        Assert.Contains("Use the room temperature of the file as heating setpoint", cut.Find(SCHALTER).TextContent);
    }

    [Fact]
    public void Mit_der_echten_Huelle_aendert_der_Schalter_Sollwertfeld_und_Zonenwerte()
    {
        string probe = Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_z6_cad.ifc");
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(probe, "ifc4_z6_cad.ifc", new FileInfo(probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
        });
        Einlesen(cut);
        double? Tag() => cut.Instance.Zeilen.Single(z => z.Zielfeld == "SOLL_TAG").Wert;

        Assert.NotEmpty(cut.FindAll(SCHALTER));
        Assert.Equal(20.0, Tag());

        // Die Regel Z6 wählen: die Zonen tragen ohne Schalter keinen eigenen Sollwert.
        IElement wahl = cut.FindAll("select").First(s => s.TextContent.Contains("Z6"));
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z6", StringComparison.Ordinal)).GetAttribute("value"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonensollwert")));
        Assert.All(cut.FindAll(".epos-gebimport-zonensollwert"), z => Assert.DoesNotContain("°C", z.TextContent));

        Eingabe(cut).Change(true);
        Assert.Equal(19.3, Tag());
        Assert.Equal(new[] { "20,1 °C", "15 °C" }, cut.FindAll(".epos-gebimport-zonensollwert").Select(z => z.TextContent.Trim()));
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
