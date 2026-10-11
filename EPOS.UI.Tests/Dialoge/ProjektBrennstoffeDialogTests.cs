using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „Brennstoffe des Projekts…" (Anwenderentscheid 03.10.2026: Projektkopie des Brennstoffkatalogs):
/// Feldbestand, Kennzeichnung der Abweichung vom Katalog, Bearbeiten in der Überlagerung (nur die
/// geänderten Felder gehen an die Hülle), Zurücksetzen nach Rückfrage mit Vorgabe „Nein", Übernehmen
/// aus dem Katalog, Schließen und Esc, ohne Projekt und ohne Gaben. Die Kultur ist auf de-DE gepinnt
/// (Hausvorrichtung).
/// </summary>
public class ProjektBrennstoffeDialogTests : EposBunitContext
{
    private static Dictionary<string, double?> Werte(double hi, double co2) => new()
    {
        [ProjektBrennstoffFelder.Hi] = hi,
        [ProjektBrennstoffFelder.Hs] = hi * 1.1,
        [ProjektBrennstoffFelder.Co2] = co2,
        [ProjektBrennstoffFelder.So2] = 0,
        [ProjektBrennstoffFelder.Nox] = 0,
        [ProjektBrennstoffFelder.Staub] = null,
        [ProjektBrennstoffFelder.Pe] = 1.1,
        [ProjektBrennstoffFelder.Grundpreis] = 0,
        [ProjektBrennstoffFelder.Arbeitspreis] = 0,
        [ProjektBrennstoffFelder.Leistungspreis] = 0,
    };

    private static readonly ProjektBrennstoffZeile ERDGAS =
        new(3, "Erdgas E", "kWh", Werte(10.5, 240), Werte(10.5, 240), true, Array.Empty<string>());

    private static readonly ProjektBrennstoffZeile HEIZOEL =
        new(9, "Heizöl EL", "L", Werte(10, 300), Werte(10, 310), true, new[] { ProjektBrennstoffFelder.Co2 });

    private static readonly ProjektBrennstoffZeile ALT =
        new(30, "Altbrennstoff", "kg", Werte(5, 100), new Dictionary<string, double?>(), false, Array.Empty<string>());

    private static readonly ProjektBrennstoffeStand STAND = new("Musterprojekt", new[] { ERDGAS, HEIZOEL, ALT },
        new[] { new ProjektBrennstoffKatalogsatz(25, "Wasserstoff"), new ProjektBrennstoffKatalogsatz(14, "Biogas") });

