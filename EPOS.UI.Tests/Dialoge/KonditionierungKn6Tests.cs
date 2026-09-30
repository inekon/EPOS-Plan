using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Zählfall KN6</b> (Stufe KP2, Welle U2; Entwurf KP2 Abschnitt 7, SA1 Schritt 4): Wie viele
/// Handgriffe braucht es, in allen fünf Größen die Vorlage „Büro" zu übernehmen? Die Zahl entscheidet nach
/// der Sichtabnahme SA1 über die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen" — sie wird
/// hier GEZÄHLT, nicht gebaut.
/// </summary>
/// <remarks>
/// <para><b>Gezählt wird jeder Griff des Anwenders</b> im offenen Katalogeditor: das Öffnen des Reiters
/// „Konditionierung", in der schmalen Anordnung der Reiter je Größe, je Karte die Wahl in der Liste und
/// „Übernehmen", jede Antwort auf eine Rückfrage. Der Editor steht über dem Weg der Hülle ohne Datenbank
/// mit den 14 ausgelieferten Vorlagen; „Büro" steht in jeder Liste an erster Stelle. Das Protokoll der
/// Griffe steht in der Testausgabe.</para>
/// <para><b>Die Fälle:</b> breit ohne angelegte Kalender (die Regel), breit an einer Gesamtangabe der
/// Lüftung (eine Rückfrage „aufteilen"), breit mit fünf angelegten Kalendern (je eine Rückfrage nach P12)
/// und schmal (fünf Reiter je Größe).</para>
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

    private IRenderedComponent<GebaeudeKatalogDialog> Editor(GebaeudeKatalogDaten satz)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, satz)
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Konditionierung, KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat()))
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
}
