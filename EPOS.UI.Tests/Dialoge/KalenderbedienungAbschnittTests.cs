using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Kalenderbedienung</b> (Konzept Konditionierungsprofile 7.8, E110; Welle K1b) — der Baustein im Abschnitt
/// „Kalender {Größe} im Einzelnen" über dem echten Weg der Hülle OHNE Datenbank
/// (<c>KonditionierungHuelle.ReinerWeg</c>): Wochenprofil mit Pinsel, Zuordnungszeile mit „gilt für", Einzeltag,
/// Ferien, Feiertage, Jahresraster, Monat kopieren, Vorlage für alle Größen, Warnzeile der Rangfolge.
/// </summary>
/// <remarks>Jede Handlung geht in den Arbeitsstand; geschrieben wird erst mit dem OK des Editors. Kultur de-DE.</remarks>
public class KalenderbedienungAbschnittTests : EposBunitContext
{
    public KalenderbedienungAbschnittTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private KonditionierungBearbeitung _bearbeitung = default!;
    private GebaeudeArbeitsstand _arbeit = default!;
    private readonly List<string> _meldungen = new();

    private const KonditionierungGroesse H = KonditionierungGroesse.Heizen;
    private const KonditionierungGroesse L = KonditionierungGroesse.Lueftung;

    private IRenderedComponent<KonditionierungReiter> Aufbauen(KonditionierungWeg? weg = null)
    {
        _arbeit = new GebaeudeArbeitsstand();
        _arbeit.Laden(KalenderkarteTests.Satz(), neu: false);
        KonditionierungWeg w = weg ?? KalenderkarteTests.Weg(null);
        _bearbeitung = new KonditionierungBearbeitung(_arbeit, () => w) { Melden = (m, _) => _meldungen.Add(m) };
        return Render<KonditionierungReiter>(p => p.Add(x => x.Bearbeitung, _bearbeitung).Add(x => x.EntprellungMs, 0));
    }

    private static IElement Knopf(IElement wurzel, string klasse)
        => wurzel.QuerySelector("button." + klasse) ?? throw new InvalidOperationException("Kein Knopf ." + klasse);

    /// <summary>Legt die Kalender an und klappt die Karte der ersten Größe auf; liefert die Bedienung.</summary>
    private IRenderedComponent<KalenderbedienungAbschnitt> Bedienung(IRenderedComponent<KonditionierungReiter> cut, params KonditionierungGroesse[] groessen)
    {
        foreach (KonditionierungGroesse g in groessen)
            Knopf(KalenderkarteTests.Karte(cut, g), "epos-kond-anlegen").Click();
        Knopf(KalenderkarteTests.Karte(cut, groessen[0]), "epos-kond-einzelheiten").Click();
        return cut.FindComponents<KalenderbedienungAbschnitt>().Single();
    }

    private static IElement Wurzel(IRenderedComponent<KalenderbedienungAbschnitt> b) => b.Find(".epos-kalb");

    /// <summary>Legt über den Editor eine Zuordnungszeile an.</summary>
    private static void ZeileAnlegen(IRenderedComponent<KalenderbedienungAbschnitt> b, string von, string bis, string name,
                                     params KonditionierungGroesse[] abwaehlen)
    {
        Knopf(Wurzel(b), "epos-kalb-zeile-neu").Click();
        IElement editor = b.Find(".epos-kalb-zuordnung .epos-kalb-editor");
        IReadOnlyList<IElement> daten = editor.QuerySelectorAll("input[type=date]").ToList();
        daten[0].Change(von);
        b.Find(".epos-kalb-zuordnung .epos-kalb-editor").QuerySelectorAll("input[type=date]")[1].Change(bis);
        b.Find(".epos-kalb-zuordnung .epos-kalb-editor input:not([type=date])").Input(name);
        foreach (KonditionierungGroesse g in abwaehlen)
            b.Find($".epos-kalb-zuordnung .epos-kalb-gilt[data-groesse='{(int)g}']").Click();
        Knopf(b.Find(".epos-kalb-zuordnung .epos-kalb-editor"), "epos-kalb-uebernehmen").Click();
    }

