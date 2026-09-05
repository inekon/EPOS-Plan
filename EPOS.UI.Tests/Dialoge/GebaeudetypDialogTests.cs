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
/// Gebäudetypen-Verwaltung (iU9-W8.4). Soll ist die Feldkarte von
/// <c>Form_EingGebTyp</c>: 33 Zeilen — Typliste, Beschreibung, Kurvenliste, 24
/// Stundenfelder, Bild und vier Knöpfe.
/// </summary>
public class GebaeudetypDialogTests : BunitContext
{
    private static readonly string[] TYPEN = { "Buerogebaeude", "Wohngebaeude VDI 2067" };

    private static readonly string[] KURZ =
    { "Winter-heiter", "Winter-trübe", "Übergang-heiter", "Übergang-trübe", "Sommertag" };

    private static readonly string[] LANG =
    { "Winter-Wochentag", "Winter-Wochenende", "Übergang1-Wochentag", "Übergang1-Wochenende",
      "Sommer-Wochentag", "Sommer-Wochenende", "Übergang2-Wochentag", "Übergang2-Wochenende" };

    public GebaeudetypDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Typ, dessen Werte die laufende Nummer 1…n × 24 SIND.</summary>
    private static GebaeudetypDaten Typ(string name, int kurven, bool aenderbar = true, int id = 7)
    {
        var v = new double[kurven, 24];
        for (int n = 0; n < kurven; n++)
            for (int s = 0; s < 24; s++) v[n, s] = n * 24 + s + 1;

        return new GebaeudetypDaten
        {
            Id = id,
            Name = name,
            Beschreibung = "Beschreibung " + name,
            Aenderbar = aenderbar,
            Verteilung = v,
            Kurvennamen = kurven <= 5 ? KURZ.Take(kurven).ToList() : LANG.Take(kurven).ToList()
        };
    }

    private IRenderedComponent<GebaeudetypDialog> Aufbauen(
        Func<IReadOnlyList<string>>? typen = null,
        Func<string, GebaeudetypDaten?>? lies = null,
        Func<int, double[,], bool>? speichern = null,
        Func<string, string, int>? anlegen = null,
        Func<int, bool>? loeschen = null,
        Func<double[], byte[]>? bild = null,
        Action<bool>? geschlossen = null)
        => Render<GebaeudetypDialog>(p => p
            .Add(x => x.Typen, typen ?? (() => TYPEN))
            .Add(x => x.Lies, lies ?? (n => Typ(n, n.StartsWith("Wohn") ? 5 : 8)))
            .Add(x => x.Speichern, speichern ?? ((_, _) => true))
            .Add(x => x.Anlegen, anlegen ?? ((_, _) => 42))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Bild, bild ?? (_ => new byte[] { 1, 2, 3 }))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<GebaeudetypDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(24, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(2, cut.FindAll(".epos-auswahlspalte .epos-raster").Count);
        Assert.Single(cut.FindAll("textarea"));

        Assert.Contains("Gebäudetypen Verwaltung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Kurvenverlauf für den Tag:", cut.Markup);
        Assert.Contains("Stundenwerteeingabe [kW, kWh oder %]", cut.Markup);

        var knoepfe = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Typ hinzufügen", "Typ Löschen", "Typ Speichern", "OK" }, knoepfe);
    }

    /// <summary>Die Beschreibung ist eine ANZEIGE — der Vorläufer setzte sie nur.</summary>
    [Fact]
    public void Die_Beschreibung_ist_nur_lesbar()
    {
        var cut = Aufbauen();
        Assert.True(cut.Find("textarea").HasAttribute("readonly"));
    }

    // =================================================================================
    // Kurvenzahl und Kurvennamen
    // =================================================================================

    /// <summary>
    /// Fünf Kurven nehmen die kurze Namensliste, acht die lange — entschieden über die
    /// KURVENZAHL, nicht über die Listenposition.
    /// </summary>
    [Fact]
    public void Fuenf_und_acht_Kurven_tragen_verschiedene_Namen()
    {
        var cut = Aufbauen();

        // "Buerogebaeude" steht vorn und hat acht Kurven.
        var namen = cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll("tbody tr td:last-child")
                       .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(8, namen.Count);
        Assert.Equal("Winter-Wochentag", namen[0]);
        Assert.Equal("Übergang2-Wochenende", namen[7]);

        // Umschalten auf "Wohngebaeude VDI 2067" (fuenf Kurven).
        cut.FindAll(".epos-auswahlspalte")[0].QuerySelectorAll("button")[1].Click();
        namen = cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll("tbody tr td:last-child")
                   .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(5, namen.Count);
        Assert.Equal("Winter-heiter", namen[0]);
        Assert.Equal("Sommertag", namen[4]);
    }

    [Fact]
    public void Die_Kurvenwahl_zeigt_die_vierundzwanzig_Werte()
    {
        var cut = Aufbauen();

        Assert.Equal(0, cut.Instance.Kurvenwahl);
        Assert.Equal(1.0, cut.Instance.Felder[0]);

        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll("button")[2].Click();
        Assert.Equal(2, cut.Instance.Kurvenwahl);
        Assert.Equal(49.0, cut.Instance.Felder[0]);      // 2 * 24 + 1
    }

