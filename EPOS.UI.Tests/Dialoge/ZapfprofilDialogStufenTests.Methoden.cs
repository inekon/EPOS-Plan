using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Methoden als Bereiche (Konzept Zapfprofil — Bereiche und Methoden, ZC5): In der Stufe Experte stehen unter dem
/// Block der Bereiche sechs Klappgruppen — Bedarf, Kalender, Messwert, Jahresreihe, Zirkulation und Anlage,
/// Auslegung —, jede mit Wirkungsmarke im Kopf, einem Satz unter dem Kopf und der Zusammenfassung zugeklappt. Felder je
/// Bereich stehen unter der Marke des markierten Bereichs, Felder je Projekt darunter unter „Gebäude und Anlage".
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    private static readonly string[] METHODEN = { "bedarf", "kalender", "messwert", "jahresreihe", "zirkulation", "auslegung" };

    /// <summary>Die Marken der Teile einer Methode, in ihrer Reihenfolge.</summary>
    private static string[] Teilmarken(IRenderedComponent<ZapfprofilDialog> cut, string methode)
        => cut.FindAll(".epos-zapfprofil-karte--" + methode + " .epos-zapfprofil-teilmarke").Select(t => t.TextContent.Trim()).ToArray();

    private static IElement Rechenwegoption(IRenderedComponent<ZapfprofilDialog> cut, string text)
        => cut.FindAll(".epos-zapfprofil-karte--jahresreihe fieldset[aria-label='Rechenweg der Jahresreihe'] label.epos-option")
              .First(l => l.TextContent.Trim() == text).QuerySelector("input")!;

    private static bool HatWahlfeld(IRenderedComponent<ZapfprofilDialog> cut, string anfang)
        => cut.FindAll("label.epos-feld").Any(l => l.TextContent.TrimStart().StartsWith(anfang, StringComparison.Ordinal));

    [Fact]
    public void ZC5_Sechs_Methoden_in_Reihenfolge_mit_Wirkungsmarke_Satz_und_Zusammenfassung()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte", aufklappen: false);

        IElement[] gruppen = cut.FindAll(".epos-zapfprofil-methoden > .epos-zapfprofil-methode").ToArray();
        Assert.Equal(6, gruppen.Length);
        for (int i = 0; i < METHODEN.Length; i++)
            Assert.Contains("epos-zapfprofil-karte--" + METHODEN[i], gruppen[i].ClassName ?? "");
        Assert.Equal(new[] { "Bedarf", "Kalender", "Messwert", "Jahresreihe", "Zirkulation und Anlage", "Auslegung" },
                     gruppen.Select(g => g.QuerySelector(".epos-klappgruppe-text")!.TextContent.Trim()).ToArray());

        // Die Wirkungsmarke steht rechts im Kopf — außerhalb des Klappknopfs, auch zugeklappt.
        Assert.Equal(new[] { "Summe", "Verteilung", "Summe", "Jahresreihe", "Simulation", "nur Auslegung" },
                     gruppen.Select(g => g.QuerySelector(".epos-klappgruppe-handlungen .epos-zapfprofil-wirkung")!.TextContent.Trim()).ToArray());
        Assert.Equal("Wirkt auf: nur Auslegung", gruppen[5].QuerySelector(".epos-zapfprofil-wirkung")!.GetAttribute("title"));
        Assert.All(gruppen, g => Assert.Null(g.QuerySelector(".epos-klappgruppe-knopf .epos-zapfprofil-wirkung")));

        // Zugeklappt: jede nennt ihre wirksamen Werte, der Satz steht erst aufgeklappt.
        Assert.All(gruppen, g => Assert.False(string.IsNullOrWhiteSpace(g.QuerySelector(".epos-klappgruppe-zusatz")?.TextContent)));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-methodensatz"));

        ZapfprofilKarten.AlleAuf(cut);
        var texte = new ZapfprofilTexte();
        Assert.Equal(new[] { texte.SatzBedarf, texte.SatzKalender, texte.SatzMesswert, texte.SatzJahresreihe, texte.SatzZirkulation,
                             texte.SatzAuslegung },
                     cut.FindAll(".epos-zapfprofil-methodensatz").Select(s => s.TextContent.Trim()).ToArray());
        // Der Satz steht als erstes im Inhalt seiner Gruppe.
        Assert.All(cut.FindAll(".epos-zapfprofil-methode > .epos-klappgruppe-inhalt"),
                   inhalt => Assert.Contains("epos-zapfprofil-methodensatz", inhalt.FirstElementChild!.ClassName ?? ""));
    }

    [Fact]
    public void ZC5_Felder_je_Bereich_stehen_unter_der_Marke_des_Bereichs_und_wandern_mit_der_Wahl()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");

        // Je Bereich zuerst, je Projekt darunter - in den Gruppen, die beides tragen.
        Assert.Equal(new[] { "Bereich „Wohnen“" }, Teilmarken(cut, "bedarf"));
        Assert.Equal(new[] { "Bereich „Wohnen“" }, Teilmarken(cut, "kalender"));
        Assert.Equal(new[] { "Bereich „Wohnen“" }, Teilmarken(cut, "messwert"));
        foreach (string m in new[] { "jahresreihe", "zirkulation", "auslegung" })
            Assert.Equal(new[] { "Bereich „Wohnen“", "Gebäude und Anlage" }, Teilmarken(cut, m));
        Assert.Equal("Bereich „Wohnen“", cut.FindAll(".epos-gruppenkopf")
                                            .First(g => g.QuerySelector(".epos-gruppenkopf-titel")!.TextContent.Trim() == "Methoden")
                                            .QuerySelector(".epos-gruppenkopf-summe")!.TextContent.Trim());

        // Ein Feld je Bereich gehört dem markierten Bereich, ein Feld je Projekt allen.
        Assert.NotNull(cut.Find(".epos-zapfprofil-karte--bedarf .epos-zapfprofil-methodenteil--bereich")
                          .QuerySelectorAll(".epos-feld-text").FirstOrDefault(t => t.TextContent.Trim() == "Zapftemperatur"));
        Assert.NotNull(cut.Find(".epos-zapfprofil-karte--auslegung .epos-zapfprofil-methodenteil--projekt")
                          .QuerySelectorAll(".epos-feld-text").FirstOrDefault(t => t.TextContent.Trim() == "Speichertemperatur"));
        Feld(cut, "Zapftemperatur").Input("55");
        Feld(cut, "Speichertemperatur").Input("62");
        Assert.Equal(55.0, cut.Instance.Eingabe.Zonen[0].Angaben!.ZapftemperaturC);

        Bereichzeilen(cut)[1].QuerySelector("button")!.Click();
        Assert.Equal(new[] { "Bereich „Büro“", "Gebäude und Anlage" }, Teilmarken(cut, "auslegung"));
        Assert.Equal("", Feld(cut, "Zapftemperatur").GetAttribute("value") ?? "");
        Assert.Equal("62", Feld(cut, "Speichertemperatur").GetAttribute("value"));
        // Die Gruppen bleiben offen - die Wahl des Bereichs klappt nichts zu.
        Assert.All(METHODEN, m => Assert.True(cut.Instance.KarteIstOffen(m), m));

        Bereichzeilen(cut)[0].QuerySelector("button")!.Click();
        Assert.Equal("55", Feld(cut, "Zapftemperatur").GetAttribute("value"));
    }

    [Fact]
    public void ZC5_Die_Jahresreihe_zeigt_nur_die_Felder_des_gewaehlten_Wegs()
    {
        var cut = AufbauenMitTyptagen(Typtagstand(zonen: 1));
        Stufe(cut, "Experte");
        Assert.True(Rechenwegoption(cut, "Norm-Jahresgang").HasAttribute("checked"));
        Assert.Equal("Norm-Jahresgang", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));
        // Seed und Realisierungen stehen bei jedem Weg; beim Norm-Jahresgang wirken sie nur auf das Ensemble der Auslegung.
        Assert.True(HatFeld(cut, "Zufallssaat (Seed)"));
        Assert.True(HatFeld(cut, "Realisierungen"));
        Assert.Contains("Wirkt hier nur auf das Ensemble der Auslegung.", cut.Find(".epos-zapfprofil-karte--jahresreihe").TextContent);
        Assert.False(HatWahlfeld(cut, "Klimazone"));

        // stochastisch: dazu der Hinweis auf die Konsistenzprobe.
        Rechenwegoption(cut, "stochastisch").Change("1");
        Assert.True(cut.Instance.Eingabe.JahresreiheStochastisch);
        Assert.True(HatFeld(cut, "Zufallssaat (Seed)"));
        Assert.True(HatFeld(cut, "Realisierungen"));
        Assert.False(HatWahlfeld(cut, "Klimazone"));
        Assert.Contains("Die Konsistenzprobe steht nach dem Lauf im Reiter Kennzahlen.", cut.Find(".epos-zapfprofil-karte--jahresreihe").TextContent);
        Assert.Equal("stochastisch · 10 Jahre · Seed 1", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));

        // Typtage: Klimazone und Gebäudeart, die Stochastik geht aus.
        Rechenwegoption(cut, "Typtage VDI 4655").Change("2");
        Assert.True(cut.Instance.Eingabe.TyptageAktiv);
        Assert.False(cut.Instance.Eingabe.JahresreiheStochastisch);
        Assert.True(HatFeld(cut, "Zufallssaat (Seed)"));
        Assert.True(HatWahlfeld(cut, "Klimazone"));
        Assert.True(HatWahlfeld(cut, "Gebäudeart"));
        Assert.Equal("Typtage VDI 4655", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));

        // Zurück zum Norm-Jahresgang: beide aus, keine Felder des Wegs.
        Rechenwegoption(cut, "Norm-Jahresgang").Change("0");
        Assert.False(cut.Instance.Eingabe.TyptageAktiv);
        Assert.False(cut.Instance.Eingabe.JahresreiheStochastisch);
        Assert.False(HatWahlfeld(cut, "Klimazone"));
    }

    [Fact]
    public void ZC5_Die_Topologie_steht_in_der_Auslegung_und_nicht_mehr_in_der_Detailzeile()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");

        IElement topologie = Auswahl(cut, "Anlagentopologie");
        Assert.NotNull(topologie.Closest(".epos-zapfprofil-karte--auslegung .epos-zapfprofil-methodenteil--bereich"));
        Assert.Single(cut.FindAll(".epos-feld-text").Where(t => t.TextContent.Trim() == "Anlagentopologie"));
        Assert.DoesNotContain("Topologie", cut.Find(".epos-zapfprofil-bereichdetail").TextContent);

        string durchfluss = topologie.QuerySelectorAll("option").First(o => o.TextContent.Trim() == "Durchfluss").GetAttribute("value")!;
        topologie.Change(durchfluss);
        Assert.Equal(ZapfprofilTopologie.Durchfluss, cut.Instance.Eingabe.Zonen[0].Angaben!.Topologie);
        Assert.StartsWith("Durchfluss · ", ZapfprofilKarten.Zusatz(cut, "auslegung"));

        // „Auslegung…" steht in der Methode und weiter in der Schlussleiste.
        Assert.NotNull(cut.Find(".epos-zapfprofil-karte--auslegung .epos-zapfprofil-auslegung-methode"));
        Assert.Equal(2, cut.FindAll("button").Count(b => b.TextContent.Trim() == "Auslegung…"));
    }

    [Fact]
    public void ZC5_Einfach_zeigt_keine_der_sechs_Methoden()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-zapfprofil-methode"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-wirkung"));

        Stufe(cut, "Experte", aufklappen: false);
        Assert.Equal(6, cut.FindAll(".epos-zapfprofil-methode").Count);

        Stufe(cut, "Einfach", aufklappen: false);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-methode"));
        Assert.Equal(new[] { "Nutzungsbereiche", "Vorschau" }, Gruppen(cut));
        Assert.False(HatFeld(cut, "Anlagentopologie"));
    }
}
