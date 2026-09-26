using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Kostenverwaltung (iU9-W4.2), Vorbild
/// <c>Views/Kosten/Form_KostenKomponente</c>.
///
/// <para>Soll ist die Feldkarte: Kopf mit Titel und Untertitel, der
/// Netto-Hinweis mit Kreuz, die Kontextzeile (Komponente, Kategorie), die
/// Variantenzeile, das Positionsraster mit sieben Spalten, die drei Knöpfe
/// darunter, der Summenfuß und die Schlussleiste. Dazu die fünf Unterdialoge
/// der Wellen 1 bis 3, die jetzt in einer Überlagerung stehen.</para>
/// </summary>
public class KostenKomponenteDialogTests : BunitContext
{
    // Gate sept39 (11.09.2026, Gegenprobe #230b, LANG=en_US.UTF-8):
    // Der_Spaltenkopf_Nutzungsdauer_traegt_keinen_Zeilenumbruch vergleicht gegen
    // Resource.KDLG_SP_NUTZUNG ("Nutzungsdauer [a]") — ohne Pinnung lieferte die
    // Ressource unter en-US "Service life [a]". Hausvorrichtung seit #168,
    // Rückstellung in Dispose.
    private readonly Kulturvorrichtung _kultur = new();

    public KostenKomponenteDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kultur.Dispose();
        }

        base.Dispose(disposing);
    }

    // ---- Probendaten -----------------------------------------------------

    private static readonly (int Id, string Text)[] EINTRAEGE =
    {
        (0, "Wärmepumpe"), (1, "Heizkessel")
    };

    private static readonly (int Id, string Text)[] BEMESSUNGEN =
    {
        (0, "fester Betrag"), (1, "% der Investition"), (2, "je kW Leistung")
    };

    private static KostenPositionZeile Zeile(int id, string name, double satz)
        => new KostenPositionZeile
        {
            Id = id,
            Bezeichnung = name,
            BemessungId = 0,
            Satz = satz,
            BetragText = satz.ToString("0.##"),
            Kette = true,
            Einheit = "€",
            Schreibbar = true
        };

    private KostenKomponenteStand _stand = new();
    private int _geladen;

    private KostenKomponenteStand Standard(bool projekt = false, bool nurLesen = false)
        => new KostenKomponenteStand
        {
            Titel = projekt ? "Kostenverwaltung Wärmepumpe — Musterprojekt"
                            : "Kostenverwaltung Wärmepumpe",
            Untertitel = "Investitionskosten nach VDI 2067",
            Varianten = new[] { (5, "Standard"), (6, "Variante 1") },
            VarianteId = 5,
            VariantePflegbar = !projekt,
            NurLesen = nurLesen,
            Zeilen = new[] { Zeile(11, "Montage", 1200), Zeile(12, "Gerät", 8000) },
            Bemessungen = BEMESSUNGEN,
            SpalteBetrag = "Betrag netto [€]",
            MitNutzungsdauer = true,
            MitWorstBest = projekt,
            Summen = new[] { ("Summe Investitionskosten netto: 9.200,00 €", true) },
            PositionNeuMoeglich = !nurLesen,
            VarianteLoeschbar = !nurLesen
        };

    /// <summary>Der zuletzt gestellte Kontext (Prüfhilfe).</summary>
    private KostenKomponenteKontext? _gefragt;

    private IRenderedComponent<KostenKomponenteDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<KostenKomponenteDialog>>? mehr = null,
        KostenKomponenteStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        _gefragt = null;
        return Render<KostenKomponenteDialog>(p =>
        {
            p.Add(x => x.Eintraege, EINTRAEGE);
            p.Add(x => x.Laden, k => { _geladen++; _gefragt = k; return _stand; });
            p.Add(x => x.Summen, () => _stand.Summen);
            mehr?.Invoke(p);
        });
    }

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Kontext_Raster_und_Schlussleiste()
    {
        var cut = Zeige();

        Assert.Equal("Kostenverwaltung Wärmepumpe", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Investitionskosten nach VDI 2067", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Single(cut.FindAll(".epos-zeilenraster"));
        Assert.Equal(2, cut.FindAll(".epos-optionsgruppe .epos-option").Count);
    }

    [Fact]
    public void Der_Spaltenkopf_traegt_die_sieben_Ueberschriften_der_Feldkarte()
    {
        var cut = Zeige(p => p
            .Add(x => x.SpalteAktionen, "Aktionen")
            .Add(x => x.SpaltePosition, "Position")
            .Add(x => x.SpalteBemessung, "Bemessung")
            .Add(x => x.SpalteSatz, "Satz")
            .Add(x => x.SpalteNutzung, "Nutzung [a]")
            .Add(x => x.SpalteWorstBest, "Worst/Best"));

        var koepfe = cut.FindAll(".epos-zr-kopfzelle");
        Assert.Equal(7, koepfe.Count);
        Assert.Equal("Aktionen", koepfe[0].TextContent);
        Assert.Equal("Betrag netto [€]", koepfe[4].TextContent);
        Assert.Equal("Worst/Best", koepfe[6].TextContent);
    }

    /// <summary>
    /// Befund 1a der Abnahmeliste vom 11.09.2026 (#186): <c>KDLG_SP_NUTZUNG</c>
    /// trug einen echten Zeilenumbruch ("Nutzungs-\ndauer [a]"), der in WinForms
    /// wirkte und in HTML zu einem Leerzeichen kollabierte - dadurch verdeckt
    /// blieb, dass die Kopfzelle des Zeilenrasters keinen Umbruch verkraftete
    /// (<c>white-space: nowrap</c> ohne Overflow) und der Kopftext in die
    /// Nachbarspalte lief. Dieser Fall haelt den RESSOURCENWERT fest, den die
    /// Windows-Huelle (<c>KostenKomponenteHuelle</c>) unveraendert an
    /// <see cref="KostenKomponenteDialog.SpalteNutzung"/> reicht, und dass der
    /// Spaltenkopf ihn ohne Zeilenumbruchzeichen zeigt.
    /// </summary>
    [Fact]
    public void Der_Spaltenkopf_Nutzungsdauer_traegt_keinen_Zeilenumbruch()
    {
        string spalte = Resource.KDLG_SP_NUTZUNG;
        Assert.DoesNotContain("\n", spalte);
        Assert.DoesNotContain("\r", spalte);
        Assert.Equal("Nutzungsdauer [a]", spalte);

        var cut = Zeige(p => p.Add(x => x.SpalteNutzung, spalte));

        string kopf = cut.FindAll(".epos-zr-kopfzelle")[5].TextContent;   // sechste Spalte

        Assert.Equal("Nutzungsdauer [a]", kopf);
        Assert.DoesNotContain("\n", kopf);
    }

    [Fact]
    public void Der_Netto_Hinweis_laesst_sich_ausblenden()
    {
        var cut = Zeige(p => p.Add(x => x.BannerText, "Alle Beträge sind NETTO."));

        Assert.Single(cut.FindAll(".epos-bannerzeile"));
        cut.Find(".epos-bannerzeile button").Click();
        Assert.Empty(cut.FindAll(".epos-bannerzeile"));
    }

    [Fact]
    public void Die_Variantenzeile_steht_nur_im_Stammkontext()
    {
        var stamm = Zeige();
        var projekt = Zeige(stand: Standard(projekt: true));

        Assert.Equal(2, stamm.FindAll(".epos-kontextleiste").Count);   // Kontext + Variante
        Assert.Single(projekt.FindAll(".epos-kontextleiste"));          // nur der Kontext
    }

    [Fact]
    public void Eine_Auslieferungsvorlage_meldet_sich_und_sperrt_das_Anlegen()
    {
        var cut = Zeige(stand: Standard(nurLesen: true));

        Assert.Contains("Auslieferungsvorlage", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zr-neuzeile"));
    }

    [Fact]
    public void Der_Summenfuss_zeigt_die_Nettosumme()
    {
        var cut = Zeige();

        Assert.Contains("Summe Investitionskosten netto: 9.200,00 €",
                        cut.Find(".epos-zr-summenzelle").TextContent);
    }

    // =====================================================================
    // U29 — der dreiteilige Summenfuß der Investitionsseite
    // =====================================================================

    /// <summary>
    /// Führt die Komponente eine Zuschusszeile, nennt der Fuß unter der
    /// Nettosumme die Bruttoinvestition, den Zuschuss und I₀ — drei Zeilen, die
    /// dritte betont. Gebildet wird sie in der Hülle über den Kern
    /// (<c>KostenSummenCtrl.Fuss</c>); der Dialog zeigt, was im Stand steht.
    /// </summary>
    [Fact]
    public void Mit_Zuschusszeile_traegt_der_Fuss_Bruttoinvestition_Zuschuss_und_I0()
    {
        KostenKomponenteStand s = Standard();
        s.Summen = new[]
        {
            ("Summe Investitionskosten netto: 234.772,40 €", true),
            ("Summe brutto: 279.379,16 € (Umsatzsteuer 19 % aus dem Katalog)", false),
            ("Investition brutto 240.772,40 € · Zuschuss 6.000,00 € · I₀ 234.772,40 €", true),
        };

        var cut = Zeige(stand: s);

        var zellen = cut.FindAll(".epos-zr-summenzelle");
        Assert.Equal(3, zellen.Count);
        Assert.Equal("Investition brutto 240.772,40 € · Zuschuss 6.000,00 € · I₀ 234.772,40 €",
                     zellen[2].TextContent);
        Assert.Contains("epos-zr-summenzelle--stark", zellen[2].ClassName);
    }

    /// <summary>Gegenprobe: Ohne Zuschusszeile gibt der Kern keinen dritten Text
    /// heraus — dann bleibt es bei Netto und Brutto, weil netto = I₀ ist.</summary>
    [Fact]
    public void Ohne_Zuschusszeile_bleibt_der_dreiteilige_Fuss_weg()
    {
        KostenKomponenteStand s = Standard();
        s.Summen = new[]
        {
            ("Summe Investitionskosten netto: 240.772,40 €", true),
            ("Summe brutto: 286.519,16 € (Umsatzsteuer 19 % aus dem Katalog)", false),
        };

        var cut = Zeige(stand: s);

        Assert.Equal(2, cut.FindAll(".epos-zr-summenzelle").Count);
        Assert.DoesNotContain("Investition brutto", cut.Markup);
    }

    // =====================================================================
    // U28 — die Herleitungszeile wandert bis in die Rasterzeile durch
    // =====================================================================

    /// <summary>
    /// Der Dialog reicht die Herleitung des Kerns an die Zeile weiter — sie steht
    /// unter dem Betrag, eine je gerechneter Zeile.
    /// </summary>
    [Fact]
    public void Die_Herleitung_des_Standes_steht_unter_dem_Betrag_der_Zeile()
    {
        KostenKomponenteStand s = Standard();
        KostenPositionZeile erste = s.Zeilen[0];
        erste.Herleitung = "× 300,00 kW · P_el der Anlage · Runde 1";

        var cut = Zeige(stand: s);

        var herleitungen = cut.FindAll(".epos-zr-herleitung");
        Assert.Single(herleitungen);
        Assert.Equal("× 300,00 kW · P_el der Anlage · Runde 1", herleitungen[0].TextContent);
    }

    // =====================================================================
    // U31 — Betriebsseite: Schloss, Empfehlungszeile, Laufstand,
    //       Endenergiegruppe und die Doppelpflege-Warnung
    // =====================================================================

    /// <summary>
    /// Eine Pflichtzeile trägt in der Aktionsspalte ein SCHLOSS statt des
    /// Papierkorbs — der Papierkorb versprach etwas, was die Zeile nie einlöst.
    /// Gegenprobe in derselben Maske: die freie Zeile behält ihren Papierkorb.
    /// </summary>
    [Fact]
    public void Eine_Pflichtzeile_traegt_ein_Schloss_statt_des_Papierkorbs()
    {
        var cut = Zeige(p => p
            .Add(x => x.IstPflicht, (KostenPositionZeile z) => z.Id == 11)
            .Add(x => x.PflichtKurztext, "Pflichtposition, kann nicht gelöscht werden"));

        var ersteKnoepfe = Datenzeilen(cut)[0].QuerySelectorAll("button");
        var zweiteKnoepfe = Datenzeilen(cut)[1].QuerySelectorAll("button");

        Assert.Equal("🔒", ersteKnoepfe[1].TextContent);
        Assert.Contains("epos-zr-schloss", ersteKnoepfe[1].ClassName);
        Assert.Equal("Pflichtposition, kann nicht gelöscht werden",
                     ersteKnoepfe[1].GetAttribute("title"));

        Assert.Equal("🗑️", zweiteKnoepfe[1].TextContent);
        Assert.DoesNotContain("epos-zr-schloss", zweiteKnoepfe[1].ClassName);
    }

    /// <summary>
    /// Das Schloss ist kein stummes Zeichen: Der Löschversuch bleibt abgewiesen
    /// wie bisher — mit derselben Meldung und dem Ausweg, ohne Rückfrage und
    /// ohne Löschung.
    /// </summary>
    [Fact]
    public void Der_Loeschversuch_an_der_Pflichtzeile_bleibt_abgewiesen()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.IstPflicht, (KostenPositionZeile z) => true)
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; })
            .Add(x => x.VorlagePflichtLoeschen, "„{0}\" ist eine Pflichtposition."));

        Datenzeilen(cut)[0].QuerySelectorAll("button")[1].Click();

        Assert.Equal(0, geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Contains("„Montage\" ist eine Pflichtposition.", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der Empfehlungsbereich steht SICHTBAR unter dem Satzfeld — bis dahin nur
    /// im Werkzeugtipp. Der Werkzeugtipp bleibt zusätzlich stehen.
    /// </summary>
    [Fact]
    public void Der_Empfehlungsbereich_steht_als_Zeile_unter_dem_Satzfeld()
    {
        KostenKomponenteStand s = Standard();
        s.Zeilen[0].EmpfehlungKurztext = "Empfehlung: 0,02 – 0,04 €/kWh";
        s.Zeilen[0].EmpfehlungZeile = "Empfehlung 0,02 bis 0,04 €/kWh";

        var cut = Zeige(stand: s);

        var zeilen = cut.FindAll(".epos-zr-empfehlung");
        Assert.Single(zeilen);
        Assert.Equal("Empfehlung 0,02 bis 0,04 €/kWh", zeilen[0].TextContent);
        Assert.Contains("Empfehlung: 0,02 – 0,04 €/kWh", cut.Markup);
    }

    /// <summary>Ohne gepflegten Bereich gibt es keine Zeile.</summary>
    [Fact]
    public void Ohne_Empfehlungsbereich_bleibt_die_Zeile_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-zr-empfehlung"));
    }

    // =====================================================================
    // ANWENDERENTSCHEID 19.09.2026 — der Hinweis auf die Standardvorlage
    // =====================================================================

    /// <summary>
    /// Weicht die Bemessung einer Projektposition von der der Standardvorlage ab,
    /// steht der Vorlagenwert als leise Zeile unter der Herleitung — mit dem
    /// Handgriff „übernehmen" daneben (Muster U23: Textzeile, kein Warnbanner).
    /// </summary>
    [Fact]
    public void Der_Vorlagenhinweis_steht_unter_der_Herleitung_mit_Handgriff()
    {
        KostenKomponenteStand s = Standard();
        s.Zeilen[0].Herleitung = "× 300,00 kW · P_el der Anlage";
        s.Zeilen[0].VorlagenHinweis = "Vorlage „Standard“: % des Endenergiebedarfs";
        s.Zeilen[0].VorlagenBemessungId = 2;

        var cut = Zeige(p => p.Add(x => x.VorlageUebernehmenText, "übernehmen"), s);

        var hinweise = cut.FindAll(".epos-zr-vorlage");
        Assert.Single(hinweise);
        Assert.Contains("Vorlage „Standard“: % des Endenergiebedarfs",
                        hinweise[0].TextContent);
        Assert.Equal("übernehmen", hinweise[0].QuerySelector("button")!.TextContent);
    }

    /// <summary>„übernehmen" setzt die Bemessung auf den Vorlagenwert, lässt Satz
    /// und Betrag stehen und schreibt über den Weg, den auch „Speichern" ruft —
    /// danach lädt der Dialog neu, und der Hinweis ist fort.</summary>
    [Fact]
    public void Uebernehmen_setzt_die_Bemessung_speichert_und_der_Hinweis_verschwindet()
    {
        KostenKomponenteStand mitHinweis = Standard();
        mitHinweis.Zeilen[0].VorlagenHinweis = "Vorlage „Standard“: je kW Leistung";
        mitHinweis.Zeilen[0].VorlagenBemessungId = 2;

        KostenKomponenteStand ohneHinweis = Standard();
        ohneHinweis.Zeilen[0].BemessungId = 2;

        int gespeichert = 0;
        KostenPositionZeile? nachgezogen = null;
        var cut = Zeige(p => p
            .Add(x => x.Nachziehen, (KostenPositionZeile z) => nachgezogen = z)
            .Add(x => x.Speichern, () => { gespeichert++; _stand = ohneHinweis; return true; }),
            mitHinweis);

        double satzVorher = mitHinweis.Zeilen[0].Satz ?? 0;
        cut.Find(".epos-zr-vorlage button").Click();

        Assert.Equal(2, mitHinweis.Zeilen[0].BemessungId);
        Assert.Equal(satzVorher, mitHinweis.Zeilen[0].Satz);
        Assert.Same(mitHinweis.Zeilen[0], nachgezogen);
        Assert.Equal(1, gespeichert);
        Assert.Empty(cut.FindAll(".epos-zr-vorlage"));
    }

    /// <summary>
    /// An einer NICHT schreibbaren Zeile (Auslieferungsvorlage, Lesemodus) bleibt
    /// die Auskunft stehen, der Handgriff nicht: Anbieten, was nicht geht, wäre
    /// schlimmer als schweigen.
    /// </summary>
    [Fact]
    public void Im_Lesemodus_steht_der_Hinweis_ohne_Knopf()
    {
        KostenKomponenteStand s = Standard(nurLesen: true);
        s.Zeilen[0].Schreibbar = false;
        s.Zeilen[0].VorlagenHinweis = "Vorlage „Standard“: je kW Leistung";
        s.Zeilen[0].VorlagenBemessungId = 2;

        var cut = Zeige(stand: s);

        var hinweise = cut.FindAll(".epos-zr-vorlage");
        Assert.Single(hinweise);
        Assert.Null(hinweise[0].QuerySelector("button"));
    }

    /// <summary>Ohne Abweichung gibt es keine Zeile.</summary>
    [Fact]
    public void Ohne_Vorlagenhinweis_bleibt_die_Zeile_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-zr-vorlage"));
    }

    /// <summary>
    /// Der LAUFSTAND steht über dem Raster: aus welchem Simulationslauf die
    /// Mengen der Betriebsseite stammen. Der Text kommt fertig aus dem Kern.
    /// </summary>
    [Fact]
    public void Der_Laufstand_steht_ueber_dem_Raster()
    {
        KostenKomponenteStand s = Standard();
        s.Laufstand = "Mengen stammen aus dem Simulationslauf vom 18.09.2026 14:05";

        var cut = Zeige(stand: s);

        Assert.Equal("Mengen stammen aus dem Simulationslauf vom 18.09.2026 14:05",
                     cut.Find(".epos-kdlg-laufstand").TextContent);
    }

    /// <summary>Auf der Investitionsseite und im Stammkontext gibt der Kern keinen
    /// Laufstand heraus — dann steht dort auch nichts.</summary>
    [Fact]
    public void Ohne_Laufstand_bleibt_die_Zeile_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kdlg-laufstand"));
    }

    /// <summary>
    /// Die Gruppe „Endenergie je Komponente" unter dem Raster nennt je Anlage
    /// Bedarf, Kosten und Herkunft — die Zahlenprobe des Mockups am 300-kW-
    /// Blockheizkraftwerk.
    /// </summary>
    [Fact]
    public void Die_Endenergiegruppe_nennt_Bedarf_Kosten_und_Herkunft()
    {
        KostenKomponenteStand s = Standard();
        s.Endenergie = new[]
        {
            new EndenergieZeile("BHKW — BHKW 1", "4.342.100 kWh", "312.631,20 €/a",
                                "BHKW „BHKW 1\""),
            new EndenergieZeile("Heizkessel — Kessel 1", "2.056.700 kWh", "148.082,40 €/a",
                                "Heizkessel „Kessel 1\""),
        };

        var cut = Zeige(p => p
            .Add(x => x.GruppeEndenergieTitel, "Endenergie je Komponente")
            .Add(x => x.HinweisEndenergie, "Diese Mengen sind die Bezugsgrößen."),
            stand: s);

        Assert.Equal("Endenergie je Komponente",
                     cut.Find(".epos-gruppenkopf-titel").TextContent);

        var zeilen = cut.FindAll(".epos-kdlg-endenergie tbody tr");
        Assert.Equal(2, zeilen.Count);

        var erste = zeilen[0].QuerySelectorAll("td");
        Assert.Equal("BHKW — BHKW 1", erste[0].TextContent);
        Assert.Equal("4.342.100 kWh", erste[1].TextContent);
        Assert.Equal("312.631,20 €/a", erste[2].TextContent);
        Assert.Equal("BHKW „BHKW 1\"", erste[3].TextContent);

        Assert.Contains("Diese Mengen sind die Bezugsgrößen.", cut.Markup);
    }

    /// <summary>
    /// E1 — DER ELEKTROKESSEL steht in derselben Gruppe: mit seinem Stromeinsatz in
    /// kWh, dem Betrag zum Arbeitspreis des Stromträgers und der Herkunft
    /// „Strom · Netzbezug". Seine Energie ist damit sichtbar, ohne ein zweites Mal
    /// bepreist zu werden — bezahlt wird sie im Reststrombedarf des Projekts.
    /// </summary>
    [Fact]
    public void Die_Endenergiegruppe_zeigt_den_Elektrokessel_mit_Netzbezug()
    {
        KostenKomponenteStand s = Standard();
        s.Endenergie = new[]
        {
            new EndenergieZeile("Heizkessel — Elektrokessel 1", "52.990 kWh", "24.771,30 €/a",
                                "Elektrokessel „Elektrokessel 1\" — Strom · Netzbezug "
                                + "(im Reststrombedarf des Projekts bepreist)"),
        };

        var cut = Zeige(p => p
            .Add(x => x.GruppeEndenergieTitel, "Endenergie je Komponente"),
            stand: s);

        var felder = cut.FindAll(".epos-kdlg-endenergie tbody tr td");
        Assert.Equal("Heizkessel — Elektrokessel 1", felder[0].TextContent);
        Assert.Equal("52.990 kWh", felder[1].TextContent);
        Assert.Equal("24.771,30 €/a", felder[2].TextContent);
        Assert.Contains("Strom · Netzbezug", felder[3].TextContent);
    }

    /// <summary>Ohne Endenergie keine Gruppe — Photovoltaik, Solarthermie und die
    /// Speicher führen keine.</summary>
    [Fact]
    public void Ohne_Endenergie_bleibt_die_Gruppe_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kdlg-endenergie"));
    }

    /// <summary>
    /// Die Doppelpflege-Warnung der Kohärenzprüfung steht auch hier — wortgleich,
    /// als Warnbanner über dem Raster.
    /// </summary>
    [Fact]
    public void Die_Doppelpflege_Warnung_steht_ueber_dem_Raster()
    {
        KostenKomponenteStand s = Standard();
        s.DoppelpflegeWarnung =
            "Hilfsenergie doppelt gepflegt (Menge an der Anlage und Kostenposition "
            + "Hilfsenergiekosten): BHKW 1 führt einen Hilfsenergieanteil von 2,00 %.";

        var cut = Zeige(stand: s);

        var banner = cut.FindAll(".epos-warnbanner-text");
        Assert.Contains(banner, b => b.TextContent.StartsWith("Hilfsenergie doppelt gepflegt"));
    }

    /// <summary>Gegenprobe: Ohne Doppelpflege liefert die Kohärenzprüfung keinen
    /// Text — dann steht kein Banner da.</summary>
    [Fact]
    public void Ohne_Doppelpflege_bleibt_das_Warnbanner_weg()
    {
        var cut = Zeige();

        Assert.DoesNotContain("Hilfsenergie doppelt gepflegt", cut.Markup);
    }

    [Fact]
    public void Jede_Position_erscheint_als_eigene_Zeile()
    {
        var cut = Zeige();

        // Zwei gepflegte Zeilen plus die Abschlusszeile.
        Assert.Equal(3, cut.FindAll(".epos-zr-zeile").Count);
        Assert.Contains("Montage", cut.Markup);
        Assert.Contains("Gerät", cut.Markup);
    }

    // =====================================================================
    // Kontextwechsel
    // =====================================================================

    [Fact]
    public void Ein_Komponentenwechsel_fragt_die_Huelle_neu()
    {
        var cut = Zeige();

        cut.Find(".epos-kontextleiste select").Change("1");

        Assert.Equal(1, _gefragt!.EintragId);
        Assert.True(_gefragt.Invest);
        Assert.Null(_gefragt.VarianteId);
    }

    [Fact]
    public void Ein_Kategoriewechsel_fragt_mit_Betrieb()
    {
        var cut = Zeige();

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].Change(true);

        Assert.False(_gefragt!.Invest);
    }

    [Fact]
    public void Ein_Variantenwechsel_fragt_mit_der_Variante()
    {
        var cut = Zeige();

        cut.FindAll(".epos-kontextleiste select")[1].Change("6");

        Assert.Equal(6, _gefragt!.VarianteId);
    }

    [Fact]
    public void Die_Vorwahl_wird_uebernommen()
    {
        KostenKomponenteKontext? gefragt = null;
        Render<KostenKomponenteDialog>(p => p
            .Add(x => x.Eintraege, EINTRAEGE)
            .Add(x => x.EintragVorwahl, 1)
            .Add(x => x.InvestVorwahl, false)
            .Add(x => x.Laden, k => { gefragt = k; return Standard(); }));

        Assert.Equal(1, gefragt!.EintragId);
        Assert.False(gefragt.Invest);
        Assert.False(gefragt.Invest);
    }

    // =====================================================================
    // Feldänderungen (Ä12/Ä19: erst „Speichern" schreibt)
    // =====================================================================

    [Fact]
    public void Eine_Feldaenderung_zieht_Kopplung_und_Summen_nach_ohne_zu_schreiben()
    {
        KostenPositionZeile? nachgezogen = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Nachziehen, (KostenPositionZeile z) => nachgezogen = z)
            .Add(x => x.Speichern, () => { gespeichert++; return true; }));

        cut.FindAll(".epos-zr-zeile input[type=text]")[1].Input("1500");

        Assert.NotNull(nachgezogen);
        Assert.Equal(1500.0, nachgezogen!.Satz);
        Assert.Equal(0, gespeichert);
    }

    [Fact]
    public void Speichern_schreibt_alles_und_bestaetigt_in_der_Statuszeile()
    {
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.VorlageGespeichert, "gespeichert {0} Uhr"));

        // DL-2 (Nr. 6): Der Fuß läuft Speichern · Abbrechen · OK.
        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[0].Click();

        Assert.Equal(1, gespeichert);
        Assert.StartsWith("gespeichert ", cut.Instance.Status);
        // Der Statustext steht seit DL-2 IN der Fußleiste.
        Assert.StartsWith("gespeichert ",
                          cut.FindAll(".epos-leiste")[^1].QuerySelector(".epos-status")!
                             .TextContent.Trim());
    }

    /// <summary>
    /// Hausmuster der <c>SpeichernLeiste</c>: Der Vermerk „gespeichert …" gilt bis zur
    /// nächsten Eingabe; ein abgelehntes Speichern sagt „Nicht gespeichert" in Rot.
    /// </summary>
    [Fact]
    public void Der_Vermerk_faellt_mit_der_naechsten_Eingabe_und_ein_Fehlschlag_ist_rot()
    {
        bool gelingt = true;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => gelingt)
            .Add(x => x.VorlageGespeichert, "gespeichert {0} Uhr"));
        AngleSharp.Dom.IElement Status() => cut.FindAll(".epos-leiste")[^1].QuerySelector(".epos-status")!;

        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[0].Click();
        Assert.StartsWith("gespeichert ", Status().TextContent);

        cut.FindAll(".epos-zr-zeile input[type=text]")[1].Input("1500");
        Assert.Equal("", Status().TextContent.Trim());

        gelingt = false;
        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[0].Click();
        Assert.Equal(Resource.ADM_STATUS_FEHLER, Status().TextContent.Trim());
        Assert.Contains("epos-status--fehler", Status().ClassName);
    }

    /// <summary>„Speichern unter…" schreibt eine Kopie — derselbe Vermerk wie „Speichern".</summary>
    [Fact]
    public void Speichern_unter_meldet_den_Vermerk_in_der_Leiste()
    {
        var cut = Zeige(p => p
            .Add(x => x.VariantenGaben, (bool k) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["TitelText"] = "Speichern unter", ["FrageText"] = "Name:",
                    ["Vorbelegung"] = "Kopie"
                })
            .Add(x => x.VarianteNeu, (bool k, string n) => 7)
            .Add(x => x.VorlageGespeichert, "gespeichert {0} Uhr"));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[1].Click();
        cut.Find(".epos-ueberlagerung .epos-leiste").QuerySelectorAll("button")[^1].Click();

        Assert.StartsWith("gespeichert ",
                          cut.FindAll(".epos-leiste")[^1].QuerySelector(".epos-status")!.TextContent);
    }

    [Fact]
    public void OK_speichert_und_schliesst_mit_true()
    {
        bool? ergebnis = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[2].Click();

        Assert.Equal(1, gespeichert);
        Assert.True(ergebnis);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_speichern()
    {
        bool? ergebnis = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        // DL-2 (Nr. 6): Abbrechen steht unmittelbar vor OK.
        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[1].Click();

        Assert.Equal(0, gespeichert);
        Assert.False(ergebnis);
    }

    /// <summary>
    /// <b>DL-2 (Nr. 6, Schritt 8):</b> Der Fuß ist eine <c>SpeichernLeiste</c> —
    /// Status (zugleich Füller) · Speichern · Abbrechen · OK, und OK ist der
    /// einzige primäre Knopf und der letzte.
    /// </summary>
    [Fact]
    public void Der_Fuss_laeuft_Speichern_Abbrechen_OK()
    {
        var cut = Zeige(p => p
            .Add(x => x.SpeichernText, "Speichern")
            .Add(x => x.AbbrechenText, "Abbrechen")
            .Add(x => x.OkText, "OK"));

        var fuss = cut.FindAll(".epos-leiste")[^1];
        var knoepfe = fuss.QuerySelectorAll("button");

        Assert.Equal(new[] { "Speichern", "Abbrechen", "OK" },
                     knoepfe.Select(b => b.TextContent.Trim()).ToArray());
        Assert.NotNull(fuss.QuerySelector(".epos-status"));
        Assert.Single(fuss.QuerySelectorAll(".epos-knopf--primaer"));
        Assert.Contains("epos-knopf--primaer", knoepfe[^1].ClassName);
    }

    /// <summary>
    /// <b>DL-2 (Entscheid DL-Q3 a):</b> Die Zeilenaktionen des Reiters „Kosten"
    /// schreiben sofort; der Kurztext des Abbrechen-Knopfes sagt das.
    /// </summary>
    [Fact]
    public void Der_Abbrechen_Knopf_traegt_den_Kurztext_zum_Arbeitsstand()
    {
        var cut = Zeige(p => p
            .Add(x => x.AbbrechenText, "Abbrechen")
            .Add(x => x.AbbrechenKurztext,
                 "Verwirft nur die ungespeicherten Eingaben; angelegte Positionen bleiben."));

        var abbrechen = cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[1];

        Assert.Contains("angelegte Positionen bleiben", abbrechen.GetAttribute("title"));
    }

    /// <summary>
    /// <b>DL-2 (Nr. 6):</b> Die Reiterleiste im Reiter „Kosten" bleibt Blattleiste —
    /// ihre vier Knöpfe rufen dieselben Delegaten wie zuvor, keiner ist primär.
    /// </summary>
    [Fact]
    public void Die_Reiterleiste_bleibt_im_Blatt_und_ohne_Primaerknopf()
    {
        // ETAPPE E2: Der vierte Knopf steht nur, wo er WIRKT — der Stand muss ihn
        // deshalb ausdrücklich erlauben, sonst zählt die Leiste drei Knöpfe.
        KostenKomponenteStand mit = Standard();
        mit.NutzungsdauerVorbelegbar = true;

        var cut = Zeige(p => p
            .Add(x => x.NutzungsdauerVorbelegen, (bool a) => new NutzungsdauerVorbelegung(0, 0)),
            stand: mit);

        var blatt = cut.FindAll(".epos-leiste")[0];

        Assert.Equal(4, blatt.QuerySelectorAll("button").Length);
        Assert.Empty(blatt.QuerySelectorAll(".epos-knopf--primaer"));
    }

    // =====================================================================
    // Positionen
    // =====================================================================

    [Fact]
    public void Die_Abschlusszeile_legt_mit_ihrem_Namen_an()
    {
        string? name = null;
        var cut = Zeige(p => p.Add(x => x.PositionNeu, (string n) => { name = n; return 42; }));

        var neu = cut.Find(".epos-zr-neuzeile");
        neu.QuerySelectorAll("input[type=text]")[0].Input("Wartung");
        neu.QuerySelector("button")!.Click();

        Assert.Equal("Wartung", name);
    }

    [Fact]
    public void Der_Knopf_Position_hinzufuegen_nimmt_die_Vorgabe()
    {
        string? name = null;
        var cut = Zeige(p => p
            .Add(x => x.PositionNeuVorgabe, "Neue Position")
            .Add(x => x.PositionNeu, (string n) => { name = n; return 42; }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[0].Click();

        Assert.Equal("Neue Position", name);
    }

    [Fact]
    public void Der_Papierkorb_fragt_erst_nach()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; })
            .Add(x => x.VorlagePositionLoeschen, "Position „{0}\" löschen?"));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();

        Assert.Contains("Position „Montage\" löschen?", cut.Markup);
        Assert.Equal(0, geloescht);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        Assert.Equal(1, geloescht);
    }

    [Fact]
    public void Nein_loescht_nicht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[1].Click();

        Assert.Equal(0, geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Eine_Pflichtposition_wird_erklaert_statt_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.IstPflicht, (KostenPositionZeile z) => true)
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; })
            .Add(x => x.VorlagePflichtLoeschen, "„{0}\" ist eine Pflichtposition."));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();

        Assert.Equal(0, geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Contains("„Montage\" ist eine Pflichtposition.", cut.Instance.Meldung);
    }

    // =====================================================================
    // Zeilenaktionen: das Neuladen behaelt die ungespeicherten Eingaben
    // =====================================================================

    /// <summary>
    /// Eine „Datenbank", wie die Windows-Hülle sie führt: <c>Laden</c> baut bei
    /// JEDEM Aufruf NEUE <see cref="KostenPositionZeile"/> aus den persistierten
    /// Positionen (<c>VorlagenRasterAufbauen</c>: <c>_bindungen.Clear()</c>,
    /// <c>_zeilen = new List&lt;…&gt;()</c>). Ein Fake, der immer dieselben Objekte
    /// zurückgibt, kann den Anwenderbefund gar nicht zeigen — er verliert die
    /// Eingabe nicht, weil sie im selben Objekt stehen bleibt.
    /// </summary>
    private sealed class Ablage
    {
        public readonly List<(int Id, string Name, double Satz)> Positionen
            = new List<(int, string, double)> { (11, "Montage", 1200), (12, "Gerät", 8000) };

        private int _naechste = 13;

        /// <summary>Wie <c>PositionNeu</c> der Hülle: schreibt SOFORT (Ä12).</summary>
        public int Anlegen(string name)
        {
            int id = _naechste++;
            Positionen.Add((id, name, 0));
            return id;
        }

        public bool Loeschen(int id) => Positionen.RemoveAll(p => p.Id == id) > 0;
    }

    /// <summary>Die Datenzeilen des Rasters ohne die Abschlusszeile.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Datenzeilen(
        IRenderedComponent<KostenKomponenteDialog> cut)
        => cut.FindAll(".epos-zeilenraster > .epos-zr-zeile");

    /// <summary>Das Satzfeld der <paramref name="nr"/>-ten Datenzeile (vierte Spur).</summary>
    private static AngleSharp.Dom.IElement Satzfeld(
        IRenderedComponent<KostenKomponenteDialog> cut, int nr)
        => Datenzeilen(cut)[nr].QuerySelectorAll("input[type=text]")[1];

    private KostenKomponenteStand AusAblage(Ablage ablage)
    {
        KostenKomponenteStand s = Standard();
        var zeilen = new List<KostenPositionZeile>();
        foreach ((int id, string name, double satz) in ablage.Positionen)
        {
            zeilen.Add(Zeile(id, name, satz));
        }

        s.Zeilen = zeilen;
        _stand = s;
        s.Summen = Summenfuss();
        return s;
    }

    /// <summary>Der Summenfuß fällt aus dem STAND, nicht aus der Ablage (Ä12/Ä19).</summary>
    private IReadOnlyList<(string Text, bool Stark)> Summenfuss()
    {
        double summe = 0;
        foreach (KostenPositionZeile z in _stand.Zeilen) summe += z.Satz ?? 0;
        return new[] { ("Summe " + summe.ToString("0.##"), true) };
    }

    private IRenderedComponent<KostenKomponenteDialog> ZeigeMitAblage(
        Ablage ablage,
        Action<Bunit.ComponentParameterCollectionBuilder<KostenKomponenteDialog>>? mehr = null)
    {
        _geladen = 0;
        _gefragt = null;

        return Render<KostenKomponenteDialog>(p =>
        {
            p.Add(x => x.Eintraege, EINTRAEGE);
            p.Add(x => x.Laden, k => { _geladen++; _gefragt = k; return AusAblage(ablage); });
            p.Add(x => x.Summen, Summenfuss);
            p.Add(x => x.Nachziehen, (KostenPositionZeile z) =>
                z.BetragText = (z.Satz ?? 0).ToString("0.##"));
            p.Add(x => x.PositionNeu, (string n) => ablage.Anlegen(n));
            p.Add(x => x.PositionLoeschen, (KostenPositionZeile z) => ablage.Loeschen(z.Id));
            p.Add(x => x.PositionNeuVorgabe, "Neue Position");
            mehr?.Invoke(p);
        });
    }

    /// <summary>
    /// ANWENDERBEFUND 14.09.2026 (Kostenverwaltung, Reiter „Kosten Invest/Betrieb"):
    /// „Bei zufügen von Position werden zuvor eingegebene Werte auf null gesetzt."
    ///
    /// <para>Ursache: „Position hinzufügen" schreibt die NEUE Position sofort und
    /// lässt danach den ganzen Stand neu laden; die Hülle baut dabei jede Zeile neu
    /// aus der Datenbank, in der die noch nicht gespeicherten Eingaben der übrigen
    /// Zeilen nicht stehen. Die Regel Ä12/Ä19 („die Änderung lebt bis Speichern nur
    /// im Objekt") trägt nur, solange niemand die Objekte austauscht — beim
    /// Auffrischen werden die Eingaben deshalb übertragen.</para>
    /// </summary>
    [Fact]
    public void Position_hinzufuegen_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(3, Datenzeilen(cut).Count));
        Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(1500.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("8000", Satzfeld(cut, 1).GetAttribute("value"));
        Assert.Equal("Neue Position", cut.Instance.Stand.Zeilen[2].Bezeichnung);
        Assert.Equal("Summe 9500", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>Derselbe Weg über die Abschlusszeile (Name eintippen, dann ＋).</summary>
    [Fact]
    public void Die_Abschlusszeile_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 1).Input("9000");
        cut.WaitForAssertion(() => Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value")));

        cut.Find(".epos-zr-neuzeile").QuerySelectorAll("input[type=text]")[0].Input("Wartung");
        cut.Find(".epos-zr-neuzeile").QuerySelector("button")!.Click();

        cut.WaitForAssertion(() => Assert.Equal(3, Datenzeilen(cut).Count));
        Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value"));
        Assert.Equal(9000.0, cut.Instance.Stand.Zeilen[1].Satz);
        Assert.Equal("Wartung", cut.Instance.Stand.Zeilen[2].Bezeichnung);
        Assert.Equal("Summe 10200", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>„Position löschen" lädt denselben Stand neu — und behält ebenso.</summary>
    [Fact]
    public void Position_loeschen_behaelt_die_ungespeicherten_Eingaben_der_uebrigen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage, p => p
            .Add(x => x.VorlagePositionLoeschen, "Position „{0}\" löschen?"));

        Satzfeld(cut, 1).Input("9000");
        cut.WaitForAssertion(() => Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value")));

        Datenzeilen(cut)[0].QuerySelectorAll("button")[1].Click();      // 🗑️ der ersten Zeile
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();         // Ja

        cut.WaitForAssertion(() => Assert.Single(Datenzeilen(cut)));
        Assert.Equal("9000", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(9000.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("Summe 9000", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>
    /// „In Projekt übernehmen" lädt nach dem Schließen ebenfalls neu
    /// (<c>btnUebernahme_Click</c>: übernommene Positionen sofort zeigen).
    /// </summary>
    [Fact]
    public void Die_Uebernahme_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage, p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                }));

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();   // Übernahme
        cut.FindAll(".epos-ueberlagerung .epos-leiste")[^1]
           .QuerySelectorAll("button")[0].Click();                              // Abbrechen

        cut.WaitForAssertion(() => Assert.False(cut.Instance.UeberlagerungOffen));
        Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(1500.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("Summe 9500", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>
    /// <b>ANWENDERENTSCHEID 19.09.2026:</b> Übernommen wird in die im Katalogblock
    /// gewählte KATEGORIE — der Dialog schaltet danach auf sie um, sonst stünde die
    /// Bestätigung über einer Liste, in der von der Übernahme nichts zu sehen ist.
    /// </summary>
    [Fact]
    public void Nach_der_Uebernahme_zeigt_der_Dialog_die_Kategorie_der_Uebernahme()
    {
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") },
                    ["InvestVorwahl"] = true,
                    ["VorlagenZu"] = new Func<bool, IReadOnlyList<(int, string)>>(
                        invest => invest ? new[] { (5, "Standard") } : new[] { (7, "Standard Betrieb") }),
                    ["Vorschau"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>(
                        _ => new VorlagenUebernahmeVorschau("1 Position", true)),
                    ["Uebernehmen"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeAntwort>(
                        _ => new VorlagenUebernahmeAntwort(false, "angelegt"))
                }));

        Assert.True(_gefragt!.Invest);

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();   // Übernahme

        // Die Kategorien der Überlagerung stehen als drittes und viertes Optionsfeld.
        var optionen = cut.Find(".epos-ueberlagerung").QuerySelectorAll("input[type=radio]");
        optionen[2].Change(true);                                               // Betriebskosten
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();           // OK

        cut.WaitForAssertion(() => Assert.False(cut.Instance.UeberlagerungOffen));
        Assert.False(_gefragt!.Invest);
        Assert.Null(_gefragt.VarianteId);
    }

    /// <summary>
    /// Der GEGENFALL und damit die Grenze der Regel: Ein echter Kontextwechsel
    /// (andere Komponente, andere Kategorie, andere Variante) lädt OHNE Übertrag —
    /// dort ist das Verwerfen gewollt, die Zeilen des neuen Kontexts sind andere.
    /// </summary>
    [Fact]
    public void Ein_Kontextwechsel_laedt_ohne_Uebertrag()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-kontextleiste select")[0].Change("1");

        cut.WaitForAssertion(() => Assert.Equal(1200.0, cut.Instance.Stand.Zeilen[0].Satz));
        Assert.Equal("1200", Satzfeld(cut, 0).GetAttribute("value"));
    }

    // =====================================================================
    // Die fünf Unterdialoge in der Überlagerung
    // =====================================================================

    [Fact]
    public void Der_Stift_oeffnet_den_Zeileneditor_als_Ueberlagerung()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.True(cut.Instance.UeberlagerungOffen);
    }

    [Fact]
    public void Das_Plusminus_steht_nur_im_Projektmodus_und_oeffnet_Worst_Best()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0
                }),
            stand: Standard(projekt: true));

        var knoepfe = cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button");
        Assert.Equal(3, knoepfe.Length);
        knoepfe[2].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Neu_und_Speichern_unter_oeffnen_die_Namensabfrage()
    {
        bool? kopie = null;
        var cut = Zeige(p => p.Add(x => x.VariantenGaben, (bool k) =>
        {
            kopie = k;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["TitelText"] = k ? "Speichern unter" : "Neue Variante",
                ["FrageText"] = "Name der neuen Variante:"
            };
        }));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();
        Assert.False(kopie);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ein_belegter_Variantenname_meldet_sich()
    {
        var cut = Zeige(p => p
            .Add(x => x.VariantenGaben, (bool k) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["TitelText"] = "Neue Variante", ["FrageText"] = "Name:",
                    ["Vorbelegung"] = "Wärmepumpe — Variante 2"
                })
            .Add(x => x.VarianteNeu, (bool k, string n) => 0)
            .Add(x => x.MeldungNameBelegt, "Der Name ist bereits vergeben oder leer."));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();
        // OK der Namensabfrage: die letzte Leiste in der Überlagerung.
        var leiste = cut.Find(".epos-ueberlagerung .epos-leiste");
        leiste.QuerySelectorAll("button")[^1].Click();

        Assert.Contains("bereits vergeben", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Standardvorlage_laesst_sich_nicht_loeschen()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.VarianteIstStandard, () => true)
            .Add(x => x.VarianteLoeschen, () => { geloescht++; return true; })
            .Add(x => x.MeldungStandardLoeschen, "Die Standardvorlage kann nicht gelöscht werden."));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[2].Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("Standardvorlage", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Variante_wird_nach_Rueckfrage_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.VarianteIstStandard, () => false)
            .Add(x => x.VarianteLoeschen, () => { geloescht++; return true; })
            .Add(x => x.VorlageVarianteLoeschen, "Variante „{0}\" wirklich löschen?"));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[2].Click();
        Assert.Contains("Variante „Standard\" wirklich löschen?", cut.Markup);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        Assert.Equal(1, geloescht);
    }

    [Fact]
    public void Uebernahme_und_Katalog_erscheinen_als_Ueberlagerung()
    {
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    // =====================================================================
    // #187 — "Ein Titel, eine Stelle" (W11b-B-9): der Titel einer
    // Ueberlagerung mit eingebettetem Unterdialog erscheint GENAU EINMAL.
    // Vor #187 trugen CaseEingabeDialog, VorlagenPositionDialog und
    // KostenfaktorKatalogDialog ihren TitelText UNBEDINGT — bei gleichem
    // Ressourcenschluessel wie CaseTitel/EditorTitel/KatalogTitel stand der
    // Text zweimal uebereinander. Die Huelle liefert TitelText seither leer
    // (KostenKomponenteHuelle.CaseGaben/EditorGaben,
    // KostenfaktorKatalogHuelle.Gaben); hier wird das am gerenderten Markup
    // nachgewiesen, nicht nur an der Huelle.
    // =====================================================================

    [Fact]
    public void Der_Titel_des_Zeileneditors_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") },
                    ["TitelText"] = ""            // wie KostenKomponenteHuelle.EditorGaben seit #187
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Position bearbeiten", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    [Fact]
    public void Der_Titel_von_Worst_Best_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0,
                    ["TitelText"] = ""            // wie KostenKomponenteHuelle.CaseGaben seit #187
                }),
            stand: Standard(projekt: true));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[2].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Eingabe Worst/Best Case", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    [Fact]
    public void Der_Titel_des_Kostenfaktorkatalogs_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") },
                    ["TitelText"] = ""            // wie KostenfaktorKatalogHuelle.Gaben seit #187
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Administration Kostenfaktoren", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    // =====================================================================
    // Ertrag/Bonus
    // =====================================================================

    [Fact]
    public void Der_Reiter_Ertrag_Bonus_erscheint_nur_wenn_die_Huelle_ihn_meldet()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["Projekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
        };

        var ohne = Zeige();
        var cut = Zeige(stand: mit);

        // iU9-W5.0 (Nachzug A-2): Der Vorlaeufer ENTFERNTE die zweite
        // Reiterseite zur Laufzeit (ErtragReiterSteuern); hier fehlt der
        // zweite Reiterknopf.
        Assert.Single(ohne.FindAll(".epos-reiter-knopf"));
        Assert.Equal(2, cut.FindAll(".epos-reiter-knopf").Count);
        Assert.Equal("Ertrag/Bonus", cut.FindAll(".epos-reiter-knopf")[1].TextContent.Trim());

        Assert.Empty(cut.FindAll(".epos-ertragbonus"));   // erst nach dem Wechsel
        cut.FindAll(".epos-reiter-knopf")[1].Click();
        Assert.Single(cut.FindAll(".epos-ertragbonus"));
    }

    /// <summary>
    /// <b>Die SECHSTE Ueberlagerung: der Gesetzeskatalog</b> (iU9-W14c.3). Bis dahin
    /// sprang <c>ErtragBonus</c> mit <c>Sprungziel.Gesetzesparameter</c> ueber die
    /// <c>Sprungbruecke</c> in ein WinForms-Fenster; das Ziel ist jetzt selbst Razor,
    /// und zwei WebViews uebereinander sind Risiko R2. Das Reiterblatt kann keine
    /// Ueberlagerung oeffnen - es meldet den Wunsch nach oben.
    /// </summary>
    [Fact]
    public void Der_Gesetzeskatalog_erscheint_als_sechste_Ueberlagerung()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstBhkw"] = true
        };

        int gerufen = 0;
        var cut = Zeige(p => p.Add(x => x.GesetzeGaben, () =>
        {
            gerufen++;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
            };
        }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter Ertrag/Bonus
        Assert.False(cut.Instance.UeberlagerungOffen);

        cut.Find(".epos-ertragbonus button").Click();          // "Gesetzesparameter..."

        Assert.Equal(1, gerufen);
        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Single(cut.FindComponents<EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogDialog>());
    }

    /// <summary>
    /// <b>Die SIEBTE Ueberlagerung: die PV-Verguetung</b> (E3/6). Bis dahin fuhr
    /// der Knopf des Reiterblatts ein ZWEITES WinForms-Fenster ueber diesem hoch
    /// (nachgelagert, ueber eine Naht der Windows-Schale); seit die PV-Huelle
    /// selbst plattformfrei ist, steht sie als Ueberlagerung im selben Fenster —
    /// auf Windows wie auf iOS. Der Satz kommt je PROJEKT, denn im Admin-Kontext
    /// waehlt das Reiterblatt das Stammprojekt selbst.
    /// </summary>
    [Fact]
    public void Die_Pv_Verguetung_erscheint_als_siebte_Ueberlagerung()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["ProjektlisteZeigen"] = false,
            ["ProjektVorwahl"] = (int?)9
        };

        int gerufenFuer = 0;
        var cut = Zeige(p => p.Add(x => x.PvGaben, (int id) =>
        {
            gerufenFuer = id;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>();
        }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter Ertrag/Bonus
        Assert.False(cut.Instance.UeberlagerungOffen);

        cut.Find(".epos-ertragbonus button").Click();          // "PV-Verguetungsdialog..."

        Assert.Equal(9, gerufenFuer);
        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Single(cut.FindComponents<
            EPOS.UI.Dialoge.Wirtschaftlichkeit.PhotovoltaikVerguetungDialog>());
    }

    /// <summary>
    /// <b>E3/7 — der Sprung „Tarif…" wird zur ACHTEN Überlagerung.</b> Der
    /// PV-Vergütungsdialog nimmt für ihn seit #405 den OK-Weg (prüfen,
    /// schreiben, springen); das Ziel war bis dahin ein zweites Fenster und
    /// nach dessen Fall ein Sprung ins Leere.
    /// </summary>
    [Fact]
    public void Der_Tarif_Sprung_der_Pv_Verguetung_oeffnet_die_achte_Ueberlagerung()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["ProjektlisteZeigen"] = false,
            ["ProjektVorwahl"] = (int?)9
        };

        int tarifFuer = 0;
        var cut = Zeige(p => p
            .Add(x => x.PvGaben, (int id) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>())
            .Add(x => x.TarifGaben, (int id) =>
            {
                tarifFuer = id;
                return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>();
            }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();
        cut.Find(".epos-ertragbonus button").Click();

        var dialog = cut.FindComponent<
            EPOS.UI.Dialoge.Wirtschaftlichkeit.PhotovoltaikVerguetungDialog>();
        cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new EPOS.UI.Dialoge.Wirtschaftlichkeit.PvVerguetungErgebnis(
                true, EPOS.UI.Dialoge.Wirtschaftlichkeit.PvSprung.Tarif)));

        Assert.Equal(9, tarifFuer);
        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Single(cut.FindComponents<
            EPOS.UI.Dialoge.Wirtschaftlichkeit.TarifstrukturDialog>());
    }

    /// <summary>
    /// Kein Delegat, kein Sprungziel: Ohne <c>TarifGaben</c> schließt der
    /// Vergütungsdialog nur (E3/7).
    /// </summary>
    [Fact]
    public void Ohne_Tarif_Gaben_bleibt_der_Sprung_folgenlos()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["ProjektlisteZeigen"] = false,
            ["ProjektVorwahl"] = (int?)9
        };

        var cut = Zeige(p => p.Add(x => x.PvGaben, (int id) =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>()), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();
        cut.Find(".epos-ertragbonus button").Click();

        var dialog = cut.FindComponent<
            EPOS.UI.Dialoge.Wirtschaftlichkeit.PhotovoltaikVerguetungDialog>();
        cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new EPOS.UI.Dialoge.Wirtschaftlichkeit.PvVerguetungErgebnis(
                true, EPOS.UI.Dialoge.Wirtschaftlichkeit.PvSprung.Tarif)));

        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.Empty(cut.FindComponents<
            EPOS.UI.Dialoge.Wirtschaftlichkeit.TarifstrukturDialog>());
    }

    /// <summary>
    /// Ohne Gaben bleibt der Knopf im Reiterblatt weg — „kein Delegat, kein
    /// Knopf", dieselbe Wache wie beim Gesetzeskatalog (E3/6).
    /// </summary>
    [Fact]
    public void Ohne_Pv_Gaben_fehlt_der_Verguetungsknopf()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["ProjektlisteZeigen"] = false,
            ["ProjektVorwahl"] = (int?)9
        };

        var cut = Zeige(stand: mit);
        cut.FindAll(".epos-reiter-knopf")[1].Click();

        Assert.Empty(cut.FindAll(".epos-ertragbonus button"));
    }

    /// <summary>
    /// Ohne Gaben bleibt der Knopf im Reiterblatt weg - "kein Delegat, kein Knopf".
    /// </summary>
    [Fact]
    public void Ohne_Gesetzesgaben_fehlt_der_Katalogknopf()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        var cut = Zeige(stand: mit);
        cut.FindAll(".epos-reiter-knopf")[1].Click();

        Assert.Empty(cut.FindAll(".epos-ertragbonus button"));
    }

    // =====================================================================
    // Tastatur
    // =====================================================================

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog").KeyDown(key: "Escape");

        Assert.False(ergebnis);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Abbrechen()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>
    /// <b>Esc schliesst immer nur die OBERSTE Ebene</b> (R-W14c-3): Steht eine
    /// Ueberlagerung, bleibt der Wirt stehen - je Ebene geprueft, auch fuer die neue
    /// sechste.
    /// </summary>
    [Theory]
    [InlineData("editor")]
    [InlineData("uebernahme")]
    [InlineData("katalog")]
    [InlineData("gesetze")]
    public void Esc_laesst_den_Wirt_stehen_solange_eine_Ueberlagerung_offen_ist(string ebene)
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok)
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                })
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                })
            .Add(x => x.GesetzeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
                }),
            stand: mit);

        switch (ebene)
        {
            case "editor":
                cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();
                break;
            case "uebernahme":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
                break;
            case "katalog":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();
                break;
            case "gesetze":
                cut.FindAll(".epos-reiter-knopf")[1].Click();
                cut.Find(".epos-ertragbonus button").Click();
                break;
        }

        Assert.True(cut.Instance.UeberlagerungOffen, "Die Ebene " + ebene + " steht nicht.");

        cut.Find(".epos-dialog").KeyDown(key: "Escape");

        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Die sechs Unterdialog-Überlagerungen waren <c>Schliessbar="false"</c> — seit dem
    /// Anwenderentscheid 15.09.2026 tragen sie ihr eigenes Kreuz (Titel jeweils gesetzt).
    /// Es schliesst NUR die Überlagerung, wie Esc es auch tut (R-W14c-3) — der Wirt
    /// bleibt stehen.
    /// </summary>
    [Theory]
    [InlineData("editor")]
    [InlineData("uebernahme")]
    [InlineData("katalog")]
    [InlineData("gesetze")]
    public void Das_Kreuz_der_Ueberlagerung_schliesst_nur_sie_selbst(string ebene)
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok)
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                })
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                })
            .Add(x => x.GesetzeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
                }),
            stand: mit);

        switch (ebene)
        {
            case "editor":
                cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();
                break;
            case "uebernahme":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
                break;
            case "katalog":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();
                break;
            case "gesetze":
                cut.FindAll(".epos-reiter-knopf")[1].Click();
                cut.Find(".epos-ertragbonus button").Click();
                break;
        }

        Assert.True(cut.Instance.UeberlagerungOffen, "Die Ebene " + ebene + " steht nicht.");

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog").KeyDown(key: "Enter");

        Assert.Null(ergebnis);
    }

    [Fact]
    public void Der_Hilfeschluessel_bleibt_der_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);   // gewinnt gegen KeineHilfe

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_KostenKomponente.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    // Kein stilles 0 (Anwenderbefund 14.09.2026)
    // =====================================================================

    private static KostenKomponenteStand StandOhneBasis()
    {
        var stand = new KostenKomponenteStand
        {
            Titel = "Kostenverwaltung Solarthermie — Musterprojekt",
            Untertitel = "Investitionskosten nach VDI 2067",
            Zeilen = new[]
            {
                Zeile(21, "Montage", 1200),
                new KostenPositionZeile
                {
                    Id = 22,
                    Bezeichnung = "Solarthermie",
                    BemessungId = 2,
                    Satz = 700,
                    Einheit = "€/kW",
                    BetragText = "0,00",
                    OhneBasis = true,
                    BetragKurztext = "Keine Bezugsgröße: kein Gerät mit dieser Baugröße "
                                     + "im Projekt. Es gilt der erfasste Betrag.",
                    Schreibbar = true
                }
            },
            Bemessungen = BEMESSUNGEN,
            SpalteBetrag = "Betrag netto [€]",
            MitNutzungsdauer = true,
            MitWorstBest = true,
            PositionNeuMoeglich = true
        };
        return stand;
    }

    /// <summary>
    /// DER BEFUND: Im Bildschirmfoto stand unter dem Raster nichts. Der Grund lag
    /// allein im Werkzeugtipp des Betragsfeldes. Jetzt sammelt eine LEISE Zeile
    /// unter dem Raster Bezeichnung und Grund jeder Zeile ohne Bezugsgröße.
    /// </summary>
    [Fact]
    public void Zeilen_ohne_Bezugsgroesse_stehen_unter_dem_Raster()
    {
        var cut = Zeige(stand: StandOhneBasis());

        string zeile = cut.Find(".epos-zr-ohnebasis-zeile").TextContent;

        Assert.Contains("Solarthermie", zeile);
        Assert.Contains("kein Gerät mit dieser Baugröße im Projekt", zeile);
        Assert.DoesNotContain("Montage", zeile);
    }

    /// <summary>Hat jede Zeile ihre Bezugsgröße — der Regelfall —, bleibt die
    /// Zeile weg; ein Dauerhinweis wäre Lärm.</summary>
    [Fact]
    public void Ohne_solche_Zeilen_bleibt_der_Hinweis_weg()
    {
        var cut = Zeige(stand: Standard(projekt: true));

        Assert.Empty(cut.FindAll(".epos-zr-ohnebasis-zeile"));
        Assert.Equal("", cut.Instance.OhneBasisText);
    }

    /// <summary>Und die Zeile selbst trägt das Zeichen — der Wirt reicht das
    /// Kennzeichen durch.</summary>
    [Fact]
    public void Die_betroffene_Zeile_traegt_das_Zeichen_im_Raster()
    {
        var cut = Zeige(stand: StandOhneBasis());

        Assert.Single(cut.FindAll(".epos-zr-zeile .epos-zr-ohnebasis"));
    }

    // =====================================================================
    // ANWENDERBEFUND 19.09.2026 (#363): dasselbe auf der BETRIEBSSEITE
    //
    // Im Bildschirmfoto des Befundes trugen drei Betriebszeilen das ⚠, und unter
    // dem Raster war nichts zu sehen. Die Hinweiszeile hängt an keiner Kategorie —
    // aber das war bis hierher durch keinen Fall gedeckt, und der Grund selbst muss
    // die ABHILFE nennen, sonst sucht der Anwender an der falschen Stelle.
    // =====================================================================

    /// <summary>Der Betriebsstand: Laufstandzeile über dem Raster, eine Zeile mit
    /// Bezugsgröße und zwei ohne — Preis und Anlage, die beiden Gründe des
    /// Befundes.</summary>
    private static KostenKomponenteStand StandBetriebOhneBasis()
        => new KostenKomponenteStand
        {
            Titel = "Kostenverwaltung Heizkessel — Musterprojekt",
            Untertitel = "Betriebskosten nach VDI 2067",
            Laufstand = "Mengen stammen aus dem Simulationslauf vom 07.09.2026 23:42",
            Zeilen = new[]
            {
                Zeile(31, "Instandhaltung Heizkessel", 1.5),
                new KostenPositionZeile
                {
                    Id = 32,
                    Bezeichnung = "Vollwartung / Wartung Kessel",
                    BemessungId = 2,
                    Satz = 0.01,
                    Einheit = "€/kWh",
                    BetragText = "0,00",
                    OhneBasis = true,
                    BetragKurztext = "Keine Bezugsgröße: diese Anlage steht nicht im "
                                     + "Simulationslauf — sie braucht einen Platz in der "
                                     + "Simulationskonfiguration. Es gilt der erfasste Betrag.",
                    Schreibbar = true
                },
                new KostenPositionZeile
                {
                    Id = 33,
                    Bezeichnung = "Hilfsenergiekosten (Strom)",
                    BemessungId = 1,
                    Satz = 6,
                    Einheit = "%",
                    BetragText = "0,00",
                    OhneBasis = true,
                    BetragKurztext = "Keine Bezugsgröße: für den Energieträger ist kein "
                                     + "Arbeitspreis gepflegt — er ist in der "
                                     + "Energieträgerverwaltung zu erfassen. Es gilt der "
                                     + "erfasste Betrag.",
                    Schreibbar = true
                }
            },
            Bemessungen = BEMESSUNGEN,
            SpalteBetrag = "Betrag netto [€/a]",
            MitNutzungsdauer = false,
            MitWorstBest = true,
            PositionNeuMoeglich = true
        };

    /// <summary>
    /// DER BEFUND: Drei Betriebszeilen, zwei davon ohne Bezugsgröße — die
    /// Hinweiszeile unter dem Raster muss BEIDE nennen, mit Bezeichnung und Grund.
    /// </summary>
    [Fact]
    public void Betriebszeilen_ohne_Bezugsgroesse_stehen_ebenfalls_unter_dem_Raster()
    {
        var cut = Zeige(stand: StandBetriebOhneBasis());

        string zeile = cut.Find(".epos-zr-ohnebasis-zeile").TextContent;

        Assert.Contains("Vollwartung / Wartung Kessel", zeile);
        Assert.Contains("Hilfsenergiekosten (Strom)", zeile);
        Assert.DoesNotContain("Instandhaltung Heizkessel", zeile);
        Assert.Equal(2, cut.FindAll(".epos-zr-zeile .epos-zr-ohnebasis").Count);
    }

    /// <summary>
    /// Und der Grund nennt die ABHILFE, nicht nur die Lage: die
    /// Energieträgerverwaltung beim fehlenden Preis, die Simulationskonfiguration
    /// bei der Anlage außerhalb des Laufs. Er steht im ⚠ als Werkzeugtipp
    /// (<c>title</c>) und als Vorlesetext (<c>aria-label</c>).
    /// </summary>
    [Fact]
    public void Der_Grund_nennt_die_Abhilfe_und_haengt_am_Zeichen()
    {
        var cut = Zeige(stand: StandBetriebOhneBasis());

        var zeichen = cut.FindAll(".epos-zr-zeile .epos-zr-ohnebasis");
        string titel = string.Join(" ", zeichen.Select(z => z.GetAttribute("title") ?? ""));
        string vorlesen = string.Join(" ", zeichen.Select(z => z.GetAttribute("aria-label") ?? ""));

        Assert.Contains("Energieträgerverwaltung", titel);
        Assert.Contains("Simulationskonfiguration", titel);
        Assert.Contains("Energieträgerverwaltung", vorlesen);
        Assert.Contains("Simulationskonfiguration", vorlesen);
    }

    // =====================================================================
    //  „Das Kreuz steht beim Titel" (Anwenderentscheid 15.09.2026): Die
    //  Ueberlagerung traegt Titel UND ✕, das eingebettete Blatt keins von
    //  beidem — je Einbettungsstelle ein Fall.
    // =====================================================================

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    /// <summary>Die Prüfung je Überlagerung: genau EIN ✕, kein zweiter Titel darunter.</summary>
    private static void NurEinKreuz(IRenderedComponent<KostenKomponenteDialog> cut)
    {
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Die_Ueberlagerung_Worst_Best_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0
                }),
            stand: Standard(projekt: true));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[2].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Zeileneditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Namensabfrage_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.VariantenGaben, (bool k) =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["TitelText"] = "Neue Variante",
                ["FrageText"] = "Name der neuen Variante:"
            }));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Uebernahme_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Kostenfaktorkatalog_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Gesetzeskatalog_zeigt_nur_ein_Kreuz()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        var cut = Zeige(p => p.Add(x => x.GesetzeGaben, () =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
            }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter Ertrag/Bonus
        cut.Find(".epos-ertragbonus button").Click();          // „Gesetzesparameter…"

        NurEinKreuz(cut);
    }

    // =====================================================================
    // U8 (Stufe S2) — „Nutzungsdauern vorbelegen…"
    // =====================================================================

    /// <summary>Kein Delegat, kein Knopf (Hausregel).</summary>
    [Fact]
    public void Ohne_Delegat_fehlt_der_Knopf_Nutzungsdauern_vorbelegen()
    {
        var cut = Zeige();

        Assert.Equal(3, cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button").Length);
    }

    /// <summary>
    /// U8: Der vierte Knopf der Raster-Leiste füllt die leeren Nutzungsdauern und
    /// nennt die Zahl in der Statuszeile. Ohne überschreibbare Zeilen folgt keine
    /// Rückfrage.
    /// </summary>
    [Fact]
    public void Der_Knopf_belegt_die_leeren_Nutzungsdauern_vor()
    {
        bool? gefragt = null;
        KostenKomponenteStand mit = Standard();
        mit.NutzungsdauerVorbelegbar = true;

        var cut = Zeige(p => p
            .Add(x => x.NutzungsdauerVorbelegen, (bool u) =>
            {
                gefragt = u;
                return new NutzungsdauerVorbelegung(2, 0);
            })
            .Add(x => x.VorbelegenStatus, "{0} vorbelegt"), stand: mit);

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[3].Click();

        Assert.False(gefragt);
        Assert.Equal("2 vorbelegt", cut.Instance.Status);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>
    /// Anwenderentscheid ND‑Q4 (b): Gepflegte Werte bleiben stehen, bis der Anwender
    /// das Überschreiben bestätigt — die Rückfrage nennt ihre Anzahl, und erst ein
    /// „Ja" ruft den Delegaten ein zweites Mal.
    /// </summary>
    [Fact]
    public void Gefuellte_Nutzungsdauern_werden_erst_nach_der_Rueckfrage_ueberschrieben()
    {
        var rufe = new List<bool>();
        KostenKomponenteStand mit = Standard();
        mit.NutzungsdauerVorbelegbar = true;

        var cut = Zeige(p => p
            .Add(x => x.NutzungsdauerVorbelegen, (bool u) =>
            {
                rufe.Add(u);
                return u ? new NutzungsdauerVorbelegung(3, 0)
                         : new NutzungsdauerVorbelegung(1, 3);
            })
            .Add(x => x.VorbelegenStatus, "{0} vorbelegt")
            .Add(x => x.VorbelegenFrage, "{0} bereits gepflegt — überschreiben?"),
            stand: mit);

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[3].Click();

        Assert.Equal(new[] { false }, rufe);
        Assert.Contains("3 bereits gepflegt", cut.Find(".epos-rueckfrage").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();          // Ja

        Assert.Equal(new[] { false, true }, rufe);
        Assert.Equal("4 vorbelegt", cut.Instance.Status);
    }

    /// <summary>Gibt es nichts vorzubelegen, sagt es der Dialog — statt still nichts zu tun.</summary>
    [Fact]
    public void Ohne_Vorgabe_meldet_der_Knopf_dass_es_nichts_vorzubelegen_gibt()
    {
        KostenKomponenteStand mit = Standard();
        mit.NutzungsdauerVorbelegbar = true;

        var cut = Zeige(p => p
            .Add(x => x.NutzungsdauerVorbelegen,
                 (bool _) => new NutzungsdauerVorbelegung(0, 0))
            .Add(x => x.VorbelegenKeine, "nichts vorzubelegen"), stand: mit);

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[3].Click();

        Assert.Equal("nichts vorzubelegen", cut.Instance.Meldung);
    }

    /// <summary>
    /// Auf einer Auslieferungsvorlage fehlt der Knopf.
    ///
    /// <para><b>ETAPPE E2 (Mockup-Prüfung 03/#26).</b> Bis hierher stand er dort
    /// gesperrt da und behauptete eine Bedienung, die es in diesem Kontext nicht
    /// gibt — ohne jeden Grund am Bedienelement. Ein gesperrtes Bedienelement bleibt
    /// nur stehen, wo es seinen Grund erklären kann; hier ist der Grund der Kontext,
    /// und der steht im Reiter. Er erscheint deshalb nur, wo er wirkt.</para>
    /// </summary>
    [Fact]
    public void Auf_einer_Auslieferungsvorlage_fehlt_der_Knopf()
    {
        KostenKomponenteStand nurLesen = Standard(nurLesen: true);
        nurLesen.NutzungsdauerVorbelegbar = false;

        var cut = Zeige(p => p.Add(x => x.NutzungsdauerVorbelegen,
                                   (bool _) => new NutzungsdauerVorbelegung(0, 0)),
                        stand: nurLesen);

        Assert.Equal(3, cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button").Length);
    }

    /// <summary>
    /// ETAPPE E10 (Stufe S3): Auf der BETRIEBSSEITE heißt derselbe Knopf „Sätze
    /// vorbelegen…" und meldet mit eigenem Text; die Rückfrage vor dem Überschreiben
    /// abweichender Sätze bleibt dieselbe Bedienung.
    /// </summary>
    [Fact]
    public void Auf_der_Betriebsseite_belegt_der_Knopf_die_Saetze_vor()
    {
        var rufe = new List<bool>();
        KostenKomponenteStand mit = Standard();
        mit.SaetzeVorbelegbar = true;

        var cut = Zeige(p => p
            .Add(x => x.NutzungsdauerVorbelegen, (bool u) =>
            {
                rufe.Add(u);
                return u ? new NutzungsdauerVorbelegung(1, 0) : new NutzungsdauerVorbelegung(3, 1);
            })
            .Add(x => x.SaetzeVorbelegenStatus, "{0} Sätze vorbelegt")
            .Add(x => x.SaetzeVorbelegenFrage, "{0} Satz abweichend — überschreiben?"),
            stand: mit);

        var knopf = cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[3];
        Assert.Equal("Sätze vorbelegen…", knopf.TextContent.Trim());
        knopf.Click();

        Assert.Equal(new[] { false }, rufe);
        Assert.Equal("3 Sätze vorbelegt", cut.Instance.Status);
        Assert.Contains("1 Satz abweichend", cut.Find(".epos-rueckfrage").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();          // Ja
        Assert.Equal(new[] { false, true }, rufe);
        Assert.Equal("4 Sätze vorbelegt", cut.Instance.Status);
    }

    /// <summary>
    /// ETAPPE E10: Unter dem Satzfeld steht die Herkunftszeile, wenn die
    /// Nutzungsdauertabelle den Satz stellt — und nur dann.
    /// </summary>
    [Fact]
    public void Die_Herkunftszeile_des_Satzes_steht_unter_dem_Satzfeld()
    {
        KostenKomponenteStand mit = Standard();
        KostenPositionZeile instandhaltung = Zeile(13, "Instandhaltung BHKW", 6.0);
        instandhaltung.SatzHerleitung =
            "6 % · Satz aus Nutzungsdauertabelle: Blockheizkraftwerk · Modul (Instandsetzung)";
        mit.Zeilen = new[] { Zeile(11, "Montage", 1200), instandhaltung };

        var cut = Zeige(stand: mit);

        var zeilen = cut.FindAll(".epos-zr-satzherkunft");
        Assert.Single(zeilen);
        Assert.Equal(instandhaltung.SatzHerleitung, zeilen[0].TextContent.Trim());
    }

    // =====================================================================
    // U30 — die Tafel „Ersatz und Restwert"
    // =====================================================================

    private KostenKomponenteStand MitTafel()
    {
        KostenKomponenteStand stand = Standard(projekt: true);
        stand.SpalteRestwert = "Restwert Jahr 20";
        stand.ErsatzRestwertHinweis =
            "Betrachtungszeitraum 20 a über der Vorgabe 15 a der Technik Blockheizkraftwerk.";
        stand.ErsatzRestwert = new[]
        {
            new ErsatzRestwertZeile("BHKW-Modul", false, "196.080,00", "15 a", "Jahr 15",
                                    "Barwert 125.856", "130.720,00", "Barwert 72.376",
                                    "Vorgabe der Technik"),
            new ErsatzRestwertZeile("Planung", false, "21.888,40", "—", "— läuft wie T",
                                    "", "—", "", "keine Dauer gepflegt"),
            new ErsatzRestwertZeile("Blockheizkraftwerk", true, "217.968,40", "1 von 2",
                                    "Jahr 15 · 196.080,00", "Barwert 125.856", "130.720,00",
                                    "Barwert 72.376", "Vorgabe der Technik 15 a")
        };
        return stand;
    }

    /// <summary>U30: Die Tafel steht unter dem Raster, je Zeile eine Tabellenzeile.</summary>
    [Fact]
    public void Die_Tafel_Ersatz_und_Restwert_zeigt_ihre_Zeilen()
    {
        var cut = Zeige(stand: MitTafel());

        var zeilen = cut.FindAll(".epos-kdlg-ersatz tbody tr");
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("196.080,00", zeilen[0].TextContent);
        Assert.Contains("Barwert 125.856", zeilen[0].TextContent);
        Assert.Contains("keine Dauer gepflegt", zeilen[1].TextContent);
    }

    /// <summary>Die Summenzeile der Komponente ist als solche gekennzeichnet.</summary>
    [Fact]
    public void Die_Summenzeile_der_Tafel_traegt_ihre_Klasse()
    {
        var cut = Zeige(stand: MitTafel());

        Assert.Single(cut.FindAll(".epos-kdlg-ersatz--summe"));
        Assert.Contains("1 von 2", cut.Find(".epos-kdlg-ersatz--summe").TextContent);
    }

    /// <summary>Die Spalteüberschrift des Restwerts nennt T — sie kommt aus dem Stand.</summary>
    [Fact]
    public void Der_Spaltenkopf_des_Restwerts_nennt_den_Betrachtungszeitraum()
    {
        var cut = Zeige(stand: MitTafel());

        Assert.Contains(cut.FindAll(".epos-kdlg-ersatz thead th"),
                        e => e.TextContent == "Restwert Jahr 20");
    }

    /// <summary>
    /// Der Hinweis steht ÜBER der Tafel und ist ein Prüfauftrag, kein Fehler —
    /// deshalb die Stufe „Hinweis".
    /// </summary>
    [Fact]
    public void Der_Hinweis_steht_ueber_der_Tafel()
    {
        var cut = Zeige(stand: MitTafel());

        Assert.Contains(cut.FindAll(".epos-warnbanner"),
                        e => e.TextContent.Contains("Vorgabe 15 a"));
    }

    /// <summary>Ohne Zeilen keine Tafel — Betriebsseite und Katalogkontext.</summary>
    [Fact]
    public void Ohne_Zeilen_bleibt_die_Tafel_weg()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kdlg-ersatz"));
    }

    /// <summary>U8: Die Herleitung der Nutzungsdauer reicht der Wirt an die Zeile durch.</summary>
    [Fact]
    public void Die_Nutzungsdauer_Herleitung_erreicht_die_Zeile()
    {
        KostenKomponenteStand mit = Standard();
        mit.Zeilen[0].NutzungsdauerHerleitung = "15 a · Vorgabe der Technik";

        var cut = Zeige(stand: mit);

        Assert.Contains(cut.FindAll(".epos-zr-herleitung"),
                        e => e.TextContent == "15 a · Vorgabe der Technik");
    }

    // =====================================================================
    // Der Hilfe-Assistent: die drei Lücken der Welle KI-F4 (KI-F7)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE der Sichtklasse <c>KostenKomponenteKiSicht</c></b>
    /// (Anwenderentscheid 21.09.2026, KI‑D‑Q7).
    /// </summary>
    /// <remarks>
    /// <para>Geprüft wird beides: dass die BISHERIGEN Felder unverändert
    /// weiterlaufen — Kopfsatz, Wahlfeld <c>variante</c> und das Positionsraster mit
    /// seinen Spalten — und dass die drei Lücken jetzt dastehen. Die
    /// KOMPONENTENWAHL steht in einem privaten Feld des Dialogs, die PV‑WAHL und das
    /// PV‑PROJEKT im Baustein <c>ErtragBonus</c>; an <c>KostenKomponenteStand</c> gibt
    /// es keine davon, und genau deshalb gibt es die Sicht.</para>
    /// <para>Der Reiter „Ertrag" wird gezeichnet, sobald der Anwender ihn öffnet —
    /// vorher steht der Baustein nicht, und die zwei Felder lesen leer.</para>
    /// </remarks>
    [Fact]
    public void Der_Assistent_liest_die_drei_Luecken_des_Reiters_Ertrag()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["IstVariante"] = true,
            ["Uebernommen"] = false,
            ["Projekte"] = (IReadOnlyList<(int, string)>)new[] { (7, "Musterprojekt"),
                                                                 (8, "Zweitprojekt") }
        };

        var cut = Zeige(stand: mit);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.KOSTENVERWALTUNG));

        // Der BESTAND läuft unverändert weiter.
        Assert.Equal("Kostenverwaltung Wärmepumpe",
                     KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG,
                                                "komponente").Lesen());
        Assert.Equal(5, KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG,
                                                   "variante").Lesen());
        Assert.Equal(1200.0, KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG,
                                                        "satz_1").Lesen());

        // Die KOMPONENTENWAHL: lesbar samt ihren Alternativen, nicht setzbar.
        KiFeldzugang wahl =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG, "komponentenwahl");
        Assert.NotNull(wahl);
        Assert.Equal(0, wahl.Lesen());
        Assert.False(wahl.Setzbar);
        Assert.Equal(2, wahl.Wahleintraege().Count);

        // Solange der Reiter nicht offen steht, gibt es den Baustein nicht.
        Assert.Null(KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG,
                                               "pv_verguetung").Lesen());

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter „Ertrag/Bonus"

        // Die PV-WAHL: 1 = eigene Vergütung (Uebernommen = false), nur lesbar.
        KiFeldzugang pv =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG, "pv_verguetung");
        Assert.Equal(1, pv.Lesen());
        Assert.False(pv.Setzbar);
        Assert.Equal(2, pv.Wahleintraege().Count);

        // Das PV-PROJEKT: vorbelegt auf den ersten Eintrag, setzbar über die Liste.
        KiFeldzugang projekt =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG, "pv_projekt");
        Assert.Equal(7, projekt.Lesen());
        Assert.True(projekt.Setzbar);

        KiFeldumsetzung ziel = KiFeldwandler.Wandle(projekt, "Zweitprojekt");
        Assert.True(ziel.Ok, ziel.Grund);
        projekt.Setzen(ziel.Wert);

        Assert.Equal(8, cut.FindComponent<ErtragBonus>().Instance.GewaehltesProjekt);
    }

    /// <summary>
    /// <b>Ohne Projektliste steht nichts zur Wahl</b> (VV‑Q7): Im Projektmodus zeigt
    /// die Maske die Liste nicht — dann lehnt der Assistent die Setzung benannt ab,
    /// statt eine Id zu raten, die der Anwender nirgends sieht.
    /// </summary>
    [Fact]
    public void Ohne_Projektliste_hat_das_PV_Projekt_keine_Eintraege()
    {
        KostenKomponenteStand mit = Standard(projekt: true);
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["ProjektlisteZeigen"] = false,
            ["ProjektVorwahl"] = (int?)42,
            ["Projekte"] = (IReadOnlyList<(int, string)>)new[] { (7, "Musterprojekt") }
        };

        var cut = Zeige(stand: mit);
        cut.FindAll(".epos-reiter-knopf")[1].Click();

        KiFeldzugang projekt =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.KOSTENVERWALTUNG, "pv_projekt");

        Assert.Equal(42, projekt.Lesen());
        Assert.Empty(projekt.Wahleintraege());

        KiFeldumsetzung ziel = KiFeldwandler.Wandle(projekt, "Musterprojekt");
        Assert.False(ziel.Ok);
    }
}
