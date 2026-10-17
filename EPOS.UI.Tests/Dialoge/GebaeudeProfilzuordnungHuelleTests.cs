using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Hülle der Profilzuordnung</b> (ZZ-B) an der Beispieldatei <c>Quellen/Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc</c>
/// (113 Räume) mit dem Katalog einer Arbeitskopie der Testdatenbank: Zähler vor und nach „Alle Vorschläge übernehmen“, die
/// Zuordnung der Rückfragen über ihren ersten Kandidaten bis zur grünen Ampel, das Zurücknehmen — und derselbe Weg im
/// Importdialog über Knopf und Überlagerung. Fehlen Datei oder Testdatenbank (LFS), ist der Fall leer.
/// </summary>
public sealed class GebaeudeProfilzuordnungHuelleTests : EposBunitContext
{
    private const string DATEI = "Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc";

    private readonly ITestOutputHelper _aus;
    private readonly string _pfadVorher = DataRepository.PfadUeberschreibung;
    private readonly string? _ordner;

    public GebaeudeProfilzuordnungHuelleTests(ITestOutputHelper aus)
    {
        _aus = aus;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        string quelle = Path.Combine(Wurzel(), "Referenzlaeufe", "Kenndaten_Test.sqlite");
        if (File.Exists(quelle) && new FileInfo(quelle).Length > 1_000_000)
        {
            _ordner = Path.Combine(Path.GetTempPath(), "epos-uitest-pz-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_ordner);
            string ziel = Path.Combine(_ordner, "Kenndaten.sqlite");
            File.Copy(quelle, ziel);
            DataRepository.PfadUeberschreibung = ziel;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        DataRepository.PfadUeberschreibung = _pfadVorher;   // nie null (OhneAnwenderdatenbank)
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (_ordner is not null) Directory.Delete(_ordner, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))) d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string Probe => Path.Combine(Wurzel(), "Quellen", DATEI);

    private bool Bereit => _ordner is not null && File.Exists(Probe) && new FileInfo(Probe).Length > 10_000;

    private void Zeigen(string titel, GebaeudeProfilzuordnungDaten d)
        => _aus.WriteLine(titel + ": Bündel " + d.Buendel.Count + " (offen " + d.OffeneBuendel + "), " + d.Zaehlertext + ", Ampel " + d.Ampel);

    private static int Summe(GebaeudeProfilzuordnungDaten d) => d.Eindeutig + d.Vorschlag + d.Rueckfrage + d.Ohne + d.Hand;

    [Fact]
    public async Task Zaehler_vor_und_nach_Alle_Vorschlaege_uebernehmen_an_der_Verwaltung()
    {
        if (!Bereit) { _aus.WriteLine("Datei oder Testdatenbank fehlt — übersprungen."); return; }
        var huelle = new GebaeudeImportHuelle();
        var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)huelle.Gaben()["Zuordnen"];
        GebaeudeLesestand lese = await huelle.LesenAsync(Probe, null!, CancellationToken.None);
        Assert.True(lese.Gelesen);
        var anfrage = new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>());

        GebaeudeProfilzuordnungDaten vor = zuordnen(anfrage).Zonierung!.Plan!.Profilzuordnung!;
        Zeigen("vorher", vor);
        Assert.Equal(113, Summe(vor));
        Assert.Equal(113, vor.Buendel.Sum(b => b.Raeume.Count));
        Assert.InRange(vor.Buendel.Count, 10, 30);   // gebündelt statt 113 Zeilen (Entwurf: 17)
        Assert.True(vor.Vorschlag > 0);
        Assert.True(vor.Rueckfrage > 0);
        Assert.Equal(0, vor.Hand);
        Assert.Equal(GebaeudeProfilampel.ROT, vor.Ampel);
        Assert.Equal(vor.Buendel.Count(b => b.Offen), vor.OffeneBuendel);
        // Offene zuerst.
        int ersteNichtOffene = vor.Buendel.ToList().FindIndex(b => !b.Offen);
        Assert.All(vor.Buendel.Skip(ersteNichtOffene), b => Assert.False(b.Offen));
        Assert.Contains(vor.Katalog, k => k.Kinder.Count > 0);
        // Einzelraum: jedes Bündel trägt seine Räume als Zeilen; ohne Projektdatei keine Restraeume.
        Assert.All(vor.Buendel, b => Assert.Equal(b.Raeume, b.Raumzeilen.Select(r => r.Kennung)));
        Assert.Contains(vor.Buendel, b => b.Beheizt);
        Assert.Empty(vor.Restraeume);
        Assert.Equal(vor.Vorschlag, vor.Buendel.Where(b => b.Stand == GebaeudeProfilstand.VORSCHLAG).Sum(b => b.Raeume.Count)
                                    + vor.Dateizonen.Sum(z => z.Raeume.Count));