    public ProjektBrennstoffeDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<ProjektBrennstoffeDialog> Zeige(
        Func<int, IReadOnlyDictionary<string, double?>, Task<ProjektBrennstoffeAntwort>>? speichern = null,
        Func<int, Task<ProjektBrennstoffeAntwort>>? zuruecksetzen = null,
        Func<int, Task<ProjektBrennstoffeAntwort>>? uebernehmen = null,
        Action<bool>? geschlossen = null,
        ProjektBrennstoffeStand? stand = null)
        => Render<ProjektBrennstoffeDialog>(p => p
            .Add(x => x.Stand, stand ?? STAND)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Zuruecksetzen, zuruecksetzen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static Task<ProjektBrennstoffeAntwort> Ok(string meldung, ProjektBrennstoffeStand? stand = null) =>
        Task.FromResult(new ProjektBrennstoffeAntwort(true, meldung, stand));

    [Fact]
    public void Feldbestand_Spalten_Stand_und_Knoepfe()
    {
        var cut = Zeige((_, _) => Ok(""), _ => Ok(""), _ => Ok(""));

        Assert.Equal("Brennstoffe des Projekts", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Musterprojekt", cut.Find(".epos-pbrs-projekt").TextContent.Trim());
        // Brennstoff, Einheit, zehn Werte, Stand, Aktionen.
        Assert.Equal(14, cut.FindAll(".epos-pbrs-liste thead th").Count);
        Assert.Equal(3, cut.FindAll(".epos-pbrs-liste tbody tr").Count);
        Assert.Equal(new[] { "wie Katalog", "abweichend", "nicht mehr im Katalog" },
                     cut.FindAll(".epos-pbrs-stand").Select(e => e.TextContent.Trim()));
        Assert.Equal(3, cut.FindAll(".epos-pbrs-bearbeiten").Count);
        Assert.Equal(2, cut.FindAll(".epos-pbrs-zuruecksetzen").Count);     // ohne Katalogsatz kein Zurücksetzen
        Assert.Single(cut.FindComponents<Schliesskreuz>());
        Assert.Equal(2, cut.FindAll(".epos-pbrs-katalogwahl option").Count);
    }

    [Fact]
    public void Eine_abweichende_Zelle_nennt_den_Katalogwert()
    {
        var cut = Zeige();
        var zellen = cut.FindAll(".epos-pbrs-zelle--abweichend");
        var zelle = Assert.Single(zellen);
        Assert.Equal("300", zelle.TextContent.Trim());
        Assert.Equal("Katalogwert: 310", zelle.GetAttribute("title"));
        Assert.Contains("epos-pbrs-abweichend", cut.FindAll(".epos-pbrs-liste tbody tr")[1].ClassName);
    }

    [Fact]
    public void Bearbeiten_reicht_nur_die_geaenderten_Felder_und_zeigt_den_neuen_Stand()
    {
        (int, IReadOnlyDictionary<string, double?>)? gerufen = null;
        var neuerStand = new ProjektBrennstoffeStand("Musterprojekt", new[] { ERDGAS with { Werte = Werte(10.5, 230) } },
                                                     Array.Empty<ProjektBrennstoffKatalogsatz>());
        var cut = Zeige((id, w) => { gerufen = (id, w); return Ok("Die Werte von „Erdgas E“ sind gespeichert.", neuerStand); });

        cut.FindAll(".epos-pbrs-bearbeiten")[0].Click();
        var ueber = cut.FindComponents<Ueberlagerung>().Single(u => u.Instance.Offen);
        Assert.Contains("Erdgas E", ueber.Instance.Titel);
        Assert.Equal(10, ueber.FindAll("input").Count);

        ueber.FindAll("input")[2].Input("230");       // CO₂
        cut.Find(".epos-pbrs-speichern").Click();

        Assert.NotNull(gerufen);
        Assert.Equal(3, gerufen!.Value.Item1);
        Assert.Equal(new[] { ProjektBrennstoffFelder.Co2 }, gerufen.Value.Item2.Keys);
        Assert.Equal(230.0, gerufen.Value.Item2[ProjektBrennstoffFelder.Co2]);
        Assert.DoesNotContain(cut.FindComponents<Ueberlagerung>(), u => u.Instance.Offen);
        Assert.Contains("sind gespeichert", cut.Markup);
        Assert.Single(cut.FindAll(".epos-pbrs-liste tbody tr"));
        Assert.True(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Bearbeiten_ohne_Aenderung_schreibt_nichts()
    {
        int rufe = 0;
        var cut = Zeige((_, _) => { rufe++; return Ok(""); });
        cut.FindAll(".epos-pbrs-bearbeiten")[1].Click();
        cut.Find(".epos-pbrs-speichern").Click();
        Assert.Equal(0, rufe);
        Assert.False(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Eine_abgelehnte_Eingabe_haelt_die_Ueberlagerung_offen()
    {
        var cut = Zeige((_, _) => Task.FromResult(new ProjektBrennstoffeAntwort(false, "Der Wert für CO2 ist ungültig.", null)));
        cut.FindAll(".epos-pbrs-bearbeiten")[0].Click();
        cut.FindComponents<Ueberlagerung>().Single(u => u.Instance.Offen).FindAll("input")[2].Input("5");
        cut.Find(".epos-pbrs-speichern").Click();
        Assert.Contains(cut.FindComponents<Ueberlagerung>(), u => u.Instance.Offen);
        Assert.Contains("ungültig", cut.Markup);
        Assert.False(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Zuruecksetzen_fragt_mit_Vorgabe_Nein_und_schreibt_erst_nach_Ja()
    {
        var gerufen = new List<int>();
        var cut = Zeige(zuruecksetzen: id => { gerufen.Add(id); return Ok("„Heizöl EL“ steht wieder auf dem Katalogstand.", STAND); });

        cut.FindAll(".epos-pbrs-zuruecksetzen")[1].Click();
        var frage = cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Contains("Heizöl EL", frage.Instance.Frage);
        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Empty(gerufen);

        cut.FindAll(".epos-pbrs-zuruecksetzen")[1].Click();
        cut.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 9 }, gerufen);
        Assert.True(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Uebernehmen_nimmt_den_gewaehlten_Katalogsatz()
    {
        var gerufen = new List<int>();
        var cut = Zeige(uebernehmen: id =>
        {
            gerufen.Add(id);
            return Ok("„Biogas“ ist aus dem Katalog übernommen.",
                      STAND with { Katalog = new[] { new ProjektBrennstoffKatalogsatz(25, "Wasserstoff") } });
        });

        cut.Find(".epos-pbrs-katalogwahl").Change("14");
        cut.Find(".epos-pbrs-uebernehmen").Click();
        Assert.Equal(new[] { 14 }, gerufen);
        Assert.Single(cut.FindAll(".epos-pbrs-katalogwahl option"));
        Assert.Contains("übernommen", cut.Markup);

        // Ist der Katalog ausgeschöpft, steht die leise Zeile statt der Wahl.
        var leer = Zeige(uebernehmen: _ => Ok(""), stand: STAND with { Katalog = Array.Empty<ProjektBrennstoffKatalogsatz>() });
        Assert.Empty(leer.FindAll(".epos-pbrs-katalogwahl"));
        Assert.Contains("Alle Brennstoffe des Katalogs sind im Projekt.", leer.Markup);
    }

    [Fact]
    public void Schliessen_und_Esc_melden_ob_geschrieben_wurde()
    {
        bool? zu = null;
        var cut = Zeige(geschlossen: b => zu = b);
        cut.Find(".epos-projektbrennstoffe").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zu);

        zu = null;
        var zwei = Zeige(zuruecksetzen: _ => Ok("", STAND), geschlossen: b => zu = b);
        zwei.FindAll(".epos-pbrs-zuruecksetzen")[0].Click();
        zwei.FindComponents<Rueckfrage>().First(f => f.Instance.Offen)
            .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();
        zwei.Find(".epos-knopf--primaer").Click();
        Assert.True(zu);
    }

    [Fact]
    public void Ohne_Projekt_steht_der_Hinweis()
    {
        var cut = Zeige((_, _) => Ok(""), _ => Ok(""), _ => Ok(""), stand: ProjektBrennstoffeStand.Leer);
        Assert.Contains("Kein Projekt geöffnet.", cut.Find(".epos-pbrs-kein-projekt").TextContent);
        Assert.Empty(cut.FindAll(".epos-pbrs-liste"));
        Assert.Empty(cut.FindAll(".epos-pbrs-uebernehmen"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_den_leeren_Stand()
    {
        var cut = Render<ProjektBrennstoffeDialog>();
        Assert.Empty(cut.FindAll(".epos-pbrs-bearbeiten"));
        Assert.Empty(cut.FindAll(".epos-pbrs-zuruecksetzen"));
        Assert.Empty(cut.FindAll(".epos-pbrs-uebernehmen"));
        Assert.Contains("Kein Projekt geöffnet.", cut.Markup);
    }
}
