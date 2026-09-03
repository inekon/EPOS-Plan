using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmebedarf extern (iU9-W9.4). Soll ist die Feldkarte von
/// <c>Form_Waermebedarf</c>: 11 Zeilen — zwei Listen, zwei Pfeile, drei Knöpfe — plus das
/// KANALFELD, das der Vorläufer zur Laufzeit anlegte
/// (<c>KanalControlsAufbauen</c>:72-116).
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class WaermebedarfExternDialogTests : BunitContext
{
    private static readonly string[] KATALOG = { "Ganglinie A", "Ganglinie B", "Ganglinie C" };

    private static readonly (string Wert, string Text)[] KANAELE =
    {
        ("HEIZUNG", "Heizung"),
        ("BRAUCHWASSER", "Brauchwasser"),
        ("PROZESS", "Prozesswärme")
    };

    public WaermebedarfExternDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermebedarfExternZeile Zeile(int idZ, string name = "Ganglinie A",
                                                 string kanal = "HEIZUNG") => new()
    {
        IdZ = idZ,
        IdGanglinie = 5,
        Bezeichner = name,
        Kanal = kanal
    };

    private IRenderedComponent<WaermebedarfExternDialog> Aufbauen(
        List<WaermebedarfExternZeile>? zeilen = null,
        bool wizard = false,
        Func<string, bool>? hatZuordnung = null,
        Func<string, bool>? katalogLoeschen = null,
        Func<string, Task<bool>>? sprung = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null)
        => Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermebedarfExternZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Katalog, () => KATALOG)
            .Add(x => x.Aufnehmen, n => new WaermebedarfExternZeile
            {
                IdZ = 0, IdGanglinie = 9, Bezeichner = n, Kanal = "HEIZUNG"
            })
            .Add(x => x.HatProjektzuordnung, hatZuordnung ?? (_ => false))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.Sprung, sprung)
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermebedarfExternDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Contains("Wärmebedarf Extern", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Wärmebedarfsdaten (Ganglinien)", cut.Markup);
        Assert.Contains("Ausgewählt im Projekt", cut.Markup);
        Assert.Contains("Wärmebedarf aus DB", cut.Markup);

        // Das Kanalfeld ist die einzige Klappliste.
        Assert.Single(cut.FindAll("select"));
        Assert.Contains("Kanal", cut.Markup);

        foreach (string t in new[] { "◀", "▶", "DB Ganglinie löschen", "OK", "Abbrechen" })
            Assert.NotNull(Knopf(cut, t));
    }

    [Fact]
    public void Die_Kanalliste_fuehrt_die_drei_Kanaele()
    {
        var cut = Aufbauen();

        IElement kanal = cut.Find("select");
        Assert.Equal(3, kanal.QuerySelectorAll("option").Length);
        Assert.Contains("Heizung", cut.Markup);
        Assert.Contains("Brauchwasser", cut.Markup);
        Assert.Contains("Prozesswärme", cut.Markup);
    }

    [Fact]
    public void Im_Assistenten_gibt_es_keine_Schlussleiste()
    {
        var cut = Aufbauen(wizard: true);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "OK");
    }

    /// <summary>Ohne Sprungdelegat kein „Einlesen/Bearbeiten.."-Knopf.</summary>
    [Fact]
    public void Ohne_Sprung_gibt_es_keinen_Bearbeitenknopf()
    {
        Assert.DoesNotContain("Einlesen/Bearbeiten..", Aufbauen().Markup);
        Assert.Contains("Einlesen/Bearbeiten..",
                        Aufbauen(sprung: _ => Task.FromResult(true)).Markup);
    }

    // =================================================================================
    // Kanal
    // =================================================================================

    [Fact]
    public void Ohne_markierte_Zeile_ist_die_Kanalliste_gesperrt()
    {
        var cut = Aufbauen(zeilen: new List<WaermebedarfExternZeile>());

        Assert.True(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Kanalwahl_wirkt_auf_die_MARKIERTE_Zeile()
    {
        var zeilen = new List<WaermebedarfExternZeile> { Zeile(1), Zeile(2, "Ganglinie B") };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();   // zweite Projektzeile
        cut.Find("select").Change("1");                      // Brauchwasser

        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
        Assert.Equal("BRAUCHWASSER", zeilen[1].Kanal);
    }

    [Fact]
    public void Ein_unbekannter_Kanal_faellt_auf_Heizung_zurueck()
    {
        var cut = Aufbauen(zeilen: new List<WaermebedarfExternZeile> { Zeile(1, kanal: "XYZ") });

        Assert.Equal("0", cut.Find("select").GetAttribute("value"));
    }

    // =================================================================================
    // Uebernehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_Pfeil_nach_links_legt_eine_neue_Zeile_auf_Heizung_an()
    {
        bool gemeldet = false;
        var zeilen = new List<WaermebedarfExternZeile>();
        var cut = Aufbauen(zeilen: zeilen, geaendert: () => gemeldet = true);

        cut.FindAll("button.epos-anlagenwahl").First().Click();   // erste Katalogzeile
        Knopf(cut, "◀").Click();

        Assert.Single(zeilen);
        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
        Assert.Equal("Ganglinie A", zeilen[0].Bezeichner);
        Assert.True(gemeldet);
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// „▶" trifft die MARKIERTE Zeile. Der Vorläufer nahm die erste Zeile gleichen
    /// Namens — bei zwei Zuordnungen derselben Ganglinie die falsche (A-9).
    /// </summary>
    [Fact]
    public void Der_Pfeil_nach_rechts_trifft_die_markierte_Zeile()
    {
        var zeilen = new List<WaermebedarfExternZeile>
        {
            Zeile(1, "Ganglinie A", "HEIZUNG"),
            Zeile(2, "Ganglinie A", "PROZESS")
        };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "▶").Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].IdZ);
        Assert.Equal("HEIZUNG", zeilen[0].Kanal);
    }

    // =================================================================================
    // Katalog loeschen
    // =================================================================================

    [Fact]
    public void Loeschen_mit_Projektzuordnung_meldet_den_Grund()
    {
        bool geloescht = false;
        var cut = Aufbauen(hatZuordnung: _ => true,
                           katalogLoeschen: _ => { geloescht = true; return true; });

        // Die Projektliste steht im Markup VOR dem Katalog: Bei einer Projektzeile
        // ist der erste Katalog-Wahlknopf der zweite insgesamt.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "DB Ganglinie löschen").Click();

        Assert.False(geloescht);
        Assert.Contains("Projektzuordnung", cut.Instance.Meldung);
    }

    /// <summary>Der Vorläufer löschte auf einen Klick; jetzt wird gefragt (A-8).</summary>
    [Fact]
    public void Loeschen_ohne_Projektzuordnung_fragt_nach()
    {
        string geloescht = "";
        var cut = Aufbauen(katalogLoeschen: n => { geloescht = n; return true; });

        // Die Projektliste steht im Markup VOR dem Katalog: Bei einer Projektzeile
        // ist der erste Katalog-Wahlknopf der zweite insgesamt.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "DB Ganglinie löschen").Click();

        Assert.Contains("wirklich gelöscht", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal("Ganglinie A", geloescht);
    }

    [Fact]
    public void Loeschen_mit_Nein_laesst_alles_stehen()
    {
        bool gerufen = false;
        var cut = Aufbauen(katalogLoeschen: _ => { gerufen = true; return true; });

        // Die Projektliste steht im Markup VOR dem Katalog: Bei einer Projektzeile
        // ist der erste Katalog-Wahlknopf der zweite insgesamt.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "DB Ganglinie löschen").Click();
        Knopf(cut, "Nein").Click();

        Assert.False(gerufen);
    }

    [Fact]
    public void Bearbeiten_springt_in_die_Ganglinienverwaltung()
    {
        string ziel = "";
        var cut = Aufbauen(sprung: s => { ziel = s; return Task.FromResult(true); });

        Knopf(cut, "Einlesen/Bearbeiten..").Click();

        Assert.Equal(Sprungziel.WaermebedarfExternAdmin, ziel);
    }

    // =================================================================================
    // Tastatur und Schlussleiste
    // =================================================================================

    [Fact]
    public void Esc_schliesst_mit_Abbruch()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    [Fact]
    public void OK_meldet_true()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
    }
}
