using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalog der Kostenfaktoren (iU9-W1.5). Soll ist die Feldkarte von
/// <c>Form_KostenAdmin</c>: Liste, "Neu", "Löschen", "OK" und der
/// Einleitungssatz — dazu das Textfeld, das frueher der Unterdialog
/// <c>Form_KostenItemNeu</c> war.
/// </summary>
public class KostenfaktorKatalogDialogTests : BunitContext
{
    private static KostenfaktorKatalogDialog.KostenfaktorZeile Z(int id, string name) => new(id, name);

    private static readonly KostenfaktorKatalogDialog.KostenfaktorZeile[] Bestand =
    {
        Z(3, "Montage"),
        Z(7, "Wartung")
    };

    public KostenfaktorKatalogDialogTests()
    {
        // QuickGrid (im Raster) laedt beim ersten Zeichnen ein JS-Modul.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KostenfaktorKatalogDialog> Aufbauen(
        Action? beimSchliessen = null,
        Func<string, int>? neu = null,
        Func<int, (bool Erfolg, string Grund)>? loeschen = null,
        Func<IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>>? neuLaden = null)
    {
        return Render<KostenfaktorKatalogDialog>(p => p
            .Add(x => x.Zeilen, Bestand)
            .Add(x => x.Neu, neu ?? (_ => 11))
            .Add(x => x.Loeschen, loeschen ?? (_ => (true, "")))
            .Add(x => x.NeuLaden, neuLaden ?? (() => Bestand))
            .Add(x => x.Geschlossen, () => beimSchliessen?.Invoke()));
    }

    /// <summary>Die Loeschfrage — der Baustein <c>Rueckfrage</c> im Dialog.</summary>
    private static IRenderedComponent<EPOS.UI.Bausteine.Rueckfrage> Loeschfrage(
        IRenderedComponent<KostenfaktorKatalogDialog> cut)
        => cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();

    /// <summary>
    /// Antwortet auf die Loeschfrage. Die Knoepfe werden ueber ihre STELLUNG gewaehlt
    /// (Ja zuerst, dann Nein), nicht ueber ihren Text: Der kommt aus
    /// <c>Resource.ALLG_BTN_JA/_NEIN</c> und haengt damit an der Kultur des Laeufers,
    /// die diese Klasse bewusst nicht pinnt.
    /// </summary>
    private static void Antworten(IRenderedComponent<KostenfaktorKatalogDialog> cut, bool ja)
        => Loeschfrage(cut).FindAll(".epos-rueckfrage .epos-leiste button")[ja ? 0 : 1].Click();

