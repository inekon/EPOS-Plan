using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Wärmebedarf extern“ als „Kältebedarf extern“ (K5a, Konzept Kältebedarf 3.2, 3.4, OP-2): Modus
/// <see cref="WaermebedarfExternModus.NurKuehlung"/>, die Feldgruppe „Deckung“ in der Detailzeile der Projektzeile, die
/// Ablehnung „Split ohne EER“ und der OP-2-Hinweis im Modus „Alle“.
/// </summary>
public class WaermebedarfExternKaelteTests : EposBunitContext
{
    private static readonly (string Wert, string Text)[] KANAELE =
    {
        ("HEIZUNG", "Heizung"), ("BRAUCHWASSER", "Brauchwasser"), ("PROZESS", "Prozesswärme"), ("KUEHLUNG", "Kühlung")
    };

    public WaermebedarfExternKaelteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermebedarfExternZeile Zeile(int idZ, string name, string kanal) => new()
    {
        IdZ = idZ, IdGanglinie = 5, Bezeichner = name, Kanal = kanal
    };

    private static List<WaermebedarfExternZeile> Gemischt() => new()
    {
        Zeile(1, "Heizlast", DbWerte.KANAL_HEIZUNG),
        Zeile(2, "Kaeltelast", DbWerte.KANAL_KUEHLUNG),
        Zeile(3, "Prozess", DbWerte.KANAL_PROZESS)
    };

