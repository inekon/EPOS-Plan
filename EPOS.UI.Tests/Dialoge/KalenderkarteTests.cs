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