        // Alle Vorschläge übernehmen: die Vorschläge werden Wahlen von Hand, die Rückfragen bleiben.
        GebaeudeZuordnungsanfrage alle = anfrage.MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.VORSCHLAEGE));
        GebaeudeProfilzuordnungDaten nach = zuordnen(alle).Zonierung!.Plan!.Profilzuordnung!;
        Zeigen("nach Übernahme", nach);
        Assert.Equal(0, nach.Vorschlag);
        Assert.Equal(vor.Vorschlag, nach.Hand);
        Assert.Equal((vor.Eindeutig, vor.Rueckfrage, vor.Ohne), (nach.Eindeutig, nach.Rueckfrage, nach.Ohne));
        Assert.Equal(vor.OffeneBuendel, nach.OffeneBuendel);

        // Jede Rückfrage über ihren ersten Kandidaten: die Ampel wird grün.
        GebaeudeZuordnungsanfrage hand = alle;
        foreach (GebaeudeProfilbuendelDaten b in nach.Buendel.Where(b => b.Offen && b.Kandidaten.Count > 0))
            hand = hand.MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.PROFIL, b.Raeume, Nutzung: b.Kandidaten[0].Schluessel));
        GebaeudeZonenplanDaten planHand = zuordnen(hand).Zonierung!.Plan!;
        GebaeudeProfilzuordnungDaten gruen = planHand.Profilzuordnung!;
        Zeigen("nach Zuordnung", gruen);
        Assert.False(planHand.LetzterAbgelehnt);
        Assert.Equal(0, gruen.OffeneBuendel);
        Assert.Equal(113 - gruen.Eindeutig, gruen.Hand);
        Assert.Equal(GebaeudeProfilampel.GRUEN, gruen.Ampel);
        Assert.Empty(planHand.NichtZugeordnet);

        // Zurücknehmen: wieder der Stand vor der Übernahme.
        GebaeudeProfilzuordnungDaten zurueck = zuordnen(hand.MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.VORSCHLAEGE_ZURUECK)))
            .Zonierung!.Plan!.Profilzuordnung!;
        Assert.Equal((vor.Eindeutig, vor.Vorschlag, vor.Rueckfrage, vor.Ohne, vor.Hand),
                     (zurueck.Eindeutig, zurueck.Vorschlag, zurueck.Rueckfrage, zurueck.Ohne, zurueck.Hand));

        // Einzelraum: ein Raum eines Bündels bekommt ein anderes Profil, das Bündel ist danach gemischt.
        GebaeudeProfilbuendelDaten mehr = nach.Buendel.First(b => b.Raeume.Count > 1 && b.Kandidaten.Count > 1);
        GebaeudeProfilzuordnungDaten einzeln = zuordnen(alle
                .MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.PROFIL, mehr.Raeume, Nutzung: mehr.Kandidaten[0].Schluessel))
                .MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.PROFIL, mehr.Raeume.Take(1).ToList(), Nutzung: mehr.Kandidaten[1].Schluessel)))
            .Zonierung!.Plan!.Profilzuordnung!;
        Assert.Contains(einzeln.Buendel, b => b.Gemischt && b.Raumzeilen.Select(r => r.ProfilText).Distinct().Count() > 1);
        Assert.Equal(113, Summe(einzeln));

        // Ein unbekannter Profilschlüssel wird abgelehnt.
        GebaeudeZonenplanDaten ab = zuordnen(anfrage.MitPlanschritt(new GebaeudePlanschritt(GebaeudePlanschrittArt.PROFIL,
                                                                                            vor.Buendel[0].Raeume, Nutzung: "999"))).Zonierung!.Plan!;
        Assert.True(ab.LetzterAbgelehnt);
    }

    [Fact]
    public void Im_Importdialog_Knopf_mit_Zaehler_Ueberlagerung_und_neuer_Zonenbaum()
    {
        if (!Bereit) { _aus.WriteLine("Datei oder Testdatenbank fehlt — übersprungen."); return; }
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        var schritte = new List<GebaeudePlanschritt>();
        var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"];
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(Probe, DATEI, new FileInfo(Probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a =>
            {
                if (a.Planschritte is { Count: > 0 } s) { schritte.Clear(); schritte.AddRange(s); }
                return zuordnen(a);
            }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(_ => Task.FromResult<string?>(null)));
        });
        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).KlickAbgewartet();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum")), TimeSpan.FromSeconds(60));

        var knopf = cut.Find("button.epos-gebimport-profilzuordnung-oeffnen");
        int offen = int.Parse(knopf.GetAttribute("data-offen")!);
        Assert.True(offen > 0);
        Assert.Contains("(" + offen + " offen)", knopf.TextContent);
        Assert.Equal(GebaeudeProfilampel.ROT, cut.Find(".epos-gebimport-profilampel").GetAttribute("data-ampel"));
        int zonenVorher = cut.FindAll(".epos-gebimport-zonenbaum tr.epos-gebimport-planzone").Count;

        knopf.KlickAbgewartet();
        Assert.True(cut.Instance.ProfilzuordnungOffen);
        cut.Find("button.epos-gebimport-pz-alle").KlickAbgewartet();
        Assert.Equal(GebaeudePlanschrittArt.VORSCHLAEGE, schritte.Last().Art);

        // Das erste offene Bündel über die Schnellwahl: der Zähler am Knopf sinkt, der Zonenbaum ist neu gebildet.
        string buendel = cut.Find("tr.epos-gebimport-pz-zeile[data-stand='OFFEN'] , tr.epos-gebimport-pz-zeile[data-stand='OHNE']")
                            .GetAttribute("data-buendel")!;
        cut.Find("tr[data-buendel='" + buendel + "'] input.epos-gebimport-pz-buendelwahl").WechselAbgewartet(true);
        cut.FindAll("button.epos-gebimport-pz-kandidat").First().KlickAbgewartet();
        Assert.Equal(GebaeudePlanschrittArt.PROFIL, schritte.Last().Art);
        Assert.Equal(offen - 1, int.Parse(cut.Find("button.epos-gebimport-profilzuordnung-oeffnen").GetAttribute("data-offen")!));
        Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum tr.epos-gebimport-planzone"));
        _aus.WriteLine("Zonen vorher " + zonenVorher + ", nachher " + cut.FindAll(".epos-gebimport-zonenbaum tr.epos-gebimport-planzone").Count);

        // Rest 1: ein Schritt von Hand nach der Neubildung — die nächste Profiloperation fragt erst; „Nein“ lässt alles.
        cut.FindAll("input.epos-gebimport-zonenname").First().WechselAbgewartet("Umbenannt von Hand");
        Assert.Equal(GebaeudePlanschrittArt.UMBENENNEN, cut.Instance.Planschritte.Last().Art);
        int vorFrage = cut.Instance.Planschritte.Count;
        cut.Find("button.epos-gebimport-pz-zurueck").KlickAbgewartet();
        Assert.True(cut.Instance.ProfilFrageOffen);
        Assert.Contains("1", cut.Find(".epos-rueckfrage-text").TextContent);
        cut.FindAll("button").First(k => k.TextContent == "Nein").KlickAbgewartet();
        Assert.False(cut.Instance.ProfilFrageOffen);
        Assert.Equal(vorFrage, cut.Instance.Planschritte.Count);
        Assert.True(cut.Instance.ProfilzuordnungOffen);
        cut.Find("button.epos-gebimport-pz-zurueck").KlickAbgewartet();
        cut.FindAll("button").First(k => k.TextContent == "Ja").KlickAbgewartet();
        Assert.Equal(GebaeudePlanschrittArt.VORSCHLAEGE_ZURUECK, cut.Instance.Planschritte.Last().Art);
        // Danach steht kein Schritt von Hand mehr nach der Neubildung: keine Rückfrage.
        cut.Find("button.epos-gebimport-pz-alle").KlickAbgewartet();
        Assert.False(cut.Instance.ProfilFrageOffen);
        Assert.Equal(GebaeudePlanschrittArt.VORSCHLAEGE, cut.Instance.Planschritte.Last().Art);
    }

    [Fact]
    public void Gefaehrdet_zaehlt_die_Schritte_von_Hand_nach_der_letzten_Neubildung_ohne_Haken()
    {
        GebaeudePlanschritt S(string art) => new GebaeudePlanschritt(art);
        Assert.Equal(0, GebaeudePlanschrittArt.Gefaehrdet(Array.Empty<GebaeudePlanschritt>()));
        Assert.Equal(2, GebaeudePlanschrittArt.Gefaehrdet(new[] { S(GebaeudePlanschrittArt.ANLEGEN), S(GebaeudePlanschrittArt.HAKEN), S(GebaeudePlanschrittArt.ZUORDNEN) }));
        Assert.Equal(0, GebaeudePlanschrittArt.Gefaehrdet(new[] { S(GebaeudePlanschrittArt.ANLEGEN), S(GebaeudePlanschrittArt.PROFIL) }));
        Assert.Equal(1, GebaeudePlanschrittArt.Gefaehrdet(new[] { S(GebaeudePlanschrittArt.UMBENENNEN), S(GebaeudePlanschrittArt.VORSCHLAEGE), S(GebaeudePlanschrittArt.RESTPROFIL) }));
        Assert.Equal(1, GebaeudePlanschrittArt.Gefaehrdet(new[] { S(GebaeudePlanschrittArt.PROJEKTDATEI) }));
        Assert.Equal(0, GebaeudePlanschrittArt.Gefaehrdet(new[] { S(GebaeudePlanschrittArt.PROFIL_ZURUECK), S(GebaeudePlanschrittArt.HAKEN) }));
    }
}
