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
        Func<int, GeraetWahl>? modulUebernehmen = null,
        string modulhersteller = "")
        => Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.NeigungAnlage, zeile.Neigung)
            .Add(x => x.AzimutAnlage, zeile.Azimut)
            .Add(x => x.KwpAnlage, 2.752)
            .Add(x => x.Geraete, filtern is null ? KATALOG : filtern(""))
            .Add(x => x.GeraetUebernehmen, uebernehmen ?? (id => new GeraetWahl(1000 + id, Name(id))))
            .Add(x => x.Hersteller, hersteller ?? Array.Empty<string>())
            .Add(x => x.GeraeteFiltern, filtern)
            .Add(x => x.Modulhersteller, modulhersteller)
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
    /// <b>Befund W6‑B‑3</b> (Windows-Abnahme 07.09.2026, wörtlich: „die gesamte
    /// Zuordnung Wechselrichter zum PV-Modul und Strang funktioniert nicht — Auswahl
    /// Wechselrichter nicht vorhanden"): Die Option „mit Wechselrichter" ist OHNE
    /// Strangzeile frei wählbar, und der Klick SCHALTET.
    ///
    /// <para><b>Dies ist die GEGENPROBE zum gefallenen Fall</b>
    /// <c>Mit_Wechselrichter_ohne_Strang_ist_weich_gesperrt_und_meldet_den_Grund</c>:
    /// Die weiche Sperre nach W16b‑E‑6 stand am falschen Ort. Sie verlangte einen
    /// Strang, bevor sie den Weg freigab — angelegt wird ein Strang aber ausschliesslich
    /// INNERHALB dieses Weges. Damit war er von einer frischen Anlage aus unerreichbar,
    /// und der Anwender sah weiter den vereinfachten Weg mit seinem einen Knopf.</para>
    /// </summary>
    [Fact]
    public void W6B3_Mit_Wechselrichter_ohne_Strang_ist_frei_waehlbar()
    {
        var zeile = Zeile();
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern);

        var kaesten = cut.FindAll(".epos-option-kasten");
        Assert.Equal(2, kaesten.Count);
        Assert.False(kaesten[1].HasAttribute("disabled"));
        Assert.Null(kaesten[1].GetAttribute("aria-disabled"));
        Assert.Null(kaesten[1].GetAttribute("title"));

        kaesten[1].Change("1");

        Assert.True(zeile.MitWechselrichter);
        Assert.Empty(cut.FindComponents<Warnbanner>());
    }

    /// <summary>
    /// <b>W6‑B‑3, die Lage des Anwenders:</b> „mit Wechselrichter", noch kein Strang.
    /// Der Abschnitt zeigt dann ALLES, was zur Zuordnung gehört — Herstellerfilter,
    /// Klappliste der Katalog-Wechselrichter und „Strang anlegen" —, obwohl die Tabelle
    /// noch nicht steht. Vorher stand hier nur ein Satz.
    /// </summary>
    [Fact]
    public void W6B3_Ohne_Strang_stehen_Filter_Klappliste_und_Anlegen()
    {
        var cut = Aufbauen(Zeile(true), hersteller: HERSTELLER, filtern: Filtern);

        Assert.Single(cut.FindAll(".epos-strangfilter"));
        Assert.Single(cut.FindAll(".epos-geraetewahl"));

        var wahl = Wahl(cut, "Wechselrichter aus dem Katalog:");
        Assert.Equal(4, wahl.Instance.Eintraege.Count);           // "(kein Gerät)" + drei
        Assert.Equal(0, wahl.Instance.Auswahl);

        Assert.Contains(cut.FindAll(".epos-leiste .epos-knopf"),
                        k => k.TextContent.Contains("Strang anlegen", StringComparison.Ordinal));
        Assert.Empty(cut.FindAll(".epos-strangtabelle"));
        Assert.Empty(cut.FindAll(".epos-katalogleer"));
    }

    /// <summary>
    /// <b>W6‑B‑3, leerer Katalog:</b> Statt der Klappliste steht der WEG zum Import.
    /// Der Auslieferungskatalog ist leer (W6‑O‑3) — ohne diesen Satz stünde der
    /// Abschnitt stumm da, und das war die zweite Hälfte des Befunds.
    /// </summary>
    [Fact]
    public void W6B3_Ein_leerer_Katalog_nennt_den_Weg_zum_Import()
    {
        var cut = Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, Zeile(true))
            .Add(x => x.Geraete, Array.Empty<(int, string)>())
            .Add(x => x.Hersteller, Array.Empty<string>()));

        Assert.Empty(cut.FindAll(".epos-geraetewahl"));

        string satz = cut.Find(".epos-katalogleer").TextContent;
        Assert.Contains("Wechselrichterkatalog ist leer", satz, StringComparison.Ordinal);
        Assert.Contains("Administration", satz, StringComparison.Ordinal);
        Assert.Contains("Daten & Import", satz, StringComparison.Ordinal);
        Assert.Contains("Photovoltaik", satz, StringComparison.Ordinal);
        Assert.Contains("Wechselrichter (CEC, OND)", satz, StringComparison.Ordinal);

        // Der Weg bleibt trotzdem begehbar: ein Strang ohne Gerät ist erlaubt, die
        // Ampel sagt dann, was fehlt.
        Assert.Contains(cut.FindAll(".epos-leiste .epos-knopf"),
                        k => k.TextContent.Contains("Strang anlegen", StringComparison.Ordinal));

        // Und der Satz „Gerät oben wählen" bleibt fort — er zeigte auf eine
        // Klappliste, die es hier nicht gibt.
        Assert.DoesNotContain("Gerät oben wählen", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>W6‑B‑3, der Rückfallknopf:</b> Er heisst nach seinem INHALT — hinter ihm
    /// stehen die vier Pauschalen der Anlage, nicht die Katalogwahl — und er steht in
    /// einer eigenen Zeile am Fuss, nicht in der Leiste neben „Strang anlegen".
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void W6B3_Der_Rueckfallknopf_heisst_nach_seinem_Inhalt(bool mit)
    {
        var cut = Aufbauen(Zeile(mit), hersteller: HERSTELLER, filtern: Filtern);

        var knopf = cut.Find(".epos-straenge-anlagenknopf");
        Assert.Contains("Anlagenwerte", knopf.TextContent, StringComparison.Ordinal);
        Assert.Contains("Rückfall", knopf.TextContent, StringComparison.Ordinal);

        // Er steht in der Rückfallzeile und in KEINER Leiste.
        Assert.Single(cut.FindAll(".epos-strangrueckfall .epos-straenge-anlagenknopf"));
        Assert.Empty(cut.FindAll(".epos-leiste .epos-straenge-anlagenknopf"));
    }

    /// <summary>
    /// <b>W6‑B‑3:</b> Im Weg „vereinfacht" ist der Rückfall der EINZIGE Knopf — der
    /// Stand von heute, und die Zeile daneben nennt weiter den geltenden Wirkungsgrad.
    /// </summary>
    [Fact]
    public void W6B3_Vereinfacht_zeigt_nur_den_Rueckfall()
    {
        var cut = Aufbauen(Zeile(), hersteller: HERSTELLER, filtern: Filtern);

        Assert.Single(cut.FindAll("button.epos-knopf"));
        Assert.Empty(cut.FindAll(".epos-strangfilter"));
        Assert.Empty(cut.FindAll(".epos-geraetewahl"));
        Assert.Empty(cut.FindAll(".epos-katalogleer"));
        Assert.Contains("0,950", cut.Find(".epos-strangrueckfall").TextContent,
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>W6‑B‑3:</b> Das Gerät der Katalogwahl geht in den NEUEN Strang — genau das
    /// ist der Sinn der Zeile über der Tabelle. Übernommen (<c>CopyFromStamm</c>) wird
    /// es dabei, nicht schon beim Blättern in der Klappliste.
    /// </summary>
    [Fact]
    public async Task W6B3_Strang_anlegen_nimmt_das_Geraet_der_Katalogwahl_mit()
    {
        int gerufen = 0;
        var zeile = Zeile(true);
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           uebernehmen: id => { gerufen++; return new GeraetWahl(4711, GeraetName(id)); });

        var wahl = Wahl(cut, "Wechselrichter aus dem Katalog:");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(9));   // "Fremd 3000X"

        Assert.Equal(0, gerufen);                       // die Wahl allein kopiert nichts
        Assert.Empty(zeile.Straenge);
        Assert.Equal(9, cut.Instance.Katalogwahl);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Single(zeile.Straenge);
        Assert.Equal(1, gerufen);
        Assert.Equal(4711, zeile.Straenge[0].WechselrichterId);
        Assert.Equal("Fremd 3000X", zeile.Straenge[0].WechselrichterName);
        Assert.Single(cut.FindAll(".epos-strangtabelle"));
    }

    /// <summary>
    /// <b>W6‑B‑3, die Gegenprobe:</b> Ohne Gerätewahl legt „Strang anlegen" eine LEERE
    /// Zeile an wie bisher — der Strang ohne Gerät bleibt ein zulässiger Zwischenstand
    /// (die Ampel meldet ihn, sie verhindert ihn nicht).
    /// </summary>
    [Fact]
    public void W6B3_Ohne_Geraetewahl_bleibt_der_neue_Strang_leer()
    {
        int gerufen = 0;
        var zeile = Zeile(true);
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           uebernehmen: id => { gerufen++; return new GeraetWahl(4711, GeraetName(id)); });

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Single(zeile.Straenge);
        Assert.Equal(0, gerufen);
        Assert.Equal(0, zeile.Straenge[0].WechselrichterId);
    }

    /// <summary>
    /// <b>W6‑B‑3:</b> Führt der Katalog das vorgemerkte Gerät nicht mehr — der Anwender
    /// stellt den Herstellerfilter um —, fällt die Katalogwahl auf „(kein Gerät)"
    /// zurück. Sonst nähme „Strang anlegen" ein Gerät mit, das in der Klappliste gar
    /// nicht mehr steht.
    /// </summary>
    [Fact]
    public async Task W6B3_Ein_Filterwechsel_setzt_eine_unsichtbare_Katalogwahl_zurueck()
    {
        var cut = Aufbauen(Zeile(true), hersteller: HERSTELLER, filtern: Filtern);

        var wahl = Wahl(cut, "Wechselrichter aus dem Katalog:");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(9));   // "Fremd 3000X"
        Assert.Equal(9, cut.Instance.Katalogwahl);

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(2));  // "Muster"

        Assert.Equal(0, cut.Instance.Katalogwahl);
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
    // 4b2 - W6-E-6: die Vorauswahl des Filters auf den Modulhersteller
    // =================================================================================
    //
    // Anwenderentscheid vom 07.09.2026, woertlich: "Der Wechselrichter soll beliebig
    // waehlbar sein und nur die Vorauswahl auf den Modulhersteller verweisen (falls
    // Wechselrichter von dem Modulhersteller verfuegbar)." Die Faelle pruefen BEIDE
    // Haelften - die Vorauswahl UND die Freiheit, die sie nicht antasten darf.

    /// <summary>Der Name eines Geräts des Prüfkatalogs (auch „Fremd 3000X", Id 9).</summary>
    private static string GeraetName(int stammId)
    {
        foreach (var e in KATALOG_MIT_FIRMA) if (e.Id == stammId) return e.Text;
        return "";
    }

    /// <summary>Die Herstellerliste zum „beginnt mit"-Fall — der Katalogname ist länger.</summary>
    private static readonly string[] HERSTELLER_LANG = { "Alle", "Fremd", "Muster Solartechnik" };

    /// <summary>
    /// <b>Der Filter steht beim Aufmachen auf dem Hersteller des Anlagenmoduls</b>
    /// (W6‑E‑6) — und die Klappliste zeigt dessen Geräte. Das ist die eine Hälfte des
    /// Entscheids: Wer Module von „Muster" verbaut, sieht die Muster-Geräte, ohne den
    /// Filter anzufassen.
    /// </summary>
    [Fact]
    public void Die_Vorauswahl_steht_auf_dem_Hersteller_des_Anlagenmoduls()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Muster");

        Assert.Equal(2, cut.Instance.Herstellerfilter);                       // "Muster"
        Assert.Equal(2, Wahl(cut, "Filtern nach Hersteller:").Instance.Auswahl);

        var eintraege = Wahl(cut, "Wechselrichter").Instance.Eintraege;
        Assert.Equal(3, eintraege.Count);                                     // + "(kein Geraet)"
        Assert.Contains(eintraege, e => e.Text == "Muster 2500TL");
        Assert.DoesNotContain(eintraege, e => e.Text == "Fremd 3000X");
    }

    /// <summary>
    /// <b>Führt der Wechselrichterkatalog kein Gerät des Modulherstellers, steht der
    /// Filter auf „Alle"</b> — die Bedingung aus dem Wortlaut („falls Wechselrichter
    /// von dem Modulhersteller verfügbar"). Ein Filter auf einen Hersteller ohne Gerät
    /// zeigte eine leere Klappliste, und die läse sich wie ein leerer Katalog.
    /// </summary>
    [Fact]
    public void Ohne_Geraet_dieses_Herstellers_steht_der_Filter_auf_Alle()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Jinkosolar");

        Assert.Equal(0, cut.Instance.Herstellerfilter);
        Assert.Equal(4, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);

        string satz = cut.Find(".epos-strangfilter + .epos-herleitung").TextContent;
        Assert.Contains("Kein Wechselrichter", satz, StringComparison.Ordinal);
        Assert.Contains("Jinkosolar", satz, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der zweite Versuch „beginnt mit" trifft in BEIDE Richtungen</b> — Modul „SMA"
    /// gegen Gerät „SMA America" und umgekehrt: Modul- und Gerätekatalog stammen aus
    /// verschiedenen Quellen und schreiben denselben Hersteller verschieden.
    /// </summary>
    [Fact]
    public void Der_zweite_Versuch_trifft_ueber_beginnt_mit()
    {
        // Katalogname laenger als der Modulhersteller.
        var lang = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                            hersteller: HERSTELLER_LANG, filtern: Filtern,
                            modulhersteller: "Muster");
        Assert.Equal(2, lang.Instance.Herstellerfilter);

        // Modulhersteller laenger als der Katalogname.
        var kurz = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                            hersteller: HERSTELLER, filtern: Filtern,
                            modulhersteller: "Muster Solartechnik AG");
        Assert.Equal(2, kurz.Instance.Herstellerfilter);
    }

    /// <summary>
    /// <b>Die Gegenprobe zum zweiten Versuch:</b> Zwei Buchstaben sind kein Name. „Mu"
    /// stünde auf „Muster" so gut wie auf „Munich Energy" — der Versuch verlangt
    /// deshalb drei Zeichen, und darunter bleibt es bei „Alle".
    /// </summary>
    [Fact]
    public void Ein_zu_kurzes_Praefix_trifft_nicht()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern, modulhersteller: "Mu");

        Assert.Equal(0, cut.Instance.Herstellerfilter);
    }

    /// <summary>
    /// <b>Gross-/Kleinschreibung und Randleerzeichen sind egal</b> — ein
    /// Katalogbestand aus zwei Quellen schreibt „SMA" und „sma " ohne jede Absicht.
    /// </summary>
    [Fact]
    public void Gross_Kleinschreibung_und_Randleerzeichen_sind_egal()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "  mUsTeR  ");

        Assert.Equal(2, cut.Instance.Herstellerfilter);
    }

    /// <summary>
    /// <b>Die andere Hälfte des Entscheids: Der Wechselrichter bleibt BELIEBIG
    /// wählbar.</b> Nach der Vorauswahl auf „Muster" führt der Filter „Fremd" zum
    /// fremden Gerät, und die Wahl trägt sich in die Zeile ein; „Alle" zeigt wieder
    /// den ganzen Katalog. Nichts sperrt einen fremden Hersteller — hier steht es als
    /// Fall.
    /// </summary>
    [Fact]
    public async Task Ein_fremder_Hersteller_bleibt_nach_der_Vorauswahl_waehlbar()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Muster",
                           uebernehmen: id => new GeraetWahl(4711, GeraetName(id)));

        Assert.Equal(2, cut.Instance.Herstellerfilter);

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(1));   // "Fremd"

        var wahl = Wahl(cut, "Wechselrichter");
        Assert.Contains(wahl.Instance.Eintraege, e => e.Text == "Fremd 3000X");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(9));

        Assert.Equal(4711, zeile.Straenge[0].WechselrichterId);
        Assert.Equal("Fremd 3000X", zeile.Straenge[0].WechselrichterName);

        // Und "Alle" zeigt weiterhin den ganzen Katalog.
        await cut.InvokeAsync(() => Wahl(cut, "Filtern nach Hersteller:")
                                        .Instance.AuswahlChanged.InvokeAsync(0));
        Assert.Equal(4, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);
    }

    /// <summary>
    /// <b>Eine bereits gewählte Zeile behält ihr Gerät</b>, auch wenn die Vorauswahl
    /// auf einen anderen Hersteller zeigt (Regel W6‑O‑4, jetzt gegen die Vorauswahl
    /// geprüft): Die Vorauswahl ist eine ANZEIGEhilfe und ändert keine Zuordnung.
    /// </summary>
    [Fact]
    public void Eine_gewaehlte_Zeile_behaelt_ihr_Geraet_trotz_Vorauswahl()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 10, WechselrichterId = 4711, WechselrichterName = "Fremd 3000X"
        });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Muster");

        Assert.Equal(2, cut.Instance.Herstellerfilter);

        var wahl = Wahl(cut, "Wechselrichter");
        Assert.Contains(wahl.Instance.Eintraege, e => e.Text == "Fremd 3000X");
        Assert.Equal(9, wahl.Instance.Auswahl);
        Assert.Equal("Fremd 3000X", zeile.Straenge[0].WechselrichterName);
    }

    /// <summary>
    /// <b>Eine Anwenderwahl überlebt das Neuzeichnen.</b> Die Hülle setzt die Parameter
    /// nach JEDER Zellenänderung neu; stellte die Komponente dabei wieder vor, nähme sie
    /// dem Anwender seinen Filter im selben Augenblick wieder ab.
    /// </summary>
    [Fact]
    public async Task Eine_Anwenderwahl_ueberlebt_das_Neuzeichnen()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Muster");

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(0));   // "Alle"
        Assert.Equal(0, cut.Instance.Herstellerfilter);

        cut.Render(p => p.Add(x => x.Modulhersteller, "Muster"));

        Assert.Equal(0, cut.Instance.Herstellerfilter);
        Assert.Equal(4, Wahl(cut, "Wechselrichter").Instance.Eintraege.Count);
    }

    /// <summary>
    /// <b>Ein anderes Anlagenmodul stellt neu vor.</b> Wer in der linken Liste eine
    /// zweite PV-Anlage markiert (oder das Modul wechselt, während der Abschnitt
    /// zugeklappt war), bekommt die Vorauswahl seines Herstellers — die Merkregel gilt
    /// dem Modul, nicht der Sitzung.
    /// </summary>
    [Fact]
    public async Task Ein_anderes_Anlagenmodul_stellt_neu_vor()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, hersteller: HERSTELLER, filtern: Filtern,
                           modulhersteller: "Muster");

        var filter = Wahl(cut, "Filtern nach Hersteller:");
        await cut.InvokeAsync(() => filter.Instance.AuswahlChanged.InvokeAsync(0));   // "Alle"

        cut.Render(p => p.Add(x => x.Modulhersteller, "Fremd"));

        Assert.Equal(1, cut.Instance.Herstellerfilter);                       // "Fremd"
        var eintraege = Wahl(cut, "Wechselrichter").Instance.Eintraege;
        Assert.Equal(2, eintraege.Count);
        Assert.Contains(eintraege, e => e.Text == "Fremd 3000X");
    }

    /// <summary>
    /// <b>Die Herleitungszeile sagt, woher die Vorauswahl kommt</b> — und im selben Satz,
    /// dass sie nichts sperrt. Ohne bekannten Modulhersteller bleibt der zweite Halbsatz.
    /// </summary>
    [Fact]
    public void Die_Herleitungszeile_nennt_die_Vorauswahl_und_die_freie_Wahl()
    {
        var mit = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           hersteller: HERSTELLER, filtern: Filtern, modulhersteller: "Muster");

        string satz = mit.Find(".epos-strangfilter + .epos-herleitung").TextContent;
        Assert.Contains("Vorauswahl", satz, StringComparison.Ordinal);
        Assert.Contains("Muster", satz, StringComparison.Ordinal);
        Assert.Contains("Jeder Hersteller ist wählbar.", satz, StringComparison.Ordinal);

        var ohne = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                            hersteller: HERSTELLER, filtern: Filtern);

        Assert.Equal(0, ohne.Instance.Herstellerfilter);
        Assert.Equal("Jeder Hersteller ist wählbar.",
                     ohne.Find(".epos-strangfilter + .epos-herleitung").TextContent);
    }

    /// <summary>
    /// Ohne Herstellerliste gibt es auch KEINE Herleitungszeile — sie erklärt ein
    /// Bedienelement, das dann nicht da ist.
    /// </summary>
    [Fact]
    public void Ohne_Herstellerliste_gibt_es_auch_keine_Herleitungszeile()
    {
        var cut = Aufbauen(Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 }),
                           modulhersteller: "Muster");

        Assert.Empty(cut.FindAll(".epos-strangfilter + .epos-herleitung"));
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

    // =================================================================================
    // 6 - W6-B-4 (Windows-Abnahme 07.09.2026): die Tabelle, die Vorbelegung und die
    //     Klappliste, die zeigt, was die Zeile wirklich traegt
    // =================================================================================

    /// <summary>
    /// <b>Die ZAHLENSPALTEN stehen vor den zwei Klapplisten</b> (W6‑B‑4). Der Anwender
    /// fragte „Anzahl Module fehlt?" — sie stand da, nur 757 px rechts ausserhalb des
    /// sichtbaren Bereichs, hinter zwei breiten <c>&lt;select&gt;</c>. bunit misst keine
    /// Breite (Lehre W6‑B‑1); geprüft wird die REIHENFOLGE, die den Ausschlag gibt:
    /// alles Schmale zuerst, die zwei elastischen Listen zuletzt.
    /// </summary>
    [Fact]
    public void W6B4_Die_Zahlenspalten_stehen_vor_den_Klapplisten()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, module: MODULE);

        string[] koepfe = cut.FindAll("table.epos-strangtabelle thead th")
                             .Select(th => th.TextContent.Trim()).ToArray();

        Assert.Equal(
            new[]
            {
                "Wahl", "Rang", "Bezeichner", "Gerät", "MPPT", "Module in Reihe",
                "Stränge parallel", "Neigung [°]", "Azimut [°]", "Modul", "Wechselrichter"
            },
            koepfe);
    }

    /// <summary>
    /// <b>Die Spaltenklassen tragen die Stilregel</b> (W6‑B‑4): Die fünf Zahlenspalten
    /// heissen <c>epos-strangtabelle-zahl</c> (schmal, Kopf darf umbrechen), die zwei
    /// Klapplisten <c>epos-strangtabelle-liste</c> (gedeckelt, Auslassung). Ohne die
    /// Klassen im Markup greift keine Regel des Stilblatts — der Fall dazu steht in
    /// <c>StilblattTests</c>.
    /// </summary>
    [Fact]
    public void W6B4_Die_Spaltenklassen_stehen_im_Markup()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, module: MODULE);

        Assert.Equal(6, cut.FindAll("table.epos-strangtabelle thead th.epos-strangtabelle-zahl").Count);
        Assert.Equal(6, cut.FindAll("table.epos-strangtabelle tbody td.epos-strangtabelle-zahl").Count);
        Assert.Equal(2, cut.FindAll("table.epos-strangtabelle thead th.epos-strangtabelle-liste").Count);
        Assert.Equal(2, cut.FindAll("table.epos-strangtabelle tbody td.epos-strangtabelle-liste").Count);
    }

    /// <summary>
    /// <b>Der ERSTE Strang trägt die Modulzahl der Anlage</b> (W6‑B‑4). Bis hierher
    /// begann jeder neue Strang mit „0 Module in Reihe, 1 parallel" — die Anlage KENNT
    /// ihre Modulzahl, und der erste Strang ist im Regelfall die ganze Anlage.
    /// Gerechnet wird die Vorbelegung im Kern (<c>Strangvorbelegung</c>); hier steht,
    /// dass die Komponente sie ruft und das Ergebnis in die Zeile schreibt.
    /// </summary>
    [Fact]
    public void W6B4_Der_erste_Strang_traegt_die_Modulzahl_der_Anlage()
    {
        var zeile = Zeile(true);                       // AnzahlModule = 10, kein Strang
        var cut = Aufbauen(zeile);

        cut.Find(".epos-knopf--primaer").Click();

        StrangZeile s = Assert.Single(zeile.Straenge);
        Assert.Equal(10, s.ModuleReihe);
        Assert.Equal(1, s.StraengeParallel);
        Assert.Equal(1, s.Mppt);
        Assert.Equal(1, s.Geraetenummer);
        Assert.Equal(10, s.Modulzahl);
    }

    /// <summary>
    /// <b>Jeder WEITERE Strang bekommt die noch nicht zugeordneten Module</b> — und die
    /// Summe stimmt danach mit der „Anzahl Module" der Anlage überein, also bleibt P8
    /// grün (der Nachweis dazu steht im Kern).
    /// </summary>
    [Fact]
    public void W6B4_Der_zweite_Strang_bekommt_die_noch_freien_Module()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 6, StraengeParallel = 1, Mppt = 1, Geraetenummer = 1
        });
        var cut = Aufbauen(zeile);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(2, zeile.Straenge.Count);
        Assert.Equal(4, zeile.Straenge[1].ModuleReihe);          // 10 − 6
        Assert.Equal(1, zeile.Straenge[1].StraengeParallel);
        Assert.Equal(10, zeile.Straenge[0].Modulzahl + zeile.Straenge[1].Modulzahl);
    }

    /// <summary>
    /// <b>Die Modulklappliste zeigt „(Modul der Anlage)" als GEWÄHLT</b> (W6‑B‑4).
    /// Der Anwender sah dort den ersten Katalogeintrag, obwohl der Strang mit dem Modul
    /// der Anlage rechnet: Das <c>&lt;option&gt;</c> trug kein <c>selected</c>, und die
    /// Auswahl hing allein an der Wertzuweisung, die der Blazor-Zeichner NACH dem
    /// Einhängen nachreicht — sie überlebt kein Auswechseln der Einträge.
    /// </summary>
    [Fact]
    public void W6B4_Die_Modulklappliste_traegt_selected_am_Modul_der_Anlage()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        var cut = Aufbauen(zeile, module: MODULE);

        var gewaehlt = Klappliste(cut, "Modul").QuerySelectorAll("option[selected]");

        Assert.Single(gewaehlt);
        Assert.Equal("(Modul der Anlage)", gewaehlt[0].TextContent);
    }

    /// <summary>
    /// Die Gegenprobe: Trägt der Strang einen EIGENEN Modultyp, steht <c>selected</c>
    /// an ihm — und nicht mehr am Rückfall.
    /// </summary>
    [Fact]
    public void W6B4_Ein_eigener_Modultyp_traegt_das_selected()
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 10, ModulId = 5150, ModulName = "Jinkosolar JKM 260P-60"
        });
        var cut = Aufbauen(zeile, module: MODULE);

        var gewaehlt = Klappliste(cut, "Modul").QuerySelectorAll("option[selected]");

        Assert.Single(gewaehlt);
        Assert.Equal("Jinkosolar JKM 260P-60", gewaehlt[0].TextContent);
    }

    /// <summary>
    /// <b>Die Ampel prüft gegen das Modul der ANLAGE</b>, solange der Strang keinen
    /// eigenen Modultyp trägt (W6‑O‑5/O‑6) — auch dann, wenn der Katalog ein anderes
    /// Modul an erster Stelle führt. Der Prüfstand bekommt <c>ModulId == 0</c> zu
    /// sehen, und genau daran erkennt der Kern den Rückfall.
    /// </summary>
    [Fact]
    public void W6B4_Ohne_eigenen_Modultyp_prueft_die_Ampel_gegen_das_Anlagenmodul()
    {
        var zeile = Zeile(true, new StrangZeile { Rang = 1, ModuleReihe = 10 });
        zeile.Bezeichner = "Jinkosolar JKM 260P-60";
        int gesehen = -1;

        var cut = Aufbauen(zeile, module: MODULE, pruefen: (z, s) =>
        {
            gesehen = s[0].ModulId;
            return new StrangBefund(
                new[] { new Ampelzeile(Ampelfarbe.Gelb, "Strang 1: Modul " + z.Bezeichner) },
                Array.Empty<Ampelzeile>(), 10);
        });

        Assert.Equal(0, gesehen);                                    // der Rueckfall, nicht "Ablytek"
        Assert.Contains("Modul Jinkosolar JKM 260P-60", cut.Markup, StringComparison.Ordinal);
    }


    /// <summary>
    /// <b>Der MPPT-Eingang wandert weiter, wenn das Gerät mehrere Tracker führt</b>
    /// (W6‑B‑4): Der zweite Strang am SELBEN Gerät bekommt Tracker 2. Ohne bekannte
    /// Trackerzahl bleibt es bei 1 — dem konservativen Fall, auf dem auch die Ampel
    /// rechnet (W6‑O‑2).
    /// </summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(1, 1)]
    [InlineData(null, 1)]
    public async Task W6B4_Der_zweite_Strang_nimmt_den_naechsten_freien_Tracker(
        int? tracker, int erwartet)
    {
        var zeile = Zeile(true, new StrangZeile
        {
            Rang = 1, ModuleReihe = 6, StraengeParallel = 1, Mppt = 1, Geraetenummer = 1,
            WechselrichterId = 4711, WechselrichterName = "Muster 2500TL"
        });

        var cut = Render<PvStraengeFelder>(p => p
            .Add(x => x.Zeile, zeile)
            .Add(x => x.Geraete, KATALOG)
            .Add(x => x.GeraetUebernehmen, id => new GeraetWahl(4711, Name(id)))
            .Add(x => x.TrackerZahl, _ => tracker));

        // Das Geraet der Katalogwahl ist DASSELBE, das der erste Strang traegt.
        var wahl = Wahl(cut, "Wechselrichter aus dem Katalog:");
        await cut.InvokeAsync(() => wahl.Instance.AuswahlChanged.InvokeAsync(7));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(2, zeile.Straenge.Count);
        Assert.Equal(4711, zeile.Straenge[1].WechselrichterId);
        Assert.Equal(erwartet, zeile.Straenge[1].Mppt);
    }

    /// <summary>Das <c>&lt;select&gt;</c> einer Strangzeile über sein <c>aria-label</c>.</summary>
    private static AngleSharp.Dom.IElement Klappliste(
        IRenderedComponent<PvStraengeFelder> cut, string kurzname, int zeile = 0)
        => cut.FindAll("table.epos-strangtabelle tbody select[aria-label=\"" + kurzname + "\"]")[zeile];
}
