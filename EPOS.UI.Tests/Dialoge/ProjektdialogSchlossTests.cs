using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Das Schloss im Projektdialog</b> (Entscheid AD-Q15, Anwendermeldung vom 08.10.2026 zur
/// „Verwaltung BHKW"): Die Katalogliste des Projektdialogs bietet „Schloss setzen…" und
/// „Schloss aufheben…" über denselben Baustein wie die Verwaltungen
/// (<see cref="Katalogschloss"/> → <see cref="Schlossumschaltung"/>). Ein gesperrter Satz
/// ist nur lesbar, bis das Schloss aufgehoben ist; die Verwendung im Projekt sperrt
/// weder Bearbeiten noch Löschen noch Entsperren — das Projekt führt eine eigene Kopie.
/// </summary>
public class ProjektdialogSchlossTests : EposBunitContext
{
    private readonly Katalogfilterstand _filterstand = new();

    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Bhkw,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>Zwei Katalogsätze: „Modul A" (Id 21) und „Modul B" (Id 22).</summary>
    private static IReadOnlyList<Katalogfilterzeile> Zeilen() => new[]
    {
        new Katalogfilterzeile(21, "Modul A").MitText(Katalogfilterprofil.SpBezeichner, "Modul A"),
        new Katalogfilterzeile(22, "Modul B").MitText(Katalogfilterprofil.SpBezeichner, "Modul B"),
    };

    public ProjektdialogSchlossTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static List<BrowserFeldwert> Felder() => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldFirma, Bezeichnung = "Hersteller:",
            Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Musterwerk"
        },
    };

    /// <summary>
    /// Der Dialog mit „Modul A" als Projektzeile — der Katalogsatz A ist also IM PROJEKT
    /// VERWENDET. Die Katalogzeilen tragen das Schloss nach dem Stand der Prüfung.
    /// </summary>
    private IRenderedComponent<BhkwDialog> Aufbauen(
        Schlosspruefung pruefung, bool mitSchloss = true,
        Func<int, string>? katalogLoeschen = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null)
    {
        IReadOnlyList<Katalogfilterzeile> zeilen = Zeilen();
        return Render<BhkwDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>
            {
                new() { Schluessel = 1, Bezeichner = "Modul A", GeraetId = 100, CarrierId = 5 }
            })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, () => pruefung.Markieren(zeilen))
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, n => new ErzeugerDetail(n, "", Array.Empty<(string, string)>()))
            .Add(x => x.ProjektDetail, n => new ErzeugerDetail(n, "", Array.Empty<(string, string)>()))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => ""))
            .Add(x => x.EditorGaben, editorGaben ?? (_ => new Dictionary<string, object>()))
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern ??
                 ((n, _) => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n)))
            .Add(x => x.Schloss, mitSchloss ? pruefung.Weg() : null));
    }

    private static void KatalogsatzWaehlen(IRenderedComponent<BhkwDialog> cut, int index)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[index].Click();

    private static AngleSharp.Dom.IElement Schlossknopf(IRenderedComponent<BhkwDialog> cut)
        => cut.Find("button.epos-katalogschloss");

    private static AngleSharp.Dom.IElement Knopf(IRenderedComponent<BhkwDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>„Bearbeiten…" im Katalogfuss.</summary>
    private static AngleSharp.Dom.IElement KatalogBearbeiten(IRenderedComponent<BhkwDialog> cut)
        => cut.Find(".epos-knopf--bearbeiten-katalog");

    /// <summary>
    /// UeS2: Speichern steht, wenn die Satz-Überlagerung („Bearbeiten" der Detailzeile) ein OK
    /// trägt — sie wird dafür geöffnet und wieder geschlossen.
    /// </summary>
    private static bool SpeichernSteht(IRenderedComponent<BhkwDialog> cut)
    {
        SatzOeffnen(cut);
        bool steht = cut.FindAll(".epos-satzueberlagerung-ok").Count > 0;
        cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen")[0].Click();
        return steht;
    }

    /// <summary>UeS2: „Alle Daten" stehen in der Satz-Überlagerung — „Bearbeiten" der Detailzeile öffnet sie.</summary>
    private static void SatzOeffnen(IRenderedComponent<BhkwDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten").Click();
    }

    [Fact]
    public void Die_BHKW_Verwaltung_zeigt_Schloss_setzen_und_Schloss_aufheben()
    {
        var pruefung = new Schlosspruefung(21);
        var cut = Aufbauen(pruefung);

        // Ohne gewählten Katalogsatz steht der Knopf, ist aber gesperrt.
        Assert.True(Schlossknopf(cut).HasAttribute("disabled"));

        KatalogsatzWaehlen(cut, 0);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN, Schlossknopf(cut).TextContent.Trim());
        Assert.False(Schlossknopf(cut).HasAttribute("disabled"));

        KatalogsatzWaehlen(cut, 1);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_SETZEN, Schlossknopf(cut).TextContent.Trim());
    }

    [Fact]
    public void Ohne_Schlossweg_steht_kein_Knopf()
    {
        var cut = Aufbauen(new Schlosspruefung(), mitSchloss: false);
        KatalogsatzWaehlen(cut, 0);
        Assert.Empty(cut.FindAll("button.epos-katalogschloss"));
    }

    [Fact]
    public void Ein_gesperrter_Satz_ist_nur_lesbar_und_nach_Schloss_aufheben_bearbeitbar()
    {
        var pruefung = new Schlosspruefung(21);
        string? gespeichert = null;
        var cut = Aufbauen(pruefung, speichern: (n, _) =>
        {
            gespeichert = n;
            return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
        });

        KatalogsatzWaehlen(cut, 0);

        // Gesperrt: kein Speichern im Aufklapper; „Bearbeiten…" im Katalogfuss bleibt
        // bedienbar, nennt den Grund und öffnet keinen Katalogeditor (KA-E-13, 4.6) -
        // sondern die Satz-Ueberlagerung nur lesend mit „Erst Schloss aufheben" (UeS1).
        Assert.False(SpeichernSteht(cut));
        var bearbeiten = KatalogBearbeiten(cut);
        Assert.False(bearbeiten.HasAttribute("disabled"));
        Assert.Equal(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, bearbeiten.GetAttribute("title"));
        bearbeiten.Click();
        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        cut.Find(".epos-satzueberlagerung-schliessen").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);

        // „Schloss aufheben…" - obwohl der Satz im Projekt verwendet wird.
        Schlossknopf(cut).Click();
        Assert.True(cut.Instance.Schlossfrage);
        Assert.Empty(pruefung.Aufrufe);
        Schlosspruefung.Ja(cut);

        Assert.Single(pruefung.Aufrufe);
        Assert.Equal(new[] { 21 }, pruefung.Aufrufe[0].Ids);
        Assert.False(pruefung.Aufrufe[0].Gesperrt);
        Assert.False(cut.Instance.Schlossfrage);

        // Entsperrt: Beschriftung wechselt, Speichern steht, „Bearbeiten…" ohne Sperrgrund.
        Assert.Equal(Resource.ADM_AW_SCHLOSS_SETZEN, Schlossknopf(cut).TextContent.Trim());
        Assert.True(SpeichernSteht(cut));
        Assert.Equal(Resource.AUSWAHL_BTN_BEARBEITEN_KATALOG_HINWEIS, KatalogBearbeiten(cut).GetAttribute("title"));

        SatzOeffnen(cut);
        cut.Find(".epos-modulparameter input").Input("Anderes Werk");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.Equal("Modul A", gespeichert);

        // Ein einzelner ungesperrter Satz öffnet den vollen Katalogeditor (KA-E-13).
        KatalogBearbeiten(cut).Click();
        Assert.True(cut.Instance.EditorOffen);
    }

    [Fact]
    public void Ein_im_Projekt_verwendeter_entsperrter_Satz_laesst_sich_speichern_und_loeschen()
    {
        var geloescht = new List<int>();
        string? gespeichert = null;
        var cut = Aufbauen(new Schlosspruefung(),
            katalogLoeschen: id => { geloescht.Add(id); return ""; },
            speichern: (n, _) => { gespeichert = n; return new KatalogSpeicherErgebnis(true, "ok", n); });

        KatalogsatzWaehlen(cut, 0);       // „Modul A" steht in der Projektliste

        SatzOeffnen(cut);
        cut.Find(".epos-modulparameter input").Input("Anderes Werk");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.Equal("Modul A", gespeichert);

        Knopf(cut, "Löschen").Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal(new[] { 21 }, geloescht);
    }

    [Fact]
    public void Schloss_setzen_macht_den_Satz_nur_lesbar()
    {
        var pruefung = new Schlosspruefung();
        var cut = Aufbauen(pruefung);

        KatalogsatzWaehlen(cut, 1);
        Assert.True(SpeichernSteht(cut));

        Schlossknopf(cut).Click();
        Schlosspruefung.Ja(cut);

        Assert.True(pruefung.Aufrufe.Single().Gesperrt);
        Assert.False(SpeichernSteht(cut));
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN, Schlossknopf(cut).TextContent.Trim());
    }

    [Fact]
    public void Ungespeicherte_Felder_halten_die_Schlossfrage_auf()
    {
        var pruefung = new Schlosspruefung();
        var cut = Aufbauen(pruefung);

        KatalogsatzWaehlen(cut, 1);
        // UeS2: geaendert wird in der Satz-Ueberlagerung; der Riegel gilt fuer jeden Weg zum Schloss.
        SatzOeffnen(cut);
        cut.Find(".epos-modulparameter input").Input("Anderes Werk");
        Schlossknopf(cut).Click();

        Assert.False(cut.Instance.Schlossfrage);
        Assert.Equal(Resource.ADM_MSG_UNGESPEICHERT, cut.Instance.Meldung);
        Assert.Empty(pruefung.Aufrufe);
    }

    [Fact]
    public void Esc_schliesst_nicht_solange_die_Schlossfrage_steht()
    {
        bool geschlossen = false;
        var pruefung = new Schlosspruefung(21);
        IReadOnlyList<Katalogfilterzeile> zeilen = Zeilen();
        var cut = Render<BhkwDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, () => pruefung.Markieren(zeilen))
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, n => new ErzeugerDetail(n, "", Array.Empty<(string, string)>()))
            .Add(x => x.Schloss, pruefung.Weg())
            .Add(x => x.Geschlossen, _ => geschlossen = true));

        KatalogsatzWaehlen(cut, 0);
        Schlossknopf(cut).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(geschlossen);
        Assert.True(cut.Instance.Schlossfrage);
    }
}
