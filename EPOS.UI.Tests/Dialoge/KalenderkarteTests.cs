using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Kalenderkarte mit den Vorlagen ihrer Größe</b> (Stufe KP2, Welle U2; Teilkonzept
/// Konditionierungsprofile 3.5, 7.4, 7.5; Entwurf KP2 Festlegungen 3, 13, 14; E56 F2 (a), F4 (a)).
/// </summary>
/// <remarks>
/// <para><b>Der Aufbau.</b> Der Reiter „Konditionierung" über dem echten Weg der Hülle OHNE Datenbank
/// (<c>KonditionierungHuelle.ReinerWeg</c>) mit der Ablage der 14 ausgelieferten Vorlagen aus der
/// Saattabelle (<see cref="Konditionierungsvorlagenablage.AusSaat"/>) — dieselben reinen Schritte des
/// Kerns wie in der Anwendung, dieselben Regeln der Liste. Die Entprellung der Vorschau steht auf 0,
/// außer im Fall, der sie prüft.</para>
/// <para><b>Was geprüft wird:</b> die Auswahlliste je Karte (die ausgelieferten mit Kennzeichen, die
/// eigenen ohne; gleiche Namen an derselben Stelle jeder Liste), die Vorschau der Woche VOR dem
/// Übernehmen (der Arbeitsstand bleibt), ihre Entprellung, und ohne Vorlagen im Weg keine Liste („kein
/// Delegat, kein Knopf").</para>
/// <para>Die Kultur ist auf de-DE gepinnt (deutsche Rückfalltexte).</para>
/// </remarks>
public class KalenderkarteTests : EposBunitContext
{
    public KalenderkarteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private KonditionierungBearbeitung _bearbeitung = default!;
    private GebaeudeArbeitsstand _arbeit = default!;
    private readonly List<string> _meldungen = new();

    /// <summary>Ein Katalogbau mit Heizwerten (Tag 20 °C, Nacht 17 °C von 22 bis 6 Uhr), gekühlt, Lüftung getrennt.</summary>
    internal static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Probebau",
        Konditionierung = new KonditionierungDaten(),
        SollTag = 20,
        NachtAbsenkung = 17,
        NachtBeginn = 22,
        NachtEnde = 6,
        KuehlungAktiv = true,
        KuehlSollwert = 26,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Luftwechselrate = 0.5,
        LuftwechselInfiltration = 0.2,
        LuftwechselNutzer = 0.3
    };

    /// <summary>Der Weg der Hülle ohne Datenbank, mit der Ablage <paramref name="ablage"/> (<c>null</c> = ohne Vorlagen).</summary>
    internal static KonditionierungWeg Weg(IKonditionierungsvorlagen? ablage)
        => KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false, vorlagen: ablage);

    private IRenderedComponent<KonditionierungReiter> Aufbauen(KonditionierungWeg weg, GebaeudeKatalogDaten? satz = null,
                                                               int entprellungMs = 0, bool lesemodus = false)
    {
        _arbeit = new GebaeudeArbeitsstand();
        _arbeit.Laden(satz ?? Satz(), neu: false);
        _bearbeitung = new KonditionierungBearbeitung(_arbeit, () => weg) { Melden = (m, _) => _meldungen.Add(m) };
        return Render<KonditionierungReiter>(p => p
            .Add(x => x.Bearbeitung, _bearbeitung)
            .Add(x => x.Lesemodus, lesemodus)
            .Add(x => x.EntprellungMs, entprellungMs));
    }

    /// <summary>Die Karte einer Größe.</summary>
    internal static IElement Karte(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => cut.Find($"section.epos-kond-karte[data-groesse='{(int)g}']");

    /// <summary>Die Auswahlliste der Karte einer Größe.</summary>
    internal static IElement Liste(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => Karte(cut, g).QuerySelector(".epos-kond-vorlagewahl select")
           ?? throw new InvalidOperationException("Die Karte " + g + " trägt keine Auswahlliste.");

    /// <summary>Die Texte der Einträge einer Auswahlliste, ohne den leeren Eintrag.</summary>
    private static List<string> Namen(IElement liste)
        => liste.QuerySelectorAll("option").Where(o => o.GetAttribute("value") != "")
                .Select(o => o.TextContent.Trim()).ToList();

    /// <summary>Wählt in der Karte die Vorlage mit diesem Namen („keine Vorlage gewählt" = keine).</summary>
    internal static void Waehlen(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g, string name)
    {
        IElement liste = Liste(cut, g);
        string id = liste.QuerySelectorAll("option").First(o => o.TextContent.Trim() == name).GetAttribute("value")!;
        liste.Change(id);
    }

    /// <summary>Das Eingabefeld der Matrixzelle mit diesem Namen („Heizen · Tag").</summary>
    internal static IElement Eingabe(IRenderedComponent<KonditionierungReiter> cut, string feld)
        => cut.FindAll("table.epos-kond-matrix label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")!.TextContent.Trim() == feld)
              .QuerySelector("input")!;

    // =================================================================================
    // Die Auswahlliste
    // =================================================================================

    [Fact]
    public void Ohne_Vorlagen_im_Weg_steht_keine_Auswahlliste()
    {
        var cut = Aufbauen(Weg(null));

        Assert.Equal(5, cut.FindAll("section.epos-kond-karte").Count);
        Assert.Empty(cut.FindAll(".epos-kond-vorlagewahl"));
    }

    [Fact]
    public void Jede_Karte_fuehrt_die_Vorlagen_ihrer_Groesse_und_Buero_steht_ueberall_an_derselben_Stelle()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));

        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            IElement liste = Liste(cut, g);
            Assert.Equal("keine Vorlage gewählt", liste.QuerySelector("option[value='']")!.TextContent.Trim());
            List<string> namen = Namen(liste);
            Assert.Equal(g == KonditionierungGroesse.Lueftung ? new[] { "Büro", "Schule" } : new[] { "Büro", "Schule", "Wohnen" },
                         namen);
            Assert.Equal(0, namen.IndexOf("Büro"));
        }
        Assert.Empty(cut.FindAll(".epos-schloss"));
    }

    [Fact]
    public void Eine_ausgelieferte_Vorlage_traegt_das_Kennzeichen_eine_eigene_nicht_und_steht_hinten()
    {
        var ablage = Konditionierungsvorlagenablage.AusSaat();
        ablage.Hinzufuegen(Konditionierungsgroesse.Heizsoll, "Aula", "eigene Vorlage", DbWerte.KOND_NUTZUNG_SCHULE,
                           ausgeliefert: false, Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                               .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(19)));
        var cut = Aufbauen(Weg(ablage));

        Assert.Equal(new[] { "Büro", "Schule", "Wohnen", "Aula" }, Namen(Liste(cut, KonditionierungGroesse.Heizen)));

        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        IElement karte = Karte(cut, KonditionierungGroesse.Heizen);
        IElement schloss = karte.QuerySelector(".epos-kond-vorlagewahl .epos-schloss")!;
        Assert.NotNull(schloss);
        Assert.Contains("Ausgelieferte Vorlage", schloss.GetAttribute("aria-label"));
        Assert.Contains("EPOS-Muster", karte.QuerySelector(".epos-kond-vorlage-beschreibung")!.TextContent);

        Waehlen(cut, KonditionierungGroesse.Heizen, "Aula");
        Assert.Null(Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-schloss"));
        Assert.Equal("eigene Vorlage",
                     Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorlage-beschreibung")!.TextContent.Trim());
    }

    [Fact]
    public void Im_Lesemodus_steht_keine_Auswahlliste_und_keine_Vorschau()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()), lesemodus: true);

        Assert.Empty(cut.FindAll(".epos-kond-vorlagewahl"));
        Assert.Empty(cut.FindAll(".epos-kond-vorschau"));
    }

    // =================================================================================
    // Übernehmen (P11, P12) und die Zeile „Vorlage"
    // =================================================================================

    /// <summary>Der Knopf „Übernehmen" der Karte einer Größe.</summary>
    internal static IElement Uebernehmen(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => Karte(cut, g).QuerySelector("button.epos-kond-uebernehmen")
           ?? throw new InvalidOperationException("Die Karte " + g + " trägt kein „Übernehmen“.");

    /// <summary>Beantwortet die offene Rückfrage des Reiters: Ja oder Nein.</summary>
    internal static void Antworten(IRenderedComponent<KonditionierungReiter> cut, bool ja)
        => cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == (ja ? "Ja" : "Nein")).Click();

    /// <summary>Die Zelle der Zeile „Vorlage" in der Matrix.</summary>
    private static string Herkunftszelle(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
        => cut.Find($"tr.epos-kond-herkunftzeile td[data-groesse='{(int)g}']").TextContent.Trim();

    [Fact]
    public void Uebernehmen_legt_den_Kalender_an_setzt_die_Zellen_und_nennt_die_Herkunft()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));
        Assert.Equal("—", Herkunftszelle(cut, KonditionierungGroesse.Heizen));

        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();

        // Der Kalender ist angelegt, die Herkunft steht in Karte und Matrix; die Wahl ist wieder leer.
        Assert.True(_bearbeitung.Angelegt(KonditionierungGroesse.Heizen));
        Assert.Equal("aus Vorlage Büro", Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-karte-zustand")!.TextContent.Trim());
        Assert.Equal("Büro", Herkunftszelle(cut, KonditionierungGroesse.Heizen));
        Assert.Equal("—", Herkunftszelle(cut, KonditionierungGroesse.Kuehlen));
        Assert.Null(_bearbeitung.GewaehlteVorlage(KonditionierungGroesse.Heizen));

        // Die Zellen der Vorlage stehen in der Spalte: Nacht 16 °C von 18 bis 7 Uhr, Wochenende und Ferien
        // 16 °C; der Tag (20 °C) gleicht dem Ziel. Die Feiertage „wie Sonntag" stehen als Regeln im Kalender.
        Assert.Equal(20, _arbeit.Stand.SollTag);
        Assert.Equal(16, _arbeit.Stand.NachtAbsenkung);
        Assert.Equal((18, 7), (_arbeit.Stand.NachtBeginn, _arbeit.Stand.NachtEnde));
        Assert.Equal(16, _arbeit.Stand.WochenendAbsenkung);
        Assert.Equal(16, _arbeit.Stand.SollFerien);
        Assert.Equal(9, _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Perioden.Count(p => p.Art == KonditionierungPeriodenart.Feiertag));
        Assert.Empty(_meldungen);
    }

    [Fact]
    public void Eine_leere_Zelle_der_Vorlage_laesst_die_des_Ziels_und_Nennwert_und_Saison_bleiben()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));

        // Die Lüftungsvorlage „Büro" führt keinen Tag: die Nutzerlüftung (0,3 1/h) und die Infiltration bleiben.
        Waehlen(cut, KonditionierungGroesse.Lueftung, "Büro");
        Uebernehmen(cut, KonditionierungGroesse.Lueftung).Click();
        Assert.Equal(0.3, _arbeit.Stand.LuftwechselNutzer);
        Assert.Equal(0.2, _arbeit.Stand.LuftwechselInfiltration);
        Assert.Equal(0.1, _bearbeitung.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));

        // Personen: der Nennwert bleibt dem Objekt; die Saison des Heizens ebenso.
        _bearbeitung.ZeitenSetzen(KonditionierungGroesse.Heizen, KonditionierungZeile.Saison, 274, 120);
        cut.Render();
        Waehlen(cut, KonditionierungGroesse.Heizen, "Schule");
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();
        Assert.Equal((274, 120), _bearbeitung.Zeiten(KonditionierungGroesse.Heizen, KonditionierungZeile.Saison));
        Assert.Equal("Schule", Herkunftszelle(cut, KonditionierungGroesse.Heizen));
    }

    [Fact]
    public void Ohne_Wahl_und_an_der_gesperrten_Kuehlspalte_ist_Uebernehmen_weich_gesperrt_und_nennt_den_Grund()
    {
        var cut = Render<KonditionierungReiter>(p => p
            .Add(x => x.Bearbeitung, Bearbeitung(Weg(Konditionierungsvorlagenablage.AusSaat())))
            .Add(x => x.Kuehlsperre, "Kühlung ist aus.")
            .Add(x => x.EntprellungMs, 0));

        IElement heizen = Uebernehmen(cut, KonditionierungGroesse.Heizen);
        Assert.Equal("true", heizen.GetAttribute("aria-disabled"));
        Assert.Equal("Erst eine Vorlage aus der Liste wählen.", heizen.GetAttribute("title"));
        Assert.False(heizen.HasAttribute("disabled"));
        heizen.Click();
        Assert.Equal(new[] { "Erst eine Vorlage aus der Liste wählen." }, _meldungen);
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Heizen));

        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        Assert.Null(Uebernehmen(cut, KonditionierungGroesse.Heizen).GetAttribute("aria-disabled"));
        Assert.Contains("nur auf diese Größe", Uebernehmen(cut, KonditionierungGroesse.Heizen).GetAttribute("title"));

        Waehlen(cut, KonditionierungGroesse.Kuehlen, "Büro");
        IElement kuehlen = Uebernehmen(cut, KonditionierungGroesse.Kuehlen);
        Assert.Equal("true", kuehlen.GetAttribute("aria-disabled"));
        Assert.Equal("Kühlung ist aus.", kuehlen.GetAttribute("title"));
        kuehlen.Click();
        Assert.Equal("Kühlung ist aus.", _meldungen.Last());
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Kuehlen));
    }

    [Fact]
    public void Auf_einen_angelegten_Kalender_fragt_Uebernehmen_vorher_nennt_was_ersetzt_wird_und_was_bleibt()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));
        Waehlen(cut, KonditionierungGroesse.Heizen, "Schule");
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();
        Assert.Null(_bearbeitung.OffeneFrage);                       // ohne Kalender keine Frage
        KonditionierungKalender schule = _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Kopie();

        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();
        KonditionierungBearbeitung.Rueckfrage frage = _bearbeitung.OffeneFrage!;
        Assert.NotNull(frage);
        Assert.True(frage.VorgabeNein);
        Assert.Equal("Übernehmen", frage.Titel);
        Assert.StartsWith("Die Vorlage „Büro“ auf den angelegten Kalender „Heizen“ übernehmen? Ersetzt wird: ", frage.Text);
        Assert.Contains("die Standardwoche", frage.Text);
        Assert.Contains("Es bleibt: 9 Feiertagsregeln.", frage.Text);

        // Nein: nichts ändert sich - der Kalender der Schule steht, die Wahl bleibt.
        Antworten(cut, ja: false);
        Assert.Equal("Schule", Herkunftszelle(cut, KonditionierungGroesse.Heizen));
        Assert.Equal(schule.Woche, _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Woche);
        Assert.Equal("Büro", _bearbeitung.GewaehlteVorlage(KonditionierungGroesse.Heizen)!.Name);

        // Ja: der Matrixbereich wird ersetzt, die Feiertagsregeln stehen nur einmal.
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();
        Antworten(cut, ja: true);
        Assert.Equal("Büro", Herkunftszelle(cut, KonditionierungGroesse.Heizen));
        Assert.Equal(9, _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Perioden.Count(p => p.Art == KonditionierungPeriodenart.Feiertag));
        Assert.Equal(16, schule.Woche![17]);                                                 // Schule: Nacht ab 15 Uhr
        Assert.Equal(20, _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Woche![17]);   // Büro: Nacht erst ab 18 Uhr
        Assert.True(_bearbeitung.KannZuruecknehmen);
    }

    [Fact]
    public void Nach_dem_Uebernehmen_folgt_der_unveraenderte_Kalender_der_Matrix_Vorschau_und_Kalender_zeigen_21_Grad()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));
        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        Uebernehmen(cut, KonditionierungGroesse.Heizen).Click();
        Assert.Equal(20, _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Woche![10]);
        string bildVorher = Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau svg")!.OuterHtml;

        // Heizen · Tag 21 °C: Der Kalender folgt ohne Rückfrage (E56 F2 (a)), die Feiertage bleiben.
        Eingabe(cut, "Heizen · Tag").Input("21");
        cut.Render();
        Assert.Null(_bearbeitung.OffeneFrage);
        KonditionierungKalender k = _bearbeitung.Kalender(KonditionierungGroesse.Heizen)!;
        Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
        Assert.Equal(21, k.Woche![10]);
        Assert.Equal(16, k.Woche![20]);
        Assert.Equal(9, k.Perioden.Count(p => p.Art == KonditionierungPeriodenart.Feiertag));
        Assert.Equal("Büro", Herkunftszelle(cut, KonditionierungGroesse.Heizen));

        // Die Vorschau zeigt den Kalender, wie er jetzt gilt.
        Assert.Equal(21, _bearbeitung.Vorschauwoche(KonditionierungGroesse.Heizen)![10]);
        Assert.NotEqual(bildVorher, Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau svg")!.OuterHtml);
    }

    [Fact]
    public void Eine_Lueftungsvorlage_an_der_Gesamtangabe_fragt_aufteilen_und_uebernimmt_danach_in_einem_Schritt()
    {
        GebaeudeKatalogDaten satz = Satz();
        satz.Luftwechselrate = 0.6;
        satz.LuftwechselInfiltration = null;
        satz.LuftwechselNutzer = null;
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()), satz);

        Waehlen(cut, KonditionierungGroesse.Lueftung, "Schule");
        Uebernehmen(cut, KonditionierungGroesse.Lueftung).Click();
        Assert.Contains("Aufteilen in Infiltration 0,3 1/h und Nutzerlüftung 0,3 1/h", _bearbeitung.OffeneFrage!.Text);
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Lueftung));

        Antworten(cut, ja: true);
        Assert.True(_bearbeitung.Angelegt(KonditionierungGroesse.Lueftung));
        Assert.Equal("Schule", Herkunftszelle(cut, KonditionierungGroesse.Lueftung));
        Assert.Equal(0.3, _arbeit.Stand.LuftwechselNutzer);
        Assert.Null(_bearbeitung.GewaehlteVorlage(KonditionierungGroesse.Lueftung));

        // Ein Schritt: „Zurücknehmen" nimmt Aufteilung und Vorlage zusammen zurück.
        Assert.True(_bearbeitung.Zuruecknehmen());
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Lueftung));
        Assert.Null(_arbeit.Stand.LuftwechselNutzer);
    }

    /// <summary>Eine Bearbeitung über einem frischen Satz — für Fälle, die den Reiter selbst aufbauen.</summary>
    private KonditionierungBearbeitung Bearbeitung(KonditionierungWeg weg)
    {
        _arbeit = new GebaeudeArbeitsstand();
        _arbeit.Laden(Satz(), neu: false);
        return _bearbeitung = new KonditionierungBearbeitung(_arbeit, () => weg) { Melden = (m, _) => _meldungen.Add(m) };
    }

    // =================================================================================
    // Die Vorschau
    // =================================================================================

    [Fact]
    public void Die_Vorschau_zeigt_die_Woche_der_Vorlage_vor_dem_Uebernehmen_und_aendert_nichts()
    {
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()));
        int fassung = _arbeit.Stand.Konditionierung!.Fassung;

        // Ohne Wahl: die Woche des Kalenders, wie er gilt (abgeleitet aus der Matrix): Montag 20 Uhr 20 °C.
        double[] vorher = _bearbeitung.Vorschauwoche(KonditionierungGroesse.Heizen)!;
        Assert.Equal(20, vorher[20]);
        Assert.Equal(17, vorher[23]);
        Assert.NotNull(Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau svg"));
        Assert.Null(Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau-vorlage"));

        // Mit „Büro": Mo–Fr 7–18 Uhr 20 °C, sonst 16 °C; Samstag 16 °C.
        Waehlen(cut, KonditionierungGroesse.Heizen, "Büro");
        double[] buero = _bearbeitung.Vorschauwoche(KonditionierungGroesse.Heizen)!;
        Assert.Equal(20, buero[10]);
        Assert.Equal(16, buero[20]);
        Assert.Equal(16, buero[5 * 24 + 12]);
        Assert.Contains("„Büro“", Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorschau-vorlage")!.TextContent);

        // Übernommen ist nichts: kein Kalender, dieselbe Fassung, dieselben Zellen.
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Heizen));
        Assert.Equal(fassung, _arbeit.Stand.Konditionierung!.Fassung);
        Assert.Equal(17, _arbeit.Stand.NachtAbsenkung);
        Assert.Equal("aus der Matrix", Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-karte-zustand")!.TextContent.Trim());
    }

    [Fact]
    public void Die_Vorschau_einer_Lueftungsvorlage_rechnet_an_der_Gesamtangabe_auf_der_aufgeteilten_Probe()
    {
        GebaeudeKatalogDaten satz = Satz();
        satz.Luftwechselrate = 0.6;
        satz.LuftwechselInfiltration = null;
        satz.LuftwechselNutzer = null;
        var cut = Aufbauen(Weg(Konditionierungsvorlagenablage.AusSaat()), satz);

        Waehlen(cut, KonditionierungGroesse.Lueftung, "Büro");
        double[] woche = _bearbeitung.Vorschauwoche(KonditionierungGroesse.Lueftung)!;
        Assert.Equal(0.3, woche[10], 10);    // Nutzerlüftung = 0,6 − 0,3 (F5)
        Assert.Equal(0.1, woche[20], 10);    // Nacht 18–7 Uhr: 0,1 1/h
        Assert.Null(_bearbeitung.OffeneFrage);
        Assert.Null(_arbeit.Stand.LuftwechselNutzer);   // die Aufteilung war nur die Probe
    }

    [Fact]
    public void Die_Vorschau_rechnet_nach_einer_Eingabe_entprellt_und_nach_einer_Wahl_sofort()
    {
        int bilder = 0;
        KonditionierungWeg basis = Weg(Konditionierungsvorlagenablage.AusSaat());
        var weg = new KonditionierungWeg
        {
            ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Verwerfen = basis.Verwerfen,
            MatrixErneut = basis.MatrixErneut, LuftwechselAufteilen = basis.LuftwechselAufteilen,
            Vorlagen = basis.Vorlagen, VorlageUebernehmen = basis.VorlageUebernehmen, Rueckfrage = basis.Rueckfrage,
            WochenVorschau = (g, w) =>
            {
                if (g == KonditionierungGroesse.Heizen) bilder++;
                return basis.WochenVorschau!(g, w);
            }
        };
        var cut = Aufbauen(weg, entprellungMs: 150);
        Assert.Equal(1, bilder);                        // beim Öffnen sofort

        Waehlen(cut, KonditionierungGroesse.Heizen, "Schule");
        Assert.Equal(2, bilder);                        // nach der Wahl sofort

        Waehlen(cut, KonditionierungGroesse.Heizen, "keine Vorlage gewählt");
        Assert.Equal(3, bilder);                        // die Wahl „keine" ebenso

        // Drei Eingaben hintereinander in dieselbe Zelle: EIN Bild nach der Ruhezeit. Der Wirt zeichnet
        // nach jeder Eingabe neu (im Editor über „Geaendert"); hier tut es der Prüfstand.
        foreach (string eingabe in new[] { "21", "21,5", "22" })
        {
            Eingabe(cut, "Heizen · Tag").Input(eingabe);
            cut.Render();
        }
        Assert.Equal(3, bilder);
        cut.WaitForAssertion(() => Assert.Equal(4, bilder), TimeSpan.FromSeconds(5));
        Assert.Equal(22, _bearbeitung.Vorschauwoche(KonditionierungGroesse.Heizen)![10]);
        Thread.Sleep(300);
        Assert.Equal(4, bilder);                        // und kein zweites
    }
}
