using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Vorbelegung der Einheit im Zeilendialog des Gesetzeskatalogs</b>
/// (Anwenderentscheid 14.10.2026) — die fünf Szenarien des Mockups
/// <c>Gesetzeskatalog_Einheit_Vorbelegung.html</c>, dazu die weiche Sperre von OK
/// mit ihrem Grund, „Vorschlag übernehmen" und die Schlüsselvorschläge.
///
/// <para>Die Regel selbst prüft <c>EPOS.Kern.Tests/GesetzEinheitVorbelegungTests</c>;
/// hier steht, was der Dialog daraus macht. Die Vorschlagsdaten kommen wie im
/// Mockup herein: KWKG meist ct/kWh, der Stundenschlüssel KWKG_VBH_JAHRESDECKEL
/// in h (jüngste Zeile 2030), CO2_PREIS in EUR/t, ENERGIESTEUER ohne Vorbelegung.</para>
/// </summary>
public class GesetzeskatalogZeileVorbelegungTests : EposBunitContext
{
    private static readonly (string, string)[] KLASSEN =
    {
        ("KWKG", "KWK-Gesetz"),
        ("ENERGIESTEUER", "Energiesteuer"),
        ("CO2_PREIS", "CO₂-Preis")
    };

    private const string QUELLE_DECKEL = "KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr";

    private static GesetzVorschlagsdaten Daten() => new(
        new List<GesetzSchluesselStand>
        {
            new("CO2_PREIS_NEHS", "CO2_PREIS", 2028, "EUR/t", "BEHG § 10"),
            new("KWKG_VBH_JAHRESDECKEL", "KWKG", 2030, "h", QUELLE_DECKEL),
            new("KWKG_ZUSCHLAG_BIS50KW", "KWKG", 2026, "ct/kWh", "KWKG § 7")
        },
        new Dictionary<string, string> { ["KWKG"] = "ct/kWh", ["CO2_PREIS"] = "EUR/t" });

    public GesetzeskatalogZeileVorbelegungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<GesetzeskatalogZeileDialog> Aufbauen(
        bool neu = true, string schluessel = "", string klasse = "", string einheit = "",
        string quelle = "", int jahr = 2031,
        Action<GesetzeskatalogZeileDialog.Zeilenwerte?>? geschlossen = null)
    {
        return Render<GesetzeskatalogZeileDialog>(p => p
            .Add(x => x.IstNeu, neu)
            .Add(x => x.Id, neu ? 0 : 7)
            .Add(x => x.Schluessel, schluessel)
            .Add(x => x.Klasse, klasse)
            .Add(x => x.JahrVon, jahr)
            .Add(x => x.Einheit, einheit)
            .Add(x => x.Status, "GESICHERT")
            .Add(x => x.Quelle, quelle)
            .Add(x => x.Klassen, KLASSEN.ToList())
            .Add(x => x.Einheiten, GesetzKatalog.Einheiten())
            .Add(x => x.Statuswerte, GesetzKatalog.Statuswerte())
            .Add(x => x.Vorschlagsdaten, Daten())
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    // ----- Prüfhilfen ---------------------------------------------------------

    /// <summary>Der Text der gewählten Einheit; leer = der Platzhalter steht.</summary>
    private static string Einheit(IRenderedComponent<GesetzeskatalogZeileDialog> cut)
    {
        var gewaehlt = cut.FindAll("select")[1].QuerySelectorAll("option")
                          .Where(o => o.HasAttribute("selected")).ToList();
        return gewaehlt.Count == 1 && gewaehlt[0].GetAttribute("value") != ""
            ? gewaehlt[0].TextContent.Trim() : "";
    }

    private static string Hinweis(IRenderedComponent<GesetzeskatalogZeileDialog> cut)
        => string.Join(" | ", cut.FindAll(".epos-gesetz-einheit-hinweis, .epos-vorschlagszeile")
                                 .Select(e => e.TextContent.Trim()));

    private static AngleSharp.Dom.IElement Ok(IRenderedComponent<GesetzeskatalogZeileDialog> cut)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == "OK");

    private static void EinheitWaehlen(IRenderedComponent<GesetzeskatalogZeileDialog> cut, string einheit)
    {
        int stelle = GesetzKatalog.Einheiten().ToList().IndexOf(einheit);
        cut.FindAll("select")[1].Change(stelle.ToString());
    }

    private static void KlasseWaehlen(IRenderedComponent<GesetzeskatalogZeileDialog> cut, string klasse)
    {
        int stelle = KLASSEN.Select(k => k.Item1).ToList().IndexOf(klasse);
        cut.FindAll("select")[0].Change(stelle.ToString());
    }

    // ----- (a) bestehender Schlüssel -----------------------------------------

    /// <summary>
    /// <b>(a)</b> Ein Gesetz ändert einen Satz ab einem neuen Jahr: Der Schlüssel
    /// KWKG_VBH_JAHRESDECKEL bringt „h" mit, nicht ct/kWh des Bereichs — samt Quelle und
    /// dem Hinweis auf seine jüngste Zeile.
    /// </summary>
    [Fact]
    public void A_Ein_bestehender_Schluessel_gibt_Einheit_Bereich_und_Quelle()
    {
        var cut = Aufbauen();
        cut.Find("input").Input("KWKG_VBH_JAHRESDECKEL");

        Assert.Equal("h", Einheit(cut));
        Assert.Contains("übernommen von KWKG_VBH_JAHRESDECKEL ab 2030", Hinweis(cut));
        var klasse = cut.FindAll("select")[0].QuerySelector("option[selected]");
        Assert.Equal("KWK-Gesetz", klasse!.TextContent.Trim());
        Assert.Contains(cut.FindAll("input"), i => i.GetAttribute("value") == QUELLE_DECKEL);
        Assert.Null(Ok(cut).GetAttribute("aria-disabled"));
    }

    [Fact]
    public void A_Der_bestehende_Schluessel_legt_mit_seiner_Einheit_an()
    {
        GesetzeskatalogZeileDialog.Zeilenwerte? werte = null;
        var cut = Aufbauen(schluessel: "KWKG_VBH_JAHRESDECKEL", klasse: "KWKG", geschlossen: w => werte = w);

        Ok(cut).Click();

        Assert.NotNull(werte);
        Assert.Equal("h", werte!.Einheit);
        Assert.Equal("KWKG", werte.Klasse);
        Assert.Equal(QUELLE_DECKEL, werte.Quelle);
    }

    // ----- (b) neuer Schlüssel ------------------------------------------------

    /// <summary>
    /// <b>(b)</b> Ein neuer Schlüssel im Bereich CO2_PREIS bekommt EUR/t; ein Wechsel
    /// des Bereichs zieht die Vorbelegung nach, solange keine Einheit gewählt ist.
    /// </summary>
    [Fact]
    public void B_Ein_neuer_Schluessel_bekommt_die_uebliche_Einheit_des_Bereichs()
    {
        var cut = Aufbauen(schluessel: "CO2_PREIS_BEHG_KORRIDOR_OBEN", klasse: "CO2_PREIS");

        Assert.Equal("EUR/t", Einheit(cut));
        Assert.Contains("üblich im Bereich CO₂-Preis", Hinweis(cut));

        KlasseWaehlen(cut, "KWKG");
        Assert.Equal("ct/kWh", Einheit(cut));
        Assert.Contains("üblich im Bereich KWK-Gesetz", Hinweis(cut));
    }

    // ----- (c) ENERGIESTEUER ---------------------------------------------------

    /// <summary>
    /// <b>(c)</b> Im Bereich ENERGIESTEUER wechselt die Einheit je Brennstoff: keine
    /// Vorbelegung, das Feld ist Pflicht, OK ist weich gesperrt und nennt den Grund.
    /// </summary>
    [Fact]
    public void C_Energiesteuer_belegt_nichts_vor_und_sperrt_OK()
    {
        var cut = Aufbauen(schluessel: "ENERGIESTEUER_FLUESSIGGAS", klasse: "ENERGIESTEUER");

        Assert.Equal("", Einheit(cut));
        Assert.Contains("wechselt die Einheit je Brennstoff", Hinweis(cut));
        Assert.Single(cut.FindAll(".epos-gesetz-einheit-hinweis--pflicht"));
        Assert.Equal("true", Ok(cut).GetAttribute("aria-disabled"));
        Assert.Equal("Einheit wählen.", Ok(cut).GetAttribute("title"));
    }

    [Fact]
    public void C_Ein_Klick_auf_das_gesperrte_OK_meldet_und_schliesst_nicht()
    {
        int geschlossen = 0;
        GesetzeskatalogZeileDialog.Zeilenwerte? werte = null;
        var cut = Aufbauen(schluessel: "ENERGIESTEUER_FLUESSIGGAS", klasse: "ENERGIESTEUER",
                           geschlossen: w => { geschlossen++; werte = w; });

        Ok(cut).Click();
        Assert.Equal(0, geschlossen);
        Assert.Equal("Einheit wählen.", cut.Instance.Meldung);

        // Mit gewählter Einheit ist OK frei, und die Zeile traegt genau diese Einheit -
        // kein stilles "-".
        EinheitWaehlen(cut, "EUR/1000kg");
        Assert.Null(Ok(cut).GetAttribute("aria-disabled"));
        Ok(cut).Click();
        Assert.Equal(1, geschlossen);
        Assert.Equal("EUR/1000kg", werte!.Einheit);
    }

    /// <summary>
    /// <b>Erst der Bereich, dann die Einheit:</b> Ohne Bereich nennt der Knopf
    /// „Bereich wählen."; die Klassenwahl zeigt dann ihren Platzhalter.
    /// </summary>
    [Fact]
    public void Ohne_Bereich_nennt_OK_zuerst_den_Bereich()
    {
        var cut = Aufbauen(schluessel: "GANZ_NEU");

        Assert.Equal("Bereich wählen.", Ok(cut).GetAttribute("title"));
        Assert.Contains("ohne Bereich", Hinweis(cut));
        Assert.Contains(cut.FindAll("select")[0].QuerySelectorAll("option"),
                        o => o.TextContent.Trim() == "(bitte wählen)");

        EinheitWaehlen(cut, "kW");
        Assert.Equal("Bereich wählen.", Ok(cut).GetAttribute("title"));   // der Bereich fehlt weiter

        KlasseWaehlen(cut, "KWKG");
        Assert.Null(Ok(cut).GetAttribute("title"));
        Assert.Equal("kW", Einheit(cut));                                    // die Wahl bleibt
    }

    // ----- (d) eigene Wahl ----------------------------------------------------

    /// <summary>
    /// <b>(d)</b> Die eigene Wahl gewinnt: Ein Wechsel von Bereich oder Schlüssel
    /// überschreibt sie nicht, „Vorschlag übernehmen" nennt den jeweils neuen Vorschlag
    /// und holt ihn zurück.
    /// </summary>
    [Fact]
    public void D_Die_eigene_Wahl_bleibt_und_Vorschlag_uebernehmen_holt_ihn_zurueck()
    {
        var cut = Aufbauen(schluessel: "CO2_PREIS_BEHG_KORRIDOR_OBEN", klasse: "CO2_PREIS");
        EinheitWaehlen(cut, "EUR/MWh");

        Assert.Equal("EUR/MWh", Einheit(cut));
        Assert.Contains("Ihre Wahl", Hinweis(cut));
        Assert.Contains("Vorschlag übernehmen (EUR/t)", Hinweis(cut));

        KlasseWaehlen(cut, "KWKG");
        Assert.Equal("EUR/MWh", Einheit(cut));
        Assert.Contains("Vorschlag übernehmen (ct/kWh)", Hinweis(cut));

        cut.Find("input").Input("KWKG_VBH_JAHRESDECKEL");
        Assert.Equal("EUR/MWh", Einheit(cut));
        Assert.Contains("Vorschlag übernehmen (h)", Hinweis(cut));

        cut.Find("button.epos-vorschlag").Click();
        Assert.Equal("h", Einheit(cut));
        Assert.Contains("übernommen von KWKG_VBH_JAHRESDECKEL", Hinweis(cut));
    }

    [Fact]
    public void D_Vorschlag_uebernehmen_erscheint_nur_bei_Abweichung()
    {
        var cut = Aufbauen(schluessel: "CO2_PREIS_BEHG_KORRIDOR_OBEN", klasse: "CO2_PREIS");
        EinheitWaehlen(cut, "EUR/t");                                   // = Vorschlag

        Assert.Contains("Ihre Wahl", Hinweis(cut));
        Assert.Empty(cut.FindAll(".epos-vorschlagszeile"));
    }

    /// <summary>Auch eine Wahl des Bereichs bleibt: Ein später getippter Schlüssel ändert ihn nicht.</summary>
    [Fact]
    public void D_Ein_gewaehlter_Bereich_bleibt_beim_Schluesseltreffer()
    {
        var cut = Aufbauen();
        KlasseWaehlen(cut, "CO2_PREIS");
        cut.Find("input").Input("KWKG_VBH_JAHRESDECKEL");

        Assert.Equal("CO₂-Preis", cut.FindAll("select")[0].QuerySelector("option[selected]")!.TextContent.Trim());
        Assert.Equal("h", Einheit(cut));                                // die Einheit kommt vom Schluessel
    }

    // ----- (e) Ändern -----------------------------------------------------------

    /// <summary>
    /// <b>(e)</b> Beim Ändern bleibt die gespeicherte Einheit, nichts wird vorbelegt und
    /// kein Hinweis erscheint; Schlüssel und Bereich sind gesperrt.
    /// </summary>
    [Fact]
    public void E_Beim_Aendern_bleibt_die_gespeicherte_Einheit()
    {
        GesetzeskatalogZeileDialog.Zeilenwerte? werte = null;
        var cut = Aufbauen(neu: false, schluessel: "KWKG_VBH_JAHRESDECKEL", klasse: "KWKG",
                           einheit: "h", quelle: QUELLE_DECKEL, jahr: 2030, geschlossen: w => werte = w);

        Assert.Equal("h", Einheit(cut));
        Assert.Equal("", Hinweis(cut));
        Assert.Empty(cut.FindAll(".epos-gesetz-schluesselvorschlaege"));
        Assert.True(cut.Find("input").HasAttribute("readonly"));
        Assert.True(cut.FindAll("select")[0].HasAttribute("disabled"));
        Assert.DoesNotContain(cut.FindAll("select")[1].QuerySelectorAll("option"),
                              o => o.TextContent.Trim() == "(bitte wählen)");
        Assert.Null(Ok(cut).GetAttribute("aria-disabled"));

        Ok(cut).Click();
        Assert.Equal("h", werte!.Einheit);
    }

    // ----- Schlüsselvorschläge --------------------------------------------------

    /// <summary>
    /// Die Vorschlagsliste der Schlüssel: Knöpfe unter dem Feld, sobald das Getippte zu
    /// bestehenden Schlüsseln passt — ein Klick übernimmt den Schlüssel samt Einheit.
    /// </summary>
    [Fact]
    public void Getippte_Teile_zeigen_bestehende_Schluessel_zur_Uebernahme()
    {
        var cut = Aufbauen(klasse: "KWKG");
        Assert.Empty(cut.FindAll(".epos-gesetz-schluesselvorschlaege"));

        cut.Find("input").Input("vbh");
        var knoepfe = cut.FindAll(".epos-gesetz-schluesselvorschlaege button");
        Assert.Equal(new[] { "KWKG_VBH_JAHRESDECKEL" }, knoepfe.Select(k => k.TextContent.Trim()));

        knoepfe[0].Click();
        Assert.Equal("KWKG_VBH_JAHRESDECKEL", cut.Find("input").GetAttribute("value"));
        Assert.Equal("h", Einheit(cut));
        Assert.Empty(cut.FindAll(".epos-gesetz-schluesselvorschlaege"));   // genauer Treffer
    }

    /// <summary>Ohne Vorschlagsdaten (Fall ohne Gaben) gibt es keine Vorbelegung — die Einheit ist Pflicht.</summary>
    [Fact]
    public void Ohne_Vorschlagsdaten_ist_die_Einheit_Pflicht()
    {
        var cut = Render<GesetzeskatalogZeileDialog>(p => p
            .Add(x => x.IstNeu, true)
            .Add(x => x.Klasse, "KWKG")
            .Add(x => x.Klassen, KLASSEN.ToList())
            .Add(x => x.Einheiten, GesetzKatalog.Einheiten()));

        Assert.Equal("", Einheit(cut));
        Assert.Equal("Einheit wählen.", Ok(cut).GetAttribute("title"));
    }
}
