using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// ZC4 (c): Unter der Topologie-Wahl der Gruppe „Auslegung" steht das Ergebnis der Topologiegruppe des markierten
/// Bereichs — aus der Auslegung zum Arbeitsstand (Delegat <c>Auslegungskurz</c>), entprellt mit der Vorschau; die
/// Zusammenfassung nennt Topologie und Hauptergebnis.
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    /// <summary>Die Kurzauslegung der Probe: je Topologie der Bereiche eine Gruppe, wie die Hülle sie liefert.</summary>
    private static ZapfprofilAuslegungKurzDaten Kurzauslegung(ZapfprofilEingabeDaten e)
    {
        var k = new ZapfprofilAuslegungKurzDaten
        {
            Ladeleistung = new ZapfprofilSchaetzhilfeDaten { Einheit = "kW", Vorschlag = 12, Angesetzt = 12, Auto = true }
        };
        foreach (ZapfprofilTopologie t in e.Zonen.Select(z => z.Angaben?.Topologie ?? ZapfprofilTopologie.Speicher).Distinct())
            k.Gruppen.Add(t switch
            {
                ZapfprofilTopologie.Speicher => new ZapfprofilGruppenergebnisDaten(t, "Speicher: Nenninhalt 300 l · Ladeleistung 12 kW", "", "300 l · 12 kW"),
                ZapfprofilTopologie.Wohnungsstation => new ZapfprofilGruppenergebnisDaten(t, "Wohnungsstation: Minutenspitze 60 kW (Summe)",
                                                                                          "Angegeben ist die Summe.", "60 kW"),
                _ => new ZapfprofilGruppenergebnisDaten(t, "Frischwasserstation: Minutenspitze 48 kW", "", "48 kW")
            });
        return k;
    }

    private int _kurzlaeufe;

    private IRenderedComponent<ZapfprofilDialog> MitKurzauslegung(Func<ZapfprofilEingabeDaten, ZapfprofilAuslegungKurzDaten?>? kurz = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, Vorschau)
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Auslegungskurz, kurz ?? (e => { _kurzlaeufe++; return Kurzauslegung(e); }))
            .Add(x => x.Geschlossen, _ => { }));

    private static string[] Ergebniszeilen(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.FindAll(".epos-zapfprofil-karte--auslegung .epos-zapfprofil-gruppenergebnis, .epos-zapfprofil-karte--auslegung .epos-zapfprofil-gruppenergebnis-hinweis")
              .Select(p => p.TextContent.Trim()).ToArray();

    [Fact]
    public void ZC4c_Unter_der_Topologie_steht_das_Ergebnis_ihrer_Gruppe_und_die_Zusammenfassung_nennt_es()
    {
        var cut = MitKurzauslegung();
        Stufe(cut, "Experte");

        cut.WaitForAssertion(() => Assert.Equal(new[] { "Speicher: Nenninhalt 300 l · Ladeleistung 12 kW" }, Ergebniszeilen(cut)));
        IElement zeile = cut.Find(".epos-zapfprofil-gruppenergebnis");
        Assert.NotNull(zeile.Closest(".epos-zapfprofil-methodenteil--bereich"));
        Assert.Contains("epos-leisezeile", zeile.ClassName);
        Assert.StartsWith("Speicher · 300 l · 12 kW · ", ZapfprofilKarten.Zusatz(cut, "auslegung"));
        // Der Vorschlag der Ladeleistung kommt aus demselben Lauf.
        Assert.Contains("12", cut.Find(".epos-zapfprofil-karte--auslegung").TextContent);
    }

    [Fact]
    public void ZC4c_Ein_Wechsel_der_Topologie_rechnet_neu_und_zeigt_das_Ergebnis_der_neuen_Gruppe()
    {
        var cut = MitKurzauslegung();
        Stufe(cut, "Experte");
        cut.WaitForAssertion(() => Assert.Single(Ergebniszeilen(cut)));
        int vorher = _kurzlaeufe;

        IElement topologie = Auswahl(cut, "Anlagentopologie");
        topologie.Change(topologie.QuerySelectorAll("option").First(o => o.TextContent.Trim() == "Frischwasserstation").GetAttribute("value")!);
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Frischwasserstation: Minutenspitze 48 kW" }, Ergebniszeilen(cut)));
        Assert.True(_kurzlaeufe > vorher, "Die Auslegung wurde nicht neu gerechnet.");
        Assert.StartsWith("Frischwasserstation · 48 kW · ", ZapfprofilKarten.Zusatz(cut, "auslegung"));

        topologie = Auswahl(cut, "Anlagentopologie");
        topologie.Change(topologie.QuerySelectorAll("option").First(o => o.TextContent.Trim() == "Wohnungsstation").GetAttribute("value")!);
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Wohnungsstation: Minutenspitze 60 kW (Summe)", "Angegeben ist die Summe." },
                                                Ergebniszeilen(cut)));
    }

    [Fact]
    public void ZC4c_Ohne_Lauf_der_Auslegung_steht_kein_Ergebnis_und_die_Zusammenfassung_bleibt()
    {
        var cut = MitKurzauslegung(_ => null);
        Stufe(cut, "Experte");

        Assert.Empty(Ergebniszeilen(cut));
        Assert.StartsWith("Speicher · ", ZapfprofilKarten.Zusatz(cut, "auslegung"));
        Assert.DoesNotContain(" l · ", ZapfprofilKarten.Zusatz(cut, "auslegung"));
    }
}