    /// <summary>
    /// <c>RefreshArrayValues</c>: Der Kurvenwechsel überträgt STILL — ein ungültiges oder
    /// leeres Feld lässt den bisherigen Wert stehen und meldet nicht.
    /// </summary>
    [Fact]
    public void Der_Kurvenwechsel_uebertraegt_still_und_laesst_Leeres_stehen()
    {
        double[,] geschrieben = null!;
        var cut = Aufbauen(speichern: (_, v) => { geschrieben = v; return true; });

        cut.FindAll("input[inputmode=decimal]")[0].Input("111");
        cut.FindAll("input[inputmode=decimal]")[1].Input("");        // leer bleibt leer

        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll("button")[1].Click();
        Assert.Equal("", cut.Instance.Meldung);

        // Zurueck auf Kurve 0: 111 steht, der geleerte Wert ist der alte geblieben.
        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll("button")[0].Click();
        Assert.Equal(111.0, cut.Instance.Felder[0]);
        Assert.Equal(2.0, cut.Instance.Felder[1]);
    }

    // =================================================================================
    // Sperre des Auslieferungsbestands
    // =================================================================================

    [Fact]
    public void Ein_Katalogtyp_sperrt_Speichern_und_nennt_den_Grund()
    {
        var cut = Aufbauen(lies: n => Typ(n, 8, aenderbar: false));

        Assert.True(Knopf(cut, "Typ Speichern").HasAttribute("disabled"));
        Assert.Contains("vom Softwarehersteller gelieferten Gebäudetypen",
                        cut.Find(".epos-herleitung").TextContent);
    }

    [Fact]
    public void Ein_aenderbarer_Typ_hat_keinen_Sperrhinweis()
    {
        var cut = Aufbauen();

        Assert.False(Knopf(cut, "Typ Speichern").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-herleitung"));
    }

    // =================================================================================
    // Speichern
    // =================================================================================

    [Fact]
    public void Speichern_mit_einem_leeren_Feld_meldet_die_Stunde()
    {
        bool geschrieben = false;
        var cut = Aufbauen(speichern: (_, _) => { geschrieben = true; return true; });

        cut.FindAll("input[inputmode=decimal]")[6].Input("");
        Knopf(cut, "Typ Speichern").Click();

        Assert.False(geschrieben);
        Assert.Contains("Stunde 7", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Speichern_uebergibt_Id_und_Verteilung()
    {
        int id = 0;
        double[,] verteilung = null!;
        var cut = Aufbauen(speichern: (i, v) => { id = i; verteilung = v; return true; });

        cut.FindAll("input[inputmode=decimal]")[3].Input("88");
        Knopf(cut, "Typ Speichern").Click();

        Assert.Equal(7, id);
        Assert.Equal(88.0, verteilung[0, 3]);
        Assert.Contains("Daten gespeichert!", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Anlegen und Löschen
    // =================================================================================

    [Fact]
    public void Typ_hinzufuegen_fragt_Name_UND_Beschreibung()
    {
        string angelegt = null!, beschreibung = null!;
        var liste = new List<string>(TYPEN);

        var cut = Aufbauen(typen: () => liste,
            anlegen: (n, b) => { angelegt = n; beschreibung = b; liste.Add(n); return 99; },
            lies: n => Typ(n, 8));

        Knopf(cut, "Typ hinzufügen").Click();
        Assert.True(cut.Instance.Namensfrage);

        var felder = cut.FindAll(".epos-ueberlagerung input[type=text]");
        Assert.Equal(2, felder.Count);                  // Name UND Beschreibung
        felder[0].Input("Neuer Typ");
        felder[1].Input("Ein Neuer");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Neuer Typ", angelegt);
        Assert.Equal("Ein Neuer", beschreibung);
        Assert.Contains("Neuer Typ", cut.Instance.Typliste);
    }

    [Fact]
    public void Ein_belegter_Name_meldet_und_legt_nichts_an()
    {
        bool angelegt = false;
        var cut = Aufbauen(anlegen: (_, _) => { angelegt = true; return 99; });

        Knopf(cut, "Typ hinzufügen").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Buerogebaeude");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(angelegt);
        Assert.Contains("Name existiert bereits!", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Der Vorläufer löschte OHNE Rückfrage (A‑8).</summary>
    [Fact]
    public void Loeschen_fragt_erst_nach()
    {
        int geloescht = 0;
        var liste = new List<string>(TYPEN);
        var cut = Aufbauen(typen: () => liste,
                           loeschen: _ => { geloescht++; liste.RemoveAt(0); return true; });

        Knopf(cut, "Typ Löschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Equal(0, geloescht);

        Knopf(cut, "Typ Löschen").Click();
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(1, geloescht);
    }

    [Fact]
    public void Ein_Katalogtyp_wird_nicht_geloescht()
    {
        bool geloescht = false;
        var cut = Aufbauen(lies: n => Typ(n, 8, aenderbar: false),
                           loeschen: _ => { geloescht = true; return true; });

        Knopf(cut, "Typ Löschen").Click();
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.False(geloescht);
        Assert.Contains("vom Softwarehersteller", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Bild und Tastatur
    // =================================================================================

    [Fact]
    public void Das_Bild_bekommt_24_Werte()
    {
        int laenge = 0;
        var cut = Aufbauen(bild: w => { laenge = w.Length; return new byte[] { 9 }; });

        Assert.Equal(24, laenge);
        Assert.Single(cut.FindAll("img.epos-chartbild"));
    }

    [Fact]
    public void Esc_schliesst_Enter_nicht()
    {
        int gemeldet = 0;
        var cut = Aufbauen(geschlossen: _ => gemeldet++);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, gemeldet);
    }
}