    [Fact]
    public void Ohne_Gaben_steht_keine_Kalenderbedienung_und_der_Baustein_nennt_seinen_Grund()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(KonditionierungWeg.Keiner);
        Assert.Empty(cut.FindAll(".epos-kalb"));
        Assert.False(_bearbeitung.MitKalenderbedienung);

        IRenderedComponent<KalenderbedienungAbschnitt> b = Render<KalenderbedienungAbschnitt>(p => p.Add(x => x.Bearbeitung, _bearbeitung));
        Assert.NotNull(b.Find(".epos-kalb-leer"));
        Assert.Null(b.Instance.Ansicht);
    }

    [Fact]
    public void Der_Pinsel_malt_das_Rechteck_zwischen_zwei_Zellen_in_die_Standardwoche()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Assert.Null(b.Instance.GewaehltesProfil);                                   // die Standardwoche
        Assert.Equal(7 * 24, b.FindAll(".epos-kalb-woche button.epos-kalb-zelle").Count);

        b.Find(".epos-kalb-pinsel input").Input("23");
        b.Find(".epos-kalb-zelle[data-tag='0'][data-stunde='6']").Click();
        Assert.Equal((0, 6), b.Instance.Anker);
        b.Find(".epos-kalb-zelle[data-tag='4'][data-stunde='8']").Click();
        Assert.Null(b.Instance.Anker);

        double[] woche = _bearbeitung.Kalender(H)!.Woche!;
        Assert.Equal(23.0, woche[0 * 24 + 6]);
        Assert.Equal(23.0, woche[4 * 24 + 8]);
        Assert.Equal(23.0, woche[2 * 24 + 7]);
        Assert.NotEqual(23.0, woche[5 * 24 + 7]);
        Assert.NotEqual(23.0, woche[0 * 24 + 9]);

        // Pinsel „aus": die eine Zelle zweimal geklickt.
        Knopf(Wurzel(b), "epos-kalb-pinsel-aus").Click();
        b.Find(".epos-kalb-zelle[data-tag='6'][data-stunde='3']").Click();
        b.Find(".epos-kalb-zelle[data-tag='6'][data-stunde='3']").Click();
        Assert.True(double.IsNaN(_bearbeitung.Kalender(H)!.Woche![6 * 24 + 3]));

        // „Montag nach Di–Fr" ist ein Schritt der Kern-Schicht.
        Knopf(Wurzel(b), "epos-kalb-montag").Click();
        Assert.Equal(_bearbeitung.Kalender(H)!.Woche![0 * 24 + 6], _bearbeitung.Kalender(H)!.Woche![3 * 24 + 6]);
    }

    [Fact]
    public void Ohne_angelegten_Kalender_ist_der_Pinsel_weich_gesperrt_und_meldet_den_Grund()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Render<KalenderbedienungAbschnitt>(p => p.Add(x => x.Bearbeitung, _bearbeitung)
            .Add(x => x.Groesse, H).Add(x => x.Verweigert, (string m) => _meldungen.Add(m)));
        Assert.NotNull(b.Find(".epos-kalb-profilsperre"));
        Assert.Equal("true", Knopf(Wurzel(b), "epos-kalb-montag").GetAttribute("aria-disabled"));
        b.Find(".epos-kalb-pinsel input").Input("21");
        b.Find(".epos-kalb-zelle[data-tag='0'][data-stunde='0']").Click();
        b.Find(".epos-kalb-zelle[data-tag='0'][data-stunde='0']").Click();
        Assert.NotEmpty(_meldungen);
        Assert.False(_bearbeitung.Angelegt(H));
    }

    [Fact]
    public void Eine_Zuordnungszeile_gilt_fuer_die_gewaehlten_Groessen_und_bringt_ihr_Wochenprofil_mit()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        ZeileAnlegen(b, "2025-03-01", "2025-03-10", "Messe", L);

        KalenderZuordnungszeile z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Messe");
        Assert.Equal(new[] { H }, z.GiltFuer);
        Assert.Equal(60, z.Schluessel.Beginn);
        Assert.Equal(69, z.Schluessel.Ende);
        Assert.Equal(KalenderWirkung.Wochenprofil, z.Wirkung.Wirkung);
        Assert.Contains(b.Instance.Ansicht.Profile, p => p.Name == "Messe");
        Assert.DoesNotContain(_bearbeitung.Kalender(L)!.Perioden, p => p.Name == "Messe");
        Assert.Equal("Messe", b.Find(".epos-kalb-zeilen tr.epos-kalb-zeile--gewaehlt").GetAttribute("data-name"));

        // Das Jahresband: der Abschnitt der Zeile wählt sie; ihr eigenes Wochenprofil wird gewählt.
        b.Find(".epos-kalb-bandteil--zeile[data-name='Messe']").Click();
        Assert.Equal("Messe", b.Instance.GewaehlteZeile!.Name);
        Assert.Equal(z.Raenge[H], b.Instance.GewaehltesProfil);

        // Entfernen wirkt in allen Größen.
        Knopf(b.Find(".epos-kalb-zeilen tr[data-name='Messe']"), "epos-kalb-entfernen").Click();
        Assert.DoesNotContain(b.Instance.Ansicht!.Zuordnungen, x => x.Schluessel.Name == "Messe");
    }

    [Fact]
    public void Ein_Einzeltag_wirkt_wie_Sonntag_in_allen_angelegten_Groessen()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        Knopf(Wurzel(b), "epos-kalb-einzeltag-neu").Click();
        IElement editor = b.Find(".epos-kalb-einzeltage .epos-kalb-editor");
        editor.QuerySelector("input[type=date]")!.Change("2025-06-02");
        b.Find(".epos-kalb-einzeltage .epos-kalb-editor input:not([type=date])").Input("Betriebsausflug");
        Knopf(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"), "epos-kalb-uebernehmen").Click();

        KalenderZuordnungszeile z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Betriebsausflug");
        Assert.True(z.IstEinzeltag);
        Assert.Equal(153, z.ErsterTag);
        Assert.Equal(new[] { H, L }, z.GiltFuer);
        Assert.Equal(KalenderWirkung.WieWochentag, z.Wirkung.Wirkung);
        Assert.Equal(7, z.Wirkung.Wochentag);
        Assert.NotNull(b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug']"));
    }

    [Fact]
    public void Ferien_werden_hinzugefuegt_und_entfernt_und_fuellen_die_Gebaeudespalten()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        int vorher = b.FindAll(".epos-kalb-ferienzeile").Count;

        Knopf(Wurzel(b), "epos-kalb-ferien-neu").Click();
        IElement neu = b.FindAll(".epos-kalb-ferienzeile")[vorher];
        neu.QuerySelectorAll("input[type=date]")[0].Change("2025-08-04");
        b.FindAll(".epos-kalb-ferienzeile")[vorher].QuerySelectorAll("input[type=date]")[1].Change("2025-08-15");

        Assert.Equal(vorher + 1, b.Instance.Ansicht!.Ferien.Count);
        Assert.Equal(216, _arbeit.Ferienbeginne()[vorher]);
        Assert.Equal(227, _arbeit.Ferienenden()[vorher]);

        Knopf(b.FindAll(".epos-kalb-ferienzeile")[vorher], "epos-kalb-ferien-entfernen").Click();
        Assert.Equal(vorher, b.Instance.Ansicht!.Ferien.Count);
        Assert.Equal(vorher, b.FindAll(".epos-kalb-ferienzeile").Count);
    }

    [Fact]
    public void Feiertage_laden_legt_die_neun_Regeln_in_jeder_angelegten_Groesse_an()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        Knopf(Wurzel(b), "epos-kalb-feiertage-laden").Click();
        Assert.Equal(9, _bearbeitung.Kalender(H)!.Perioden.Count(p => p.Feiertagsregel is not null));
        Assert.Equal(9, _bearbeitung.Kalender(L)!.Perioden.Count(p => p.Feiertagsregel is not null));
        Assert.Equal(9, b.FindAll(".epos-kalb-tage tbody tr").Count);
    }

    [Fact]
    public void Ein_Klick_im_Jahresraster_waehlt_die_Zeile_die_den_Tag_bestimmt()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Assert.Equal(365, b.FindAll(".epos-kalb-jahr button.epos-kalb-jahrtag").Count);
        ZeileAnlegen(b, "2025-03-01", "2025-03-10", "Messe");
        Assert.Empty(b.FindAll(".epos-kalb-zuordnung .epos-kalb-editor"));

        b.Find(".epos-kalb-jahrtag[data-tag='1']").Click();                       // Standardwoche: keine Zeile
        Assert.Equal("Messe", b.Instance.GewaehlteZeile?.Name);                     // die zuletzt angelegte bleibt
        b.Find(".epos-kalb-jahrtag[data-tag='65']").Click();                       // 06.03.
        Assert.Equal("Messe", b.Instance.GewaehlteZeile!.Name);
        Assert.Contains("epos-kalb-jahrtag--gewaehlt", b.Find(".epos-kalb-jahrtag[data-tag='65']").ClassList);
        Assert.Contains("Messe", b.Find(".epos-kalb-tagzeile").TextContent);
        // Das eigene Wochenprofil der Zeile ist gewählt.
        Assert.NotNull(b.Instance.GewaehltesProfil);
    }

    [Fact]
    public void Monat_kopieren_uebertraegt_die_Zeile_in_den_Zielmonat()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        ZeileAnlegen(b, "2025-03-01", "2025-03-10", "Messe");
        b.FindAll(".epos-kalb-zuordnung .epos-formularraster select")[^2].Change("3");
        b.FindAll(".epos-kalb-zuordnung .epos-formularraster select")[^1].Change("4");
        Knopf(Wurzel(b), "epos-kalb-monat-kopieren").Click();
        Assert.Contains(b.Instance.Ansicht!.Zuordnungen, z => z.Schluessel.Name == "Messe" && z.Schluessel.Beginn == 91 && z.Schluessel.Ende == 100);
    }

    [Fact]
    public void Die_Vorlage_fuer_alle_Groessen_stellt_die_Rueckfrage_der_Bearbeitung()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen(KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat()));
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        IElement knopf = Knopf(Wurzel(b), "epos-kalb-vorlage-alle");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        b.Find(".epos-kalb-vorlage select").Change("1");
        Knopf(Wurzel(b), "epos-kalb-vorlage-alle").Click();
        Assert.NotNull(_bearbeitung.OffeneFrage);
    }

    [Fact]
    public void Die_Warnzeile_nennt_gekoppelte_Zeilen_mit_abweichender_Rangfolge()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        ZeileAnlegen(b, "2025-04-10", "2025-04-30", "A");
        ZeileAnlegen(b, "2025-04-20", "2025-05-10", "B");
        Assert.Empty(b.FindAll(".epos-kalb-warnung"));

        int rangB = b.Instance.Ansicht!.Zuordnungen.Single(z => z.Schluessel.Name == "B").Raenge[H];
        Assert.True(_bearbeitung.RangVerschieben(H, rangB, false));
        cut.Render();
        IElement warnung = cut.FindComponents<KalenderbedienungAbschnitt>().Single().Find(".epos-kalb-warnung");
        Assert.Contains("„A“", warnung.TextContent);
        Assert.Contains("„B“", warnung.TextContent);
        Assert.Contains("Heizen", warnung.TextContent);
    }

    [Fact]
    public void Die_Matrix_zeigt_Wochenende_und_Ferien_nur_als_Uebersicht_die_Schnellfelder_setzen_sie()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        // Matrix: die Zeilen Wochenende (3) und Ferien (4) tragen kein Eingabefeld mehr, nur Text mit Hinweis.
        foreach (int zeile in new[] { 3, 4 })
        {
            IElement zelle = cut.Find($"td.epos-kond-zelle[data-groesse='0'][data-zeile='{zeile}']");
            Assert.Empty(zelle.QuerySelectorAll("input"));
            Assert.NotNull(zelle.QuerySelector(".epos-kond-uebersicht"));
        }
        Assert.NotEmpty(cut.Find("td.epos-kond-zelle[data-groesse='0'][data-zeile='1']").QuerySelectorAll("input"));

        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        b.Find(".epos-kalb-wochenende input").Input("15");
        Assert.Equal(15.0, _bearbeitung.Wert(H, KonditionierungZeile.Wochenende));
        Assert.Contains("15", cut.Find("td.epos-kond-zelle[data-groesse='0'][data-zeile='3']").TextContent);

        // Ohne Kalenderbedienung (leerer Weg) bleibt die Matrix bedienbar.
        IRenderedComponent<KonditionierungReiter> ohne = Aufbauen(KonditionierungWeg.Keiner);
        Assert.Empty(ohne.FindAll(".epos-kond-uebersicht"));
    }

    // ================================================================ Stufe 2 (Welle K2-U)

    /// <summary>Legt über den Editor einen Einzeltag „wie Sonntag" in allen angelegten Größen an.</summary>
    private static void EinzeltagAnlegen(IRenderedComponent<KalenderbedienungAbschnitt> b, string datum, string name)
    {
        Knopf(Wurzel(b), "epos-kalb-einzeltag-neu").Click();
        b.Find(".epos-kalb-einzeltage .epos-kalb-editor input[type=date]").Change(datum);
        b.Find(".epos-kalb-einzeltage .epos-kalb-editor input:not([type=date])").Input(name);
        Knopf(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"), "epos-kalb-uebernehmen").Click();
    }

    [Fact]
    public void Die_Maske_gilt_fuer_wird_je_Zeile_ueber_fuenf_Kaestchen_und_alle_gesetzt()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        EinzeltagAnlegen(b, "2025-06-02", "Betriebsausflug");
        KalenderZuordnungszeile z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Betriebsausflug");
        Assert.True(z.IstGemeinsam);

        IElement zeile = b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug']");
        Assert.Equal(5, zeile.QuerySelectorAll(".epos-kalb-maske button.epos-kalb-maskenbit").Length);
        Assert.NotNull(zeile.QuerySelector(".epos-kalb-maske button.epos-kalb-maske-alle"));
        Assert.Empty(zeile.QuerySelectorAll(".epos-kalb-getrennt"));

        // Lüftung abwählen: die Zeile gilt nur noch für Heizen.
        b.Find($".epos-kalb-tage tr[data-name='Betriebsausflug'] .epos-kalb-maskenbit[data-groesse='{(int)L}']").Click();
        z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Betriebsausflug");
        Assert.Equal(1, z.Maske);
        Assert.Equal(new[] { H }, z.GiltFuer);
        Assert.Equal("false", b.Find($".epos-kalb-tage tr[data-name='Betriebsausflug'] .epos-kalb-maskenbit[data-groesse='{(int)L}']")
                                .GetAttribute("aria-pressed"));

        // Auch Heizen abwählen wäre eine leere Maske: benannt abgelehnt, nie still.
        _meldungen.Clear();
        b.Find($".epos-kalb-tage tr[data-name='Betriebsausflug'] .epos-kalb-maskenbit[data-groesse='{(int)H}']").Click();
        Assert.NotEmpty(_meldungen);
        Assert.Equal(1, b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Betriebsausflug").Maske);

        // „alle" setzt die volle Maske.
        b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug'] .epos-kalb-maske-alle").Click();
        z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Betriebsausflug");
        Assert.Equal(31, z.Maske);
        Assert.Contains(L, z.GiltFuer);
    }

    [Fact]
    public void Eine_Zeile_der_Lesebruecke_ist_kenntlich_und_traegt_keine_Maskenbedienung()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        ZeileAnlegen(b, "2025-03-01", "2025-03-10", "Messe");            // Standardwoche über zwei Größen: je Größe eine Kopie

        KalenderZuordnungszeile z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Messe");
        Assert.False(z.IstGemeinsam);
        IElement zeile = b.Find(".epos-kalb-zeilen tr[data-name='Messe']");
        IElement kennzeichen = zeile.QuerySelector(".epos-kalb-getrennt")!;
        Assert.NotNull(kennzeichen);
        Assert.Contains("je Größe getrennt", kennzeichen.TextContent);
        Assert.Contains("Je Größe getrennt", kennzeichen.GetAttribute("title"));
        Assert.Empty(zeile.QuerySelectorAll(".epos-kalb-maske"));
        Assert.Contains("Heizen", zeile.QuerySelector(".epos-kalb-giltliste")!.TextContent);
    }

    [Fact]
    public void Benannte_Wochen_werden_angelegt_umbenannt_und_erst_ohne_Verweis_geloescht()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);

        // Umbenennen und Löschen ohne gewählte benannte Woche: weich gesperrt mit Grund.
        Assert.Equal("true", Knopf(Wurzel(b), "epos-kalb-woche-loeschen").GetAttribute("aria-disabled"));

        b.Find(".epos-kalb-wochen input").Input("Büro");
        Knopf(Wurzel(b), "epos-kalb-woche-anlegen").Click();
        KalenderWochenprofil buero = b.Instance.Ansicht!.Profile.Single(p => p.IstBenannt);
        Assert.Equal("Büro", buero.Name);
        Assert.Equal(buero.IdWoche, b.Instance.GewaehlteWoche);
        Assert.Contains("epos-kalb-profil--benannt", b.Find($".epos-kalb-profil[data-woche='{buero.IdWoche}']").ClassList);

        // Ein doppelter Name wird benannt abgelehnt.
        _meldungen.Clear();
        Knopf(Wurzel(b), "epos-kalb-woche-anlegen").Click();
        Assert.NotEmpty(_meldungen);
        Assert.Single(b.Instance.Ansicht!.Profile, p => p.IstBenannt);

        // Umbenennen.
        b.Find(".epos-kalb-wochen input").Input("Büro kurz");
        Knopf(Wurzel(b), "epos-kalb-woche-umbenennen").Click();
        Assert.Equal("Büro kurz", b.Instance.Ansicht!.Profile.Single(p => p.IstBenannt).Name);

        // Der Pinsel malt in die benannte Woche (über ihren Verweis), nicht in die Standardwoche.
        double[] standard = b.Instance.Ansicht!.Profile.First(p => p.IstStandardwoche).Werte;
        b.Find(".epos-kalb-pinsel input").Input("12");
        b.Find(".epos-kalb-woche button.epos-kalb-zelle[data-tag='0'][data-stunde='3']").Click();
        b.Find(".epos-kalb-woche button.epos-kalb-zelle[data-tag='0'][data-stunde='3']").Click();
        Assert.Equal(12.0, b.Instance.Ansicht!.Profile.Single(p => p.IstBenannt).Werte[3]);
        Assert.Equal(standard[3], b.Instance.Ansicht!.Profile.First(p => p.IstStandardwoche).Werte[3]);

        // Eine Zeile wählt die Woche als Wochenprofil (Verweis IdWoche).
        Knopf(Wurzel(b), "epos-kalb-zeile-neu").Click();
        IElement editor = b.Find(".epos-kalb-zuordnung .epos-kalb-editor");
        editor.QuerySelectorAll("input[type=date]")[0].Change("2025-03-01");
        b.Find(".epos-kalb-zuordnung .epos-kalb-editor").QuerySelectorAll("input[type=date]")[1].Change("2025-03-10");
        b.Find(".epos-kalb-zuordnung .epos-kalb-editor input:not([type=date])").Input("Messe");
        int index = b.Instance.Ansicht!.Profile.ToList().FindIndex(p => p.IstBenannt) + 1;
        b.FindAll(".epos-kalb-zuordnung .epos-kalb-editor select")[1].Change(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Knopf(b.Find(".epos-kalb-zuordnung .epos-kalb-editor"), "epos-kalb-uebernehmen").Click();
        KalenderZuordnungszeile z = b.Instance.Ansicht!.Zuordnungen.Single(x => x.Schluessel.Name == "Messe");
        long id = b.Instance.Ansicht!.Profile.Single(p => p.IstBenannt).IdWoche!.Value;
        Assert.Equal(id, z.IdWoche);
        Assert.True(z.IstGemeinsam);

        // Löschen, solange die Zeile verweist: abgelehnt mit Name und Zeilenzahl im Banner des Wirts.
        b.Find($".epos-kalb-profil[data-woche='{id}']").Click();
        _meldungen.Clear();
        Knopf(Wurzel(b), "epos-kalb-woche-loeschen").Click();
        Assert.Contains(_meldungen, m => m.Contains("Büro kurz"));
        Assert.Single(b.Instance.Ansicht!.Profile, p => p.IstBenannt);

        // Ohne Verweis geht es.
        Knopf(b.Find(".epos-kalb-zeilen tr[data-name='Messe']"), "epos-kalb-entfernen").Click();
        b.Find($".epos-kalb-profil[data-woche='{id}']").Click();
        Knopf(Wurzel(b), "epos-kalb-woche-loeschen").Click();
        Assert.DoesNotContain(b.Instance.Ansicht!.Profile, p => p.IstBenannt);
        Assert.Null(b.Instance.GewaehlteWoche);
    }

    [Fact]
    public void Das_Wochenende_wird_ueber_sieben_Tageskaestchen_und_Sa_So_gesetzt()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Assert.Equal(7, b.FindAll(".epos-kalb-wochenende button.epos-kalb-wochenendtag").Count);
        Assert.Equal(new[] { 5, 6 }, b.Instance.Ansicht!.Wochenendtage);
        Assert.Equal("true", b.Find(".epos-kalb-wochenendtag[data-tag='6']").GetAttribute("aria-pressed"));

        b.Find(".epos-kalb-wochenendtag[data-tag='4']").Click();                  // Freitag dazu
        Assert.Equal(new[] { 4, 5, 6 }, b.Instance.Ansicht!.Wochenendtage);
        b.Find(".epos-kalb-wochenendtag[data-tag='5']").Click();                  // Samstag weg
        Assert.Equal(new[] { 4, 6 }, b.Instance.Ansicht!.Wochenendtage);
        Assert.Equal("true", b.Find(".epos-kalb-wochenendtag[data-tag='4']").GetAttribute("aria-pressed"));

        Knopf(Wurzel(b), "epos-kalb-sa-so").Click();
        Assert.Equal(new[] { 5, 6 }, b.Instance.Ansicht!.Wochenendtage);
    }

    [Fact]
    public void Das_Feiertagsland_fuegt_seine_Regeln_hinzu_und_nennt_sie_in_der_Hinweiszeile()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Assert.Null(b.Instance.Ansicht!.Feiertagsland);
        Assert.Contains("neun bundeseinheitlichen", b.Find(".epos-kalb-land-hinweis").TextContent);
        Assert.Contains("Baden-Württemberg", b.Find(".epos-kalb-land select").TextContent);
        int vorher = b.Instance.Ansicht!.Zuordnungen.Count;

        b.Find(".epos-kalb-land select").Change("1");                                // BW
        Assert.Equal("BW", b.Instance.Ansicht!.Feiertagsland);
        Assert.Contains("Heilige Drei Könige", b.Find(".epos-kalb-land-hinweis").TextContent);
        Assert.Contains("Fronleichnam", b.Find(".epos-kalb-land-hinweis").TextContent);
        Assert.True(b.Instance.Ansicht!.Zuordnungen.Count > vorher);
        Assert.Contains(b.Instance.Ansicht!.Raster, t => t.Art == KalenderTagart.Feiertag && t.Quelle == "Fronleichnam");

        b.Find(".epos-kalb-land select").Change("");                                 // nur bundeseinheitlich
        Assert.Null(b.Instance.Ansicht!.Feiertagsland);
        Assert.Equal(vorher, b.Instance.Ansicht!.Zuordnungen.Count);
    }

    [Fact]
    public void Die_Ferienliste_fuehrt_benannte_Zeitraeume_beliebig_viele()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        int vorher = b.FindAll(".epos-kalb-ferienzeile").Count;
        string[] monate = { "02", "03", "04", "05", "06", "07" };
        for (int k = 0; k < monate.Length; k++)
        {
            Knopf(Wurzel(b), "epos-kalb-ferien-neu").Click();
            int n = vorher + k;
            b.FindAll(".epos-kalb-ferienzeile")[n].QuerySelectorAll("input[type=date]")[0].Change($"2025-{monate[k]}-10");
            b.FindAll(".epos-kalb-ferienzeile")[n].QuerySelectorAll("input[type=date]")[1].Change($"2025-{monate[k]}-12");
        }
        Assert.Equal(vorher + monate.Length, b.Instance.Ansicht!.Ferien.Count);

        Assert.Equal(vorher + monate.Length, b.FindAll(".epos-kalb-ferienzeile").Count);

        // Die Bezeichnung ist ein eigenes Feld, für jeden Zeitraum schreibbar: Ab dem fünften reist sie mit der Ferienliste,
        // die ersten vier tragen sie im Bezeichner ihrer Spiegelperiode.
        int letzte = vorher + monate.Length - 1;
        Assert.True(letzte >= 4);
        IElement erstes = b.FindAll(".epos-kalb-ferienzeile")[0].QuerySelector("input:not([type=date])")!;
        Assert.False(erstes.HasAttribute("readonly"));
        erstes.Input("Winterferien");
        Assert.Equal("Winterferien", b.Instance.Ansicht!.Ferien[0].Name);
        Assert.Equal("Winterferien", b.FindAll(".epos-kalb-ferienzeile")[0].QuerySelector("input:not([type=date])")!.GetAttribute("value"));
        b.FindAll(".epos-kalb-ferienzeile")[letzte].QuerySelector("input:not([type=date])")!.Input("Sommerpause");
        Assert.Contains(b.Instance.Ansicht!.Ferien, f => f.Name == "Sommerpause" && f.Beginn == Kalendertage.Jahrestag(7, 10));
        Assert.Equal("Sommerpause", b.FindAll(".epos-kalb-ferienzeile")[letzte].QuerySelector("input:not([type=date])")!.GetAttribute("value"));
    }

    [Fact]
    public void Ein_Klick_im_Jahresraster_oeffnet_die_bestimmende_Zeile_und_kennzeichnet_gemeinsame_Zeilen()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H, L);
        EinzeltagAnlegen(b, "2025-06-02", "Betriebsausflug");
        Assert.Contains("epos-kalb-jahrtag--gemeinsam", b.Find(".epos-kalb-jahrtag[data-tag='153']").ClassList);
        Assert.DoesNotContain("epos-kalb-jahrtag--gemeinsam", b.Find(".epos-kalb-jahrtag[data-tag='152']").ClassList);
        Assert.NotNull(b.Find(".epos-kalb-legende .epos-kalb-muster--gemeinsam"));

        Assert.Empty(b.FindAll(".epos-kalb-einzeltage .epos-kalb-editor"));
        b.Find(".epos-kalb-jahrtag[data-tag='153']").Click();
        Assert.Equal("Betriebsausflug", b.Instance.GewaehlteZeile!.Name);
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"));
    }
}
