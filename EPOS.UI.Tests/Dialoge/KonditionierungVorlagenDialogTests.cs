using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
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
}
