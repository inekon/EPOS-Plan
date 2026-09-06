using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Abschnitt <c>PvStraengeFelder</c> — Stufe S2 des Wechselrichterkonzepts
/// (Anwenderentscheide <b>W6‑E‑2</b> und <b>W6‑E‑3</b> vom 06.09.2026, Kapitel 7
/// und 7.1, Mockup M1).
///
/// <para>Geprüft wird, was die Komponente selbst entscheidet: die zwei sichtbaren
/// Optionen und ihr Umschalten, die weiche Sperre nach W16b‑E‑6, das Anlegen und
/// Entfernen von Strängen samt lückenloser Rangvergabe, die Übernahme eines
/// Katalogsatzes, die Ampelzeilen und der Hinweis auf Stufe S3.</para>
///
/// <para><b>Nicht geprüft wird die Ampel selbst</b> — die rechnet der Kern
/// (<c>StrangPlausibilitaet</c>, geprüft in <c>StrangPlausibilitaetTests</c> gegen
/// Anhang A); hier kommt sie über einen Delegaten herein.</para>
/// </summary>
public class PvStraengeFelderTests : BunitContext
{
    public PvStraengeFelderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
    }

    private static readonly (int Id, string Text)[] KATALOG =
    {
        (7, "Muster 2500TL"),
        (8, "Muster 5000TL-2M")
    };

    private static ErzeugerZeile Zeile(bool mit = false, params StrangZeile[] straenge)
    {
        var z = new ErzeugerZeile
        {
            Schluessel = 1,
            Bezeichner = "Ablytek 6MN6A275",
            Neigung = 30,
            Azimut = 0,
            AnzahlModule = 10,
            MitWechselrichter = mit
        };
        z.Straenge.AddRange(straenge);
        return z;
    }

    private IRenderedComponent<PvStraengeFelder> Aufbauen(
        ErzeugerZeile zeile,
        Action? geaendert = null,
        Func<ErzeugerZeile, IReadOnlyList<StrangZeile>, StrangBefund>? pruefen = null,
        Func<int, GeraetWahl>? uebernehmen = null,
        IReadOnlyList<string>? hersteller = null,
        Func<string, IReadOnlyList<(int Id, string Text)>>? filtern = null,
        IReadOnlyList<(int Id, string Text)>? module = null,
        Func<int, GeraetWahl>? modulUebernehmen = null)
        => Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.NeigungAnlage, zeile.Neigung)
            .Add(x => x.AzimutAnlage, zeile.Azimut)
            .Add(x => x.KwpAnlage, 2.752)
            .Add(x => x.Geraete, filtern is null ? KATALOG : filtern(""))
            .Add(x => x.GeraetUebernehmen, uebernehmen ?? (id => new GeraetWahl(1000 + id, Name(id))))
            .Add(x => x.Hersteller, hersteller ?? Array.Empty<string>())
            .Add(x => x.GeraeteFiltern, filtern)
            .Add(x => x.Module, module ?? Array.Empty<(int, string)>())
            .Add(x => x.ModulUebernehmen,
                 modulUebernehmen ?? (id => new GeraetWahl(2000 + id, Modulname(id))))
            .Add(x => x.Pruefen, pruefen)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    private static string Name(int stammId)
    {
        foreach (var e in KATALOG) if (e.Id == stammId) return e.Text;
        return "";
    }

    /// <summary>Der Wechselrichterkatalog nach Hersteller (W6‑O‑4) — die Hüllenseite.</summary>
    private static readonly (int Id, string Text, string Firma)[] KATALOG_MIT_FIRMA =
    {
        (7, "Muster 2500TL", "Muster"),
        (8, "Muster 5000TL-2M", "Muster"),
        (9, "Fremd 3000X", "Fremd")
    };

    private static readonly string[] HERSTELLER = { "Alle", "Fremd", "Muster" };

    private static IReadOnlyList<(int Id, string Text)> Filtern(string firma)
    {
        var liste = new List<(int, string)>();
        foreach (var z in KATALOG_MIT_FIRMA)
            if (firma.Length == 0 || firma == "Alle" ||
                string.Equals(z.Firma, firma, StringComparison.Ordinal))
                liste.Add((z.Id, z.Text));
        return liste;
    }

    /// <summary>Der MODULkatalog der Strangspalte (W6‑O‑6).</summary>
    private static readonly (int Id, string Text)[] MODULE =
    {
        (31, "Ablytek 6MN6A275"),
        (32, "Jinkosolar JKM 260P-60")
    };

    private static string Modulname(int stammId)
    {
        foreach (var e in MODULE) if (e.Id == stammId) return e.Text;
        return "";
    }

    /// <summary>Die Klappliste EINER Strangzeile — der Filter steht als Index 0 davor.</summary>
    private static IRenderedComponent<Auswahlfeld> Wahl(
        IRenderedComponent<PvStraengeFelder> cut, string kurzname, int zeile = 0)
    {
        var treffer = new List<IRenderedComponent<Auswahlfeld>>();
        foreach (var f in cut.FindComponents<Auswahlfeld>())
            if (string.Equals(f.Instance.Kurzname, kurzname, StringComparison.Ordinal))
                treffer.Add(f);
        return treffer[zeile];
    }

    // =================================================================================
    // 1 - Die zwei Optionen aus W6-E-3
    // =================================================================================

    /// <summary>
    /// <b>Der Abschnitt zeigt ZWEI Optionen</b>, und die Vorgabe ist „vereinfacht" —
    /// der Weg von heute. Ohne diese Vorgabe wäre die Ergebnisneutralität eine Sache
    /// des Zufalls.
    /// </summary>
    [Fact]
    public void Der_Abschnitt_zeigt_zwei_Optionen_und_steht_auf_vereinfacht()
    {
        var cut = Aufbauen(Zeile());

        var gruppe = cut.FindComponent<Optionsgruppe>();
        Assert.Equal(2, gruppe.Instance.Eintraege.Count);
        Assert.Equal(0, gruppe.Instance.Auswahl);
        Assert.Empty(cut.FindAll(".epos-strangtabelle"));
    }

    /// <summary>
    /// <b>Die weiche Sperre</b> (W16b‑E‑6): „mit Wechselrichter" ohne Strangzeile
    /// bleibt ANKLICKBAR, trägt <c>aria-disabled</c> und seinen Grund als
    /// <c>title</c> — und der Versuch MELDET sich, statt zu schalten. Ein
    /// <c>disabled</c>-Bedienelement könnte seinen Grund gar nicht sagen.
    /// </summary>
    [Fact]
    public void Mit_Wechselrichter_ohne_Strang_ist_weich_gesperrt_und_meldet_den_Grund()
    {
        var zeile = Zeile();
        var cut = Aufbauen(zeile);

        var kaesten = cut.FindAll(".epos-option-kasten");
        Assert.Equal(2, kaesten.Count);
        Assert.False(kaesten[1].HasAttribute("disabled"));
        Assert.Equal("true", kaesten[1].GetAttribute("aria-disabled"));
        Assert.Contains("kein Strang", kaesten[1].GetAttribute("title") ?? "",
                        StringComparison.OrdinalIgnoreCase);

        kaesten[1].Change("1");

        Assert.False(zeile.MitWechselrichter);
        Assert.Contains("kein Strang", cut.Instance.Meldung, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.FindComponents<Warnbanner>());
    }

    /// <summary>
    /// Mit einer Strangzeile ist die Option frei, das Umschalten meldet sich beim
    /// Wirt, und die Tabelle erscheint.
    /// </summary>
    [Fact]
    public async Task Mit_einem_Strang_laesst_sich_der_Weg_umschalten()
    {
        int gemeldet = 0;
        var zeile = Zeile(false, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, () => gemeldet++);

        var kaesten = cut.FindAll(".epos-option-kasten");
        Assert.Null(kaesten[1].GetAttribute("aria-disabled"));

        var gruppe = cut.FindComponent<Optionsgruppe>();
        await cut.InvokeAsync(() => gruppe.Instance.AuswahlChanged.InvokeAsync(1));

        Assert.True(zeile.MitWechselrichter);
        Assert.Equal(1, gemeldet);
        Assert.Single(cut.FindAll(".epos-strangtabelle"));
    }

    /// <summary>
    /// <b>Der Schalter PARKT die Zuordnung, er löscht sie nicht</b> (Konzept 7.1,
    /// Grund 1): Zurück auf „vereinfacht" lässt die Strangzeilen stehen — sonst
    /// verlöre der Planer genau die Arbeit, die er vergleichen will.
    /// </summary>
    [Fact]
    public async Task Zurueck_auf_vereinfacht_parkt_die_Straenge()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile);

        var gruppe = cut.FindComponent<Optionsgruppe>();
        await cut.InvokeAsync(() => gruppe.Instance.AuswahlChanged.InvokeAsync(0));

        Assert.False(zeile.MitWechselrichter);
        Assert.Single(zeile.Straenge);
        Assert.Empty(cut.FindAll(".epos-strangtabelle"));
    }

    // =================================================================================
    // 2 - Was mit Stufe S3 aus dem Abschnitt VERSCHWUNDEN ist
    // =================================================================================

    /// <summary>
    /// <b>Der S3-Hinweis ist fort</b> (Punkt S3.5 des Wechselrichterkonzepts). Bis
    /// Stufe S2 stand unter der Optionsgruppe die Zeile „Die Strangrechnung folgt mit
    /// Stufe S3 — bis dahin rechnet die Anlage vereinfacht"; sie war die Wache gegen
    /// eine zweite Wahrheit, solange die Oberfläche mehr versprach, als der Kern tat.
    /// Seit S3 rechnet der Kern, und der Satz wäre falsch.
    ///
    /// <para>Dieser Fall ist die GEGENPROBE zu <c>Der_S3_Hinweis_steht_in_beiden_Wegen</c>
    /// aus S2 — er prüft dasselbe Merkmal mit umgekehrtem Vorzeichen, damit die Zeile
    /// nicht unbemerkt zurückkommt.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Der_S3_Hinweis_ist_fort(bool mit)
    {
        var zeile = mit
            ? Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 })
            : Zeile();

        var cut = Aufbauen(zeile);

        Assert.Empty(cut.FindAll(".epos-straenge-s3"));
        Assert.DoesNotContain("Stufe S3", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Im Weg „vereinfacht" steht die Zeile mit dem Wirkungsgrad, der WIRKLICH gilt —
    /// 0,95 ohne gepflegten Wert, sonst der Anlagenwert.
    /// </summary>
    [Fact]
    public void Der_vereinfachte_Weg_nennt_den_geltenden_Wirkungsgrad()
    {
        var cut = Aufbauen(Zeile());
        Assert.Contains("0,950", cut.Markup, StringComparison.Ordinal);

        var zeile = Zeile();
        zeile.WrWirkungsgrad = 0.982;
        var cut2 = Aufbauen(zeile);
        Assert.Contains("0,982", cut2.Markup, StringComparison.Ordinal);
    }

    // =================================================================================
    // 3 - Straenge anlegen, aendern, entfernen
    // =================================================================================

    /// <summary>
    /// <b>„Strang anlegen"</b> legt eine Zeile an, wählt sie, schaltet den Weg auf
    /// „mit Wechselrichter" — wer einen Strang anlegt, will ihn gerechnet sehen — und
    /// meldet sich beim Wirt.
    /// </summary>
    [Fact]
    public void Strang_anlegen_erzeugt_eine_Zeile_und_schaltet_den_Weg()
    {
        int gemeldet = 0;
        var zeile = Zeile();
        var cut = Aufbauen(zeile, () => gemeldet++);

        // Ohne Strang gibt es die Leiste noch nicht - erst umschalten geht nicht
        // (weiche Sperre), also legt der Anwender ueber die Leiste des Katalogwegs an.
        zeile.MitWechselrichter = true;
        cut.Render();

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Single(zeile.Straenge);
        Assert.Equal(1, zeile.Straenge[0].Rang);
        Assert.True(zeile.MitWechselrichter);
        Assert.Equal(1, gemeldet);
        Assert.Same(zeile.Straenge[0], cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>„Entfernen"</b> nimmt die GEWÄHLTE Zeile heraus und vergibt die Ränge
    /// lückenlos neu — dieselbe Regel wie im Controller.
    /// </summary>
    [Fact]
    public void Entfernen_nimmt_die_gewaehlte_Zeile_und_vergibt_die_Raenge_neu()
    {
        var zeile = Zeile(true,
            new StrangZeile { Rang = 1, Bezeichner = "Ost", ModuleReihe = 11 },
            new StrangZeile { Rang = 2, Bezeichner = "West", ModuleReihe = 11 },
            new StrangZeile { Rang = 3, Bezeichner = "Nord", ModuleReihe = 9 });
        var cut = Aufbauen(zeile);

        // Die zweite Zeile waehlen (Spalte "Wahl" der Tabelle).
        cut.FindComponents<Zeilenwahl>()[1].Find("button").Click();
        Assert.Equal("West", cut.Instance.Gewaehlt!.Bezeichner);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();

        Assert.Equal(2, zeile.Straenge.Count);
        Assert.Equal(new[] { "Ost", "Nord" }, zeile.Straenge.Select(s => s.Bezeichner).ToArray());
        Assert.Equal(new[] { 1, 2 }, zeile.Straenge.Select(s => s.Rang).ToArray());
        Assert.Null(cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// Ohne gewählte Zeile ist „Entfernen" gesperrt — hier HART, denn es gibt nichts
    /// zu erklären: Der Anwender wählt eine Zeile, und der Knopf geht auf.
    /// </summary>
    [Fact]
    public void Entfernen_ist_ohne_Wahl_gesperrt()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }));

        Assert.True(cut.FindAll(".epos-leiste .epos-knopf")[1].HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Ein Katalogsatz wird beim Wählen ÜBERNOMMEN</b> (<c>CopyFromStamm</c>, genau
    /// wie ein Modul): Die Zeile trägt danach die PROJEKTKOPIE und den Namen, und über
    /// den Namen findet die Klappliste ihren Eintrag wieder.
    /// </summary>
    [Fact]
    public async Task Ein_Geraet_aus_dem_Katalog_wird_beim_Waehlen_uebernommen()
    {
        int gerufen = 0;
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, uebernehmen: id => { gerufen++; return new GeraetWahl(4711, Name(id)); });

        var wahl = Wahl(cut, "Wechselrichter");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(7));

        Assert.Equal(1, gerufen);
        Assert.Equal(4711, zeile.Straenge[0].WechselrichterId);
        Assert.Equal("Muster 2500TL", zeile.Straenge[0].WechselrichterName);

        // Die Klappliste findet ihren Eintrag ueber den BEZEICHNER wieder.
        Assert.Equal(7, Wahl(cut, "Wechselrichter").Instance.Auswahl);
    }

    /// <summary>„(kein Gerät)" nimmt die Zuordnung wieder heraus.</summary>
    [Fact]
    public async Task Kein_Geraet_nimmt_die_Zuordnung_heraus()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 10, WechselrichterId = 4711, WechselrichterName = "Muster 2500TL"
        });
        var cut = Aufbauen(zeile);

        var wahl = Wahl(cut, "Wechselrichter");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(0));

        Assert.Equal(0, zeile.Straenge[0].WechselrichterId);
        Assert.Equal("", zeile.Straenge[0].WechselrichterName);
    }

    /// <summary>
    /// <b>Neigung und Azimut stehen in KLAMMERN, solange sie geerbt sind</b>
    /// (Konzept 7) — als Platzhalter im leeren Feld. Wer hineinschreibt, macht das
    /// Teilfeld eigenständig; die 0 ist dabei ein GÜLTIGER Wert (Süden) und keine
    /// Leere.
    /// </summary>
    [Fact]
    public async Task Neigung_und_Azimut_zeigen_den_geerbten_Wert_in_Klammern()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile);

        var felder = cut.FindComponents<Ganzzahlfeld>();
        Ganzzahlfeld neigung = felder[felder.Count - 2].Instance;
        Ganzzahlfeld azimut = felder[felder.Count - 1].Instance;

        Assert.Equal("(30)", neigung.Platzhalter);
        Assert.Equal("(0)", azimut.Platzhalter);
        Assert.Null(zeile.Straenge[0].Neigung);

        await cut.InvokeAsync(() => felder[felder.Count - 1].Instance.WertChanged.InvokeAsync(0));
        Assert.Equal(0, zeile.Straenge[0].Azimut);
    }

    // =================================================================================
    // 4 - Die Ampel
    // =================================================================================

    /// <summary>
    /// Die Ampelzeilen kommen aus dem Kern und stehen UNTER der Tabelle — je
    /// Strangzeile eine, mit ihrer Farbe als Klasse; die Gerätechips stehen im Kopf.
    /// </summary>
    [Fact]
    public void Die_Ampelzeilen_stehen_unter_der_Tabelle_und_tragen_ihre_Farbe()
    {
        StrangBefund befund = new(
            new[] { new Ampelzeile(Ampelfarbe.Rot, "Strang 1: U_oc 638 V > 600 V") },
            new[] { new Ampelzeile(Ampelfarbe.Gelb, "DC/AC 1,65") },
            15,
            "beta_OC-Naeherung");

        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 15 }),
                           pruefen: (_, _) => befund);

        var zeilen = cut.FindAll(".epos-ampel");
        Assert.Single(zeilen);
        Assert.Contains("epos-ampel--rot", zeilen[0].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("638 V", zeilen[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("beta_OC-Naeherung", zeilen[0].GetAttribute("title"));

        var chips = cut.FindAll(".epos-strangkopf .epos-chip");
        Assert.Single(chips);
        Assert.Contains("epos-chip--warnung", chips[0].ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("DC/AC 1,65", chips[0].TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Prüfstand wird nach JEDER Änderung gefragt — beim Aufbau, beim Anlegen und
    /// bei jeder Zelle. Sonst zeigte die Ampel den Stand von vorhin.
    /// </summary>
    [Fact]
    public async Task Der_Pruefstand_wird_nach_jeder_Aenderung_gefragt()
    {
        int gerufen = 0;
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, pruefen: (_, _) => { gerufen++; return StrangBefund.Leer; });

        int nachAufbau = gerufen;
        Assert.True(nachAufbau > 0);

        var felder = cut.FindComponents<Ganzzahlfeld>();
        await cut.InvokeAsync(() => felder[2].Instance.WertChanged.InvokeAsync(12));
        Assert.True(gerufen > nachAufbau);
        Assert.Equal(12, zeile.Straenge[0].ModuleReihe);
    }

    /// <summary>
    /// Ohne Delegat bleibt die Ampel LEER statt zu werfen — die Regel „jede Seite
    /// zeichnet auch ohne Gaben".
    /// </summary>
    [Fact]
    public void Ohne_Pruefstand_bleibt_die_Ampel_leer()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }));

        Assert.Empty(cut.FindAll(".epos-ampel"));
        Assert.Empty(cut.Instance.Befund.Straenge);
    }

    // =================================================================================
    // 4b - W6-O-4: der Herstellerfilter ueber der Tabelle
    // =================================================================================

    /// <summary>
    /// <b>Der Filter verengt die Klappliste ALLER Zeilen</b> (W6‑O‑4). Er steht ÜBER
    /// der Tabelle, nicht in ihr — in der Zeile hätte er keinen Platz —, und wirkt auf
    /// jede Gerätespalte.
    /// </summary>
    [Fact]
    public async Task Der_Herstellerfilter_verengt_die_Geraeteklappliste()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern);

        // "Alle" (Index 0) zeigt alle drei Geraete, dazu "(kein Geraet)".
        Assert.Equal(4, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(1));   // "Fremd"

        var eintraege = Wahl(cut, "Wechselrichter").Instance.Eintraege;
        Assert.Equal(2, eintraege.Count);
        Assert.Contains(eintraege, e => e.Text == "Fremd 3000X");
        Assert.DoesNotContain(eintraege, e => e.Text == "Muster 2500TL");
    }

    /// <summary>
    /// <b>„Alle" zeigt wieder alles</b> — der Filter ist eine Einengung, keine
    /// Entscheidung.
    /// </summary>
    [Fact]
    public async Task Alle_zeigt_wieder_den_ganzen_Katalog()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern);

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(2));   // "Muster"
        Assert.Equal(3, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);

        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(0));   // "Alle"
        Assert.Equal(4, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);
    }

    /// <summary>
    /// <b>Ein bereits gewähltes Gerät bleibt in SEINER Zeile sichtbar</b>, auch wenn
    /// der Filter es ausschliesst: Sonst stünde in der Zeile nichts, und der Anwender
    /// hielte die Zuordnung für verloren.
    /// </summary>
    [Fact]
    public async Task Ein_gewaehltes_Geraet_bleibt_trotz_Filter_sichtbar()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 10, WechselrichterId = 4711, WechselrichterName = "Fremd 3000X"
        });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern);

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(2));   // "Muster"

        var wahl = Wahl(cut, "Wechselrichter");
        Assert.Contains(wahl.Instance.Eintraege, e => e.Text == "Fremd 3000X");
        Assert.Equal(9, wahl.Instance.Auswahl);
    }

    /// <summary>
    /// Ohne Herstellerliste gibt es KEINE Filterzeile — dieselbe Regel wie überall im
    /// Haus: kein Delegat, kein Bedienelement.
    /// </summary>
    [Fact]
    public void Ohne_Herstellerliste_gibt_es_keine_Filterzeile()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }));

        Assert.Empty(cut.FindAll(".epos-strangfilter"));
    }

    // =================================================================================
    // 4c - W6-O-6: das Modul je Strang
    // =================================================================================

    /// <summary>
    /// <b>Die Modulspalte trägt „(Modul der Anlage)" als Vorgabe</b> — leer heisst
    /// nicht „kein Modul", sondern „das der Anlage" (dieselbe Rückfallregel wie bei
    /// Neigung und Azimut).
    /// </summary>
    [Fact]
    public void Die_Modulspalte_steht_auf_dem_Modul_der_Anlage()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, module: MODULE);

        var wahl = Wahl(cut, "Modul");
        Assert.Equal(0, wahl.Instance.Auswahl);
        Assert.Equal(3, wahl.Instance.Eintraege.Count);          // Rueckfall + zwei Module
        Assert.Equal(0, zeile.Straenge[0].ModulId);
        Assert.Contains("Modul der Anlage", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Ein anderer Modultyp wird beim Wählen ÜBERNOMMEN</b> (<c>CopyFromStamm</c>,
    /// genau wie ein Gerät): Die Zeile trägt danach die PROJEKTKOPIE und den Namen, und
    /// über den Namen findet die Klappliste ihren Eintrag wieder.
    /// </summary>
    [Fact]
    public async Task Ein_abweichendes_Modul_wird_beim_Waehlen_uebernommen()
    {
        int gerufen = 0;
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, module: MODULE,
                           modulUebernehmen: id => { gerufen++; return new GeraetWahl(5150, Modulname(id)); });

        var wahl = Wahl(cut, "Modul");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(32));

        Assert.Equal(1, gerufen);
        Assert.Equal(5150, zeile.Straenge[0].ModulId);
        Assert.Equal("Jinkosolar JKM 260P-60", zeile.Straenge[0].ModulName);
        Assert.Equal(32, Wahl(cut, "Modul").Instance.Auswahl);
    }

    /// <summary>
    /// <b>„(Modul der Anlage)" nimmt die Abweichung wieder heraus.</b> Ohne diesen
    /// Rückweg wäre ein einmal gesetzter Modultyp nicht mehr aufzuheben.
    /// </summary>
    [Fact]
    public async Task Das_Modul_der_Anlage_nimmt_die_Abweichung_heraus()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 10, ModulId = 5150, ModulName = "Jinkosolar JKM 260P-60"
        });
        var cut = Aufbauen(zeile, module: MODULE);

        var wahl = Wahl(cut, "Modul");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(0));

        Assert.Equal(0, zeile.Straenge[0].ModulId);
        Assert.Equal("", zeile.Straenge[0].ModulName);
    }

    /// <summary>
    /// Der Prüfstand bekommt die GEWÄHLTE Projektzeile mit (<b>W6‑O‑5</b>) — sie sagt,
    /// gegen welches Modul die Ampel prüft.
    /// </summary>
    [Fact]
    public void Der_Pruefstand_bekommt_die_gewaehlte_Projektzeile()
    {
        ErzeugerZeile? gesehen = null;
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });

        Aufbauen(zeile, pruefen: (z, _) => { gesehen = z; return StrangBefund.Leer; });

        Assert.Same(zeile, gesehen);
    }

    // =================================================================================
    // 5 - Die Ueberlagerung mit den fuenf Anlagenwerten
    // =================================================================================

    /// <summary>
    /// <b>Der Anlagenrückfall bleibt erreichbar — in BEIDEN Wegen und OHNE Sperre</b>
    /// (Konzept 7, Q5). Die Überlagerung schreibt erst mit OK; 0 heisst „nicht
    /// bekannt" und wird NULL.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Die_Anlagenueberlagerung_ist_in_beiden_Wegen_offen(bool mit)
    {
        var zeile = mit
            ? Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 })
            : Zeile();
        var cut = Aufbauen(zeile);

        var knopf = cut.Find(".epos-straenge-anlagenknopf");
        Assert.False(knopf.HasAttribute("disabled"));

        knopf.Click();
        Assert.True(cut.Instance.WechselrichterOffen);
        Assert.Contains("kein Clipping", cut.Instance.DcAcText, StringComparison.OrdinalIgnoreCase);

        var felder = cut.FindComponents<Zahlenfeld>();
        await cut.InvokeAsync(() => felder[0].Instance.WertChanged.InvokeAsync(2.5));
        await cut.InvokeAsync(() => felder[1].Instance.WertChanged.InvokeAsync(0.94));
        Assert.Contains("1,10", cut.Instance.DcAcText, StringComparison.Ordinal);  // 2,752 auf 2,50
        Assert.Null(zeile.WrNennleistungKw);                                        // noch nicht geschrieben

        await cut.InvokeAsync(() => cut.FindComponent<SpeichernLeiste>()
                                       .Instance.Ergebnis.InvokeAsync(true));

        Assert.Equal(2.5, zeile.WrNennleistungKw);
        Assert.Equal(0.94, zeile.WrEta10);
        Assert.Null(zeile.WrEta50);
        Assert.False(cut.Instance.WechselrichterOffen);
    }
}
