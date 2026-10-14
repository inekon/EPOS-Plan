using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using Resource = WindowsFormsApplication1.MyResource.Resource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KBP-3b: die Grafik der markierten Kältezeile in der Bearbeiten-Überlagerung des <see cref="BedarfsProfileDialog"/></b>
/// (Konzept Kältebedarfsprofile 5): Kennzahlenzeile mit vier Karten über den Blättern Jahresgang, Monate, Woche und
/// Dauerlinie; Blattwechsel; die Wahl „Datum | Stunde im Jahr“ nur am Jahresgang; das Ergebnis bleibt je Rechenstand
/// liegen und folgt der Jahressumme des Arbeitsstands; ohne Weg der Hülle keine Grafik.
/// </summary>
public class BedarfsProfileKaeltegrafikTests : EposBunitContext
{
    public BedarfsProfileKaeltegrafikTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static List<BedarfsProfilZeile> Zeilen() => new()
    {
        new() { IdZ = 1, IdStamm = 3, Name = "Büro", Summe = 20, Deckung = new DeckungsartDaten(), KaelteArt = DbWerte.KAELTE_ART_RAUM },
        new() { IdZ = 2, IdStamm = 4, Name = "Serverraum", Summe = 10, Deckung = new DeckungsartDaten(), KaelteArt = DbWerte.KAELTE_ART_RAUM },
    };

    /// <summary>Werktags 7 bis 19 Uhr Volllast, sonst ein Fünftel; skaliert auf die Jahressumme der Zeile [MWh].</summary>
    private static double[] Reihe(double summeMwh)
    {
        var r = new double[8760];
        for (int d = 0; d < 365; d++)
            for (int h = 0; h < 24; h++)
                r[d * 24 + h] = (d + 3) % 7 < 5 && h >= 7 && h < 19 ? 1.0 : 0.2;
        double s = r.Sum();
        for (int i = 0; i < r.Length; i++) r[i] *= summeMwh * 1000.0 / s;
        return r;
    }

    private int _aufrufe;

