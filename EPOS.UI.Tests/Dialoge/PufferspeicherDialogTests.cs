using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        Func<int, string>? katalogLoeschen = null,
        Func<string, IReadOnlyList<BrowserFeldwert>?>? katalogfelder = null,
        Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? felderSpeichern = null,
        Func<ErzeugerZeile?, bool, Task>? kostenOeffnen = null,
        Action<bool>? geschlossen = null,
        Satzbearbeitungswege? projektsatzWege = null,
        Satzbearbeitungswege? katalogsatzWege = null,
        Rueckwegwege? rueckwegWege = null,
        Func<string, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<string>? summe = null,
        Func<IReadOnlyList<Katalogfilterzeile>>? katalogzeilen = null,
        Func<int, Task>? auslegen = null,
        Func<ErzeugerZeile, (double Invest, double Betrieb)>? kostensumme = null,
        Func<ErzeugerZeile, Pufferangaben?>? projektangaben = null)
    {
        return Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, katalogzeilen ?? Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.KatalogDetail, id => Detail("Speicher " + id))
            .Add(x => x.ProjektDetail, projektDetail ?? (id => Detail("Projektkopie " + id)))
            .Add(x => x.Dublettenfrage, dublettenfrage ?? (_ => ""))
            .Add(x => x.Aufnehmen, aufnehmen ??
                 ((id, _) => new AufnahmeErgebnis(Zeile(9, "Speicher 800 Ltr", id))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => ""))
            .Add(x => x.ProjektsatzWege, projektsatzWege)
            .Add(x => x.KatalogsatzWege, katalogsatzWege)
            .Add(x => x.RueckwegWege, rueckwegWege)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.SummeVolumen, summe)
            .Add(x => x.AuslegenOeffnen, auslegen)
            .Add(x => x.Kostensumme, kostensumme)
            .Add(x => x.Projektangaben, projektangaben)
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
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);

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
        Assert.Equal(5, cut.FindAll(".epos-satzueberlagerung-koerper input[readonly]").Count);
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
        Assert.Equal(Profil.Spalten.Count(s => !s.StandardAus) + 2, kopf.Count);   // dazu die Stiftspalte (UeS2)   // „im Projekt verwendet“ standardmaessig aus (4.10)

        // Fuenf Trichter: alles ausser der Wahlspalte und dem Kennzeichen Q12.
        Assert.Equal(Profil.Spalten.Count(x => x.Filterbar), cut.FindAll(".epos-trichter").Count);
    }

    // =================================================================================
    // Detailquellen
    // =================================================================================

    [Fact]
    public void Eine_Projektzeile_zeigt_ihre_Kopie_ein_Katalogsatz_den_Stamm()
    {
        // Befund 4: Die Projektkopie kann anders heissen als die Vorlage.
        var cut = Aufbauen();
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.Equal("Projektkopie 51",
                     cut.FindAll(".epos-satzueberlagerung-koerper input[readonly]")[0].GetAttribute("value"));

        SatzZu(cut);
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Satz(cut);

        Assert.Equal("Speicher 51",
                     cut.FindAll(".epos-satzueberlagerung-koerper input[readonly]")[0].GetAttribute("value"));
    }

    // =================================================================================
    // Die Eindeutigkeitsrueckfrage
    // =================================================================================

    [Fact]
    public void Ein_neues_Geraet_wird_ohne_Rueckfrage_aufgenommen()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[1].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
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

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
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
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Loeschen_geht_ueber_die_Katalog_Id()
    {
        // V0-9: Der fruehere Weg ueber den Bezeichner traf bei gleichnamigen
        // Katalogeintraegen alle Namensvettern auf einmal.
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return ""; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[1].Click();
        cut.Find(".epos-knopf--loeschen").Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(new[] { 52 }, geloescht);
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
        Satz(ohne);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Empty(ohne.FindAll(".epos-modulparameter-knopf"));

        var mit = Aufbauen(katalogfelder: Katalogfelder);
        Satz(mit);
        Assert.Empty(mit.FindAll(".epos-modulparameter-knopf"));   // erste Projektzeile
        SatzZu(mit);

        KatalogZeileWaehlen(mit, 0);
        Satz(mit);
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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Equal("false", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));

        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.NotEmpty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        // UeS2: In der Ueberlagerung speichert ihr OK - der Aufklapper traegt keinen eigenen Knopf.
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Speichern");

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("3000");
        var speichern = cut.Find(".epos-satzueberlagerung-ok");
        Assert.False(speichern.HasAttribute("disabled"));
        speichern.Click();

        Assert.Equal("Speicher 600 Ltr", name);
        Assert.NotNull(gesehen);
        Assert.Equal("3000",
            gesehen!.First(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Wert);
        // Kein Band im Dialogkopf; die Ueberlagerung schliesst.
        Assert.Equal("", cut.Instance.Meldung);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf:</b> „Gespeichert um …" steht neben „Speichern", nicht als
    /// Band; ohne Änderung ist der Knopf weich gesperrt, ein Klick nennt den Grund, und
    /// die nächste Eingabe nimmt den Vermerk zurück.
    /// </summary>
    [Fact]
    public void UeS2_OK_schreibt_nur_eine_Aenderung_und_ein_zweites_Oeffnen_schreibt_nicht_erneut()
    {
        int schreibvorgaenge = 0;
        var cut = Aufbauen(katalogfelder: Katalogfelder,
                           felderSpeichern: (n, _) =>
                           {
                               schreibvorgaenge++;
                               return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n);
                           });
        KatalogZeileWaehlen(cut, 0);
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("42");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("", cut.Instance.Meldung);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);

        // Wieder geoeffnet und ohne Aenderung bestaetigt: kein zweiter Schreibvorgang.
        Satz(cut);
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.Equal(1, schreibvorgaenge);

        // Die naechste Eingabe schreibt wieder.
        Satz(cut);
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("43");
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.Equal(2, schreibvorgaenge);
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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung

        // Erst eine gueltige Aenderung - sie gibt den Knopf frei.
        var zahl = cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0];
        zahl.Input("3000");
        Assert.False(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));

        // Dann die Fehleingabe: der Knopf geht wieder zu.
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("dreitausend");

        Assert.True(cut.Find(".epos-satzueberlagerung-ok").HasAttribute("disabled"));
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
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung
        cut.Find(".epos-modulparameter").QuerySelectorAll("input[inputmode=decimal]")[0].Input("3000");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
        Assert.True(cut.Instance.ParameterOffen);

        // Der Grund steht auch rot in der Fussleiste der Ueberlagerung, die offen bleibt (UeS2).
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        var vermerk = cut.Find(".epos-satzueberlagerung-hinweis");
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
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

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
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        cut.FindAll(".epos-kostenleiste button")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(1, gesehen!.Schluessel);
    }

    /// <summary>Wählt die Katalogzeile mit dieser Nummer in der rechten Liste.</summary>
    private static void KatalogZeileWaehlen(IRenderedComponent<PufferspeicherDialog> cut, int nr)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[nr].Click();

    /// <summary>
    /// UeS2: Das Fragment des Satzes steht nur in der Satz-Überlagerung — „Bearbeiten" der
    /// Detailzeile öffnet sie (falls zu); zurück kommt ihr Körper.
    /// </summary>
    private static AngleSharp.Dom.IElement Satz(IRenderedComponent<PufferspeicherDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    /// <summary>UeS2: Schließt eine offene Satz-Überlagerung wie Abbrechen bzw. Schließen.</summary>
    private static void SatzZu(IRenderedComponent<PufferspeicherDialog> cut)
    {
        var knopf = cut.FindAll(".epos-satzueberlagerung-abbrechen, .epos-satzueberlagerung-schliessen");
        if (knopf.Count > 0) knopf[0].Click();
    }

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
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

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

        Katalogzeilen(cut)[0].QuerySelector(".epos-zeilenzelle--name")!.Click();
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpVolumen, "700..900");
        cut.Render();

        Assert.Single(Katalogzeilen(cut));
        Assert.False(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>S2.3 / Frage Q12 — „im Projekt verwendet".</b> EINMAL für die ganze Liste
    /// aus der Projektliste des Dialogs gestempelt, nicht je Zeile und nicht aus
    /// der Datenbank: Der Dialog schreibt erst beim OK zurück, eine Zählabfrage
    /// wäre nach der ersten Übernahme veraltet.
    /// </summary>
    [Fact]
    public void S2_3_Die_Verwendungsmarke_zaehlt_die_Projektliste()
    {
        var cut = Aufbauen(zeilen: new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) });

        var kopf = cut.FindAll(".epos-raster")[1]
                      .QuerySelectorAll("th").Select(e => e.TextContent.Trim()).ToList();
        // Konzept 4.10: die Verwendung steht als Marke am Bezeichner, die Spalte ist
        // nur noch waehlbar (standardmaessig aus).
        Assert.DoesNotContain(kopf, k => k.StartsWith("im Projekt verwendet"));

        var zeilen = Katalogzeilen(cut);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(1, zeilen.Count(z => z.QuerySelector(".epos-verwendet-marke") is not null));
        Assert.Equal(1, zeilen.Count(z => (z.ClassName ?? "").Contains("epos-zeile--verwendet")));

        var traegt = zeilen.First(z => z.QuerySelector(".epos-verwendet-marke") is not null);
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

    // =========================================================================
    //  Stufe P2 — „Auslegen…" in der Kachel-Verwaltung
    // =========================================================================

    /// <summary>
    /// Einstieg A des Konzepts Pufferspeicher-Auslegung: „Auslegen…" steht in der Knopfzeile des
    /// Projektsatzes und reicht die Gerätenummer der gewählten Projektzeile an die Hülle; die Zeile
    /// darunter sagt, dass der Knopf die Verwaltung ohne Speichern verlässt.
    /// </summary>
    [Fact]
    public void Auslegen_reicht_die_Geraetenummer_der_Projektzeile_an_die_Huelle()
    {
        var geoeffnet = new List<int>();
        var cut = Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.ProjektDetail, id => Detail("Projektkopie " + id))
            .Add(x => x.AuslegenOeffnen, id => { geoeffnet.Add(id); return Task.CompletedTask; }));

        Satz(cut);   // UeS2: „Auslegen…" steht in der Satz-Ueberlagerung
        Assert.Contains("ohne zu speichern", cut.Find(".epos-pspd-auslegen-hinweis").TextContent);
        // Die erste Projektzeile ist vorgewählt: ihre Gerätenummer.
        cut.Find("button.epos-pspd-auslegen").Click();
        Assert.Equal(new[] { 51 }, geoeffnet);

        // Katalogauswahl V1 (4.9): „Auslegen…" gehört dem PROJEKTSATZ - beim Katalogsatz fehlt der Knopf.
        SatzZu(cut);
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Satz(cut);
        Assert.Empty(cut.FindAll("button.epos-pspd-auslegen"));
        Assert.Equal(new[] { 51 }, geoeffnet);

        var ohne = Aufbauen();
        Assert.Empty(ohne.FindAll("button.epos-pspd-auslegen"));
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Knöpfe an ihrem Ort, Detailzeile, Bearbeiten je Bereich
    // =================================================================================

    private static List<BrowserFeldwert> Felder() => Katalogfelder("x");

    private static Satzbearbeitungswege Wege(List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>? gespeichert = null,
                                             KatalogSpeicherErgebnis? ergebnis = null)
        => new()
        {
            Lesen = _ => Felder(),
            Speichern = l => { gespeichert?.AddRange(l); return ergebnis ?? new KatalogSpeicherErgebnis(true, "ok", ""); }
        };

    private static IReadOnlyList<Katalogfilterzeile> MitSchloss(params int[] gesperrt)
    {
        var zeilen = Katalogzeilen();
        foreach (var z in zeilen) z.Geschuetzt = gesperrt.Contains(z.Id);
        return zeilen;
    }

    private static void KatalogAnkreuzen(IRenderedComponent<PufferspeicherDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
    }

    private static void ProjektAnkreuzen(IRenderedComponent<PufferspeicherDialog> cut, params int[] zeilen)
    {
        foreach (int i in zeilen)
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
    }

    [Fact]
    public void S3_Die_Knoepfe_stehen_an_ihrem_Ort_und_der_Katalogfuss_hat_kein_Neu()
    {
        var cut = Aufbauen(projektsatzWege: Wege(), katalogsatzWege: Wege(), summe: () => "1400",
                           editorGaben: _ => new Dictionary<string, object>());

        // D: Kontext im Dialogkopf, keine eigene Kontextzeile mehr.
        Assert.Equal("Geben Sie die Daten der Pufferspeicher ein",
                     cut.Find(".epos-dialog-kopf > .epos-dialog-kontext").TextContent);
        Assert.Empty(cut.FindAll(".epos-kontextzeile"));

        // P: Summe Volumen (die Klasse des Bausteins, nie gekuerzt), Bearbeiten…, Entfernen.
        var p = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste");
        var summe = p.QuerySelector(".epos-zweispalten-summe")!;
        Assert.Contains("Summe Volumen [l]:", summe.TextContent);
        Assert.Equal("1400", summe.QuerySelector("b")!.TextContent);
        Assert.NotNull(p.QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(p.QuerySelector(".epos-zweispalten-knopf--entfernen"));
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));

        // K-Fuss: Vergleichen, Schloss, Loeschen, Bearbeiten nach dem Namen - KEIN Neu (4.9).
        KatalogZeileWaehlen(cut, 0);
        var fuss = cut.Find(".epos-zweispalten-fussleiste").Children.ToList();
        Assert.Equal("Speicher 600 Ltr:", fuss[0].TextContent);
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--vergleichen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--loeschen"));
        Assert.Contains(fuss, e => e.ClassList.Contains("epos-knopf--bearbeiten-katalog"));
        Assert.Empty(cut.FindAll(".epos-knopf--neu"));

        // Die Speicherverwaltung als Ueberlagerung gibt es nicht mehr.
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void S3_Die_Detailzeile_nennt_Marke_Name_und_Kenndaten()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(51));
        var zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Equal("Speicher 600 Liter", zeile.QuerySelector(".epos-zweispalten-satzname")!.TextContent);
        Assert.Contains("Hersteller Musterwerk", zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);

        KatalogZeileWaehlen(cut, 0);
        zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, zeile.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Contains(Resource.AUSWAHL_SATZ_NUR_LESEN, zeile.QuerySelector(".epos-zweispalten-satzkenndaten")!.TextContent);
    }

    [Fact]
    public void S3_Kosten_und_Auslegen_stehen_nur_beim_Projektsatz()
    {
        var cut = Aufbauen(kostenOeffnen: (_, _) => Task.CompletedTask, auslegen: _ => Task.CompletedTask);
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung
        Assert.Equal(2, cut.FindAll(".epos-kostenleiste button").Count);
        Assert.Single(cut.FindAll("button.epos-pspd-auslegen"));

        SatzZu(cut);
        KatalogZeileWaehlen(cut, 0);
        Satz(cut);
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
        Assert.Empty(cut.FindAll("button.epos-pspd-auslegen"));
    }

    [Fact]
    public void S3_Alle_Daten_des_Projektsatzes_lesen_und_speichern_die_Projektkopie()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));
        Satz(cut);   // UeS2: das Fragment steht in der Satz-Ueberlagerung

        Assert.True(cut.Instance.ParameterOffen);
        var felder = cut.FindComponent<EPOS.UI.Bausteine.Katalogfelder>();
        Assert.Contains(felder.Instance.Felder, f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten && f.Editierbar);

        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("3100");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.Equal(51, Assert.Single(gespeichert).Id);
    }

    /// <summary>
    /// Die Projektkopie entsteht beim Übernehmen (Hülle, wie Heizkessel und BHKW): Die frisch aufgenommene Zeile trägt
    /// die Id der Kopie und ist sofort bearbeitbar — Bearbeiten…, Rückweg und „Alle Daten" wirken auf sie, ein
    /// Sperrhinweis „ohne Kopie" gibt es nicht mehr.
    /// </summary>
    [Fact]
    public void S3_Eine_frisch_aufgenommene_Zeile_ist_sofort_bearbeitbar()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var cut = Aufbauen(new List<ErzeugerZeile>(), projektsatzWege: Wege(),
                           aufnehmen: (id, _) => new AufnahmeErgebnis(Zeile(9, "Speicher 800 Ltr", 777)),
                           rueckwegWege: Rueckweg(new[] { new Rueckwegvorschlag(777, "Speicher 800 Ltr", "", Rueckwegsperre.UrsprungUnbekannt, "Speicher 800 Ltr") },
                                                  gefragt: gefragt));

        KatalogZeileWaehlen(cut, 1);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.False(cut.Find(".epos-knopf--bearbeiten-projekt").HasAttribute("disabled"));
        Assert.False(cut.Find(".epos-knopf--rueckweg").HasAttribute("disabled"));
        Assert.Equal(Resource.KATRUECK_BTN_HINWEIS, cut.Find(".epos-knopf--rueckweg").GetAttribute("title"));
        Satz(cut);   // UeS2: „Alle Daten" stehen in der Satz-Ueberlagerung
        Assert.Single(cut.FindAll(".epos-modulparameter"));
        Assert.Empty(cut.FindAll(".epos-pspd-ohne-kopie"));
        SatzZu(cut);

        // UeS1: EINE Projektkopie oeffnet die Satz-Ueberlagerung auf der frischen Zeile.
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(777, cut.Instance.Projektzeile!.GeraetId);
    }

    /// <summary>
    /// UeS1: „Bearbeiten…" EINER Projektkopie öffnet die Satz-Überlagerung — Marke, Name, „Alle
    /// Daten" aufgeklappt, das Fragment nur dort, kein zweiter Speichern-Knopf; „Auslegen…" ist
    /// auch aus der Überlagerung erreichbar und reicht die Gerätenummer an die Hülle.
    /// </summary>
    [Fact]
    public void UeS1_Bearbeiten_einer_Projektkopie_oeffnet_die_Satzueberlagerung_mit_Auslegen()
    {
        var geoeffnet = new List<int>();
        var cut = Aufbauen(projektsatzWege: Wege(), auslegen: id => { geoeffnet.Add(id); return Task.CompletedTask; });

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        var kopf = cut.Find(".epos-ueberlagerung--satz > .epos-ueberlagerung-kopf");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, kopf.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
        Assert.Single(cut.FindAll(".epos-modulparameter"));
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-koerper .epos-modulparameter"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper .epos-speichervermerk"));

        cut.Find(".epos-satzueberlagerung-koerper button.epos-pspd-auslegen").Click();
        Assert.Equal(new[] { 51 }, geoeffnet);
    }

    /// <summary>UeS1: OK schreibt die Projektkopie über den Speicherweg der Satzfläche und schließt.</summary>
    [Fact]
    public void UeS1_OK_der_Satzueberlagerung_schreibt_die_Projektkopie()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        var satz = Assert.Single(gespeichert);
        Assert.Equal(51, satz.Id);
        Assert.Equal("Neuwerk", satz.Felder.First(f => f.Schluessel == KatalogBrowserProfil.FeldFirma).Wert);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(cut.FindAll(".epos-modulparameter"));   // UeS2: zu steht nur die Zusammenfassung
    }

    /// <summary>UeS1: Eine abgelehnte Übernahme hält die Überlagerung mit dem Grund offen.</summary>
    [Fact]
    public void UeS1_Eine_abgelehnte_Uebernahme_haelt_die_Satzueberlagerung_offen()
    {
        var cut = Aufbauen(projektsatzWege: Wege(ergebnis: new KatalogSpeicherErgebnis(false, "Satz gesperrt", "")));

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains("Satz gesperrt", cut.Find(".epos-satzueberlagerung-fuss [role=alert]").TextContent);
    }

    /// <summary>UeS1: Abbrechen liest den Satz neu — die Feldänderung ist verworfen, nichts geschrieben.</summary>
    [Fact]
    public void UeS1_Abbrechen_der_Satzueberlagerung_verwirft_die_Feldaenderung()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(projektsatzWege: Wege(gespeichert));

        cut.Find(".epos-knopf--bearbeiten-projekt").Click();
        cut.Find(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])").Input("Neuwerk");
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.Empty(gespeichert);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Satz(cut);   // UeS2: wieder geoeffnet steht der verworfene Wert nicht mehr da
        Assert.Equal("Musterwerk", cut.Find(".epos-satzueberlagerung-koerper .epos-modulparameter input[type=text]:not([readonly])").GetAttribute("value"));
    }

    /// <summary>
    /// „Auslegen…" in der Kopfleiste des Projektbereichs: ohne gewählte Projektzeile (leere Liste) die Auslegung für
    /// einen NEUEN Speicher (Gerätenummer 0), mit gewählter Zeile dieselbe wie der Knopf beim Projektsatz.
    /// </summary>
    [Fact]
    public void S3_Auslegen_in_der_Kopfleiste_oeffnet_ohne_Wahl_die_Auslegung_fuer_einen_neuen_Speicher()
    {
        var geoeffnet = new List<int>();
        Func<int, Task> auslegen = id => { geoeffnet.Add(id); return Task.CompletedTask; };

        var leer = Aufbauen(new List<ErzeugerZeile>(), auslegen: auslegen);
        var kopf = leer.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste button.epos-pspd-auslegen-kopf");
        Assert.Equal(Resource.PAUS_AUSLEGEN_VERWIRFT, kopf.GetAttribute("title"));
        Assert.Empty(leer.FindAll("button.epos-pspd-auslegen"));     // kein Projektsatz, kein Satzknopf
        kopf.Click();
        Assert.Equal(new[] { 0 }, geoeffnet);

        // Ein Katalogsatz ist gewählt, keine Projektzeile: weiter der neue Speicher.
        KatalogZeileWaehlen(leer, 0);
        leer.Find("button.epos-pspd-auslegen-kopf").Click();
        Assert.Equal(new[] { 0, 0 }, geoeffnet);

        // Mit gewählter Projektzeile: dieselbe Gerätenummer wie der Knopf beim Projektsatz.
        geoeffnet.Clear();
        var mit = Aufbauen(auslegen: auslegen);
        mit.Find("button.epos-pspd-auslegen-kopf").Click();
        Satz(mit);   // UeS2: der Satzknopf steht in der Satz-Ueberlagerung
        mit.Find("button.epos-pspd-auslegen").Click();
        Assert.Equal(new[] { 51, 51 }, geoeffnet);

        Assert.Empty(Aufbauen().FindAll("button.epos-pspd-auslegen-kopf"));
    }

    [Fact]
    public void S3_Bearbeiten_im_Projektbereich_oeffnet_die_Projektkopien_der_Auswahl_je_Geraet_einmal()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher A", 51), Zeile(2, "Speicher B", 52), Zeile(3, "Speicher A", 51) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege());

        ProjektAnkreuzen(cut, 0, 1, 2);
        cut.Find(".epos-knopf--bearbeiten-projekt").Click();

        Assert.Equal(Satzmarke.Projektsatz, cut.Instance.Bearbeitung!.Value.Art);
        Assert.Equal(new[] { 51, 52 }, cut.Instance.Bearbeitung!.Value.Saetze.Select(s => s.Id).ToArray());
        var kopf = cut.Find(".epos-ueberlagerung-kopf");
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, kopf.QuerySelector(".epos-zweispalten-marke--satz")!.TextContent);
    }

    [Fact]
    public void S3_Ein_einzelner_ungesperrter_Katalogsatz_oeffnet_den_vollen_Editor()
    {
        var gaben = new List<string>();
        var cut = Aufbauen(katalogsatzWege: Wege(),
                           editorGaben: n => { gaben.Add(n); return new Dictionary<string, object>(); });
        KatalogZeileWaehlen(cut, 1);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();
        Assert.True(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.Equal(new[] { "Speicher 800 Ltr" }, gaben);
        Assert.NotNull(cut.FindComponent<PufferSpKatalogDialog>());
    }

    [Fact]
    public void S3_Mehrfach_Bearbeiten_blaettert_und_speichert_alle_in_einem_Vorgang()
    {
        var gespeichert = new List<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>();
        var cut = Aufbauen(katalogsatzWege: Wege(gespeichert), editorGaben: _ => new Dictionary<string, object>());

        KatalogAnkreuzen(cut, 0, 1);
        Assert.Equal(2, cut.Instance.KatalogWahl.Anzahl);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        Assert.False(cut.Instance.EditorOffen);
        var sb = cut.FindComponent<Satzbearbeitung>();
        Assert.Equal(2, sb.Instance.Aktiv.Count);
        cut.Find(".epos-satzbearbeitung input[type=text]:not([readonly])").Input("Neuwerk");
        cut.FindAll(".epos-satzbearbeitung-fueralle input")[0].Change(true);
        cut.Find(".epos-satzbearbeitung-speichern").Click();

        Assert.Equal(new[] { 51, 52 }, gespeichert.Select(g => g.Id).ToArray());
        Assert.All(gespeichert, g => Assert.Equal("Neuwerk", g.Felder.First(f => f.Schluessel == KatalogBrowserProfil.FeldFirma).Wert));
        Assert.Null(cut.Instance.Bearbeitung);
    }

    [Fact]
    public void S3_Ein_gesperrter_Katalogsatz_allein_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogsatzWege: Wege(), katalogzeilen: () => MitSchloss(51),
                           katalogfelder: Katalogfelder, felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: _ => new Dictionary<string, object>());
        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-knopf--bearbeiten-katalog").Click();

        // UeS1: EIN gesperrter Satz oeffnet die Satz-Ueberlagerung nur lesend.
        Assert.False(cut.Instance.EditorOffen);
        Assert.Null(cut.Instance.Bearbeitung);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(Resource.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper input[type=text]:not([readonly])"));
        cut.Find(".epos-satzueberlagerung-schliessen").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    [Fact]
    public void S3_Die_Sammeluebernahme_fragt_die_Dublette_je_Satz_und_nimmt_die_uebrigen_auf()
    {
        var aufgenommen = new List<(int Id, bool Erzwingen)>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Ltr", 51) };
        var cut = Aufbauen(zeilen, dublettenfrage: id => id == 51 ? "steht bereits" : "",
            aufnehmen: (id, e) => { aufgenommen.Add((id, e)); return new AufnahmeErgebnis(Zeile(10 + id, "Neu " + id, id)); });

        KatalogAnkreuzen(cut, 0, 1);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.True(cut.Instance.Dublettenwarnung);
        Assert.Empty(aufgenommen);

        cut.FindAll(".epos-rueckfrage button")[1].Click();      // „Nein" - Satz 51 uebergangen

        Assert.False(cut.Instance.Dublettenwarnung);
        Assert.Equal(new[] { (52, false) }, aufgenommen.ToArray());
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void S3_Entfernen_wirkt_auf_alle_gewaehlten_Projektzeilen_und_die_Summe_folgt()
    {
        int summen = 0;
        var entfernt = new List<string>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher A", 51), Zeile(2, "Speicher B", 52), Zeile(3, "Speicher C", 53) };
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z.Bezeichner), summe: () => (++summen).ToString());

        ProjektAnkreuzen(cut, 0, 2);
        cut.Find(".epos-zweispalten-knopf--entfernen").Click();

        Assert.Equal(new[] { "Speicher A", "Speicher C" }, entfernt.ToArray());
        Assert.Equal("Speicher B", Assert.Single(zeilen).Bezeichner);
        Assert.Equal(2, summen);
        Assert.Equal("2", cut.Instance.Summe);
    }

    [Fact]
    public void S3_Loeschen_mehrerer_ueberspringt_gesperrte_und_ist_ohne_Wahl_gesperrt()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(52), katalogLoeschen: id => { geloescht.Add(id); return ""; });
        Assert.True(cut.Find(".epos-knopf--loeschen").HasAttribute("disabled"));

        KatalogAnkreuzen(cut, 0, 1);
        cut.Find(".epos-knopf--loeschen").Click();
        Assert.Contains(string.Format(Resource.Culture, Resource.SATZBEARB_UEBERSPRUNGEN, "„Speicher 800 Ltr“"),
                        cut.Find(".epos-rueckfrage").TextContent);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 51 }, geloescht.ToArray());
    }

    [Fact]
    public void S3_Eine_Ablehnung_des_Loeschens_nennt_den_Grund()
    {
        var cut = Aufbauen(katalogLoeschen: _ => "schreibgeschützt");
        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-knopf--loeschen").Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal("schreibgeschützt", cut.Instance.Meldung);
    }

    // =================================================================================
    // Katalogauswahl V1, Stufe 3: Rückweg „In die Datenbank übernehmen…" (5.2, KA‑E‑9)
    // =================================================================================

    private static Rueckwegwege Rueckweg(IReadOnlyList<Rueckwegvorschlag> zeilen, List<Rueckwegwahl>? geschrieben = null,
                                         KatalogSpeicherErgebnis? ergebnis = null, List<IReadOnlyList<int>>? gefragt = null)
        => new()
        {
            Vorschau = ids => { gefragt?.Add(ids); return zeilen; },
            NameBelegt = _ => false,
            Uebernehmen = w => { geschrieben?.AddRange(w); return ergebnis ?? new KatalogSpeicherErgebnis(true, "1 Satz übernommen", ""); },
        };

    [Fact]
    public void S3_Der_Rueckweg_steht_nach_Bearbeiten_und_fragt_je_Geraet_einmal()
    {
        var gefragt = new List<IReadOnlyList<int>>();
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher A", 51), Zeile(2, "Speicher B", 52), Zeile(3, "Speicher A", 51) };
        var cut = Aufbauen(zeilen, projektsatzWege: Wege(),
                           rueckwegWege: Rueckweg(new[] { new Rueckwegvorschlag(51, "Speicher A", "", Rueckwegsperre.UrsprungUnbekannt, "Speicher A") },
                                                  gefragt: gefragt));

        var knoepfe = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste").QuerySelectorAll("button").ToList();
        int bearbeiten = knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--bearbeiten-projekt"));
        Assert.Equal(bearbeiten + 1, knoepfe.FindIndex(k => k.ClassList.Contains("epos-knopf--rueckweg")));

        ProjektAnkreuzen(cut, 0, 1, 2);
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Equal(new[] { 51, 52 }, Assert.Single(gefragt).ToArray());
        Assert.NotNull(cut.Instance.Rueckweg);
    }

    [Fact]
    public void S3_Der_Rueckweg_schreibt_die_Wahl_meldet_und_nennt_was_im_Projekt_bleibt()
    {
        var geschrieben = new List<Rueckwegwahl>();
        var cut = Render<PufferspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51) })
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.RueckwegWege, Rueckweg(new[] { new Rueckwegvorschlag(51, "Speicher 600 Liter", "Speicher 600 Ltr", Rueckwegsperre.Keine, "Speicher 600 Liter") }, geschrieben))
            .Add(x => x.RueckwegBleibtText, "Im Projekt bleiben: Verwendung und Senken."));
        cut.Find(".epos-knopf--rueckweg").Click();
        Assert.Contains("Im Projekt bleiben: Verwendung und Senken.", cut.Find(".epos-rueckweg").TextContent);
        Assert.False(cut.Find(".epos-rueckweg-zeile .epos-rueckweg-ueber").HasAttribute("disabled"));

        cut.Find(".epos-rueckweg-uebernehmen").Click();

        Assert.Equal(new[] { new Rueckwegwahl(51, false, "Speicher 600 Liter") }, geschrieben.ToArray());
        Assert.Null(cut.Instance.Rueckweg);
        Assert.Equal("1 Satz übernommen", cut.Instance.Meldung);
    }

    [Fact]
    public void S3_Ohne_Rueckwegwege_kein_Knopf()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        Assert.Empty(cut.FindAll(".epos-knopf--rueckweg"));
    }
    // =================================================================================
    // UeS2: Zusammenfassung der Detailzeile und Stift je Zeile (Anwenderentscheid 10.10.2026)
    // =================================================================================

    private static Dictionary<string, string> Angaben(IRenderedComponent<PufferspeicherDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung-angabe")
              .ToDictionary(a => a.QuerySelector("dt")!.TextContent, a => a.QuerySelector("dd")!.TextContent);

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Projektsatzes_nennt_die_Speicherdaten()
    {
        var cut = Aufbauen(
            projektDetail: id => new ErzeugerDetail("Projektkopie " + id, "",
                new[] { (Resource.PSPD_LBL_HERSTELLER, "Musterwerk"), ("Volumen [l]:", "999") },
                Kennwerte: new ErzeugerKennwerte { VolumenLiter = 600 }),
            kostenOeffnen: (_, _) => Task.CompletedTask,
            kostensumme: _ => (2500.0, 40.0),
            projektangaben: _ => new Pufferangaben(70, 50, 10, 95, "Heizung"));

        cut.Find(".epos-zweispalten-satzzeile").Click();

        var k = System.Globalization.CultureInfo.CurrentCulture;
        var angaben = Angaben(cut);
        Assert.Equal("600 l", angaben[Resource.AUSWAHL_ZF_VOLUMEN]);
        Assert.Equal("70/50 °C", angaben[Resource.AUSWAHL_ZF_VORLAUF_RUECKLAUF]);
        Assert.Equal("10/95 %", angaben[Resource.AUSWAHL_ZF_SCHWELLEN]);
        Assert.Equal("Heizung", angaben[Resource.AUSWAHL_ZF_VERWENDUNG]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO, 2500.0), angaben[Resource.AUSWAHL_ZF_INVEST]);
        Assert.Equal(string.Format(k, Resource.AUSWAHL_ZF_EURO_JAHR, 40.0), angaben[Resource.AUSWAHL_ZF_BETRIEB]);
        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
        Assert.NotEmpty(Satz(cut).QuerySelectorAll(".epos-kostenleiste button"));
    }

    [Fact]
    public void UeS2_Ohne_Wege_nennt_die_Zusammenfassung_weder_Kosten_noch_Projektwerte()
    {
        var cut = Aufbauen();
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var angaben = Angaben(cut);
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_INVEST));
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_SCHWELLEN));
        Assert.False(angaben.ContainsKey(Resource.AUSWAHL_ZF_VORLAUF_RUECKLAUF));
    }

    [Fact]
    public void UeS2_Die_Zusammenfassung_des_Katalogsatzes_nennt_Hersteller_Volumen_und_Verlust()
    {
        var cut = Aufbauen();
        KatalogZeileWaehlen(cut, 0);
        cut.Find(".epos-zweispalten-satzzeile").Click();

        var texte = cut.FindAll(".epos-satzzusammenfassung-angabe dd").Select(e => e.TextContent).ToList();
        Assert.Equal(3, texte.Count);
        Assert.Contains("Musterwerk", texte);
        Assert.Contains(texte, t => t.StartsWith("600") && t.EndsWith(" l"));
        Assert.Contains(texte, t => t.EndsWith(" kWh/d"));
        Assert.DoesNotContain("stehend", texte);
    }

    [Fact]
    public void UeS2_Der_Stift_der_Projektzeile_oeffnet_die_Projektkopie_in_der_Ueberlagerung()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 600 Liter", 51), Zeile(2, "Speicher 800 Liter", 52) };
        var cut = Aufbauen(zeilen: zeilen, projektsatzWege: Wege());

        var stifte = cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift");
        Assert.Equal(2, stifte.Length);
        stifte[1].Click();

        Assert.Equal(52, cut.Instance.Projektzeile!.GeraetId);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Single(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Ohne_Weg_der_Projektkopie_traegt_die_Projektliste_keinen_Stift()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift"));
        Assert.Equal(2, cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift").Length);
    }

    [Fact]
    public void UeS2_Der_Stift_einer_gesperrten_Katalogzeile_oeffnet_nur_lesend()
    {
        var cut = Aufbauen(katalogzeilen: () => MitSchloss(51), katalogfelder: Katalogfelder,
                           felderSpeichern: (_, _) => new KatalogSpeicherErgebnis(true, "ok", ""),
                           editorGaben: _ => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[0].Click();

        Assert.Equal("Speicher 600 Ltr", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.False(cut.Instance.EditorOffen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void UeS2_Der_Stift_einer_ungesperrten_Katalogzeile_oeffnet_den_Katalogeditor()
    {
        var cut = Aufbauen(katalogfelder: Katalogfelder, editorGaben: _ => new Dictionary<string, object>());

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[1].Click();

        Assert.Equal("Speicher 800 Ltr", cut.Instance.Katalogzeile!.Bezeichner);
        Assert.True(cut.Instance.EditorOffen);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
    }

    /// <summary>UeS2b: „Grundlagen" und „Berechnung" im Kopf der Detailzeile, offen in der Überlagerung — je Ansicht einmal.</summary>
    [Fact]
    public void UeS2b_Die_Infoknoepfe_stehen_im_Kopf_der_Detailzeile_und_nie_doppelt()
    {
        var cut = Aufbauen(projektsatzWege: Wege());
        string[] schluessel = { "Form_PufferSp.Grundlagen", "Form_PufferSp.Berechnung" };

        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-satzkopf .epos-zweispalten-satzkopfknoepfe .epos-hilfepille").Count);

        Satz(cut);
        foreach (string s in schluessel)
            Assert.Single(cut.FindComponents<InfoKnopf>(), k => k.Instance.Schluessel == s);
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopfknoepfe"));
    }
}