    private IRenderedComponent<WaermebedarfExternDialog> Zeige(List<WaermebedarfExternZeile> zeilen,
        WaermebedarfExternModus modus, Action<bool>? geschlossen = null, Action? geaendert = null)
        => Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Modus, modus)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[]
            {
                Zeitreihenproben.Zeile(1, "Ganglinie A", jahresarbeitMwh: 10, spitzeKw: 5)
            }))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, n => new WaermebedarfExternZeile { IdZ = 0, IdGanglinie = 9, Bezeichner = n, Kanal = "HEIZUNG" })
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Stromtraeger, new[] { (7, "Strom") })
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IReadOnlyList<IElement> Projektzeilen(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.FindAll(".epos-projektzeile");

    private static IElement Knopf(IRenderedComponent<WaermebedarfExternDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static string Zusammenfassung(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.Find(".epos-satzzusammenfassung").TextContent;

    private static void Bearbeiten(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt, .epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog").Click();

    [Fact]
    public void NurKuehlung_zeigt_nur_Kuehlzeilen_und_keine_Kanalwahl()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.NurKuehlung);

        Assert.Equal(new[] { "Kaeltelast" }, Projektzeilen(cut).Select(z => z.TextContent.Trim()));
        Assert.Equal("Kaeltelast", cut.Instance.Gewaehlt?.Bezeichner);
        Assert.DoesNotContain(cut.FindAll(".epos-zweispalten-kanal .epos-feld-text"), e => e.TextContent.Trim() == "Kanal");
        Assert.True(cut.Instance.DeckungSichtbar);
        // K5c: Die Satzzeile nennt die Deckung in einem Satz; die Feldgruppe steht nur in der Bearbeiten-Überlagerung.
        Assert.Contains(Resource.DECK_ART_ZENTRAL, Zusammenfassung(cut));
        Assert.Empty(cut.FindComponents<DeckungsartFelder>());
        Assert.Empty(cut.FindAll(".epos-knopf--deckung"));
        // OP-2 gilt nur im Modus „Alle".
        Assert.Empty(cut.FindAll(".epos-zweispalten-kanalinfo"));
    }

    [Fact]
    public void NurKuehlung_legt_eine_neue_Zeile_auf_Kuehlung_an()
    {
        var zeilen = Gemischt();
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung);

        cut.FindAll(".epos-zweispalten-spalte--unten .epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        WaermebedarfExternZeile neu = zeilen.Last();
        Assert.Equal("Ganglinie A", neu.Bezeichner);
        Assert.Equal(DbWerte.KANAL_KUEHLUNG, neu.Kanal);
        Assert.Equal(2, Projektzeilen(cut).Count);
        // Die Wärmezeilen bleiben unberührt in der Liste der Hülle.
        Assert.Equal(DbWerte.KANAL_HEIZUNG, zeilen[0].Kanal);
        Assert.Equal(DbWerte.KANAL_PROZESS, zeilen[2].Kanal);
    }

    [Fact]
    public void Alle_zeigt_bei_einer_Kuehlzeile_den_OP2_Hinweis_und_die_Deckung()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.Alle);
        Assert.Equal(3, Projektzeilen(cut).Count);

        // Heizzeile gewählt: keine Deckung, kein Hinweis.
        Assert.False(cut.Instance.DeckungSichtbar);
        Assert.Empty(cut.FindAll(".epos-zweispalten-kanalinfo"));
        Assert.DoesNotContain(Resource.DECK_GRUPPE, Zusammenfassung(cut));

        Projektzeilen(cut)[1].Click();
        Assert.True(cut.Instance.DeckungSichtbar);
        Assert.Equal(Resource.WBX_HRL_KAELTE_EXTERN, cut.Find(".epos-zweispalten-kanalinfo").TextContent.Trim());
        Assert.Contains(Resource.DECK_GRUPPE, Zusammenfassung(cut));

        // In der Überlagerung: Stammdaten mit OP-2, Ganglinie und Deckung.
        Bearbeiten(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.WBX_HRL_KAELTE_EXTERN, cut.Find(".epos-wbx-hinweis--op2").TextContent.Trim());
        Assert.Single(cut.FindComponents<DeckungsartFelder>());
    }

    [Fact]
    public void Ok_lehnt_Split_ohne_EER_ab_und_bleibt_offen()
    {
        var zeilen = Gemischt();
        zeilen[1].Deckung.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        zeilen[1].Deckung.Eer1 = 0;
        bool? geschlossen = null;
        var cut = Zeige(zeilen, WaermebedarfExternModus.Alle, b => geschlossen = b);

        Knopf(cut, "OK").Click();

        Assert.Null(geschlossen);
        Assert.Equal(Resource.DECK_MSG_OHNE_EER, cut.Instance.Meldung);
        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
        Assert.Contains(Resource.DECK_MSG_OHNE_EER, cut.Find(".epos-warnbanner-text").TextContent);

        // Mit EER > 0 geht OK durch.
        zeilen[1].Deckung.Eer1 = 2.5;
        Knopf(cut, "OK").Click();
        Assert.True(geschlossen);
    }

    [Fact]
    public void Die_Ueberlagerung_prueft_beim_OK_und_meldet_die_Aenderung()
    {
        int gemeldet = 0;
        var zeilen = Gemischt();
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung, geaendert: () => gemeldet++);

        Bearbeiten(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");
        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, zeilen[1].Deckungsart);
        Assert.Equal(DeckungsartDaten.VORGABE_EER_1, zeilen[1].Eer1);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[0].Deckungsart);

        // OK der Überlagerung prüft mit der Regel des Kerns: EER 0 bleibt offen und nennt den Grund in der Fußleiste.
        zeilen[1].Deckung.Eer1 = 0;
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(Resource.DECK_MSG_OHNE_EER, cut.Instance.UeberlagerungFehler);
        Assert.Contains(Resource.DECK_MSG_OHNE_EER, cut.Find(".epos-satzueberlagerung-fuss").TextContent);

        zeilen[1].Deckung.Eer1 = 3.2;
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.True(gemeldet > 0);
        Assert.Contains(Resource.DECK_ART_SPLIT, Zusammenfassung(cut));
        Assert.Contains("3", Zusammenfassung(cut));
    }

    [Fact]
    public void Abbrechen_der_Ueberlagerung_verwirft_Deckung_und_Art()
    {
        var zeilen = Gemischt();
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung);

        Bearbeiten(cut);
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");
        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, zeilen[1].Deckungsart);
        cut.FindComponent<WaermebedarfExternFelder>().FindAll("select")[0].Change("1");
        Assert.Equal(DbWerte.KAELTE_ART_PROZESS, zeilen[1].KaelteArt);

        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[1].Deckungsart);
        Assert.Null(zeilen[1].Eer1);
        Assert.Equal(DbWerte.KAELTE_ART_RAUM, zeilen[1].KaelteArt);
    }

    [Fact]
    public void Die_Satzzeile_traegt_nur_das_Wesentliche_und_die_Knopfleiste()
    {
        var cut = Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Modus, WaermebedarfExternModus.NurKuehlung)
            .Add(x => x.Zeilen, Gemischt())
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Kennzahlen, _ => Task.FromResult<GanglinienKennzahlen?>(new GanglinienKennzahlen(12.5, 5, 2500)))
            .Add(x => x.DateiWaehlen, _ => Task.FromResult<string?>(null))
            .Add(x => x.Einlesen, (_, _, _) => Task.FromResult(new GanglinienImportErgebnis()))
            .Add(x => x.Kopieren, (_, _) => Task.FromResult(true))
            .Add(x => x.KatalogLoeschen, _ => true)
            .Add(x => x.Geschlossen, _ => { }));
        cut.WaitForState(() => cut.Instance.Grafikkennzahlen is not null);

        string z = Zusammenfassung(cut);
        Assert.Contains(Resource.KAELTE_ART_RAUM, z);
        Assert.Contains("12", z);
        Assert.Contains("2500", z);
        Assert.Contains(Resource.DECK_GRUPPE, z);

        // Die Knopfleiste am Satz: Vergleichen…, Schloss/Löschen, Bearbeiten…, Neu… - kein Diagramm, kein Importknopf.
        IElement leiste = cut.Find(".epos-zweispalten-satzknoepfe");
        string[] knoepfe = leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { Resource.AUSWAHL_BTN_VERGLEICHEN, Resource.STROMGL_BTN_LOESCHEN, Resource.STROMGL_BTN_BEARBEITEN,
                             Resource.AUSWAHL_BTN_NEU }, knoepfe);
        Assert.Empty(cut.FindComponents<DiagrammSvg>());
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == Resource.STROMGL_BTN_IMPORTIEREN);
        // Löschen ist beim Projektsatz weich gesperrt mit „Katalogsatz wählen".
        Assert.Equal(Resource.AUSWAHL_SPERRE_KATALOGSATZ, leiste.QuerySelector(".epos-knopf--loeschen")!.GetAttribute("title"));

        // Bearbeiten… öffnet die Überlagerung mit Ganglinie und Diagramm; Import und Kopie sind weich gesperrt.
        Bearbeiten(cut);
        Assert.NotEmpty(cut.FindComponents<GanglinienGrafik>());
        IElement import = cut.Find(".epos-wbx-ganglinienleiste .epos-knopf--import");
        Assert.Equal("true", import.GetAttribute("aria-disabled"));
    }

    /// <summary>
    /// Esc bei offener Bearbeiten-Überlagerung schließt nur die Überlagerung und verwirft wie Abbrechen; der Dialog bleibt
    /// offen (Befund aus K5b: in K5a schloss Esc den ganzen Dialog).
    /// </summary>
    [Fact]
    public void Esc_schliesst_nur_die_Ueberlagerung_und_verwirft()
    {
        var zeilen = Gemischt();
        bool? geschlossen = null;
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung, b => geschlossen = b);

        Bearbeiten(cut);
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");
        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, zeilen[1].Deckungsart);

        // Die Esc-Sperre des Dialogs: bei offener Überlagerung schließt Esc am Dialog nichts.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(geschlossen);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);

        // Esc in der Überlagerung schließt sie und stellt den Stand beim Öffnen wieder her.
        cut.Find(".epos-ueberlagerung--satz").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[1].Deckungsart);
        Assert.Null(geschlossen);
    }
    // =================================================================================
    // WP1b (KA-E-25): zweispaltige Überlagerung mit dem Steckbrief „Gewählter Lastgang"
    // =================================================================================

    /// <summary>
    /// <b>Links Stammdaten, Deckung und Steckbrief, rechts die Ganglinie</b>; der Steckbrief nennt Name, Kanal, Art,
    /// Deckung und Herkunft und folgt einer Eingabe links (Kanal) im selben Zeichenlauf.
    /// </summary>
    [Fact]
    public void WP1b_Der_Steckbrief_steht_links_und_folgt_dem_Kanal()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.Alle);
        Projektzeilen(cut)[1].Click();
        Bearbeiten(cut);

        var links = cut.Find(".epos-satz-zweispaltig > .epos-satz-zweispaltig-links");
        Assert.NotNull(links.QuerySelector(".epos-wbx-abschnitt--stamm"));
        Assert.NotNull(links.QuerySelector(".epos-wbx-abschnitt--deckung"));
        Assert.NotNull(cut.Find(".epos-satz-zweispaltig > .epos-satz-zweispaltig-rechts").QuerySelector(".epos-wbx-abschnitt--ganglinie"));
        var sb = links.QuerySelector(".epos-wbx-steckbrief")!;
        Assert.Equal("Kaeltelast", sb.QuerySelector("dd[data-zeile=name]")!.TextContent.Trim());
        Assert.NotNull(sb.QuerySelector("dd[data-zeile=art]"));
        Assert.NotNull(sb.QuerySelector("dd[data-zeile=deckung]"));
        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKTSATZ, sb.QuerySelector("dd[data-zeile=herkunft]")!.TextContent.Trim());

        var kanal = links.QuerySelector(".epos-wbx-abschnitt--stamm select")!;
        string erster = kanal.QuerySelector("option[value='0']")!.TextContent.Trim();
        kanal.Change("0");
        Assert.Equal(erster, cut.Find(".epos-wbx-steckbrief dd[data-zeile=kanal]").TextContent.Trim());
    }

    /// <summary><b>Der Katalogsatz</b>: links Stammdaten und Steckbrief ohne Kanal, Art und Deckung; Herkunft Katalog.</summary>
    [Fact]
    public void WP1b_Der_Katalogsatz_zeigt_den_Steckbrief_ohne_Projektzeilen()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.Alle);
        cut.FindAll(".epos-zweispalten-spalte--unten .epos-zeilenzelle--name")[0].Click();
        Bearbeiten(cut);

        var sb = cut.Find(".epos-satz-zweispaltig-links .epos-wbx-steckbrief");
        Assert.Equal("Ganglinie A", sb.QuerySelector("dd[data-zeile=name]")!.TextContent.Trim());
        Assert.Null(sb.QuerySelector("dd[data-zeile=kanal]"));
        Assert.Null(sb.QuerySelector("dd[data-zeile=deckung]"));
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, sb.QuerySelector("dd[data-zeile=herkunft]")!.TextContent.Trim());
    }
}
