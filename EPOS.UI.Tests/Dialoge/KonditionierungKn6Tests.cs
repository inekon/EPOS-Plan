using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Zählfall KN6</b> (Stufe KP2, Wellen U2 und U5; Entwurf KP2 Abschnitt 7, SA1 Schritt 4): Wie viele
/// Handgriffe braucht es, in allen fünf Größen die Vorlage „Büro" zu übernehmen? Über die Karten 11 (breit)
/// bis 16; danach hat der Anwender die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…"
/// aufgenommen (E57) — mit ihr sind es in jedem Fall 3.
/// </summary>
/// <remarks>
/// <para><b>Gezählt wird jeder Griff des Anwenders</b> im offenen Katalogeditor: das Öffnen des Reiters
/// „Konditionierung", in der schmalen Anordnung der Reiter je Größe, je Karte die Wahl in der Liste und
/// „Übernehmen", jede Antwort auf eine Rückfrage. Der Editor steht über dem Weg der Hülle ohne Datenbank
/// mit den 14 ausgelieferten Vorlagen; „Büro" steht in jeder Liste an erster Stelle. Das Protokoll der
/// Griffe steht in der Testausgabe.</para>
/// <para><b>Die Fälle je Karte</b> (der Weg je Karte bleibt, P11): breit ohne angelegte Kalender (die
/// Regel), breit an einer Gesamtangabe der Lüftung (eine Rückfrage „aufteilen"), breit mit fünf angelegten
/// Kalendern (je eine Rückfrage nach P12) und schmal (fünf Reiter je Größe).</para>
/// <para><b>Die Fälle mit der Abkürzung</b> (E57, Welle U5): dieselben vier — Reiter, die Wahl „Büro" in der
/// Liste „alle Größen" der Zeile „Vorlage" und „Ja" auf die EINE Rückfrage, die je Größe eine Zeile nennt
/// (übernehmen, P12, aufteilen). Dazu: „Nein" lässt alles, „Zurücknehmen" nimmt alle fünf in einem Schritt
/// zurück, eine Größe ohne gleichnamige Vorlage bleibt, und der Assistent setzt <c>kond_vorlage_alle</c>.</para>
/// </remarks>
public class KonditionierungKn6Tests : EposBunitContext
{
    private readonly ITestOutputHelper _ausgabe;

