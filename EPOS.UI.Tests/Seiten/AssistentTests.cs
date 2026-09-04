using System.Globalization;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Seiten.Assistent;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// NACHWEIS N5 der Welle iU9-W16a — der PROJEKTASSISTENT
/// (<c>EPOS.UI/Seiten/Assistent/AssistentSeite.razor</c>), Vorbild
/// <c>Views/Wizard/WizardParent</c>.
///
/// <para>Geprüft wird, was der Rahmen ausmacht: die dreizehn Seiten in ihrer festen
/// Reihenfolge, „Weiter"/„Zurück" NUR über aktive Seiten, „Weiter" wird auf der
/// letzten aktiven Seite zu „Speichern", die zwei Pflichtprüfungen erscheinen als
/// EINE Meldung (Entscheid E-4) und das linke Band steht nur in Betriebsart
/// BEARBEITEN auf Schritt 0.</para>
/// </summary>
public class AssistentTests : BunitContext
{
    public AssistentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    // Aufbau
    // =====================================================================

    /// <summary>
    /// Ein Gabensatz je Seite: Für den Test genügt der Komponentenschritt mit
    /// dreizehn leeren Kacheln — er ist die einzige Seite, die ohne Datenbank
    /// vollständig zeichnet.
    /// </summary>
    private static IReadOnlyDictionary<string, object> Gaben(int nr)
    {
        if (nr != 0) return new Dictionary<string, object>();

        var zeilen = new List<KomponentenZeile>();
        for (int k = 0; k < 13; k++)
            zeilen.Add(new KomponentenZeile
            {
                Kennung = k,
                Titel = "Kachel " + k,
                SeitenIndex = k < 11 ? k + 2 : KomponentenZeile.OHNE_SEITE
            });

        return new Dictionary<string, object> { ["Zeilen"] = zeilen };
    }

