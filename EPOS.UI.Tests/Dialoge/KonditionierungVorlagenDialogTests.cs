using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Vorlagen im Gebäude-Katalogeditor</b> (Stufe KP2, Welle U2; Teilkonzept Konditionierungsprofile
/// 3.5, 7.4; Entwurf KP2 Festlegungen 1, 13; E56 F4 (a)) — was sich erst am ganzen Dialog zeigt: dass
/// „Übernehmen" mit dem OK des Editors geschrieben wird und Abbrechen es verwirft.
/// </summary>
/// <remarks>
/// Der Editor steht über dem Weg der Hülle OHNE Datenbank mit der Ablage der 14 ausgelieferten Vorlagen
/// (<see cref="Konditionierungsvorlagenablage.AusSaat"/>); sein Schreibweg merkt sich den Aufruf. Die
/// Kultur ist auf de-DE gepinnt.
/// </remarks>
public class KonditionierungVorlagenDialogTests : EposBunitContext
{
    public KonditionierungVorlagenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());
    }

    private const string REITER = "Konditionierung";

    /// <summary>
    /// Ein vollständig belegter Satz (das Maß von <c>GebaeudeKatalogDialogTests</c>) mit leerer
    /// Konditionierung, Heizwerten samt Nachtfenster, Kühlung und getrennter Lüftung.
    /// </summary>
    internal static GebaeudeKatalogDaten Vollsatz()
    {
        GebaeudeKatalogDaten d = KalenderkarteTests.Satz();
        d.Typ = "Wohnblock";
        d.Beschreibung = "Probesatz der Vorlagen";
        d.Gebaeudeart = "grosses Mehrfamilienhaus";
        d.Verwendung = "Wohngebaeude";
        d.Baualtersklasse = 4;
        d.Bauart = 1;
        d.Fensterdurchlassgrad = 0.4;
        d.Raumhoehe = 2.5;
        d.FensterflaecheNord = 10;
        d.FensterflaecheSued = 20;
        d.FensterflaecheOstWest = 15;
        d.FlaecheAussenwand = 200;
        d.Dachflaeche = 120;
        d.Grundflaeche = 100;
        d.SonstigeFlaechen = 5;
        d.UWertAussenwand = 0.3;
        d.UWertFenster = 1.3;
        d.UWertDachflaeche = 0.2;
        d.UWertGrundflaeche = 0.35;
        d.UWertSonstiges = 0.5;
        d.WbvkFensterWand = 0.1;
        d.AnschlussFensterWand = 50;
        d.MaxTemperatur = 24;
        d.WochenendAbsenkung = 0;
        d.SollFerien = 0;
        d.WwBedarf = 700;
        return d;
    }

    /// <summary>Die Aufrufe des Schreibwegs: der geschriebene Satz.</summary>
    private readonly List<GebaeudeKatalogDaten> _geschrieben = new();

    /// <summary>Der Ausgang des Dialogs: <c>true</c> OK, <c>false</c> Abbrechen, <c>null</c> offen.</summary>
    private bool? _ausgang;

    internal IRenderedComponent<GebaeudeKatalogDialog> Editor(Konditionierungsvorlagenablage ablage,
                                                              GebaeudeKatalogDaten? satz = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, satz ?? Vollsatz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Konditionierung, KalenderkarteTests.Weg(ablage))
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Gebaeudetypen, () => new[] { "Wohnblock" })
            .Add(x => x.Gebaeudearten, () => new[] { "grosses Mehrfamilienhaus" })
            .Add(x => x.Speichern, (d, _, _) => { _geschrieben.Add(d); return new GebaeudeKatalogErgebnis(true, ""); })
            .Add(x => x.Geschlossen, ok => _ausgang = ok));

    internal static void ReiterWaehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, string titel)
        => cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == titel).Click();

    internal static IElement Karte(IRenderedComponent<GebaeudeKatalogDialog> cut, KonditionierungGroesse g)
        => cut.Find($"section.epos-kond-karte[data-groesse='{(int)g}']");

    internal static void Waehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, KonditionierungGroesse g, string name)
    {
        IElement liste = Karte(cut, g).QuerySelector(".epos-kond-vorlagewahl select")!;
        liste.Change(liste.QuerySelectorAll("option").First(o => o.TextContent.Trim() == name).GetAttribute("value")!);
    }

    internal static void Uebernehmen(IRenderedComponent<GebaeudeKatalogDialog> cut, KonditionierungGroesse g)
        => Karte(cut, g).QuerySelector("button.epos-kond-uebernehmen")!.Click();

    private static IElement Knopf(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Die Vorlagenverwaltung als Blatt (E56 F4 (a), Festlegung 13)
    // =================================================================================

    private static IElement Blatt(IRenderedComponent<GebaeudeKatalogDialog> cut) => cut.Find("section.epos-blatt");

    /// <summary>Die Zeile einer Vorlage in der Liste der Verwaltung.</summary>
    private static IElement Zeile(IRenderedComponent<GebaeudeKatalogDialog> cut, string name)
        => cut.FindAll("table.epos-kond-vorlagenliste tbody tr")
              .First(z => (z.QuerySelector(".epos-kond-vorlage-zeigen")?.TextContent.Trim() ?? "") == name);

    private static List<string> Namen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("table.epos-kond-vorlagenliste .epos-kond-vorlage-zeigen").Select(b => b.TextContent.Trim()).ToList();

    [Fact]
    public void Vorlagen_verwalten_oeffnet_das_Blatt_im_Editor_und_Esc_fuehrt_zurueck()
    {
        var cut = Editor(Konditionierungsvorlagenablage.AusSaat());
        ReiterWaehlen(cut, REITER);
        Karte(cut, KonditionierungGroesse.Lueftung).QuerySelector("button.epos-kond-verwalten")!.Click();

        // Das Blatt tauscht den Inhalt des Editors: kein Reiter, keine Fußleiste, dafür Rückknopf und Titel.
        Assert.True(cut.Instance.VorlagenblattOffen);
        Assert.Empty(cut.FindAll(".epos-reiter"));
        Assert.Empty(cut.FindAll(".epos-speichernleiste, .epos-leiste button.epos-knopf--primaer"));
        Assert.Equal("Vorlagen der Konditionierung", Blatt(cut).QuerySelector(".epos-blatt-titel")!.TextContent.Trim());
        Assert.StartsWith("‹", Blatt(cut).QuerySelector(".epos-blatt-zurueck")!.TextContent.Trim());
        Assert.Contains("sofort gespeichert", Blatt(cut).QuerySelector(".epos-kond-verwaltung-sofort")!.TextContent);

        // Die Größe der Karte steht vorn; die Liste mit Kennzeichen, die Handlungen immer sichtbar.
        Assert.Equal("true", cut.Find("button.epos-kond-verwaltung-groesse[data-groesse='2']").GetAttribute("aria-selected"));
        Assert.Equal(new[] { "Büro", "Schule" }, Namen(cut));
        Assert.Equal(2, cut.FindAll("table.epos-kond-vorlagenliste .epos-schloss").Count);
        Assert.Equal(2, cut.FindAll("table.epos-kond-vorlagenliste button.epos-kond-duplizieren").Count);

        // Esc führt zurück - der Editor bleibt offen, die Karte der Lüftung steht vorn.
        Blatt(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.VorlagenblattOffen);
        Assert.Null(_ausgang);
        Assert.Contains("epos-kond--aktiv", Karte(cut, KonditionierungGroesse.Lueftung).ClassList);
        Assert.Empty(_geschrieben);
    }

    [Fact]
    public void Ausgelieferte_nur_duplizieren_eigene_umbenennen_und_loeschen_jeweils_sofort()
    {
        var ablage = Konditionierungsvorlagenablage.AusSaat();
        var cut = Editor(ablage);
        ReiterWaehlen(cut, REITER);
        Karte(cut, KonditionierungGroesse.Heizen).QuerySelector("button.epos-kond-verwalten")!.Click();

        // Das Schloss: Umbenennen und Löschen weich gesperrt mit Grund; der Versuch meldet ihn.
        IElement umbenennen = Zeile(cut, "Büro").QuerySelector("button.epos-kond-umbenennen")!;
        Assert.Equal("true", umbenennen.GetAttribute("aria-disabled"));
        Assert.Contains("nur lesbar", umbenennen.GetAttribute("title"));
        umbenennen.Click();
        Assert.Contains("nur lesbar", Blatt(cut).QuerySelector(".epos-warnbanner")!.TextContent);
        Zeile(cut, "Büro").QuerySelector("button.epos-kond-loeschen")!.Click();
        Assert.Null(cut.FindAll(".epos-rueckfrage").FirstOrDefault());
        Assert.Equal(3, ablage.Liste(Konditionierungsgroesse.Heizsoll).Count);

        // Duplizieren schreibt sofort: „Büro (Kopie)", eigen, gewählt, mit Vorschau.
        Zeile(cut, "Büro").QuerySelector("button.epos-kond-duplizieren")!.Click();
        Assert.Equal(new[] { "Büro", "Schule", "Wohnen", "Büro (Kopie)" }, Namen(cut));
        Assert.Contains(ablage.Liste(Konditionierungsgroesse.Heizsoll), v => v.Bezeichner == "Büro (Kopie)" && !v.Ausgeliefert);
        Assert.Equal("Vorlage „Büro (Kopie)“ als Kopie von „Büro“ angelegt.",
                     cut.Find(".epos-kond-verwaltung-zeile").TextContent.Trim());
        Assert.Equal("true", Zeile(cut, "Büro (Kopie)").GetAttribute("aria-selected"));
        Assert.NotNull(cut.Find(".epos-kond-verwaltung-vorschau svg"));
        Assert.Empty(cut.FindAll(".epos-kond-verwaltung-vorschau .epos-diagramm-knopf"));   // ein Bild ohne Zoom
        Assert.Null(Zeile(cut, "Büro (Kopie)").QuerySelector(".epos-schloss"));

        // Umbenennen am Namensfeld: ein Doppelname steht am Feld und schreibt nichts.
        Zeile(cut, "Büro (Kopie)").QuerySelector("button.epos-kond-umbenennen")!.Click();
        cut.Find("table.epos-kond-vorlagenliste .epos-kond-vorlage-name input").Input("schule");
        cut.Find("button.epos-kond-umbenennen-schreiben").Click();
        Assert.Contains("epos-kond-feldfehler", cut.Find("table.epos-kond-vorlagenliste .epos-kond-vorlage-name").ClassList);
        Assert.Contains("„schule“", cut.Find(".epos-kond-feldmeldung").TextContent);
        Assert.Contains(ablage.Liste(Konditionierungsgroesse.Heizsoll), v => v.Bezeichner == "Büro (Kopie)");
        cut.Find("table.epos-kond-vorlagenliste .epos-kond-vorlage-name input").Input("Kontor");
        cut.Find("button.epos-kond-umbenennen-schreiben").Click();
        Assert.Contains(ablage.Liste(Konditionierungsgroesse.Heizsoll), v => v.Bezeichner == "Kontor");
        Assert.Equal("Vorlage umbenannt in „Kontor“.", cut.Find(".epos-kond-verwaltung-zeile").TextContent.Trim());

        // Löschen fragt (Vorgabe Nein) und nennt, dass kein Gebäude berührt wird.
        Zeile(cut, "Kontor").QuerySelector("button.epos-kond-loeschen")!.Click();
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.StartsWith("Die Vorlage „Kontor“ löschen?", frage.Trim());
        Assert.Contains("kein Gebäude", frage);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Contains("Kontor", Namen(cut));
        Zeile(cut, "Kontor").QuerySelector("button.epos-kond-loeschen")!.Click();
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.DoesNotContain("Kontor", Namen(cut));
        Assert.DoesNotContain(ablage.Liste(Konditionierungsgroesse.Heizsoll), v => v.Bezeichner == "Kontor");
        Assert.Equal("Vorlage „Kontor“ gelöscht.", cut.Find(".epos-kond-verwaltung-zeile").TextContent.Trim());

        // Zurück: der Editor schrieb nichts, die Karte kennt die Liste neu.
        cut.Find("button.epos-blatt-zurueck").Click();
        Assert.Empty(_geschrieben);
        Assert.Null(_ausgang);
        Assert.Equal(3, Karte(cut, KonditionierungGroesse.Heizen).QuerySelectorAll(".epos-kond-vorlagewahl option[value]:not([value=''])").Length);
    }

    [Fact]
    public void Der_Umschalter_zeigt_die_Liste_jeder_Groesse()
    {
        var cut = Editor(Konditionierungsvorlagenablage.AusSaat());
        ReiterWaehlen(cut, REITER);
        Karte(cut, KonditionierungGroesse.Heizen).QuerySelector("button.epos-kond-verwalten")!.Click();

        cut.Find("button.epos-kond-verwaltung-groesse[data-groesse='4']").Click();
        Assert.Equal("true", cut.Find("button.epos-kond-verwaltung-groesse[data-groesse='4']").GetAttribute("aria-selected"));
        Assert.Equal(new[] { "Büro", "Schule", "Wohnen" }, Namen(cut));
        Assert.Equal("4", cut.Find("table.epos-kond-vorlagenliste").GetAttribute("data-groesse"));

        // Ein Klick auf den Namen zeigt Beschreibung und Vorschau am Ziel.
        Zeile(cut, "Wohnen").QuerySelector(".epos-kond-vorlage-zeigen")!.Click();
        Assert.Contains("EPOS-Muster", cut.Find(".epos-kond-verwaltung-vorschau .epos-kond-vorlage-beschreibung").TextContent);
        Assert.NotNull(cut.Find(".epos-kond-verwaltung-vorschau svg"));
    }

    [Fact]
    public void Ohne_Handlungen_der_Verwaltung_im_Weg_steht_kein_Knopf()
    {
        KonditionierungWeg basis = KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat());
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Vollsatz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Konditionierung, new KonditionierungWeg
            {
                ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Vorlagen = basis.Vorlagen,
                VorlageUebernehmen = basis.VorlageUebernehmen, AlsVorlageSpeichern = basis.AlsVorlageSpeichern
            })
            .Add(x => x.EntprellungMs, 0));
        ReiterWaehlen(cut, REITER);

        Assert.Equal(5, cut.FindAll("button.epos-kond-als-vorlage").Count);
        Assert.Empty(cut.FindAll("button.epos-kond-verwalten"));
    }

    [Fact]
    public void Uebernehmen_wird_mit_OK_geschrieben_samt_Herkunft()
    {
        var cut = Editor(Konditionierungsvorlagenablage.AusSaat());
        ReiterWaehlen(cut, REITER);
        Waehlen(cut, KonditionierungGroesse.Personen, "Büro");
        Uebernehmen(cut, KonditionierungGroesse.Personen);
        Assert.Empty(_geschrieben);                                 // vor OK wird nichts geschrieben

        Knopf(cut, "OK").Click();
        Assert.True(_ausgang);
        GebaeudeKatalogDaten satz = Assert.Single(_geschrieben);
        KonditionierungKalender k = satz.Konditionierung!.Spalte(KonditionierungGroesse.Personen).Kalender;
        Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
        Assert.Equal("Büro", k.Vorlage);
        Assert.True(satz.Konditionierung.Fassung > 0);
        Assert.Equal(100, satz.Konditionierung.Spalte(KonditionierungGroesse.Personen).Tag.Wert);
    }

    [Fact]
    public void Abbrechen_verwirft_eine_uebernommene_Vorlage()
    {
        GebaeudeKatalogDaten satz = Vollsatz();
        var cut = Editor(Konditionierungsvorlagenablage.AusSaat(), satz);
        ReiterWaehlen(cut, REITER);
        Waehlen(cut, KonditionierungGroesse.Heizen, "Schule");
        Uebernehmen(cut, KonditionierungGroesse.Heizen);
        Assert.Contains("aus Vorlage Schule", Karte(cut, KonditionierungGroesse.Heizen).TextContent);

        Knopf(cut, "Abbrechen").Click();
        Assert.False(_ausgang);
        Assert.Empty(_geschrieben);
        // Der hereingereichte Satz bleibt unberührt.
        Assert.Equal(KonditionierungZustand.Abgeleitet, satz.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Zustand);
        Assert.Equal(17, satz.NachtAbsenkung);
    }

    // =================================================================================
    // Der Assistent (Entwurf KP2 D9: kond_<größe>_vorlage mit der Aktion des Knopfs)
    // =================================================================================

    private static KiFeldzugang KiFeld(string name) => KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_KATALOG, name)!;

    /// <summary>
    /// <b>Der Assistent übernimmt eine Vorlage</b> über das Feld <c>kond_&lt;größe&gt;_vorlage</c> — eine
    /// Wahl aus der Liste der Karte, gesetzt mit der Aktion des Knopfs „Übernehmen" in den Arbeitsstand;
    /// gelesen nennt es die Herkunft. Ein Name, den die Liste nicht führt, wird benannt abgelehnt; steht
    /// schon ein angelegter Kalender, bleibt die Rückfrage P12 dem Anwender, und der Assistent nennt sie.
    /// </summary>
    [Fact]
    public void Der_Assistent_uebernimmt_eine_Vorlage_mit_der_Aktion_des_Knopfs()
    {
        var cut = Editor(Konditionierungsvorlagenablage.AusSaat());
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;

        KiFeldzugang heizen = KiFeld("kond_heizen_vorlage");
        Assert.True(heizen.IstWahl);
        Assert.True(heizen.Setzbar);
        Assert.Equal(b.Vorlagen(KonditionierungGroesse.Heizen).Select(v => v.Name),
                     heizen.Wahleintraege().Select(e => e.Schluessel));
        Assert.Null(heizen.Lesen());

        heizen.Setzen("Büro");
        Assert.Equal("Büro", heizen.Lesen());
        Assert.Equal("Büro", b.Herkunft(KonditionierungGroesse.Heizen));
        Assert.Null(b.GewaehlteVorlage(KonditionierungGroesse.Heizen));   // E56 F2 (a): die Wahl ist danach leer
        Assert.Empty(_geschrieben);                                       // geschrieben wird mit OK

        // Ein Name, den die Liste nicht führt: benannt abgelehnt, nichts geändert.
        var unbekannt = Assert.Throws<InvalidOperationException>(() => heizen.Setzen("Sternwarte"));
        Assert.Contains("Sternwarte", unbekannt.Message);
        Assert.Equal("Büro", heizen.Lesen());

        // Der Kalender steht angelegt: Die Rückfrage P12 bleibt dem Anwender, der Assistent nennt sie.
        var frage = Assert.Throws<InvalidOperationException>(() => heizen.Setzen("Schule"));
        Assert.NotNull(b.OffeneFrage);
        Assert.Equal(b.OffeneFrage!.Text, frage.Message);
        Assert.Equal("Büro", heizen.Lesen());
        cut.Render();
        ReiterWaehlen(cut, REITER);
        cut.FindAll(".epos-rueckfrage button").First(x => x.TextContent.Trim() == "Ja").Click();
        Assert.Equal("Schule", heizen.Lesen());

        Knopf(cut, "OK").Click();
        Assert.Equal("Schule", Assert.Single(_geschrieben).Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Vorlage);
    }
}