    public KonditionierungKn6Tests(ITestOutputHelper ausgabe)
    {
        _ausgabe = ausgabe;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());
    }

    private const string BUERO = "Büro";

    /// <summary>Die Griffe: je Griff eine Zeile.</summary>
    private readonly List<string> _griffe = new();

    private GebaeudeKatalogDaten? _geschrieben;

    private IRenderedComponent<GebaeudeKatalogDialog> Editor(GebaeudeKatalogDaten satz, Konditionierungsvorlagenablage? ablage = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, satz)
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Konditionierung, KalenderkarteTests.Weg(ablage ?? Konditionierungsvorlagenablage.AusSaat()))
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Speichern, (d, _, _) => { _geschrieben = d; return new GebaeudeKatalogErgebnis(true, ""); }));

    /// <summary>Ein Griff: ein Klick oder eine Wahl, mit Protokollzeile.</summary>
    private void Griff(string was, Action handlung)
    {
        _griffe.Add(was);
        handlung();
    }

    /// <summary>Beantwortet eine offene Rückfrage mit „Ja" — ein Griff; <c>false</c> = es stand keine.</summary>
    private bool RueckfrageBejahen(IRenderedComponent<GebaeudeKatalogDialog> cut, string wozu)
    {
        IElement? ja = cut.FindAll(".epos-rueckfrage button").FirstOrDefault(b => b.TextContent.Trim() == "Ja");
        if (ja is null) return false;
        Griff("Rückfrage " + wozu + ": Ja", () => ja.Click());
        return true;
    }

    /// <summary>
    /// Übernimmt „Büro" in allen fünf Größen, Griff für Griff. <paramref name="schmal"/>: vor jeder Größe
    /// außer der ersten der Reiter der Größe (die schmale Anordnung zeigt eine Karte).
    /// </summary>
    private void BueroInAllenGroessen(IRenderedComponent<GebaeudeKatalogDialog> cut, bool schmal)
    {
        Griff("Reiter Konditionierung", () => KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, "Konditionierung"));
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            string name = KonditionierungBearbeitung.Groessenname(new KonditionierungTexte(), g);
            if (schmal && g != KonditionierungGroesse.Heizen)
                Griff("Reiter " + name, () => cut.Find($"button.epos-kond-groesse[data-groesse='{(int)g}']").Click());
            Griff(name + ": Wahl " + BUERO, () => KonditionierungVorlagenDialogTests.Waehlen(cut, g, BUERO));
            Griff(name + ": Übernehmen", () => KonditionierungVorlagenDialogTests.Uebernehmen(cut, g));
            RueckfrageBejahen(cut, name);
        }
    }

    /// <summary>Alle fünf Kalender tragen „Büro" als Herkunft; OK schreibt sie.</summary>
    private void AlleBuero(IRenderedComponent<GebaeudeKatalogDialog> cut)
    {
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Equal(BUERO, b.Herkunft(g));
        cut.FindAll("button").First(x => x.TextContent.Trim() == "OK").Click();
        Assert.NotNull(_geschrieben);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Equal(BUERO, _geschrieben!.Konditionierung!.Spalte(g).Kalender.Vorlage);
    }

    private void Protokoll(string fall)
    {
        _ausgabe.WriteLine("KN6 " + fall + ": " + _griffe.Count + " Handgriffe");
        for (int i = 0; i < _griffe.Count; i++) _ausgabe.WriteLine("  " + (i + 1) + ". " + _griffe[i]);
    }

    [Fact]
    public void KN6_breit_ohne_angelegte_Kalender_elf_Handgriffe()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        BueroInAllenGroessen(cut, schmal: false);
        Protokoll("breit");

        Assert.Equal(11, _griffe.Count);   // Reiter + 5 × (Wahl, Übernehmen), keine Rückfrage
        Assert.DoesNotContain(_griffe, g => g.StartsWith("Rückfrage", StringComparison.Ordinal));
        AlleBuero(cut);
    }

    [Fact]
    public void KN6_breit_an_der_Gesamtangabe_der_Lueftung_zwoelf_Handgriffe()
    {
        GebaeudeKatalogDaten satz = KonditionierungVorlagenDialogTests.Vollsatz();
        satz.Luftwechselrate = 0.6;
        satz.LuftwechselInfiltration = null;
        satz.LuftwechselNutzer = null;
        var cut = Editor(satz);
        BueroInAllenGroessen(cut, schmal: false);
        Protokoll("breit, Gesamtangabe");

        Assert.Equal(12, _griffe.Count);   // dazu „aufteilen" (E56 F5 (a))
        Assert.Equal(new[] { "Rückfrage Lüftung: Ja" }, _griffe.Where(g => g.StartsWith("Rückfrage", StringComparison.Ordinal)));
        AlleBuero(cut);
    }

    [Fact]
    public void KN6_breit_mit_fuenf_angelegten_Kalendern_je_eine_Rueckfrage_nach_P12()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, "Konditionierung");
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        // Vorbereitung, nicht gezählt: Geräte und Personen brauchen einen Anteil, ehe es einen Kalender gibt.
        b.WertSetzen(KonditionierungGroesse.Geraete, KonditionierungZeile.Tag, 100);
        b.WertSetzen(KonditionierungGroesse.Personen, KonditionierungZeile.Tag, 100);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            if (!b.Anlegen(g)) _ausgabe.WriteLine("Anlegen " + g + ": " + b.LetzteMeldung);
        cut.Render();
        int angelegt = KonditionierungDaten.Alle.Count(b.Angelegt);
        Assert.Equal(5, angelegt);

        BueroInAllenGroessen(cut, schmal: false);
        Protokoll("breit, fünf angelegte Kalender");

        Assert.Equal(16, _griffe.Count);   // dazu je Größe die Rückfrage vor dem Ersetzen des Matrixbereichs
        Assert.Equal(5, _griffe.Count(g => g.StartsWith("Rückfrage", StringComparison.Ordinal)));
        AlleBuero(cut);
    }

    [Fact]
    public void KN6_schmal_fuenfzehn_Handgriffe()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        BueroInAllenGroessen(cut, schmal: true);
        Protokoll("schmal");

        Assert.Equal(15, _griffe.Count);   // dazu vier Reiter je Größe (Heizen steht vorn)
        AlleBuero(cut);
    }

    // =================================================================================
    // Mit der Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…" (E57, Welle U5)
    // =================================================================================

    private const string REITER = "Konditionierung";

    /// <summary>Die Liste „alle Größen" in der Kopfzelle der Zeile „Vorlage" — in beiden Anordnungen sichtbar.</summary>
    internal static IElement ListeAlle(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find("table.epos-kond-matrix tr[data-zeile='vorlage'] > th[scope=row] .epos-kond-vorlage-alle select");

    /// <summary>Wählt in der Liste „alle Größen" den Namen.</summary>
    internal static void AlleWaehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, string name)
    {
        IElement liste = ListeAlle(cut);
        liste.Change(liste.QuerySelectorAll("option").First(o => o.TextContent.Trim() == name).GetAttribute("value")!);
    }

    /// <summary>Die Zeilen der offenen Rückfrage (der Satz, je Größe eine Zeile, die Zonen).</summary>
    private static string[] Zeilen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find(".epos-rueckfrage-text").TextContent.Trim().Split('\n').Select(z => z.Trim()).ToArray();

    /// <summary>
    /// „Büro" über die Abkürzung, Griff für Griff: der Reiter, die Wahl in der Liste „alle Größen" und „Ja" auf die
    /// eine Rückfrage. Gibt die Zeilen der Rückfrage zurück.
    /// </summary>
    private string[] BueroUeberDieAbkuerzung(IRenderedComponent<GebaeudeKatalogDialog> cut)
    {
        Griff("Reiter Konditionierung", () => KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER));
        Griff("alle Größen: Wahl " + BUERO, () => AlleWaehlen(cut, BUERO));
        string[] zeilen = Zeilen(cut);
        Assert.Equal("Vorlage in allen Größen übernehmen", cut.Instance.Konditionierungsbearbeitung.OffeneFrage!.Titel);
        Assert.True(RueckfrageBejahen(cut, "alle Größen"));
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));                       // eine Rückfrage, keine zweite
        Assert.Equal("", ListeAlle(cut).GetAttribute("value"));             // die Wahl steht wieder auf „—"
        return zeilen;
    }

    /// <summary>Die Zeile einer Größe in der Rückfrage („Heizen: übernehmen").</summary>
    private static string Zeile(string[] zeilen, KonditionierungGroesse g)
    {
        string name = KonditionierungBearbeitung.Groessenname(new KonditionierungTexte(), g);
        return Assert.Single(zeilen, z => z.StartsWith(name + ": ", StringComparison.Ordinal));
    }

    [Fact]
    public void KN6_mit_Abkuerzung_breit_ohne_angelegte_Kalender_drei_Handgriffe()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        string[] zeilen = BueroUeberDieAbkuerzung(cut);
        Protokoll("mit Abkürzung, breit");

        Assert.Equal(3, _griffe.Count);   // Reiter + Wahl „Büro" in „alle Größen" + „Ja" auf die eine Rückfrage
        Assert.Equal("Die Vorlage „Büro“ in allen Größen übernehmen?", zeilen[0]);
        Assert.Equal(6, zeilen.Length);   // der Satz und je Größe eine Zeile
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.EndsWith(": übernehmen", Zeile(zeilen, g));
        AlleBuero(cut);
    }

    [Fact]
    public void KN6_mit_Abkuerzung_breit_an_der_Gesamtangabe_der_Lueftung_drei_Handgriffe()
    {
        GebaeudeKatalogDaten satz = KonditionierungVorlagenDialogTests.Vollsatz();
        satz.Luftwechselrate = 0.6;
        satz.LuftwechselInfiltration = null;
        satz.LuftwechselNutzer = null;
        var cut = Editor(satz);
        string[] zeilen = BueroUeberDieAbkuerzung(cut);
        Protokoll("mit Abkürzung, breit, Gesamtangabe");

        Assert.Equal(3, _griffe.Count);   // „aufteilen" steht in der einen Rückfrage - keine zweite
        Assert.Equal("Lüftung: die Gesamtangabe 0,6 1/h wird aufgeteilt (Infiltration 0,3, Nutzerlüftung 0,3)",
                     Zeile(zeilen, KonditionierungGroesse.Lueftung));
        AlleBuero(cut);
        Assert.Equal(0.3, _geschrieben!.LuftwechselInfiltration);
        Assert.Equal(0.3, _geschrieben.LuftwechselNutzer);
    }

    [Fact]
    public void KN6_mit_Abkuerzung_breit_mit_fuenf_angelegten_Kalendern_drei_Handgriffe()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER);
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        // Vorbereitung, nicht gezählt: Geräte und Personen brauchen einen Anteil, ehe es einen Kalender gibt.
        b.WertSetzen(KonditionierungGroesse.Geraete, KonditionierungZeile.Tag, 100);
        b.WertSetzen(KonditionierungGroesse.Personen, KonditionierungZeile.Tag, 100);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            if (!b.Anlegen(g)) _ausgabe.WriteLine("Anlegen " + g + ": " + b.LetzteMeldung);
        cut.Render();
        Assert.Equal(5, KonditionierungDaten.Alle.Count(b.Angelegt));

        string[] zeilen = BueroUeberDieAbkuerzung(cut);
        Protokoll("mit Abkürzung, breit, fünf angelegte Kalender");

        Assert.Equal(3, _griffe.Count);   // P12 je Größe steht in der einen Rückfrage
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Contains(": ersetzt wird: ", Zeile(zeilen, g));
        AlleBuero(cut);
    }

    [Fact]
    public void KN6_mit_Abkuerzung_schmal_drei_Handgriffe()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        Griff("Reiter Konditionierung", () => KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER));
        // Schmal steht nur die Spalte der gewählten Größe (Heizen vorn); die Kopfzelle der Zeile „Vorlage" steht
        // in beiden Anordnungen - kein Reiter je Größe.
        Assert.Equal("true", cut.Find("button.epos-kond-groesse[data-groesse='0']").GetAttribute("aria-selected"));
        Assert.NotNull(ListeAlle(cut));
        Griff("alle Größen: Wahl " + BUERO, () => AlleWaehlen(cut, BUERO));
        Assert.True(RueckfrageBejahen(cut, "alle Größen"));
        Protokoll("mit Abkürzung, schmal");

        Assert.Equal(3, _griffe.Count);
        Assert.Empty(_griffe.Where(g => g.StartsWith("Reiter ", StringComparison.Ordinal) && g != "Reiter Konditionierung"));
        AlleBuero(cut);
    }

    /// <summary>„Nein" lässt alles: kein Kalender, keine Herkunft, kein Schritt; die Wahl steht wieder auf „—".</summary>
    [Fact]
    public void Abkuerzung_Nein_laesst_alles()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER);
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        var fassung = b.Daten!.Fassung;

        AlleWaehlen(cut, BUERO);
        // Solange die Frage steht, zeigt die Liste die Wahl samt Schloss (ausgeliefert); vorgewählt ist „Nein".
        Assert.Equal(BUERO, ListeAlle(cut).QuerySelector("option[selected]")!.TextContent.Trim());
        Assert.NotNull(cut.Find(".epos-kond-vorlage-alle .epos-schloss"));
        Assert.Contains("epos-knopf--primaer", cut.FindAll(".epos-rueckfrage button").First(x => x.TextContent.Trim() == "Nein").ClassList);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Null(b.Herkunft(g));                                       // vor dem „Ja" ist nichts geschrieben

        cut.FindAll(".epos-rueckfrage button").First(x => x.TextContent.Trim() == "Nein").Click();
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Equal("", ListeAlle(cut).GetAttribute("value"));
        Assert.Empty(cut.FindAll(".epos-kond-vorlage-alle .epos-schloss"));
        Assert.Null(b.GewaehlteVorlageAlle);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            Assert.Null(b.Herkunft(g));
            Assert.False(b.Angelegt(g));
        }
        Assert.False(b.KannZuruecknehmen);
        Assert.Equal(fassung, b.Daten!.Fassung);
    }

    /// <summary>„Zurücknehmen" nimmt alle fünf in EINEM Schritt zurück — Kalender, Herkunft und Bestandsfelder.</summary>
    [Fact]
    public void Abkuerzung_Zuruecknehmen_nimmt_alle_fuenf_in_einem_Schritt_zurueck()
    {
        GebaeudeKatalogDaten satz = KonditionierungVorlagenDialogTests.Vollsatz();
        var cut = Editor(satz);
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER);
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        double? sollTag = b.Stand.SollTag, nacht = b.Stand.NachtAbsenkung, kuehlen = b.Stand.KuehlSollwert;

        AlleWaehlen(cut, BUERO);
        Assert.True(RueckfrageBejahen(cut, "alle Größen"));
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Equal(BUERO, b.Herkunft(g));
        Assert.True(b.KannZuruecknehmen);

        cut.Find("button.epos-kond-zuruecknehmen").Click();
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            Assert.Null(b.Herkunft(g));
            Assert.False(b.Angelegt(g));
        }
        Assert.Equal(sollTag, b.Stand.SollTag);
        Assert.Equal(nacht, b.Stand.NachtAbsenkung);
        Assert.Equal(kuehlen, b.Stand.KuehlSollwert);
        Assert.False(b.KannZuruecknehmen);                                  // eine Stufe: nichts mehr zurückzunehmen

        cut.FindAll("button").First(x => x.TextContent.Trim() == "OK").Click();
        Assert.NotNull(_geschrieben);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.NotEqual(KonditionierungZustand.Angelegt, _geschrieben!.Konditionierung!.Spalte(g).Kalender.Zustand);
    }

    /// <summary>
    /// Eine eigene Vorlage nur in EINER Liste („Als Vorlage speichern…" an der Karte „Heizen"), dann ihr Name in
    /// allen Größen: Heizen übernimmt, die vier übrigen Größen bleiben, die Rückfrage nennt es je Größe.
    /// </summary>
    [Fact]
    public void Abkuerzung_eine_Groesse_ohne_gleichnamige_Vorlage_bleibt()
    {
        var ablage = Konditionierungsvorlagenablage.AusSaat();
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz(), ablage);
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER);
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        IElement karte = KonditionierungVorlagenDialogTests.Karte(cut, KonditionierungGroesse.Heizen);
        karte.QuerySelector("button.epos-kond-als-vorlage")!.Click();
        KonditionierungVorlagenDialogTests.Karte(cut, KonditionierungGroesse.Heizen).QuerySelector(".epos-kond-vorlage-name input")!.Input("Kontor");
        KonditionierungVorlagenDialogTests.Karte(cut, KonditionierungGroesse.Heizen).QuerySelector("button.epos-kond-vorlage-schreiben")!.Click();
        Assert.Contains(ablage.Liste(Konditionierungsgroesse.Heizsoll), v => v.Bezeichner == "Kontor");

        // Die Liste „alle Größen" führt den Namen zuletzt (eigene nach den ausgelieferten).
        Assert.Equal(new[] { "Büro", "Schule", "Wohnen", "Kontor" },
                     ListeAlle(cut).QuerySelectorAll("option").Where(o => o.GetAttribute("value") != "")
                                   .Select(o => o.TextContent.Trim()));

        AlleWaehlen(cut, "Kontor");
        string[] zeilen = Zeilen(cut);
        Assert.Empty(cut.FindAll(".epos-kond-vorlage-alle .epos-schloss"));   // eine eigene trägt kein Schloss
        Assert.Equal("Heizen: übernehmen", Zeile(zeilen, KonditionierungGroesse.Heizen));
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle.Where(g => g != KonditionierungGroesse.Heizen))
            Assert.EndsWith(": keine Vorlage dieses Namens — bleibt", Zeile(zeilen, g));
        Assert.True(RueckfrageBejahen(cut, "alle Größen"));

        Assert.Equal("Kontor", b.Herkunft(KonditionierungGroesse.Heizen));
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle.Where(g => g != KonditionierungGroesse.Heizen))
        {
            Assert.Null(b.Herkunft(g));
            Assert.False(b.Angelegt(g));
        }
    }

    /// <summary>
    /// <b>Der Assistent</b> setzt <c>kond_vorlage_alle</c> — eine Wahl aus der Liste „alle Größen"; Setzen stellt die
    /// eine Rückfrage, die der Anwender selbst beantwortet (der Assistent nennt sie), Lesen nennt danach die
    /// gemeinsame Herkunft. Ein Name, den keine Liste führt, wird benannt abgelehnt.
    /// </summary>
    [Fact]
    public void Abkuerzung_der_Assistent_setzt_kond_vorlage_alle()
    {
        var cut = Editor(KonditionierungVorlagenDialogTests.Vollsatz());
        KonditionierungBearbeitung b = cut.Instance.Konditionierungsbearbeitung;
        KiFeldzugang feld = KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDE_KATALOG, "kond_vorlage_alle")!;

        Assert.True(feld.IstWahl);
        Assert.True(feld.Setzbar);
        Assert.Equal(new[] { "Büro", "Schule", "Wohnen" }, feld.Wahleintraege().Select(e => e.Schluessel));
        Assert.Null(feld.Lesen());

        var unbekannt = Assert.Throws<InvalidOperationException>(() => feld.Setzen("Sternwarte"));
        Assert.Contains("Sternwarte", unbekannt.Message);
        Assert.Null(b.OffeneFrage);

        // Die eine Rückfrage bleibt dem Anwender; der Assistent nennt sie, geschrieben ist nichts.
        var frage = Assert.Throws<InvalidOperationException>(() => feld.Setzen(BUERO));
        Assert.NotNull(b.OffeneFrage);
        Assert.Equal(b.OffeneFrage!.Text, frage.Message);
        Assert.Equal(BUERO, b.GewaehlteVorlageAlle);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Null(b.Herkunft(g));

        cut.Render();
        KonditionierungVorlagenDialogTests.ReiterWaehlen(cut, REITER);
        Assert.True(RueckfrageBejahen(cut, "alle Größen"));
        Assert.Equal(BUERO, feld.Lesen());
        AlleBuero(cut);
    }
}
