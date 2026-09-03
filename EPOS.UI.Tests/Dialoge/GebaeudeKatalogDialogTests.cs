using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Gebäude-Katalogeditor (iU9-W9.1). Soll sind die Feldkarten von
/// <c>Form_Gebaeude1</c> (37 Zeilen) und <c>Form_Gebaeude2</c> (41) — ZWEI Masken auf
/// EINEM Satz, hier zwei Reiter.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class GebaeudeKatalogDialogTests : BunitContext
{
    private static readonly string[] TYPEN = { "Einfamilienhaus", "Hotel" };
    private static readonly string[] ARTEN = { "Einfamilienhaus", "Hotel", "Kaufhaus" };
    private static readonly string[] KLASSEN =
    { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978",
      "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise",
      "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009",
      "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016",
      "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };
    private static readonly string[] NAMEN = { "Haus A", "Haus B", "Hotel C" };

    public GebaeudeKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz — alle 17 Pflichtzahlen stehen.</summary>
    private static GebaeudeKatalogDaten Satz(string name = "Haus A") => new()
    {
        Name = name,
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung " + name,
        Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = 1,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        SonstigeFlaechen = 5,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5
    };

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        GebaeudeKatalogDaten? daten = null,
        GebaeudeKatalogModus modus = GebaeudeKatalogModus.Bearbeiten,
        Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>? speichern = null,
        Func<string, GebaeudeKatalogDaten?>? lies = null,
        Func<IReadOnlyDictionary<string, object>>? brauchwasser = null,
        Action<bool>? geschlossen = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Modus, modus)
            .Add(x => x.Gebaeudetypen, () => TYPEN)
            .Add(x => x.Gebaeudearten, () => ARTEN)
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Katalognamen, () => NAMEN)
            .Add(x => x.Lies, lies ?? (n => Satz(n)))
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => new GebaeudeKatalogErgebnis(true, "")))
            .Add(x => x.BrauchwasserGaben, brauchwasser)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static void ReiterWaehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, string titel)
        => cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == titel).Click();

    // =================================================================================
    // Feldbestand je Reiter
    // =================================================================================

    [Fact]
    public void Der_erste_Reiter_traegt_die_Felder_der_Karte_von_Form_Gebaeude1()
    {
        var cut = Aufbauen();

        // 17 Pflichtzahlen: 5 Kenngroessen + 7 Flaechen + 5 U-Werte.
        Assert.Equal(17, cut.FindAll("input[inputmode=decimal]").Count);
        // Sechs Klapplisten: Gebaeudetyp, Gebaeudeart, Baujahr, Verwendung, Bauart,
        // und im Modus Bearbeiten ist der Name ein Textfeld.
        Assert.Equal(5, cut.FindAll("select").Count);
        Assert.Single(cut.FindAll("textarea"));

        Assert.Contains("Kenngrößen", cut.Markup);
        Assert.Contains("Flächen [m²]", cut.Markup);
        Assert.Contains("U-Werte [W/m²K]", cut.Markup);
        Assert.Contains("Wohn-/Nutzfläche :", cut.Markup);
        Assert.Contains("Fensterdurchlaßgrad :", cut.Markup);
        Assert.Contains("(z.B. 0,4)", cut.Markup);
        Assert.Contains("Fensterfläche Ost + West :", cut.Markup);
        Assert.Contains("sonstige Flächen :", cut.Markup);
    }

    /// <summary>Der Designer schreibt „Fläschen"; gemeint sind Flächen (A-4).</summary>
    [Fact]
    public void Der_Titel_ist_berichtigt()
    {
        var cut = Aufbauen();

        Assert.Contains("Flächen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.DoesNotContain("Fläschen", cut.Markup);
    }

    [Fact]
    public void Der_zweite_Reiter_traegt_die_Felder_der_Karte_von_Form_Gebaeude2()
    {
        var cut = Aufbauen();
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        // 5 Raumtemperaturen + 3 Waermebruecken + 3 Anschluesse + 1 Luftwechsel = 12 Zahlen,
        // dazu 16 Ganzzahlfelder fuer die vier Ferienzeitraeume.
        Assert.Equal(12, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(16, cut.FindAll("input[inputmode=numeric]").Count);

        Assert.Contains("Raumtemperaturen", cut.Markup);
        Assert.Contains("Wärmebrückenverlustkoeffizienten [W/(mK)]", cut.Markup);
        Assert.Contains("Abmessung Anschluß [m]", cut.Markup);
        Assert.Contains("Ferien Anfang", cut.Markup);
        Assert.Contains("Ferien Ende", cut.Markup);
        Assert.Contains("Luftwechselrate :", cut.Markup);
        Assert.Contains("Winter :", cut.Markup);
        Assert.Contains("Herbst :", cut.Markup);
    }

    /// <summary>Ein nicht gewähltes Blatt wird GAR NICHT gezeichnet (Baustein `Reiterblatt`).</summary>
    [Fact]
    public void Der_zweite_Reiter_erscheint_erst_beim_Betreten()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Ferien Anfang", cut.Markup);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");
        Assert.Contains("Ferien Anfang", cut.Markup);
    }

    // =================================================================================
    // Die drei Modi
    // =================================================================================

    [Fact]
    public void Im_Modus_Bearbeiten_sind_beide_Schreibwege_frei()
    {
        var cut = Aufbauen(modus: GebaeudeKatalogModus.Bearbeiten);

        Assert.False(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
    }

    [Fact]
    public void Im_Modus_Neu_heisst_der_Knopf_Speichern_und_Ueberschreiben_ist_gesperrt()
    {
        var cut = Aufbauen(daten: new GebaeudeKatalogDaten(), modus: GebaeudeKatalogModus.Neu);

        Assert.True(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));
    }

    [Fact]
    public void Im_Modus_Admin_ist_der_Name_eine_Klappliste_und_Speichern_gesperrt()
    {
        var cut = Aufbauen(modus: GebaeudeKatalogModus.Admin);

        // Eine Klappliste mehr als im Modus Bearbeiten: der Name.
        Assert.Equal(6, cut.FindAll("select").Count);
        Assert.True(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.Contains("Haus A", cut.Markup);
        Assert.Contains("Hotel C", cut.Markup);
    }

    [Fact]
    public void Im_Modus_Admin_laedt_der_Namenswechsel_den_gewaehlten_Satz()
    {
        var cut = Aufbauen(modus: GebaeudeKatalogModus.Admin);

        cut.Find("select").Change("2");   // Hotel C

        Assert.Equal("Hotel C", cut.Instance.Ursprungsname);
    }

    // =================================================================================
    // Baujahr, Bauart, Verwendung
    // =================================================================================

    [Fact]
    public void Die_Baujahrliste_fuehrt_21_Klassen()
    {
        var cut = Aufbauen();

        IElement baujahr = cut.FindAll("select")[2];
        Assert.Equal(21, baujahr.QuerySelectorAll("option").Length);
        Assert.Contains("vor 1919", cut.Markup);
        Assert.Contains("BEG 40", cut.Markup);
    }

    /// <summary>
    /// Befund W9‑B8: Der Steuerwert der Verwendung ist getrennt vom Anzeigetext.
    /// </summary>
    [Fact]
    public void Die_Verwendung_traegt_den_Steuerwert_getrennt_vom_Anzeigetext()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten,
                           speichern: (d, _, _) => new GebaeudeKatalogErgebnis(true, ""));

        Assert.Contains("Wohngebäude", cut.Markup);      // Anzeige mit Umlaut
        cut.FindAll("select")[3].Change("1");            // Nicht Wohngebäude
        Assert.Equal("Nicht Wohngebaeude", daten.Verwendung);   // Steuerwert ohne Umlaut
    }

    [Fact]
    public void Die_Bauartliste_fuehrt_die_drei_Stufen()
    {
        var cut = Aufbauen();

        Assert.Contains("Leichte Bauart", cut.Markup);
        Assert.Contains("Schwere Bauart", cut.Markup);
        Assert.Contains("Sehr schwere Bauart", cut.Markup);
    }

    // =================================================================================
    // Pflichtzahlen
    // =================================================================================

    [Fact]
    public void Eine_fehlende_Pflichtzahl_meldet_mit_ihrem_Feldnamen()
    {
        bool geschrieben = false;
        GebaeudeKatalogDaten daten = Satz();
        daten.Raumhoehe = null;

        var cut = Aufbauen(daten: daten, speichern: (_, _, _) =>
        {
            geschrieben = true;
            return new GebaeudeKatalogErgebnis(true, "");
        });

        Knopf(cut, "Überschreiben").Click();

        Assert.False(geschrieben);
        Assert.Contains("Raumhöhe", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_leerer_Name_meldet_beim_Speichern()
    {
        bool geschrieben = false;
        GebaeudeKatalogDaten daten = Satz("");

        var cut = Aufbauen(daten: daten, modus: GebaeudeKatalogModus.Neu,
                           speichern: (_, _, _) =>
                           {
                               geschrieben = true;
                               return new GebaeudeKatalogErgebnis(true, "");
                           });

        Knopf(cut, "Speichern").Click();

        Assert.False(geschrieben);
        Assert.Contains("Gebäudenamen", cut.Instance.Meldung);
    }

    [Fact]
    public void Ueberschreiben_trifft_den_Ursprungsnamen()
    {
        string bezeichner = "";
        GebaeudeKatalogDaten daten = Satz("Haus A");

        var cut = Aufbauen(daten: daten, speichern: (_, _, bez) =>
        {
            bezeichner = bez;
            return new GebaeudeKatalogErgebnis(true, "");
        });

        daten.Name = "Haus NEU";
        Knopf(cut, "Überschreiben").Click();

        Assert.Equal("Haus A", bezeichner);
    }

    [Fact]
    public void Eine_abgelehnte_Schreibung_bleibt_als_Warnbanner_stehen()
    {
        var cut = Aufbauen(speichern: (_, _, _) =>
            new GebaeudeKatalogErgebnis(false, "Dieser Stammdatensatz ist schreibgeschützt"));

        Knopf(cut, "Überschreiben").Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.Single(cut.FindAll("[role=alert]"));
    }

    // =================================================================================
    // Reiter 2: Uebernehmen, Ferienregeln, Ableitungen
    // =================================================================================

    [Fact]
    public void Uebernehmen_hebt_eine_Maximaltemperatur_unter_1_auf_24()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        cut.FindAll("input[inputmode=decimal]")[2].Input("0");
        Knopf(cut, "Werte übernehmen").Click();

        Assert.Equal(24, daten.MaxTemperatur);
    }

    [Fact]
    public void Uebernehmen_setzt_die_Flags_Wochenende_und_Ferien_aus_den_Absenkungen()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        cut.FindAll("input[inputmode=decimal]")[3].Input("16");   // Wochenendabsenkung
        cut.FindAll("input[inputmode=decimal]")[4].Input("15");   // Soll in Ferien
        Knopf(cut, "Werte übernehmen").Click();

        Assert.Equal(1, daten.Wochenende);
        Assert.Equal(1, daten.Ferien);
        Assert.Equal(0, daten.WwBedarf);
    }

    [Fact]
    public void Uebernehmen_hebt_einen_leeren_Winterferienbeginn_auf_366()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        Knopf(cut, "Werte übernehmen").Click();

        Assert.Equal(366, daten.Ferienbeginn[0]);
    }

    [Fact]
    public void Uebernehmen_meldet_Winterferien_die_nicht_ueber_die_Jahresgrenze_gehen()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        var ganzzahl = cut.FindAll("input[inputmode=numeric]");
        ganzzahl[0].Input("1");    // Winter Beginn: 1.2.  -> Jahrestag 32
        ganzzahl[1].Input("2");
        var ende = cut.FindAll("input[inputmode=numeric]");
        ende[8].Input("1");        // Winter Ende:   1.3.  -> Jahrestag 60 > 32
        ende[9].Input("3");

        Knopf(cut, "Werte übernehmen").Click();

        Assert.Contains("Jahresgrenze", cut.Instance.Meldung);
    }

    /// <summary>
    /// Ohne „Übernehmen" wandert vom zweiten Reiter nichts in den Satz (A-6) — genau wie
    /// beim abgebrochenen zweiten Fenster des Vorläufers.
    /// </summary>
    [Fact]
    public void Ohne_Uebernehmen_bleibt_der_Satz_unberuehrt()
    {
        GebaeudeKatalogDaten daten = Satz();
        var cut = Aufbauen(daten: daten);
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        cut.FindAll("input[inputmode=decimal]")[0].Input("99");

        Assert.Equal(20, daten.SollTag);
    }

    // =================================================================================
    // Brauchwasser und Tastatur
    // =================================================================================

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Brauchwasserknopf()
    {
        var cut = Aufbauen();
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        Assert.DoesNotContain("Brauchwasser...", cut.Markup);
    }

    [Fact]
    public void Mit_Delegat_oeffnet_der_Brauchwasserknopf_die_Ueberlagerung()
    {
        bool gerufen = false;
        var cut = Aufbauen(brauchwasser: () =>
        {
            gerufen = true;
            return new Dictionary<string, object>();
        });
        ReiterWaehlen(cut, "Temperaturen, Ferien, Luftwechsel");

        Knopf(cut, "Brauchwasser...").Click();

        Assert.True(gerufen);
        Assert.True(cut.Instance.BrauchwasserOffen);
    }

    [Fact]
    public void Esc_schliesst()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(gerufen);
    }

    [Fact]
    public void Beenden_schliesst()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Knopf(cut, "Beenden").Click();

        Assert.True(gerufen);
    }
}