    /// <summary>Markiert die erste Zeile und drueckt „Löschen" — bis zur Frage.</summary>
    private static void LoeschenDruecken(IRenderedComponent<KostenfaktorKatalogDialog> cut, int zeile = 0)
    {
        cut.FindAll(".epos-anlagenwahl")[zeile].Click();
        cut.FindAll(".epos-leiste button.epos-knopf")[0].Click();
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen();

        Assert.Equal("Administration Kostenfaktoren", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Verwalten Sie hier die Kostenfaktoren", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Single(cut.FindAll("input[type=text]"));            // Bezeichner (frueher Unterdialog)
        Assert.Single(cut.FindAll(".epos-raster"));                // die Liste
        // Neu, zwei Wahlknoepfe der Zeilen, Löschen, OK.
        Assert.Equal(5, cut.FindAll("button.epos-knopf:not(.epos-dialog-zu)").Count);
    }

    [Fact]
    public void Die_Liste_zeigt_den_Bestand()
    {
        var cut = Aufbauen();

        var zellen = cut.FindAll(".epos-raster tbody td");
        Assert.Equal(4, zellen.Count);                             // 2 Zeilen x (Wahl + Bezeichnung)
        Assert.Equal("Montage", zellen[1].TextContent.Trim());
        Assert.Equal("Wartung", zellen[3].TextContent.Trim());
    }

    [Fact]
    public void Ohne_Markierung_ist_Loeschen_gesperrt()
    {
        // btnDeleteKostenfaktor_Click: SelectedItems.Count == 0 -> return.
        var cut = Aufbauen();

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.True(cut.FindAll(".epos-leiste button.epos-knopf")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Wahlspalte_markiert_eine_Zeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(7, cut.Instance.Gewaehlt);
        Assert.Equal("true", cut.FindAll(".epos-anlagenwahl")[1].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Ein_leerer_Name_legt_nichts_an()
    {
        // btnNeuKostenfaktor_Click: neueBezeichnung.Length == 0 -> return.
        bool gerufen = false;
        var cut = Aufbauen(neu: _ => { gerufen = true; return 1; });

        cut.Find("input[type=text]").Input("   ");
        cut.Find(".epos-neuzeile button.epos-knopf").Click();

        Assert.False(gerufen);
    }

    [Fact]
    public void Neu_legt_an_leert_das_Feld_und_laedt_die_Liste_neu()
    {
        string? erhalten = null;
        var bestand = new List<KostenfaktorKatalogDialog.KostenfaktorZeile>(Bestand);
        var cut = Aufbauen(
            neu: name => { erhalten = name; bestand.Add(Z(11, name)); return 11; },
            neuLaden: () => bestand);

        cut.Find("input[type=text]").Input("  Gerüst  ");
        cut.Find(".epos-neuzeile button.epos-knopf").Click();

        Assert.Equal("Gerüst", erhalten);
        Assert.Equal("", cut.Find("input[type=text]").GetAttribute("value"));
        Assert.Equal(3, cut.Instance.Angezeigt.Count);
        Assert.Equal(11, cut.Instance.Gewaehlt);   // der neue Satz ist markiert
    }

    [Fact]
    public void Ein_gescheitertes_Anlegen_meldet_sich_als_Warnbanner()
    {
        // Frueher eine MessageBox ("Der Kostenfaktor konnte nicht angelegt werden.").
        var cut = Aufbauen(neu: _ => 0);

        cut.Find("input[type=text]").Input("Gerüst");
        cut.Find(".epos-neuzeile button.epos-knopf").Click();

        Assert.Equal("Der Kostenfaktor konnte nicht angelegt werden.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    /// <summary>
    /// W-E2, Befund 04/B17: „Löschen" schreibt nicht mehr sofort. Es stellt die
    /// Frage IM Fenster (Baustein <c>Rueckfrage</c> statt MessageBox der Hülle),
    /// nennt den Namen und hebt „Nein" hervor (A-1).
    /// </summary>
    [Fact]
    public void Loeschen_fragt_erst_und_nennt_den_Namen()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return (true, ""); });

        LoeschenDruecken(cut);

        var frage = Loeschfrage(cut);
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Equal("Kostenfaktor 'Montage' wirklich löschen?", frage.Instance.Frage);
        Assert.False(geloescht);                       // der Knopf fragt nur

        // A-1: Der hervorgehobene Knopf ist "Nein", nicht "Ja".
        var knoepfe = frage.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassName);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);

        Antworten(cut, ja: true);
        Assert.True(geloescht);
    }

    [Fact]
    public void Ein_Nein_in_der_Rueckfrage_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return (true, ""); });

        LoeschenDruecken(cut);
        Antworten(cut, ja: false);

        Assert.False(geloescht);
        Assert.False(Loeschfrage(cut).Instance.Offen);
        Assert.Equal(3, cut.Instance.Gewaehlt);        // die Zeile bleibt markiert
    }

    [Fact]
    public void Loeschen_meldet_die_Id_der_markierten_Zeile()
    {
        // Abweichung zum Vorlaeufer: Er loeschte ueber die Bezeichnung.
        int erhalten = 0;
        var bestand = new List<KostenfaktorKatalogDialog.KostenfaktorZeile>(Bestand);
        var cut = Aufbauen(
            loeschen: id => { erhalten = id; bestand.RemoveAll(z => z.StammId == id); return (true, ""); },
            neuLaden: () => bestand);

        LoeschenDruecken(cut, zeile: 1);
        Antworten(cut, ja: true);

        Assert.Equal(7, erhalten);
        Assert.Single(cut.Instance.Angezeigt);
        Assert.Null(cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// Ohne Loeschdelegat steht die Frage gar nicht erst: Eine Frage, deren „Ja"
    /// nichts tut, waere eine Luege.
    /// </summary>
    [Fact]
    public void Ohne_Loeschdelegat_wird_nicht_gefragt()
    {
        var cut = Render<KostenfaktorKatalogDialog>(p => p
            .Add(x => x.Zeilen, Bestand));

        LoeschenDruecken(cut);

        Assert.False(Loeschfrage(cut).Instance.Offen);
    }

    /// <summary>
    /// Eine offene Rueckfrage faengt Esc ab — der Katalog darf nicht unter der
    /// Frage wegschliessen (Muster <c>GesetzeskatalogDialog</c>).
    /// </summary>
    [Fact]
    public void Esc_schliesst_nicht_solange_die_Loeschfrage_steht()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        LoeschenDruecken(cut);
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(0, gemeldet);

        // Nach der Antwort schliesst Esc wieder.
        Antworten(cut, ja: false);
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, gemeldet);
    }

    /// <summary>
    /// AUFTRAG #302: Ein benutzter Kostenfaktor bleibt stehen, und der BENANNTE Grund
    /// des Kerns erscheint als Warnbanner — nicht die allgemeine Meldung. Die Zeile
    /// bleibt in der Liste und bleibt markiert, damit der Anwender sieht, worum es geht.
    /// </summary>
    [Fact]
    public void Ein_benutzter_Kostenfaktor_meldet_den_Grund_und_bleibt_in_der_Liste()
    {
        const string Grund = "6 Projektposition(en) in 5 Projekt(en) und 0 " +
                             "Vorlagenposition(en) verweisen auf diesen Kostenfaktor.";
        var cut = Aufbauen(loeschen: _ => (false, Grund));

        LoeschenDruecken(cut);
        Antworten(cut, ja: true);

        Assert.Equal(Grund, cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Equal(2, cut.Instance.Angezeigt.Count);
        Assert.Equal(3, cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// Bleibt der Grund leer, steht die allgemeine Meldung da — der Rueckfall, den es
    /// vor Auftrag #302 als einzige Antwort gab.
    /// </summary>
    [Fact]
    public void Ohne_benannten_Grund_steht_die_allgemeine_Meldung()
    {
        var cut = Aufbauen(loeschen: _ => (false, ""));

        LoeschenDruecken(cut);
        Antworten(cut, ja: true);

        Assert.Equal("Der Kostenfaktor konnte nicht gelöscht werden.",
                     cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Equal(2, cut.Instance.Angezeigt.Count);
    }

    [Fact]
    public void OK_und_Esc_schliessen_den_Dialog()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(1, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);

        // Enter bleibt unbelegt (A-7): "Neu" und "Löschen" schreiben sofort.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, gemeldet);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf schliesst wie OK/Esc.</summary>
    [Fact]
    public void Das_Kreuz_schliesst_den_Dialog()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_kein_Kreuz()
    {
        var cut = Render<KostenfaktorKatalogDialog>(p => p
            .Add(x => x.Zeilen, Bestand)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_KostenAdmin.btn_Help" }, hilfe.Geoeffnet);
    }
}
