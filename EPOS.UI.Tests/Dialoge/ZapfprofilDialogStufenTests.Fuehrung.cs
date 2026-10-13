using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Benutzerführung des Zapfprofils (Konzept Zapfprofil-Benutzerführung, Welle W2): die geführte neue
/// Zone (ZB2), die waagerechte Wahl (ZB4 (a)), die verständliche Wohnungstabelle (ZB6) und die
/// Anzeigeeinstellungen außerhalb des Fachblocks (ZB10).
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    /// <summary>
    /// Eine Pflichtprüfung der Probe nach dem Muster der Hülle: je Zone ohne Nutzungsart bzw. ohne
    /// Bezugsgröße ein Satz mit der Position der Zone.
    /// </summary>
    private static IReadOnlyList<ZapfprofilMeldung> ProbePruefen(ZapfprofilEingabeDaten e)
    {
        var m = new List<ZapfprofilMeldung>();
        for (int i = 0; i < e.Zonen.Count; i++)
        {
            ZapfprofilZoneDaten z = e.Zonen[i];
            if (z.IdNutzungsart <= 0)
                m.Add(new ZapfprofilMeldung("ZPG_MSG_ZONE_OHNE_NUTZUNGSART", z.Name,
                    "Zone „" + z.Name + "“: Bitte eine Nutzungsart wählen.", ZapfprofilMeldungsart.Fehler, Position: i));
            if (!(z.Bezugsmenge > 0))
                m.Add(new ZapfprofilMeldung("ZPG_MSG_ZONE_OHNE_BEZUGSMENGE", z.Name,
                    "Zone „" + z.Name + "“: Bitte eine Bezugsgröße größer 0 eingeben.", ZapfprofilMeldungsart.Fehler, Position: i));
        }
        return m;
    }

    /// <summary>
    /// Die Vorschau der Probe mit der Ablehnung jeder Zone ohne Nutzungsart — so, wie der Kern sie
    /// liefert („trägt 0“) — samt einem Hinweis dieser Zone in der Warnliste.
    /// </summary>
    private static ZapfprofilVorschauDaten VorschauMitAblehnung(ZapfprofilEingabeDaten e)
    {
        ZapfprofilVorschauDaten v = Vorschau(e);
        for (int i = 0; i < e.Zonen.Count; i++)
        {
            if (e.Zonen[i].IdNutzungsart > 0) continue;
            v.Zonen[i].Abgelehnt = true;
            v.Zonen[i].Rechenweg = ZapfprofilZonenrechenweg.Abgelehnt;
            v.Meldungen.Add(new ZapfprofilMeldung("ZPG_SATZ_EINGABE_NUTZUNGSART_NICHT_GEWAEHLT", e.Zonen[i].Name,
                "Zone „" + e.Zonen[i].Name + "“ trägt 0: Für die Zone ist noch keine Nutzungsart gewählt.",
                ZapfprofilMeldungsart.Ablehnung, Position: i));
            v.Warnliste.Add(new ZapfprofilWarnDaten("ZPG_WARN_ZIRKULATION_OHNE_ZONE", "Zirkulation ohne Zone",
                "Die Zone „" + e.Zonen[i].Name + "“ trägt keinen Zirkulationsanteil.", ZapfprofilWarnstufe.Hinweis, Position: i));
        }
        return v;
    }

    private IRenderedComponent<ZapfprofilDialog> AufbauenGefuehrt(ZapfprofilDaten? daten = null,
        Func<ZapfprofilEingabeDaten, IReadOnlyList<ZapfprofilMeldung>>? pruefen = null,
        Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>? vorschau = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, vorschau ?? VorschauMitAblehnung)
            .Add(x => x.Pruefen, pruefen ?? ProbePruefen)
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Geschlossen, _ => { }));

    /// <summary>Die Ladeaufrufe des Fokusmoduls <c>epos-sprung.js</c> (im Prüfstand ohne Modul).</summary>
    private int Fokusaufrufe()
        => JSInterop.Invocations.Count(i => i.Identifier == "import"
                                            && i.Arguments.Any(a => a is string t && t.EndsWith("epos-sprung.js", StringComparison.Ordinal)));

    // ---- ZB2 -------------------------------------------------------------------------------

    [Fact]
    public void ZB2_Zone_hinzufuegen_fuehrt_die_neue_Zone_als_unvollstaendig_mit_naechstem_Schritt()
    {
        var cut = AufbauenGefuehrt();
        Assert.Empty(cut.FindAll(".epos-zapfprofil-unvollstaendig"));
        int fokusVorher = Fokusaufrufe();

        Knopf(cut, "Zone hinzufügen").Click();

        // Die Liste nennt die neue Zone „unvollständig“ statt „0,0 · trägt 0“.
        IElement[] zeilen = cut.FindAll("table.epos-zapfprofil-zonen tbody tr").ToArray();
        Assert.Equal(3, zeilen.Length);
        IElement zelle = zeilen[2].QuerySelector("td.epos-zapfprofil-unvollstaendig")!;
        Assert.Equal("unvollständig", zelle.TextContent.Trim());
        Assert.DoesNotContain("trägt 0", zeilen[2].TextContent);

        // Der Block nennt leise den ersten Satz der Pflichtprüfung — keine Warnung.
        IElement schritt = cut.Find(".epos-zapfprofil-naechsterschritt");
        Assert.Equal("Zone „Zone 3“: Bitte eine Nutzungsart wählen.", schritt.TextContent.Trim());
        Assert.Equal("ZPG_MSG_ZONE_OHNE_NUTZUNGSART", schritt.GetAttribute("data-kennung"));
        Assert.DoesNotContain("trägt 0", cut.Markup);
        Assert.DoesNotContain("Für die Zone ist noch keine Nutzungsart gewählt", cut.Markup);

        // Die Hinweise der Bilanz tragen die unvollständige Zone nicht.
        IElement warnliste = cut.Find(".epos-zapfprofil-warnliste");
        Assert.DoesNotContain("Zone 3", warnliste.TextContent);
        Assert.Contains("Zirkulation groß", warnliste.TextContent);

        // Der Fokus steht auf der Klappliste Nutzungsart.
        Assert.True(Fokusaufrufe() > fokusVorher, "Kein Fokus nach „Zone hinzufügen“");
    }

    [Fact]
    public void ZB2_Der_naechste_Schritt_folgt_der_Eingabe_und_verschwindet_mit_der_letzten_Luecke()
    {
        var cut = AufbauenGefuehrt();
        Knopf(cut, "Zone hinzufügen").Click();

        Auswahl(cut, "Nutzungsart").Change("2");
        Assert.Equal("Zone „Zone 3“: Bitte eine Bezugsgröße größer 0 eingeben.",
                     cut.Find(".epos-zapfprofil-naechsterschritt").TextContent.Trim());
        Assert.Equal("unvollständig", cut.Find("td.epos-zapfprofil-unvollstaendig").TextContent.Trim());

        Feld(cut, "Bezugsgröße").Input("10");
        Assert.Empty(cut.FindAll(".epos-zapfprofil-naechsterschritt"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-unvollstaendig"));
    }

    [Fact]
    public void ZB2_Eine_vollstaendige_aber_abgelehnte_Zone_bleibt_eine_Warnung()
    {
        // Die Prüfung besteht, der Rechenweg lehnt trotzdem ab (z. B. eine verschwundene Nutzungsart).
        ZapfprofilVorschauDaten MitAblehnung(ZapfprofilEingabeDaten e)
        {
            ZapfprofilVorschauDaten v = Vorschau(e);
            v.Zonen[0].Abgelehnt = true;
            v.Meldungen.Add(new ZapfprofilMeldung("ZPG_SATZ_EINGABE_NUTZUNGSART_FEHLT", "Wohnen",
                "Zone „Wohnen“ trägt 0: Die Nutzungsart steht nicht (mehr) im Katalog.", ZapfprofilMeldungsart.Ablehnung, Position: 0));
            return v;
        }
        ZapfprofilDaten daten = Daten();
        daten.Vorschau = MitAblehnung(daten.Eingabe);
        var cut = AufbauenGefuehrt(daten, vorschau: MitAblehnung);

        Assert.Empty(cut.FindAll(".epos-zapfprofil-naechsterschritt"));
        Assert.Contains("steht nicht (mehr) im Katalog", cut.Find(".epos-warnbanner").TextContent);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-unvollstaendig"));
    }

    // ---- ZB4 (a) ---------------------------------------------------------------------------

    [Fact]
    public void ZB4_Jede_Wahl_des_Zapfprofils_steht_waagerecht()
    {
        var cut = AufbauenGefuehrt();
        Stufe(cut, "Experte");
        Option(cut, "Tagesbedarf", "manuell", "1");

        IElement[] gruppen = cut.FindAll("fieldset.epos-optionsgruppe[role=radiogroup]").ToArray();
        Assert.True(gruppen.Length >= 7, "Nur " + gruppen.Length + " Optionsgruppen");
        Assert.All(gruppen, g => Assert.NotNull(g.QuerySelector(":scope > .epos-optionsgruppe-zeile")));
        foreach (string name in new[] { "Stufe", "Bedarfsniveau", "Tagesbedarf", "Ladeleistung (Gebäude)" })
            Assert.Contains(gruppen, g => g.GetAttribute("aria-label") == name);

        // Die Wahl trägt die Zustandsklasse; die Stufe folgt dem Klick.
        Assert.Equal("Experte", cut.Find("fieldset[aria-label='Stufe'] .epos-option--gewaehlt").TextContent.Trim());
    }

    // ---- ZB6 -------------------------------------------------------------------------------

    [Fact]
    public void ZB6_Die_Wohnungstabelle_erklaert_sich_und_nennt_die_Summe_des_Kerns()
    {
        var cut = AufbauenGefuehrt();
        Stufe(cut, "Erweitert");

        Assert.Equal("Wohnungen (wahlweise)", cut.Find(".epos-zapfprofil-untertitel").TextContent.Trim());
        Assert.StartsWith("Statt einer Bezugsgröße die Wohnungen nach Typ angeben",
                          cut.Find(".epos-zapfprofil-wohnungen-erklaerung").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-zapfprofil-wohnungssumme"));

        Knopf(cut, "Wohnungstyp hinzufügen").Click();
        Feld(cut, "Anzahl, Zeile 1").Input("3");

        // Spaltenköpfe mit Einheit und kurzer Hilfe; die Ausstattung steht, weil der Katalog Klassen führt.
        IElement[] koepfe = cut.FindAll(".epos-zapfprofil-wohnungen thead th span[title]").ToArray();
        Assert.Equal(new[] { "Anzahl [WE]", "Raumzahl [Räume/WE]", "Personen [P/WE]", "Ausstattungsklasse" },
                     koepfe.Select(k => k.TextContent.Trim()).ToArray());
        Assert.Contains("Küche, Bad und Flur zählen nicht", koepfe[1].GetAttribute("title"));
        Assert.Contains("leer: Belegung nach Raumzahl aus dem Katalog", koepfe[2].GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-ausstattung-fehlt"));

        // Die Summenzeile nennt, was die Vorschau wirksam ansetzt (3 WE × 2 P der Probe).
        Assert.Equal("ergibt 3 Wohneinheiten · 6 Personen", cut.Find(".epos-zapfprofil-wohnungssumme").TextContent.Trim());
        Assert.DoesNotContain("Vorgabeklasse. Mit Tabelle", cut.Markup);
    }

    [Fact]
    public void ZB6_Ohne_Klassen_im_Katalog_steht_statt_der_Spalte_der_Weg_des_Imports()
    {
        ZapfprofilDaten daten = Daten();
        daten.Ausstattungen.Clear();
        var cut = AufbauenGefuehrt(daten);
        Stufe(cut, "Erweitert");
        Knopf(cut, "Wohnungstyp hinzufügen").Click();

        Assert.DoesNotContain(cut.FindAll(".epos-zapfprofil-wohnungen thead th"), t => t.TextContent.Contains("Ausstattungsklasse"));
        string weg = cut.Find(".epos-zapfprofil-ausstattung-fehlt").TextContent;
        Assert.Contains("Katalogpaket_DIN4708", weg);
        Assert.Contains("Import…", weg);
    }

    // ---- ZB10 ------------------------------------------------------------------------------

    [Fact]
    public void ZB10_Literanzeige_und_Stundenschwelle_stehen_nur_in_den_Einstellungen()
    {
        var cut = AufbauenGefuehrt();
        Stufe(cut, "Experte");

        Assert.False(HatFeld(cut, "Temperatur der Literanzeige"));
        Assert.False(HatFeld(cut, "Schwelle der Stundenzählung"));
        Assert.Contains("Rubrik „Zapfprofil“", cut.Find(".epos-zapfprofil-anzeigeeinstellung").TextContent);
    }
}