    private Kaelteprofilbilder? Rechnen(BedarfsProfilZeile markiert, IReadOnlyList<BedarfsProfilZeile> alle)
    {
        _aufrufe++;
        return Kaelteprofilgrafik.Bilder(Reihe(markiert.Summe), alle.Select(z => (z.Name, Reihe(z.Summe))).ToList(), 3, null);
    }

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(bool mitGrafik = true)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Kaelte, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1"))
            .Add(x => x.Jahressumme, _ => 1.0)
            .Add(x => x.KaelteArten, new[] { (DbWerte.KAELTE_ART_RAUM, "Raum"), (DbWerte.KAELTE_ART_PROZESS, "Prozess") })
            .Add(x => x.KaelteGrafik, mitGrafik ? Rechnen : null)
            .Add(x => x.Einheit, Energieeinheit.MWh));

    private static IElement Ueberlagerung(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    private static IElement Reiterknopf(IRenderedComponent<BedarfsProfileDialog> cut, string titel)
        => cut.FindAll(".epos-kbp-grafik button[role='tab']").Single(b => b.TextContent.Contains(titel));

    private static IRenderedComponent<DiagrammSvg> Bild(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindComponents<DiagrammSvg>().Single(d => d.Instance.Kennung.StartsWith("kbp-", StringComparison.Ordinal));

    [Fact]
    public void Die_Kennzahlenzeile_steht_ueber_den_vier_Blaettern()
    {
        var cut = Aufbauen();
        IElement satz = Ueberlagerung(cut);
        Assert.Contains(Resource.KBP_GRAFIK_GRUPPE, satz.TextContent);

        string[] titel = cut.FindAll(".epos-kbp-kennzahlen .epos-kennzahlkachel-titel").Select(e => e.TextContent).ToArray();
        string[] werte = cut.FindAll(".epos-kbp-kennzahlen .epos-kennzahlkachel-wert").Select(e => e.TextContent).ToArray();
        Assert.Equal(new[] { Resource.KBP_KZ_SUMME, Resource.KBP_KZ_SPITZE, Resource.KBP_KZ_VBH, Resource.KBP_KZ_GRUNDLAST }, titel);
        Kaelteprofilkennzahlen kz = Kaelteprofilgrafik.Kennzahlen(Reihe(20));
        Assert.Equal(kz.JahressummeMwh.ToString("N2", CultureInfo.CurrentCulture) + " MWh/a", werte[0]);
        Assert.Equal(kz.SpitzeKw.ToString("N1", CultureInfo.CurrentCulture) + " kW", werte[1]);
        Assert.Equal(kz.VollbenutzungsstundenH.ToString("N0", CultureInfo.CurrentCulture) + " h/a", werte[2]);
        Assert.Equal(kz.GrundlastAnteilProzent.ToString("N1", CultureInfo.CurrentCulture) + " %", werte[3]);

        // Die Kennzahlenzeile steht VOR der Reiterleiste.
        string html = cut.Find(".epos-kbp-grafik").InnerHtml;
        Assert.True(html.IndexOf("epos-kbp-kennzahlen", StringComparison.Ordinal) < html.IndexOf("epos-reiter", StringComparison.Ordinal));
        Assert.Equal(4, cut.FindAll(".epos-kbp-grafik button[role='tab']").Count);
    }

    [Fact]
    public void Der_Blattwechsel_zeigt_je_Blatt_sein_Bild_Achswahl_nur_am_Jahresgang()
    {
        var cut = Aufbauen();
        Ueberlagerung(cut);

        // Jahresgang (Vorgabe): Zeitachse des Jahresrasters - die Wahl „Datum | Stunde im Jahr“ steht da.
        Assert.Equal("kbp-jahresgang", Bild(cut).Instance.Kennung);
        Assert.True(Bild(cut).Instance.MitZeiteinheit);
        Assert.Equal(2, cut.FindAll(".epos-kbp-grafik button[data-einheit]").Count);

        Reiterknopf(cut, Resource.KBP_REITER_MONATE).Click();
        Assert.Equal("kbp-monate", Bild(cut).Instance.Kennung);
        Assert.False(Bild(cut).Instance.MitZeiteinheit);
        Assert.Empty(cut.FindAll(".epos-kbp-grafik button[data-einheit]"));
        Assert.Equal(2, Bild(cut).Instance.Modell!.Reihen.Count);         // zwei Zeilen: Stapel je Zeile

        Reiterknopf(cut, Resource.KBP_REITER_WOCHE).Click();
        Assert.Equal("kbp-woche", Bild(cut).Instance.Kennung);
        Assert.Equal(168, Bild(cut).Instance.Modell!.Reihen[0].Werte.Length);
        Assert.Empty(cut.FindAll(".epos-kbp-grafik button[data-einheit]"));

        Reiterknopf(cut, Resource.KBP_REITER_DAUERLINIE).Click();
        Assert.Equal("kbp-dauerlinie", Bild(cut).Instance.Kennung);
        Assert.False(Bild(cut).Instance.MitZeiteinheit);
        Assert.Empty(cut.FindAll(".epos-kbp-grafik button[data-einheit]"));
        Assert.Equal(KaelteProfilgrafik.REITER_DAUER, cut.FindComponent<KaelteProfilgrafik>().Instance.AktivesBlatt);
    }

    [Fact]
    public void Das_Ergebnis_bleibt_je_Rechenstand_liegen_und_folgt_der_Jahressumme()
    {
        var cut = Aufbauen();
        Ueberlagerung(cut);
        Kaelteprofilbilder? erst = cut.Instance.Grafikstand;
        Assert.NotNull(erst);
        int aufrufe = _aufrufe;

        // Ein Blattwechsel rechnet nicht neu - dieselbe Referenz.
        Reiterknopf(cut, Resource.KBP_REITER_WOCHE).Click();
        Assert.Same(erst, cut.Instance.Grafikstand);
        Assert.Equal(aufrufe, _aufrufe);

        // Eine neue Jahressumme im Arbeitsstand rechnet neu.
        cut.FindComponents<Zahlenfeld>().First(f => f.Instance.Bezeichnung == cut.Instance.LabelNeuerWert)
           .Find("input").Input("40");
        cut.FindAll(".epos-satzueberlagerung-koerper button.epos-knopf").First(b => b.TextContent == cut.Instance.BtnUebernehmenText).Click();
        Assert.NotSame(erst, cut.Instance.Grafikstand);
        Assert.Equal(40.0, cut.Instance.Grafikstand!.Kennzahlen.JahressummeMwh, 6);
    }

    [Fact]
    public void Ohne_Weg_der_Huelle_steht_keine_Grafik()
    {
        var cut = Aufbauen(mitGrafik: false);
        IElement satz = Ueberlagerung(cut);
        Assert.Empty(cut.FindAll(".epos-kbp-grafik"));
        Assert.DoesNotContain(Resource.KBP_GRAFIK_GRUPPE, satz.TextContent);
    }
}
