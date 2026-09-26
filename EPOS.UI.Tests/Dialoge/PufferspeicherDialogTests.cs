using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Pufferspeicher (iU9-W6.7). Soll ist die Feldkarte von
/// <c>Form_PufferSp</c>: zwei Listen, zwei Filter (Hersteller und Volumen), der
/// Detailblock und die Eindeutigkeitsrückfrage vor dem Aufnehmen.
/// </summary>
public class PufferspeicherDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    /// <summary>
    /// Das PROFIL des Projektdialogs (W14a-E-10 / S2.1): dieselben fünf Spalten wie
    /// in der Verwaltung, dazu die sechste „im Projekt verwendet" (Q12).
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Pufferspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(51, "Speicher 600 Ltr")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 600 Ltr")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpSpeichertyp, "stehend")
            .MitZahl(Katalogfilterprofil.SpVolumen, 600.0, 0)
            .MitZahl(Katalogfilterprofil.SpVerluste, 1.8, 2),

        new Katalogfilterzeile(52, "Speicher 800 Ltr")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 800 Ltr")
            .MitText(Katalogfilterprofil.SpHersteller, "Musterwerk")
            .MitText(Katalogfilterprofil.SpSpeichertyp, "stehend")
            .MitZahl(Katalogfilterprofil.SpVolumen, 800.0, 0)
            .MitZahl(Katalogfilterprofil.SpVerluste, 2.1, 2),
    };

    public PufferspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId };

    /// <summary>
    /// Der Detailblock, wie ihn <c>PufferspeicherHuelle.DetailZu</c> baut: Hersteller,
    /// Speichertyp, Bereitschaftsverluste und Gesamtvolumen. Ein Feld
    /// „Investitionskosten" steht nicht darin — gepflegt wird der Preis im Aufklapper
    /// „Alle Daten anzeigen".
    /// </summary>
    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Hersteller:", "Musterwerk"), ("Speichertyp:", "stehend"),
                ("Bereitschaftsverluste:", "1,5"), ("Gesamtvolumen [l]:", "600,0") });

    /// <summary>
    /// Die Felder des Aufklappers „Alle Daten anzeigen" — der Feldsatz des
    /// Pufferspeicherprofils in Kurzform: der Bezeichner nur lesbar, die übrigen
    /// editierbar.
    /// </summary>
    private static List<BrowserFeldwert> Katalogfelder(string name) => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldBezeichner, Bezeichnung = "Name:",
            Art = BrowserFeldArt.Text, Editierbar = false, Wert = name
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldFirma, Bezeichnung = "Hersteller:",
            Art = BrowserFeldArt.Text, Editierbar = true, Wert = "Musterwerk"
        },
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionskosten,
            Bezeichnung = "Investitionskosten:", Einheit = "€",
            Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "2500"
        }
    };

    private IRenderedComponent<PufferspeicherDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, string>? dublettenfrage = null,
        Func<int, bool, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Func<int, ErzeugerDetail?>? projektDetail = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, id => Detail("Speicher " + id))
            .Add(x => x.ProjektDetail, projektDetail ?? (id => Detail("Projektkopie " + id)))
            .Add(x => x.Dublettenfrage, dublettenfrage ?? (_ => ""))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((id, _) => new AufnahmeErgebnis(Zeile(9, "Speicher 800 Ltr", id))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Katalogfelder, katalogfelder)
            .Add(x => x.KatalogfelderSpeichern, felderSpeichern)
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    /// <summary>
    /// Der Knopf mit DIESER Beschriftung — gleich, in welcher Leiste er steht. Über den
    /// TEXT und nicht über den Index: Seit „Bearbeiten…" in den Modulbereich gewandert
    /// ist, träfe jeder Index in der Listenleiste etwas anderes.
    /// </summary>
    private static AngleSharp.Dom.IElement Knopf(
        IRenderedComponent<PufferspeicherDialog> cut, string beschriftung)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == beschriftung);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-uebernahme button").Count);

        // W14a-E-10 / S2.1: Die zwei Klapplisten sind weg - Hersteller und
        // Speichertyp sind Spalten mit Trichter, und "200..500" im Volumenfeld
        // leistet genauer, was die sechs festen Stufen ungefaehr taten.
        Assert.Empty(cut.FindAll("select"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain("Filtern nach Hersteller:", texte);
        Assert.DoesNotContain("Filtern nach Volumen:", texte);
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));

        // Fuenf NUR LESBARE Anzeigefelder: Name, Hersteller, Typ, Verluste, Volumen.
        // Ein Feld „Investitionskosten" steht nicht mehr darunter (Anwenderentscheid
        // 21.09.2026) — gepflegt wird der Preis im Aufklapper „Alle Daten anzeigen".
        Assert.Equal(5, cut.FindAll(".epos-gruppenkopf-koerper input[readonly]").Count);
    }

    /// <summary>
    /// <b>Die sechs festen Volumenstufen sind gefallen</b> (Frage Q5, Stufe S2.1):
    /// „Sortieren nach Volumen und der Ausdruck <c>200..500</c> leisten dasselbe
    /// genauer" — und lösen den Einwand, dass eine Stufe „bis 500 l" für einen
    /// 480‑l‑Speicher jede Zeile darunter mitbringt. An ihre Stelle tritt die
    /// Zahlenspalte mit ihrem Trichter.
    /// </summary>
    [Fact]
    public void Die_Volumenspalte_traegt_einen_Trichter_statt_sechs_Stufen()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains(kopf, k => k.Contains("[l]"));     // die Volumenspalte, Kopf „V [l]"
        Assert.Equal(Profil.Spalten.Count + 1, kopf.Count);

        // Fuenf Trichter: alles ausser der Wahlspalte und dem Kennzeichen Q12.
        Assert.Equal(Profil.Spalten.Count(x => x.Filterbar), cut.FindAll(".epos-trichter").Count);
    }

    /// <summary>
    /// Ohne Parametersatz der Speicherverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.4 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Browser braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.KatalogBrowserArt.Pufferspeicher,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.KatalogBrowserWege()
        };

    // =================================================================================
    // Detailquellen
    // =================================================================================

    [Fact]
    public void Eine_Projektzeile_zeigt_ihre_Kopie_ein_Katalogsatz_den_Stamm()
    {
        // Befund 4: Die Projektkopie kann anders heissen als die Vorlage.
        var cut = Aufbauen();

        Assert.Equal("Projektkopie 51",
                     cut.FindAll(".epos-gruppenkopf-koerper input[readonly]")[0].GetAttribute("value"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Equal("Speicher 51",
                     cut.FindAll(".epos-gruppenkopf-koerper input[readonly]")[0].GetAttribute("value"));
    }

    // =================================================================================
    // Die Eindeutigkeitsrueckfrage
    // =================================================================================

    [Fact]
    public void Ein_neues_Geraet_wird_ohne_Rueckfrage_aufgenommen()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.False(cut.Instance.Dublettenwarnung);
        Assert.Equal(2, zeilen.Count);
    }

    [Fact]
    public void Ein_zweites_gleiches_Geraet_loest_die_Rueckfrage_aus()
    {
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen,
            dublettenfrage: _ => "Speicher 600 Ltr steht bereits in der Liste. Trotzdem aufnehmen?",
            aufnehmen: (id, _) => { aufgenommen = true; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();

        Assert.True(cut.Instance.Dublettenwarnung);
        Assert.False(aufgenommen);
        Assert.Contains("steht bereits", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Nein_auf_die_Rueckfrage_fuegt_nichts_hinzu()
    {
        bool aufgenommen = false;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen, dublettenfrage: _ => "steht bereits",
            aufnehmen: (id, _) => { aufgenommen = true; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(aufgenommen);
        Assert.Single(zeilen);
    }

    [Fact]
    public void Ja_auf_die_Rueckfrage_erzwingt_die_Geraetekopie()
    {
        bool? erzwungen = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen, dublettenfrage: _ => "steht bereits",
            aufnehmen: (id, e) => { erzwungen = e; return new AufnahmeErgebnis(Zeile(9, "x", id)); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.True(erzwungen);
        Assert.Equal(2, zeilen.Count);
    }

    // =================================================================================
    // Entfernen und Katalogpflege
    // =================================================================================

    [Fact]
    public void Der_Pfeil_zurueck_trifft_genau_die_gewaehlte_Zeile()
    {
        // Befund 4: Der Vorlaeufer brauchte dafuer eine Parallelliste - Items.Remove(Text)
        // traf bei gleichnamigen Eintraegen immer den ersten.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51),
                                               Zeile(2, "Speicher 600 Ltr", 51) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-uebernahme button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Loeschen_ohne_Katalogwahl_sagt_es()
    {
        // PSP_MELDUNG_MODUL_WAEHLEN - der Vorlaeufer meldete das ebenfalls.
        var cut = Aufbauen();

        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Contains("Modul", cut.Instance.Meldung);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Loeschen_geht_ueber_die_Katalog_Id()
    {
        // V0-9: Der fruehere Weg ueber den Bezeichner traf bei gleichnamigen
        // Katalogeintraegen alle Namensvettern auf einmal.
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(new[] { 52 }, geloescht);
    }

    /// <summary>
    /// „Bearbeiten…" öffnet die Speicherverwaltung als ÜBERLAGERUNG im selben
    /// Fenster — bis iU9-W14a war es ein Sprung in ein zweites Fenster
    /// (<c>Sprungziel.PufferSpAdmin</c>).
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Speicherverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);
        Knopf(cut, "Bearbeiten...").Click();

        Assert.True(cut.Instance.VerwaltungOffen);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// <b>„Bearbeiten…" steht im MODULBEREICH, „Löschen" bei der Liste</b>
    /// (Anwenderentscheid 15.09.2026, „alle sechs Erzeuger im gleichen Schema"): Der
    /// eine Knopf wirkt auf den gewählten SATZ und gehört deshalb dorthin, wo dieser
    /// Satz steht; der andere wirkt auf die Listenzeile und bleibt bei der Liste.
    /// </summary>
    [Fact]
    public void Bearbeiten_steht_im_Modulbereich_und_Loeschen_bei_der_Liste()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        var listenknoepfe = cut.FindAll(".epos-zweispalten-spalte")[1]
                               .QuerySelectorAll(".epos-leiste button")
                               .Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Löschen" }, listenknoepfe);

        var modulknoepfe = cut.FindAll(".epos-gruppenkopf-koerper")[0]
                              .QuerySelectorAll(".epos-leiste button")
                              .Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Bearbeiten...", modulknoepfe);
    }

    // =================================================================================
    // Der Aufklapper „Alle Daten anzeigen" (Anwenderentscheid 15.09.2026)
    // =================================================================================

    /// <summary>
    /// Ohne Weg zu den Katalogfeldern kein Aufklapper — Hausregel „kein Delegat, kein
    /// Knopf". Und er gehört dem KATALOGsatz: Steht eine Projektzeile, ist er weg.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_steht_nur_mit_Weg_und_nur_am_Katalogsatz()
    {
        var ohne = Aufbauen();
        KatalogZeileWaehlen(ohne, 0);
        Assert.Empty(ohne.FindAll(".epos-modulparameter-knopf"));

        var mit = Aufbauen(katalogfelder: Katalogfelder);
        Assert.Empty(mit.FindAll(".epos-modulparameter-knopf"));   // erste Projektzeile

        KatalogZeileWaehlen(mit, 0);
        Assert.Single(mit.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// <b>Geholt wird mit der WAHL des Satzes</b> (Anwenderentscheid 16.09.2026: „Unter
    /// Bearbeiten sollen alle Parameter angezeigt werden und bearbeitbar sein"). Der
    /// Aufklapper steht offen, sobald ein Katalogsatz gewählt ist — genau EINE Abfrage
    /// je Satz; ohne gewählten Satz gibt es den Block nicht.
    /// </summary>
    [Fact]
    public void Die_Felder_kommen_mit_der_Wahl_des_Satzes()
    {
        int rufe = 0;
        var cut = Aufbauen(katalogfelder: n => { rufe++; return Katalogfelder(n); });

        Assert.Equal(0, rufe);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));

        KatalogZeileWaehlen(cut, 0);

        Assert.Equal(1, rufe);
        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Equal(3, cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld").Length);
    }

    /// <summary>
    /// <b>Zuklappen geht weiterhin</b> — der Knopf bleibt, was er war, nur seine Vorgabe
    /// hat sich gedreht.
    /// </summary>
    [Fact]
    public void Der_Aufklapper_laesst_sich_weiterhin_zuklappen()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Die Knopfzeile steht als ERSTES unter dem Modulkopf</b> (Anwenderentscheid
    /// 16.09.2026: „Der Bearbeiten-Button soll weiter oben … stehen, so dass er besser
    /// sichtbar ist"): links die Kostenknöpfe, rechts „Bearbeiten…", dazwischen der
    /// Füller. Danach erst die Felder, danach der Aufklapper.
    /// </summary>
    [Fact]
    public void Die_Knopfzeile_steht_unmittelbar_unter_dem_Modulkopf()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben(),
                           kostenOeffnen: (_, _) => Task.CompletedTask,
                           katalogfelder: Katalogfelder);
        KatalogZeileWaehlen(cut, 0);

        var kinder = cut.FindAll(".epos-gruppenkopf-koerper")[0].Children.ToList();

        Assert.Contains("epos-leiste", kinder[0].ClassList);
        int raster = kinder.FindIndex(k => k.ClassList.Contains("epos-formularraster"));
        int parameter = kinder.FindIndex(k => k.ClassList.Contains("epos-modulparameter"));
        Assert.True(0 < raster && raster < parameter);

        var teile = kinder[0].Children.ToList();
        Assert.Contains("epos-kostenleiste", teile[0].ClassList);
        Assert.Contains("epos-leiste-fueller", teile[1].ClassList);
        Assert.Equal("Bearbeiten...", teile[2].TextContent.Trim());
    }

    /// <summary>
    /// „Speichern" reicht die GEÄNDERTEN Felder an den Schreibweg — und ist vorher
    /// gesperrt: Ohne Änderung gibt es nichts zu schreiben.
    /// </summary>
    [Fact]
    public void Speichern_reicht_die_geaenderten_Felder_an_den_Schreibweg()
    {
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        string? name = null;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, f) =>
                           {
                               name = n; gesehen = f;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });

        KatalogZeileWaehlen(cut, 0);

        var speichern = Knopf(cut, "Speichern");
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        Assert.False(speichern.HasAttribute("disabled"));

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("3000");
        speichern = Knopf(cut, "Speichern");
        Assert.False(speichern.HasAttribute("disabled"));
        speichern.Click();

        Assert.Equal("Speicher 600 Ltr", name);
        Assert.NotNull(gesehen);
        Assert.Equal("3000",
            gesehen!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
        // Der Erfolg steht am Knopf, nicht als Band im Dialogkopf.
        Assert.Equal("", cut.Instance.Meldung);
        Assert.StartsWith("Gespeichert um ", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf:</b> „Gespeichert um …" steht neben „Speichern", nicht als
    /// Band; ohne Änderung ist der Knopf weich gesperrt, ein Klick nennt den Grund, und
    /// die nächste Eingabe nimmt den Vermerk zurück.
    /// </summary>
    [Fact]
    public void Speichern_meldet_am_Knopf_und_die_Eingabe_nimmt_den_Vermerk_zurueck()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               schreibvorgaenge++;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });
        KatalogZeileWaehlen(cut, 0);

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();

        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.StartsWith("Gespeichert um ", cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);
        Assert.Equal("true", cut.Find(".epos-modulparameter .epos-speichervermerk button")
                                .GetAttribute("aria-disabled"));

        // Ohne Aenderung schreibt ein Klick nicht, er nennt den Grund am Knopf.
        cut.Find(".epos-modulparameter .epos-speichervermerk button").Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("Keine Änderung — es gibt nichts zu speichern.",
                     cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]").TextContent);

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("43");
        Assert.Empty(cut.FindAll(".epos-modulparameter .epos-speichervermerk [role=status]"));
        Assert.False(cut.Find(".epos-modulparameter .epos-speichervermerk button")
                        .HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// Ohne Schreibweg ist der Aufklapper reine ANZEIGE: kein Speichern-Knopf, kein
    /// beschreibbares Feld. Das ist die Lage des Katalogs, keine Entscheidung des
    /// Dialogs — er liest sie am Delegaten ab.
    /// </summary>
    [Fact]
    public void Ohne_Schreibweg_zeigt_der_Aufklapper_nur_an()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder);

        KatalogZeileWaehlen(cut, 0);

        var block = cut.Find(".epos-modulparameter");
        Assert.DoesNotContain(block.QuerySelectorAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Speichern");
        Assert.Empty(block.QuerySelectorAll("input:not([readonly])"));
    }

    /// <summary>
    /// Eine Fehleingabe in einem Zahlenfeld sperrt „Speichern" — der Dialog schriebe
    /// sonst den Stand VOR der angefangenen Zahl zurück.
    /// </summary>
    [Fact]
    public void Eine_Fehleingabe_sperrt_den_Speichern_Knopf()
    {
        bool geschrieben = false;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               geschrieben = true;
                               return new KatalogSpeicherErgebnis(true, "ok", n);
                           });

        KatalogZeileWaehlen(cut, 0);

        // Erst eine gueltige Aenderung - sie gibt den Knopf frei.
        var zahl = cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0];
        zahl.Input("3000");
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));

        // Dann die Fehleingabe: der Knopf geht wieder zu.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("dreitausend");

        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));
        Assert.False(geschrieben);
    }

    /// <summary>
    /// Ein abgelehnter Schreibweg (Schreibschutz der Auslieferung) meldet den Grund und
    /// lässt den geänderten Stand stehen — der Anwender soll ihn verbessern können.
    /// </summary>
    [Fact]
    public void Eine_Ablehnung_meldet_den_Grund()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (_, __) =>
                               new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", ""));

        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("3000");
        Knopf(cut, "Speichern").Click();

        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
        Assert.True(cut.Instance.ParameterOffen);

        // Der Grund steht auch rot AM Knopf.
        var vermerk = cut.Find(".epos-modulparameter .epos-speichervermerk [role=status]");
        Assert.Equal("Schreibgeschützt.", vermerk.TextContent);
        Assert.Contains("epos-status--fehler", vermerk.ClassName);
    }

    // =================================================================================
    // Die Kostenknöpfe im Modulbereich
    // =================================================================================

    /// <summary>
    /// <b>Zwei Knöpfe, kein dritter</b> (Anwenderentscheid 15.09.2026): Investitions-
    /// und Betriebskosten führen in DIESELBE Maske und unterscheiden sich nur im
    /// Schalter; „Energiekosten…" gibt es nicht — ein Speicher verbraucht keinen Träger.
    /// Ohne Delegat fehlt die Leiste ganz.
    /// </summary>
    [Fact]
    public void Die_Kostenleiste_steht_nur_mit_Weg_und_traegt_zwei_Knoepfe()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-kostenleiste button"));

        var gerufen = new List<bool>();
        var cut = Aufbauen(kostenOeffnen: (_, betrieb) =>
        {
            gerufen.Add(betrieb);
            return Task.CompletedTask;
        });

        var knoepfe = cut.FindAll(".epos-kostenleiste button");
        Assert.Equal(2, knoepfe.Count);

        knoepfe[0].Click();
        cut.FindAll(".epos-kostenleiste button")[1].Click();
        Assert.Equal(new[] { false, true }, gerufen);
    }

    /// <summary>Die Kostenleiste bekommt die GEWÄHLTE Projektzeile mit.</summary>
    [Fact]
    public void Die_Kostenleiste_reicht_die_gewaehlte_Projektzeile_durch()
    {
        ErzeugerZeile? gesehen = null;
        var cut = Aufbauen(kostenOeffnen: (zeile, _) =>
        {
            gesehen = zeile;
            return Task.CompletedTask;
        });

        cut.FindAll(".epos-kostenleiste button")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(1, gesehen!.Schluessel);
    }

    /// <summary>Wählt die Katalogzeile mit dieser Nummer in der rechten Liste.</summary>
    private static void KatalogZeileWaehlen(IRenderedComponent<PufferspeicherDialog> cut, int nr)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[nr].Click();

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — es schließt ohne zu speichern und meldet
    /// <c>false</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_bricht_ab_wie_Esc()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }

    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Detailblock des Projektdialogs steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Detailblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);
    }

    // =================================================================================
    //  Stufe S2.1 / S2.3 / S2.5 - Anwenderentscheid W14a-E-10 vom 07.09.2026
    // =================================================================================

    /// <summary>Die Katalogliste - das UNTERE der beiden Raster.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Katalogzeilen(
        IRenderedComponent<PufferspeicherDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr").ToList();

    /// <summary>
    /// <b>S2.1 — der Filter sitzt im SPALTENKOPF.</b> Der Ausdruck steht im
    /// <c>Katalogfilterstand</c> des Wirtes, <c>Katalogfilter.Anwenden</c>
    /// schränkt die Menge im Kern ein, und das Raster bekommt die BEREITS
    /// eingeschränkte Liste (5.6.6 — gefiltert wird vor dem Raster, nie im Raster).
    /// </summary>
    [Fact]
    public void S2_1_Der_Spaltenfilter_schraenkt_die_Katalogliste_ein()
    {
        var cut = Aufbauen();
        Assert.Equal(2, Katalogzeilen(cut).Count);
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Contains("Speicher 800 Ltr", Katalogzeilen(cut)[0].TextContent);

        // Der Trichter dieser Spalte ist jetzt GEFUELLT - im Markup, nicht nur
        // in der Farbe (Auflage des Anwenders zu Rev. 3).
        Assert.Contains(cut.FindAll(".epos-trichter-bild path"),
                        e => e.GetAttribute("fill") == "currentColor");

        // Und der Ruecksetzer der Suchzeile holt alles zurueck.
        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Equal(2, Katalogzeilen(cut).Count);
    }

    /// <summary>
    /// <b>S2.1 — jede Parameterspalte sortiert</b>, im Zyklus auf → ab → aus
    /// (höchstens eine Spalte zugleich, 5.6.2).
    /// </summary>
    [Fact]
    public void S2_1_Die_Katalogliste_laesst_sich_ueber_den_Spaltenkopf_sortieren()
    {
        var cut = Aufbauen();

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
        cut.Render();
        Assert.Contains("Speicher 600 Ltr", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
        cut.Render();
        Assert.False(_filterstand.Aufsteigend);
        Assert.Contains("Speicher 800 Ltr", Katalogzeilen(cut)[0].TextContent);

        _filterstand.Sortieren(Katalogfilterprofil.SpVolumen);
        Assert.Equal("", _filterstand.Sortierspalte);
    }

    /// <summary>
    /// <b>S2.1 — die Markierung hängt am BEZEICHNER.</b> Sie bleibt stehen, auch
    /// wenn ein Filter die Zeile ausblendet; der Übernahmeknopf bleibt frei und
    /// nimmt DENSELBEN Satz auf (Hausregel aus dem <c>EnergietraegerDialog</c>, W4).
    /// </summary>
    [Fact]
    public void S2_1_Die_Markierung_ueberlebt_einen_Filterwechsel()
    {
        var cut = Aufbauen();

        Katalogzeilen(cut)[0].QuerySelector(".epos-anlagenwahl")!.Click();
        Assert.False(cut.FindAll(".epos-zweispalten-uebernahme button")[0].HasAttribute("disabled"));

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-uebernahme button")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>S2.3 / Frage Q12 — „im Projekt verwendet".</b> EINMAL für die ganze Liste
    /// aus der Projektliste des Dialogs gestempelt, nicht je Zeile und nicht aus
    /// der Datenbank: Der Dialog schreibt erst beim OK zurück, eine Zählabfrage
    /// wäre nach der ersten Übernahme veraltet.
    /// </summary>
    [Fact]
    public void S2_3_Die_Spalte_im_Projekt_verwendet_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja"));

        var traegt = zeilen.First(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Contains("Speicher 600 Ltr", traegt.TextContent);
    }

    /// <summary>
    /// <b>S2.5 / Frage Q2 — der Filterstand überlebt Schließen und Öffnen.</b>
    /// Hier steht dafür ein EIGENER Stand statt des <c>Katalogfilterregister</c>
    /// (xunit fährt Testklassen nebeneinander); dass das Register ihn wirklich
    /// zwischen Verwaltung und Projektdialog teilt, prüft
    /// <c>EPOS.Kern.Tests/KatalogfilterstandTests</c>.
    /// </summary>
    [Fact]
    public void S2_5_Der_Filterstand_ueberlebt_einen_zweiten_Aufbau()
    {
        var ersterAufbau = Aufbauen();
        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        ersterAufbau.Render();
        Assert.Single(Katalogzeilen(ersterAufbau));

        // Der Dialog geht zu und wieder auf - derselbe Stand, dieselbe Sicht.
        var zweiterAufbau = Aufbauen();

        Assert.Single(Katalogzeilen(zweiterAufbau));
        Assert.Equal("1 von 2 Sätzen", zweiterAufbau.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(zweiterAufbau.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F1)
    // =====================================================================

    /// <summary>
    /// Nach dem Zeichnen steht die Maske an der <c>KiMaskenbruecke</c>, und die Brücke
    /// liest den Namen des gewählten Speichers. SETZEN geht nicht: Das eine Feld der
    /// Maske ist eine Anzeige (<c>nurLesen</c>), denn diese Maske führt keine
    /// Einstellwerte der Anlage.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist. Die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_gibt_den_Namen_heraus()
    {
        Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PUFFERSPEICHER_PROJEKT));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PUFFERSPEICHER_PROJEKT, "anlage");
        Assert.NotNull(zugang);
        Assert.Equal("Speicher 600 Liter", zugang.Lesen());
        Assert.False(zugang.Setzbar);
    }

    /// <summary>
    /// <b>Speichern ist der Knopf des Aufklappers „Alle Daten"</b> (Welle #458,
    /// Stufe 2) — trägt er keine Änderung, lehnt <c>dialog_speichern</c> benannt ab:
    /// Die Liste gibt die Maske erst beim OK an die Hülle.
    /// </summary>
    [Fact]
    public async Task Ohne_Aenderung_im_Aufklapper_lehnt_Speichern_benannt_ab()
    {
        Aufbauen();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PUFFERSPEICHER_PROJEKT);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);

        KiKern.KiErgebnis ergebnis = await haken.Speichern!();
        Assert.False(ergebnis.Erfolg);
        Assert.Contains("Alle Daten", ergebnis.Text, StringComparison.Ordinal);
    }
}
