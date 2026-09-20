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

    /// <summary>
    /// In der Abschlusszeile sind Satz, Nutzungsdauer — und seit Etappe E2 auch die
    /// BEMESSUNG — gesperrt: Der Wirt reicht dort keine Auswahl durch, eine bedienbare
    /// Klappliste ginge ins Leere (Mockup-Prüfung 03/#10).
    /// </summary>
    [Fact]
    public void In_der_Abschlusszeile_sind_Bemessung_Satz_und_Nutzungsdauer_gesperrt()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Neuzeile, true)
            .Add(x => x.MitNutzungsdauer, true));

        var felder = cut.FindAll("input[type=text]");
        Assert.False(felder[0].HasAttribute("disabled"));   // Bezeichnung
        Assert.True(felder[1].HasAttribute("disabled"));    // Satz
        Assert.True(felder[2].HasAttribute("disabled"));    // Nutzungsdauer

        Assert.True(cut.Find("select").HasAttribute("disabled"));   // Bemessung (E2)
    }

    /// <summary>Gegenprobe: In einer BESTEHENDEN Zeile bleibt die Klappliste bedienbar.</summary>
    [Fact]
    public void In_einer_bestehenden_Zeile_bleibt_die_Bemessung_bedienbar()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Neuzeile, false));

        Assert.False(cut.Find("select").HasAttribute("disabled"));
    }

    // =====================================================================
    // Kein stilles 0 (Anwenderbefund 14.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Befund lautete: Betrag netto 0,00 € und kein Wort dazu. Den Grund
    /// nannte der Werkzeugtipp — sichtbar war er damit nicht. Jetzt trägt die
    /// Zeile ein Zeichen, und der Grund steht als Beschriftung daran, damit auch
    /// eine Sprachausgabe ihn vorliest.
    /// </summary>
    [Fact]
    public void Ohne_Bezugsgroesse_traegt_die_Zeile_ein_sichtbares_Zeichen()
    {
        var cut = Zeige(p => p
            .Add(x => x.OhneBasis, true)
            .Add(x => x.BetragKurztext,
                 "Keine Bezugsgröße: kein Gerät mit dieser Baugröße im Projekt."));

        var zeichen = cut.Find(".epos-zr-ohnebasis");
        Assert.Equal("⚠", zeichen.TextContent);
        Assert.Equal("Keine Bezugsgröße: kein Gerät mit dieser Baugröße im Projekt.",
                     zeichen.GetAttribute("aria-label"));
    }

    /// <summary>Eine Zeile MIT Bezugsgröße bleibt ruhig — das Zeichen ist kein
    /// Dauerschmuck.</summary>
    [Fact]
    public void Mit_Bezugsgroesse_bleibt_das_Zeichen_weg()
    {
        var cut = Zeige(p => p.Add(x => x.OhneBasis, false));

        Assert.Empty(cut.FindAll(".epos-zr-ohnebasis"));
    }

    /// <summary>
    /// Die Herleitung einer GERECHNETEN Bezugsgröße steht im selben Werkzeugtipp
    /// wie der Kurztext — der Anwender fragt an EINER Stelle, woher der Betrag
    /// kommt.
    /// </summary>
    [Fact]
    public void Der_Werkzeugtipp_nennt_Kurztext_und_Herleitung()
    {
        var cut = Zeige(p => p
            .Add(x => x.BetragKurztext, "Aus Satz und Bezugsgröße berechnet: 700,00 €/kW × 17,50 kW.")
            .Add(x => x.BasisHerleitung, "0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW"));

        string tipp = cut.Find(".epos-zr-text").GetAttribute("title") ?? "";

        Assert.Contains("700,00 €/kW × 17,50 kW.", tipp);
        Assert.Contains("0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW", tipp);
    }

    /// <summary>Ohne Herleitung bleibt der Werkzeugtipp genau der Kurztext — kein
    /// angehängter Leerraum.</summary>
    [Fact]
    public void Ohne_Herleitung_bleibt_der_Werkzeugtipp_der_Kurztext()
    {
        var cut = Zeige(p => p.Add(x => x.BetragKurztext, "Aus Satz und Bezugsgröße berechnet."));

        Assert.Equal("Aus Satz und Bezugsgröße berechnet.",
                     cut.Find(".epos-zr-text").GetAttribute("title"));
    }

    // =====================================================================
    // U28 — die Herleitungszeile unter dem Betrag
    // =====================================================================

    /// <summary>
    /// Eine gerechnete Zeile trägt die Herleitung SICHTBAR unter dem Betrag —
    /// Bezugsgröße, Herkunft und Kaskadenrunde. Der Text kommt fertig aus dem
    /// Kern; die Zeile zeigt ihn nur.
    /// </summary>
    [Fact]
    public void Eine_Zeile_mit_Bezugsgroesse_traegt_die_Herleitung_unter_dem_Betrag()
    {
        var cut = Zeige(p => p
            .Add(x => x.Herleitung, "× 300,00 kW · P_el der Anlage · Runde 1"));

        var zeile = cut.Find(".epos-zr-herleitung");
        Assert.Equal("× 300,00 kW · P_el der Anlage · Runde 1", zeile.TextContent);
        // Sie steht IN der Betragszelle, unter der Zahl — nicht in einer eigenen Spur.
        Assert.Equal("1200", cut.Find(".epos-zr-text .epos-zr-betrag").TextContent);
        Assert.Contains("epos-herleitung", zeile.ClassName);
    }

    /// <summary>Eine absolute Zeile sagt genau das — sie hat keine Bezugsgröße und
    /// braucht auch keine.</summary>
    [Fact]
    public void Eine_absolute_Zeile_traegt_Satz_gleich_Betrag()
    {
        var cut = Zeige(p => p.Add(x => x.Herleitung, "Satz = Betrag"));

        Assert.Equal("Satz = Betrag", cut.Find(".epos-zr-herleitung").TextContent);
    }

    /// <summary>Im Stammkontext (und überall dort, wo eine Bezugsgröße fehlt) gibt
    /// der Kern keinen Text heraus — dann bleibt die Zeile weg.</summary>
    [Fact]
    public void Ohne_Herleitungstext_bleibt_die_Zeile_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-zr-herleitung"));
    }

    /// <summary>Die Herleitungszeile ersetzt den Werkzeugtipp nicht — er nennt
    /// dieselbe Rechnung vollständig, auch wo die Zelle abschneidet.</summary>
    [Fact]
    public void Die_Herleitungszeile_laesst_den_Werkzeugtipp_stehen()
    {
        var cut = Zeige(p => p
            .Add(x => x.BetragKurztext,
                 "Aus Satz und Bezugsgröße des Projekts berechnet: 653,60 €/kW × 300,00 kW.")
            .Add(x => x.Herleitung, "× 300,00 kW · P_el der Anlage · Runde 1"));

        Assert.Equal("Aus Satz und Bezugsgröße des Projekts berechnet: 653,60 €/kW × 300,00 kW.",
                     cut.Find(".epos-zr-text").GetAttribute("title"));
        Assert.Single(cut.FindAll(".epos-zr-herleitung"));
    }

    // =====================================================================
    // U8 (Stufe S2) — die Herleitung UNTER der Nutzungsdauer
    // =====================================================================

    /// <summary>
    /// U8: Woher die Nutzungsdauer kommt, steht sichtbar unter ihr — fertig aus
    /// dem Kern, die Zeile formatiert nichts.
    /// </summary>
    [Fact]
    public void Die_Nutzungsdauer_traegt_ihre_Herleitung_unter_sich()
    {
        var cut = Zeige(p => p.Add(x => x.NutzungsdauerHerleitung,
                                   "15 a · Vorgabe der Technik"));

        Assert.Contains(cut.FindAll(".epos-zr-herleitung"),
                        e => e.TextContent == "15 a · Vorgabe der Technik");
    }

    /// <summary>Ohne Text keine Zeile — und auf der Betriebsseite auch kein Feld.</summary>
    [Fact]
    public void Ohne_Nutzungsdauerspalte_bleibt_die_Herleitung_weg()
    {
        var cut = Render<VorlagenZeile>(p => p
            .Add(x => x.Bemessungen, BEMESSUNGEN)
            .Add(x => x.Bezeichnung, "Wartung")
            .Add(x => x.BemessungId, 0)
            .Add(x => x.MitNutzungsdauer, false)
            .Add(x => x.NutzungsdauerHerleitung, "15 a · Vorgabe der Technik"));

        Assert.Empty(cut.FindAll(".epos-zr-herleitung"));
    }
}
