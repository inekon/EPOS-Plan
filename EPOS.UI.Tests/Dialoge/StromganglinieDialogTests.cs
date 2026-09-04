using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Zuordnung von Stromganglinien zum Projekt (iU9-W12.5), Vorbild
/// <c>Views/Stromverbraucher/Form_Stromganglinie</c>.
///
/// <para>Soll ist die Feldkarte (7 Zeilen): zwei Listen, „◀"/„▶", „Bearbeiten…",
/// „OK", „Abbrechen". Geprüft werden dazu die A-Zeile (Entfernen über die ZEILE,
/// nicht über den Namen), der wörtlich übernommene Befund W12-B5 (keine
/// Dublettenprüfung) und die Verwaltung als Überlagerung.</para>
/// </summary>
public class StromganglinieDialogTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    public StromganglinieDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        base.Dispose(disposing);
    }

    private static List<GanglinienKatalogZeile> Katalog() => new()
    {
        new GanglinienKatalogZeile("Werk Nord", 4, false),
        new GanglinienKatalogZeile("Auslieferung", 1, true)
    };

    /// <summary>Ein Satz Gaben für die Verwaltung — mehr als „nicht null" braucht der Test nicht.</summary>
    private static IReadOnlyDictionary<string, object> VerwaltungsGaben() =>
        new Dictionary<string, object>
        {
            ["Katalog"] = new Func<Task<List<GanglinienKatalogZeile>>>(
                () => Task.FromResult(Katalog()))
        };

    private IRenderedComponent<StromganglinieDialog> Zeige(
        List<GanglinienProjektZeile>? zeilen = null,
        Func<Task<List<GanglinienKatalogZeile>>>? katalog = null,
        IReadOnlyDictionary<string, object>? verwaltung = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
    {
        return Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, katalog ?? (() => Task.FromResult(Katalog())))
            .Add(x => x.Verwaltung, verwaltung ?? VerwaltungsGaben())
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement Spalte(IRenderedComponent<StromganglinieDialog> cut, int i)
        => cut.FindAll(".epos-auswahlspalte")[i];

    private static IHtmlCollection<IElement> Zeilen(IRenderedComponent<StromganglinieDialog> cut, int spalte)
        => Spalte(cut, spalte).QuerySelectorAll("tbody tr");

    private static void Waehle(IRenderedComponent<StromganglinieDialog> cut, int spalte, int zeile)
        => Zeilen(cut, spalte)[zeile].QuerySelector("button")!.Click();

    private static IElement Hinzu(IRenderedComponent<StromganglinieDialog> cut)
        => cut.FindAll(".epos-auswahlpfeile button")[0];

    private static IElement Entfernen(IRenderedComponent<StromganglinieDialog> cut)
        => cut.FindAll(".epos-auswahlpfeile button")[1];

    private static IElement Fussknopf(IRenderedComponent<StromganglinieDialog> cut, int i)
        => cut.FindAll(".epos-dialog > .epos-leiste button")[i];

    // =====================================================================
    // Feldbestand (Feldkarte Form_Stromganglinie, 7 Zeilen)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_zwei_Listen_zwei_Pfeile_und_drei_Knoepfe()
    {
        var cut = Zeige();

        Assert.Equal("Stromganglinien", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2, cut.FindAll(".epos-auswahlspalte").Count);
        Assert.Equal(2, cut.FindAll(".epos-auswahlpfeile button").Count);
        Assert.Equal("◀", Hinzu(cut).TextContent);
        Assert.Equal("▶", Entfernen(cut).TextContent);

        // "Bearbeiten..." steht unter der Katalogliste, OK und Abbrechen im Fuss.
        Assert.Contains("Bearbeiten", Spalte(cut, 1).QuerySelector(".epos-leiste button")!.TextContent);
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);

        Assert.Contains("Ausgewählt im Projekt", Spalte(cut, 0).QuerySelector("h2")!.TextContent);
        Assert.Contains("Stromganglinie aus DB", Spalte(cut, 1).QuerySelector("h2")!.TextContent);
    }

    /// <summary>
    /// Der Katalog kommt über den Delegaten und steht nach dem ersten Zeichnen —
    /// der Vorläufer las ihn im Konstruktor (<c>StromganglinieStammCtrl.ReadAll</c>).
    /// </summary>
    [Fact]
    public void Der_Katalog_wird_beim_Aufbau_geladen()
    {
        var cut = Zeige();

        Assert.Equal(2, Zeilen(cut, 1).Length);
        Assert.Contains("Werk Nord", Zeilen(cut, 1)[0].TextContent);
        Assert.Empty(Zeilen(cut, 0));
    }

    /// <summary>Ohne Gaben bleibt „Bearbeiten…" weg — dieselbe Regel wie bei Dateiwahl und Sprung.</summary>
    [Fact]
    public void Ohne_Verwaltung_bleibt_der_Bearbeiten_Knopf_weg()
    {
        var cut = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalog, () => Task.FromResult(Katalog())));

        Assert.Null(Spalte(cut, 1).QuerySelector(".epos-leiste button"));
    }

    // =====================================================================
    // Hinzufuegen und Entfernen
    // =====================================================================

    /// <summary>
    /// Beide Pfeile sind gesperrt, solange in ihrer Quellspalte nichts markiert ist.
    /// </summary>
    [Fact]
    public void Die_Pfeile_bleiben_ohne_Markierung_gesperrt()
    {
        var cut = Zeige();

        Assert.True(Hinzu(cut).HasAttribute("disabled"));
        Assert.True(Entfernen(cut).HasAttribute("disabled"));

        Waehle(cut, 1, 0);
        Assert.False(Hinzu(cut).HasAttribute("disabled"));
        Assert.True(Entfernen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// „◀" legt die Zuordnung an — mit dem Zähler ab 100000 des Vorläufers („noch
    /// nicht gespeichert, also noch unbekannt") und ohne Ganglinien-Id; die holt die
    /// Hülle beim Zurückschreiben über den Bezeichner.
    /// </summary>
    [Fact]
    public void Hinzufuegen_legt_die_Zuordnung_mit_dem_Schluessel_ab_100000_an()
    {
        List<GanglinienProjektZeile> liste = new();
        var cut = Zeige(liste);

        Waehle(cut, 1, 0);
        Hinzu(cut).Click();

        Assert.Single(liste);
        Assert.Equal(StromganglinieDialog.StartIndex, liste[0].Schluessel);
        Assert.Equal(0, liste[0].GanglinieId);
        Assert.Equal("Werk Nord", liste[0].Bezeichner);
        Assert.Single(Zeilen(cut, 0));
    }

    /// <summary>
    /// <b>Befund W12-B5, wörtlich übernommen.</b> Der Vorläufer prüfte beim
    /// Hinzufügen nicht auf Dubletten; derselbe Katalogeintrag geht beliebig oft.
    /// Ob das so bleiben soll, ist eine Anwenderfrage.
    /// </summary>
    [Fact]
    public void Derselbe_Katalogeintrag_laesst_sich_mehrfach_zuordnen()
    {
        List<GanglinienProjektZeile> liste = new();
        var cut = Zeige(liste);

        Waehle(cut, 1, 0);
        Hinzu(cut).Click();
        Waehle(cut, 1, 0);
        Hinzu(cut).Click();

        Assert.Equal(2, liste.Count);
        Assert.Equal(StromganglinieDialog.StartIndex, liste[0].Schluessel);
        Assert.Equal(StromganglinieDialog.StartIndex + 1, liste[1].Schluessel);
    }

    /// <summary>
    /// <b>A-Zeile.</b> „▶" entfernt die GEWÄHLTE Zeile. Der Vorläufer suchte die
    /// erste Zeile mit demselben Namen (<c>btn_Entfernen_Click</c> :89) — bei zwei
    /// gleich benannten Zuordnungen also nicht die markierte.
    /// </summary>
    [Fact]
    public void Entfernen_trifft_die_gewaehlte_Zeile_und_nicht_den_ersten_Namensvetter()
    {
        List<GanglinienProjektZeile> liste = new()
        {
            new GanglinienProjektZeile(7, 3, "Werk Nord"),
            new GanglinienProjektZeile(8, 3, "Werk Nord")
        };
        var cut = Zeige(liste);

        Waehle(cut, 0, 1);
        Entfernen(cut).Click();

        Assert.Single(liste);
        Assert.Equal(7, liste[0].Schluessel);
    }

    /// <summary>Eine Wahl in der einen Spalte hebt die Wahl in der anderen auf.</summary>
    [Fact]
    public void Die_beiden_Listen_teilen_sich_eine_Markierung_nicht()
    {
        var cut = Zeige(new List<GanglinienProjektZeile>
        {
            new GanglinienProjektZeile(7, 3, "Werk Nord")
        });

        Waehle(cut, 0, 0);
        Assert.NotNull(cut.Instance.Projektzeile);
        Assert.Null(cut.Instance.Katalogzeile);

        Waehle(cut, 1, 0);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.NotNull(cut.Instance.Katalogzeile);
    }

    // =====================================================================
    // Verwaltung als Ueberlagerung
    // =====================================================================

    /// <summary>
    /// „Bearbeiten…" zeigt die Verwaltung als Überlagerung — kein zweites Fenster,
    /// keine zweite WebView (Risiko R2).
    /// </summary>
    [Fact]
    public void Bearbeiten_zeigt_die_Verwaltung_als_Ueberlagerung()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Spalte(cut, 1).QuerySelector(".epos-leiste button")!.Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Esc schließt die OBERSTE Ebene: Steht die Verwaltung offen, meldet der Wirt
    /// nichts.
    /// </summary>
    [Fact]
    public void Esc_meldet_nichts_solange_die_Verwaltung_offen_ist()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Spalte(cut, 1).QuerySelector(".epos-leiste button")!.Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    // =====================================================================
    // Schluss
    // =====================================================================

    [Fact]
    public void OK_meldet_true_Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Fussknopf(cut, 1).Click();
        Assert.True(ergebnis);

        Fussknopf(cut, 0).Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>
    /// Assistentenbetrieb (Vorbereitung für W16, Zwilling <c>Wizard_Stromlastgang</c>):
    /// keine Schlussleiste, und Esc meldet nichts — die Knöpfe stellt der Assistent.
    /// </summary>
    [Fact]
    public void Im_Assistentenbetrieb_faellt_die_Schlussleiste_weg()
    {
        bool? ergebnis = null;
        var cut = Zeige(wizard: true, geschlossen: b => ergebnis = b);

        Assert.Empty(cut.FindAll(".epos-dialog > .epos-leiste"));
        Assert.Equal(2, cut.FindAll(".epos-auswahlpfeile button").Count);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }
}
