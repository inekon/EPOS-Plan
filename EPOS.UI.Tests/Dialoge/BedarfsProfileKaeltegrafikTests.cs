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
    private BedarfsProfilZeile? _markiert;
    private IReadOnlyList<BedarfsProfilZeile>? _alle;

    private Kaelteprofilbilder? Rechnen(BedarfsProfilZeile markiert, IReadOnlyList<BedarfsProfilZeile> alle)
    {
        _aufrufe++;
        _markiert = markiert;
        _alle = alle;
        return Kaelteprofilgrafik.Bilder(Reihe(markiert.Summe), alle.Select(z => (z.Name, Reihe(z.Summe))).ToList(), 3, null);
    }

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(bool mitGrafik = true, List<BedarfsProfilZeile>? zeilen = null)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, zeilen ?? Zeilen())
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
        Assert.StartsWith("kbp-woche-", Bild(cut).Instance.Kennung);
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

    // ---- Nacharbeit KBP-3b: Arbeitsstand ohne Speichern, Kalenderwoche mit Tagesarten als Hintergrund -------------

    private static readonly double[] ANTEILE = { 2, 3, 5, 8, 10, 14, 16, 15, 12, 8, 4, 3 };

    private static List<BedarfsProfilZeile> ZeilenMitEingabe(string eingabeart)
    {
        List<BedarfsProfilZeile> z = Zeilen();
        z[0].Eingabeart = eingabeart;
        z[0].Monatsanteile = (double[])ANTEILE.Clone();
        z[0].Wochenprofil = Kaelteprofileingabe.WocheAusTagen(Enumerable.Repeat(1.0, 24).ToArray(),
            Enumerable.Repeat(0.5, 24).ToArray(), Enumerable.Repeat(0.2, 24).ToArray());
        return z;
    }

    private static IRenderedComponent<Auswahlfeld> Wahl(IRenderedComponent<BedarfsProfileDialog> cut, string bezeichnung)
        => cut.FindComponents<Auswahlfeld>().Single(f => f.Instance.Bezeichnung == bezeichnung);

    [Fact]
    public void Ungespeicherte_Monatswerte_gehen_ohne_OK_in_die_Grafik_die_Zeile_bleibt()
    {
        List<BedarfsProfilZeile> zeilen = ZeilenMitEingabe(DbWerte.KAELTE_EINGABE_MONAT);
        var cut = Aufbauen(zeilen: zeilen);
        Ueberlagerung(cut);
        Kaelteprofilbilder? erst = cut.Instance.Grafikstand;
        Assert.False(_markiert!.ProfilGeaendert);                         // noch nichts bearbeitet: die Zeile selbst
        Assert.Same(zeilen[0], _markiert);

        // Januar in MWh von 0,4 auf 4 - ohne OK der Überlagerung.
        cut.FindComponent<KaelteMonatswerteEditor>().FindComponents<Zahlenfeld>()[0].Find("input").Input("4");
        Assert.NotSame(erst, cut.Instance.Grafikstand);
        Assert.True(_markiert.ProfilGeaendert);
        Assert.NotSame(zeilen[0], _markiert);
        Assert.Contains(_markiert, _alle!);
        Assert.Equal(23.6, _markiert.Summe, 9);                            // Summe der Monate: 20 - 0,4 + 4
        Assert.Equal(400.0 / 23.6, _markiert.Monatsanteile![0], 9);
        Assert.Equal(23.6, cut.Instance.Grafikstand!.Kennzahlen.JahressummeMwh, 6);
        // Die Zeile selbst bleibt bis zum OK unangetastet.
        Assert.False(zeilen[0].ProfilGeaendert);
        Assert.Equal(20.0, zeilen[0].Summe);
        Assert.Equal(ANTEILE, zeilen[0].Monatsanteile);
    }

    [Fact]
    public void Ein_ungespeichertes_Wochenprofil_geht_ohne_OK_in_die_Grafik()
    {
        List<BedarfsProfilZeile> zeilen = ZeilenMitEingabe(DbWerte.KAELTE_EINGABE_JAHR);
        var cut = Aufbauen(zeilen: zeilen);
        Ueberlagerung(cut);
        Wahl(cut, Resource.KBP_LBL_EINGABEART).Find("select").Change("2");
        int aufrufe = _aufrufe;
        cut.FindComponent<KaelteWochenprofilEditor>().FindComponents<Zahlenfeld>()[8].Find("input").Input("4");
        Assert.True(_aufrufe > aufrufe);
        Assert.True(_markiert!.ProfilGeaendert);
        Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, _markiert.Eingabeart);
        for (int d = 0; d < 5; d++) Assert.Equal(4.0, _markiert.Wochenprofil![d * 24 + 8]);
        Assert.Equal(1.0, zeilen[0].Wochenprofil![8]);                      // die Zeile bleibt bis zum OK
        Assert.Null(zeilen[0].Eingabeart == DbWerte.KAELTE_EINGABE_WOCHE ? "geändert" : null);
    }

    [Fact]
    public void Die_Woche_ist_waehlbar_und_zeigt_die_Tagesarten_als_Hintergrund()
    {
        var cut = Aufbauen();
        Ueberlagerung(cut);
        Reiterknopf(cut, Resource.KBP_REITER_WOCHE).Click();
        Kaelteprofilbilder bilder = cut.Instance.Grafikstand!;
        IRenderedComponent<Auswahlfeld> wahl = Wahl(cut, Resource.KBP_LBL_KALENDERWOCHE);
        Assert.Equal(53, wahl.FindAll("option").Count);                   // 1. Januar ein Donnerstag: 53 Kalenderwochen
        Assert.Equal(bilder.Vorgabewoche, wahl.Instance.Auswahl);
        Assert.Equal("kbp-woche-" + bilder.Vorgabewoche, Bild(cut).Instance.Kennung);
        Assert.Equal(1, Bild(cut).Instance.Modell!.Reihen.Count);         // die Tagesarten sind keine Reihen
        Assert.True(Bild(cut).Instance.OhneZoom);

        int aufrufe = _aufrufe;
        wahl.Find("select").Change("27");
        Assert.Equal(aufrufe, _aufrufe);                                   // die Wahl rechnet nicht neu
        IElement woche = cut.Find(".epos-kbp-woche");
        Assert.Equal("27", woche.GetAttribute("data-kw"));
        Assert.Equal("kbp-woche-27", Bild(cut).Instance.Kennung);
        Assert.Same(bilder.Wochenbild(27).Modell, Bild(cut).Instance.Modell);
        Assert.Equal(Reihe(20).Skip(179 * 24).Take(168).ToArray(), Bild(cut).Instance.Modell!.Reihen[0].Werte);

        string stil = woche.GetAttribute("style") ?? "";
        Assert.Contains("--epos-kbp-tagesarten: linear-gradient(to right", stil, StringComparison.Ordinal);
        Assert.Contains("var(--epos-kalb-grundwoche)", stil, StringComparison.Ordinal);
        Assert.Contains("var(--epos-kalb-wochenende)", stil, StringComparison.Ordinal);
        Assert.Contains("var(--epos-kalb-feiertag)", stil, StringComparison.Ordinal);
        Assert.DoesNotContain("#", stil, StringComparison.Ordinal);
        Assert.Equal(3, cut.FindAll(".epos-kbp-tagesart").Count);          // Werktag, Samstag, Sonntag/Feiertag

        // KW 1 beginnt am 29.12. - drei Tage außerhalb des Jahres.
        wahl.Find("select").Change("1");
        Assert.Contains("var(--epos-kalb-saison)", cut.Find(".epos-kbp-woche").GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains(cut.FindAll(".epos-kbp-tagesart"), e => e.ClassList.Contains("epos-kbp-tagesart--ausserhalb"));
        Assert.Contains(Resource.KBP_GRAFIK_HERLEITUNG_ARBEIT, cut.Find(".epos-kbp-grafik").TextContent);
    }

    /// <summary>
    /// KV8 (Entscheid des Anwenders vom 17.10.2026): Eine Zeile mit Normtagen und ohne gewählten Kalender rechnet mit dem
    /// Normkalender als Vorgabe — die Herleitungszeile sagt es sichtbar, und die Kennzahlenzeile nennt die 230
    /// Nutzungstage. Mit gewähltem Kalender nennt die Zeile wieder den Kalender; ohne Normtage keine fünfte Karte.
    /// </summary>
    [Fact]
    public void KV8_Zeile_mit_Normtagen_ohne_Kalender_zeigt_den_Normkalender_als_Vorgabe()
    {
        var zeilen = new List<BedarfsProfilZeile>
        {
            new() { IdZ = 1, IdStamm = 3, Name = "Prozess", Summe = 20, Normtage = 230, Deckung = new DeckungsartDaten(),
                    KaelteArt = DbWerte.KAELTE_ART_PROZESS },
            new() { IdZ = 2, IdStamm = 4, Name = "Serverraum", Summe = 10, Deckung = new DeckungsartDaten(), KaelteArt = DbWerte.KAELTE_ART_RAUM },
        };
        var cut = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Kaelte, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1"))
            .Add(x => x.Jahressumme, _ => 1.0)
            .Add(x => x.KaelteArten, new[] { (DbWerte.KAELTE_ART_RAUM, "Raum"), (DbWerte.KAELTE_ART_PROZESS, "Prozess") })
            .Add(x => x.Betriebskalender, (IReadOnlyList<(int, string)>)new List<(int, string)> { (5, "Werkskalender") })
            // Der Weg der Hülle: Mit Normtagen nennt der Kern die Nutzungstage des wirksamen Kalenders.
            .Add(x => x.KaelteGrafik, (m, alle) => Rechnen(m, alle) is { } b && m.Normtage is int
                                                  ? b with { Nutzungstage = 230 } : null)
            .Add(x => x.Einheit, Energieeinheit.MWh));

        IElement satz = Ueberlagerung(cut);
        string vorgabe = string.Format(CultureInfo.CurrentCulture, Resource.KBP_HINW_NORMKALENDER_VORGABE, 230);
        Assert.Equal(vorgabe, cut.Instance.HinweisNormkalender);
        Assert.Contains("230", vorgabe);
        Assert.Contains(vorgabe, string.Concat(cut.Nodes.Select(n => n.TextContent)));

        string[] titel = cut.FindAll(".epos-kbp-kennzahlen .epos-kennzahlkachel-titel").Select(e => e.TextContent).ToArray();
        string[] werte = cut.FindAll(".epos-kbp-kennzahlen .epos-kennzahlkachel-wert").Select(e => e.TextContent).ToArray();
        Assert.Equal(5, titel.Length);
        Assert.Equal(Resource.KBP_KZ_NUTZUNGSTAGE, titel[4]);
        Assert.Equal(230.ToString("N0", CultureInfo.CurrentCulture) + " " + Resource.KBP_KZ_NUTZUNGSTAGE_EINHEIT, werte[4]);

        // Mit gewähltem Kalender nennt die Zeile ihn, nicht mehr die Vorgabe.
        zeilen[0].KalenderId = 5;
        Assert.NotEqual(vorgabe, cut.Instance.HinweisNormkalender);
        // Eine Zeile ohne Normtage: keine Herleitungszeile.
        zeilen[0].KalenderId = null;
        zeilen[0].Normtage = null;
        Assert.Null(cut.Instance.HinweisNormkalender);
    }
}