    private IRenderedComponent<AssistentSeite> Zeige(
        int betriebsart = 0,
        Func<int, bool>? seiteAktiv = null,
        Func<(string Text, string Titel)?>? speichern = null,
        Action<bool>? geschlossen = null,
        Action<int>? seiteVerlassen = null,
        IReadOnlyList<ProjektKopfZeile>? projekte = null,
        Action<int, string>? projektMarkiert = null,
        Action<int, string>? projektOeffnen = null)
    {
        return Render<AssistentSeite>(p => p
            .Add(x => x.Betriebsart, betriebsart)
            .Add(x => x.SeiteGaben, new Func<int, IReadOnlyDictionary<string, object>?>(Gaben))
            .Add(x => x.SeiteAktiv, seiteAktiv ?? (nr => nr <= 1))
            .Add(x => x.SeiteVerlassen, seiteVerlassen)
            .Add(x => x.Projekte, projekte ?? Array.Empty<ProjektKopfZeile>())
            .Add(x => x.ProjektMarkiert, projektMarkiert)
            .Add(x => x.ProjektOeffnen, projektOeffnen)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IHtmlCollection<IElement> Fussknoepfe(IRenderedComponent<AssistentSeite> cut)
        => cut.Find(".epos-assistent-fuss").QuerySelectorAll("button");

    private static IElement Abbrechen(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[0];
    private static IElement Zurueck(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[1];
    private static IElement Weiter(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[2];

    // =====================================================================
    // Die dreizehn Seiten
    // =====================================================================

    /// <summary>
    /// Die Seitentabelle ist die bitgleiche Übernahme von
    /// <c>AssistentSeiten.ERZEUGER</c> — dreizehn Einträge, und jede Nummer trifft
    /// die Komponente, die im Bestand an dieser Stelle stand.
    /// </summary>
    [Theory]
    [InlineData(0, "KomponentenauswahlDialog")]
    [InlineData(1, "ProjektKopfSeite")]
    [InlineData(2, "GebaeudeDialog")]
    [InlineData(3, "WaermebedarfExternDialog")]
    [InlineData(4, "BedarfsProfileDialog")]
    [InlineData(5, "BedarfsProfileDialog")]
    [InlineData(6, "StromganglinieDialog")]
    [InlineData(7, "WaermepumpenDialog")]
    [InlineData(8, "SolarkollektorenDialog")]
    [InlineData(9, "PhotovoltaikDialog")]
    [InlineData(10, "StromspeicherDialog")]
    [InlineData(11, "HeizkesselDialog")]
    [InlineData(12, "BhkwDialog")]
    public void Die_dreizehn_Seiten_stehen_in_der_Reihenfolge_des_Bestands(int nr, string typ)
    {
        Assert.Equal(typ, AssistentSeite.Seitentyp(nr).Name);
    }

    [Fact]
    public void Der_Assistent_beginnt_auf_dem_Komponentenschritt()
    {
        var cut = Zeige();

        Assert.Equal(0, cut.Instance.Schritt);
        Assert.Equal(13, cut.FindAll(".epos-kachel").Count);
    }

    // =====================================================================
    // Weiter und Zurueck - NUR ueber aktive Seiten
    // =====================================================================

    /// <summary>
    /// „Weiter" überspringt jede abgeschaltete Seite — der Ersatz für
    /// <c>GetNextUpIndex</c>. Aktiv sind hier 0, 1 und 12.
    /// </summary>
    [Fact]
    public void Weiter_geht_nur_ueber_aktive_Seiten()
    {
        var cut = Zeige(seiteAktiv: nr => nr <= 1 || nr == 12);

        Weiter(cut).Click();
        Assert.Equal(1, cut.Instance.Schritt);

        Weiter(cut).Click();
        Assert.Equal(12, cut.Instance.Schritt);
    }

    [Fact]
    public void Zurueck_geht_nur_ueber_aktive_Seiten()
    {
        var cut = Zeige(seiteAktiv: nr => nr <= 1 || nr == 12);

        Weiter(cut).Click();
        Weiter(cut).Click();
        Assert.Equal(12, cut.Instance.Schritt);

        Zurueck(cut).Click();
        Assert.Equal(1, cut.Instance.Schritt);

        Zurueck(cut).Click();
        Assert.Equal(0, cut.Instance.Schritt);
    }

    /// <summary>Auf dem ersten Schritt gibt es kein Zurück.</summary>
    [Fact]
    public void Auf_dem_ersten_Schritt_ist_Zurueck_gesperrt()
    {
        var cut = Zeige();

        Assert.True(Zurueck(cut).HasAttribute("disabled"));

        Weiter(cut).Click();
        Assert.False(Zurueck(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// Der verlassene Schritt wird gemeldet — der Wirt übernimmt dort den
    /// Projektkopf und lädt beim ersten Durchgang die sechs Listen.
    /// </summary>
    [Fact]
    public void Der_verlassene_Schritt_wird_gemeldet()
    {
        var verlassen = new List<int>();
        var cut = Zeige(seiteVerlassen: verlassen.Add);

        Weiter(cut).Click();
        Weiter(cut).Click();

        Assert.Equal(new[] { 0, 1 }, verlassen);
    }

    // =====================================================================
    // "Weiter" wird auf der letzten aktiven Seite "Speichern"
    // =====================================================================

    [Fact]
    public void Auf_der_letzten_aktiven_Seite_heisst_Weiter_Speichern()
    {
        var cut = Zeige();

        // Aktiv sind 0 und 1: auf 0 steht "Weiter", auf 1 "Speichern".
        Assert.Equal("Weiter ▶", Weiter(cut).TextContent);

        Weiter(cut).Click();
        Assert.Equal("Speichern", Weiter(cut).TextContent);
    }

    [Fact]
    public void Speichern_meldet_den_Erfolg_und_schliesst()
    {
        bool? ergebnis = null;
        int gerufen = 0;
        var cut = Zeige(speichern: () => { gerufen++; return null; },
                        geschlossen: b => ergebnis = b);

        Weiter(cut).Click();          // auf Schritt 1
        Weiter(cut).Click();          // Speichern

        Assert.Equal(1, gerufen);
        Assert.True(ergebnis);
    }

    /// <summary>
    /// Entscheid E-4: Ein Fehlschlag meldet sich mit EINEM Banner, und der Assistent
    /// bleibt STEHEN — die Eingaben gehen nicht verloren. Der Vorläufer brach
    /// siebzehnmal kommentarlos ab (Befund W16-B16).
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_meldet_sich_einmal_und_der_Assistent_bleibt_stehen()
    {
        bool? ergebnis = null;
        var cut = Zeige(speichern: () => ("Bitte eine Klimazone auswählen!", "Klimazone fehlt"),
                        geschlossen: b => ergebnis = b);

        Weiter(cut).Click();
        Weiter(cut).Click();

        Assert.Null(ergebnis);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.Contains("Bitte eine Klimazone auswählen!", cut.Markup);
        Assert.Contains("Klimazone fehlt", cut.Markup);
        Assert.Equal(1, cut.Instance.Schritt);
    }

    /// <summary>Die zweite Pflichtprüfung — derselbe Weg, anderer Satz.</summary>
    [Fact]
    public void Auch_der_fehlende_Projektname_meldet_sich_einmal()
    {
        var cut = Zeige(speichern: () => ("Bitte einen Projektnamen eingeben!", "Projektname fehlt"));

        Weiter(cut).Click();
        Weiter(cut).Click();

        Assert.Contains("Bitte einen Projektnamen eingeben!", cut.Markup);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    /// <summary>Ein Seitenwechsel räumt die Meldung weg.</summary>
    [Fact]
    public void Ein_Seitenwechsel_raeumt_die_Meldung_weg()
    {
        var cut = Zeige(speichern: () => ("Fehler", "Titel"));

        Weiter(cut).Click();
        Weiter(cut).Click();
        Assert.Single(cut.FindAll(".epos-warnbanner"));

        Zurueck(cut).Click();
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    // Abbrechen
    // =====================================================================

    [Fact]
    public void Abbrechen_meldet_ohne_zu_speichern()
    {
        bool? ergebnis = null;
        int gerufen = 0;
        var cut = Zeige(speichern: () => { gerufen++; return null; },
                        geschlossen: b => ergebnis = b);

        Abbrechen(cut).Click();

        Assert.False(ergebnis);
        Assert.Equal(0, gerufen);
    }

    // =====================================================================
    // Das linke Band
    // =====================================================================

    /// <summary>
    /// Zwei Projekte. Der Baustein <c>ProjektListe</c> sortiert nach Namen — die
    /// ERSTE Zeile ist deshalb „Laurentiuskirche" (1007), nicht die erste des
    /// Feldes.
    /// </summary>
    private static IReadOnlyList<ProjektKopfZeile> Projekte() => new[]
    {
        new ProjektKopfZeile(1030, "Referenz BHKW-Kaskade"),
        new ProjektKopfZeile(1007, "Laurentiuskirche")
    };

    /// <summary>
    /// Wörtlich <c>WizardParent.Next</c> (:305-312): Das Band steht NUR in
    /// Betriebsart BEARBEITEN und NUR auf Schritt 0.
    /// </summary>
    [Fact]
    public void Das_linke_Band_steht_nur_beim_Bearbeiten_auf_Schritt_null()
    {
        var neu = Zeige(betriebsart: 0, projekte: Projekte());
        Assert.Empty(neu.FindAll(".epos-assistent-band"));

        var bearbeiten = Zeige(betriebsart: 1, projekte: Projekte());
        Assert.Single(bearbeiten.FindAll(".epos-assistent-band"));

        // Ein Projekt markieren, damit "Weiter" frei wird, dann weiterblaettern.
        bearbeiten.Find(".epos-assistent-band tbody tr button").Click();
        Weiter(bearbeiten).Click();

        Assert.Equal(1, bearbeiten.Instance.Schritt);
        Assert.Empty(bearbeiten.FindAll(".epos-assistent-band"));
    }

    /// <summary>
    /// Im Bearbeiten-Modus bleibt „Weiter" gesperrt, solange kein Projekt markiert
    /// ist — wörtlich <c>WizardParent.Next</c> (:269-271).
    /// </summary>
    [Fact]
    public void Ohne_markiertes_Projekt_bleibt_Weiter_gesperrt()
    {
        var cut = Zeige(betriebsart: 1, projekte: Projekte());

        Assert.True(Weiter(cut).HasAttribute("disabled"));

        cut.Find(".epos-assistent-band tbody tr button").Click();
        Assert.False(Weiter(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// Eine andere Markierung meldet sich — der Wirt liest den Komponentenbestand
    /// neu und schaltet die Seiten danach.
    /// </summary>
    [Fact]
    public void Eine_neue_Markierung_wird_gemeldet()
    {
        var gemeldet = new List<(int Id, string Name)>();
        var cut = Zeige(betriebsart: 1, projekte: Projekte(),
                        projektMarkiert: (id, name) => gemeldet.Add((id, name)));

        cut.Find(".epos-assistent-band tbody tr button").Click();

        Assert.Single(gemeldet);
        Assert.Equal(1007, gemeldet[0].Id);
        Assert.Equal("Laurentiuskirche", gemeldet[0].Name);
    }

    /// <summary>
    /// „Projekt öffnen" — der Knopf ist gesperrt, solange nichts markiert ist, und
    /// meldet danach das markierte Projekt (Nutzerwunsch 30.08.2026).
    /// </summary>
    [Fact]
    public void Projekt_oeffnen_meldet_das_markierte_Projekt()
    {
        var geoeffnet = new List<(int Id, string Name)>();
        var cut = Zeige(betriebsart: 1, projekte: Projekte(),
                        projektOeffnen: (id, name) => geoeffnet.Add((id, name)));

        IElement knopf = cut.Find(".epos-assistent-band .epos-leiste button");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Equal("Projekt öffnen", knopf.TextContent);

        cut.Find(".epos-assistent-band tbody tr button").Click();
        cut.Find(".epos-assistent-band .epos-leiste button").Click();

        Assert.Single(geoeffnet);
        Assert.Equal(1007, geoeffnet[0].Id);
    }
}
