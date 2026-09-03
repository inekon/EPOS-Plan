using Bunit;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Positionszeile der Kostenverwaltung (iU9-W4.1), Vorbild
/// <c>Views/Kosten/ucVorlagenZeile</c>.
///
/// <para>Soll ist die Feldkarte: zwei Aktionsknöpfe, Bezeichnung,
/// Bemessung, Satz, Betrag (nur Anzeige), Nutzungsdauer und ±.</para>
/// </summary>
public class VorlagenZeileTests : BunitContext
{
    private static readonly (int Id, string Text)[] BEMESSUNGEN =
    {
        (0, "Betrag [€]"), (1, "% der Investition"), (2, "€/kW")
    };

    private IRenderedComponent<VorlagenZeile> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<VorlagenZeile>>? mehr = null)
        => Render<VorlagenZeile>(p =>
        {
            p.Add(x => x.Bemessungen, BEMESSUNGEN);
            p.Add(x => x.Bezeichnung, "Montage");
            p.Add(x => x.BemessungId, 0);
            p.Add(x => x.Satz, 1200.0);
            p.Add(x => x.BetragText, "1200");
            p.Add(x => x.MitNutzungsdauer, true);
            p.Add(x => x.Nutzungsdauer, 20.0);
            mehr?.Invoke(p);
        });

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Die_Zeile_hat_sieben_Zellen_in_der_Reihenfolge_der_Feldkarte()
    {
        var cut = Zeige();

        Assert.Equal(7, cut.FindAll(".epos-zr-zelle").Count);
    }

    [Fact]
    public void Sie_traegt_Stift_und_Papierkorb_und_im_Projektmodus_das_Plusminus()
    {
        var cut = Zeige(p => p.Add(x => x.MitWorstBest, true));

        var knoepfe = cut.FindAll("button");
        Assert.Equal(3, knoepfe.Count);
        Assert.Equal("✏️", knoepfe[0].TextContent);
        Assert.Equal("🗑️", knoepfe[1].TextContent);
        Assert.Equal("±", knoepfe[2].TextContent);
    }

    [Fact]
    public void Ohne_Projektmodus_fehlt_das_Plusminus()
    {
        var cut = Zeige(p => p.Add(x => x.MitWorstBest, false));

        Assert.Equal(2, cut.FindAll("button").Count);
    }

    [Fact]
    public void Die_Werte_stehen_in_den_Feldern()
    {
        var cut = Zeige();

        var texte = cut.FindAll("input[type=text]");
        Assert.Equal("Montage", texte[0].GetAttribute("value"));
        Assert.Equal("1200", cut.Find(".epos-zr-betrag").TextContent);
        Assert.Equal("0", cut.Find("select").GetAttribute("value") ?? "0");
    }

    [Fact]
    public void Ohne_Investitionskosten_gibt_es_kein_Nutzungsdauerfeld()
    {
        var mit = Zeige();
        var ohne = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.MitNutzungsdauer, false));

        Assert.Equal(3, mit.FindAll("input[type=text]").Count);
        Assert.Equal(2, ohne.FindAll("input[type=text]").Count);
    }

    // =====================================================================
    // Kopplung Satz / Betrag (KL4, § 5.4)
    // =====================================================================

    [Fact]
    public void Bei_absoluter_Bemessung_zeigt_die_Zeile_die_Kette()
    {
        var cut = Zeige(p => p.Add(x => x.Kette, true));

        Assert.Equal("🔗", cut.Find(".epos-zr-kette").TextContent);
    }

    [Fact]
    public void Ohne_Kopplung_fehlt_die_Kette()
    {
        var cut = Zeige(p => p.Add(x => x.Kette, false));

        Assert.Empty(cut.FindAll(".epos-zr-kette"));
    }

    [Fact]
    public void Der_Betrag_ist_niemals_eingebbar()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.MitNutzungsdauer, true)
            .Add(x => x.BetragText, "—"));

        // Vier Eingabefelder gäbe es mit dem Betrag; er ist reine Anzeige.
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        Assert.Equal("—", cut.Find(".epos-zr-betrag").TextContent);
    }

    [Fact]
    public void Der_Kurztext_des_Betrags_kommt_vom_Wirt()
    {
        var cut = Zeige(p => p.Add(x => x.BetragKurztext,
            "Bezugsgröße erst im Projekt bekannt."));

        Assert.Contains("Bezugsgröße erst im Projekt bekannt.", cut.Markup);
    }

    [Fact]
    public void Der_Empfehlungsbereich_steht_am_Satzfeld()
    {
        var cut = Zeige(p => p.Add(x => x.EmpfehlungKurztext, "Empfehlung: 800 – 1.400 €"));

        Assert.Contains("Empfehlung: 800 – 1.400 €", cut.Markup);
    }

    // =====================================================================
    // Meldungen
    // =====================================================================

    [Fact]
    public void Stift_Papierkorb_und_Plusminus_melden_sich()
    {
        int editor = 0, loeschen = 0, worstBest = 0;
        var cut = Zeige(p => p
            .Add(x => x.MitWorstBest, true)
            .Add(x => x.EditorAngefordert, () => editor++)
            .Add(x => x.LoeschenAngefordert, () => loeschen++)
            .Add(x => x.WorstBestAngefordert, () => worstBest++));

        cut.FindAll("button")[0].Click();
        cut.FindAll("button")[1].Click();
        cut.FindAll("button")[2].Click();

        Assert.Equal(1, editor);
        Assert.Equal(1, loeschen);
        Assert.Equal(1, worstBest);
    }

    [Fact]
    public void Eine_getippte_Bezeichnung_wird_gemeldet()
    {
        string? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.BezeichnungChanged, (string w) => gemeldet = w));

        cut.FindAll("input[type=text]")[0].Input("Wartung");

        Assert.Equal("Wartung", gemeldet);
    }

    [Fact]
    public void Ein_geaenderter_Satz_wird_gemeldet()
    {
        double? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.SatzChanged, (double? w) => gemeldet = w));

        cut.FindAll("input[type=text]")[1].Input("950,5");

        Assert.Equal(950.5, gemeldet);
    }

    [Fact]
    public void Eine_geaenderte_Bemessung_wird_gemeldet()
    {
        int? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.BemessungIdChanged, (int? w) => gemeldet = w));

        cut.Find("select").Change("2");

        Assert.Equal(2, gemeldet);
    }

    // =====================================================================
    // Schreibschutz und Neu-Modus
    // =====================================================================

    [Fact]
    public void Eine_schreibgeschuetzte_Zeile_sperrt_alle_Knoepfe_und_Felder()
    {
        var cut = Zeige(p => p
            .Add(x => x.MitWorstBest, true)
            .Add(x => x.Schreibbar, false));

        Assert.All(cut.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));
        Assert.True(cut.Find("select").HasAttribute("disabled"));
        Assert.True(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    [Fact]
    public void Die_Abschlusszeile_zeigt_den_Platzhalter_und_nur_den_Anlegeknopf()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Neuzeile, true)
            .Add(x => x.Bezeichnung, "")
            .Add(x => x.Platzhalter, "+ Neue Position…")
            .Add(x => x.MitNutzungsdauer, true));

        var knoepfe = cut.FindAll("button");
        Assert.Single(knoepfe);
        Assert.Equal("＋", knoepfe[0].TextContent);
        Assert.Equal("+ Neue Position…", cut.FindAll("input[type=text]")[0].GetAttribute("placeholder"));
    }

    [Fact]
    public void Die_Abschlusszeile_legt_erst_mit_einem_Namen_an()
    {
        int angelegt = 0;
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Neuzeile, true)
            .Add(x => x.Bezeichnung, "")
            .Add(x => x.AnlegenAngefordert, () => angelegt++));

        Assert.True(cut.Find("button").HasAttribute("disabled"));

        cut.Render(p => p.Add(x => x.Bezeichnung, "Neue Zeile"));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
        cut.Find("button").Click();

        Assert.Equal(1, angelegt);
    }

    [Fact]
    public void In_der_Abschlusszeile_sind_Satz_und_Nutzungsdauer_gesperrt()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Neuzeile, true)
            .Add(x => x.MitNutzungsdauer, true));

        var felder = cut.FindAll("input[type=text]");
        Assert.False(felder[0].HasAttribute("disabled"));   // Bezeichnung
        Assert.True(felder[1].HasAttribute("disabled"));    // Satz
        Assert.True(felder[2].HasAttribute("disabled"));    // Nutzungsdauer
    }
}
