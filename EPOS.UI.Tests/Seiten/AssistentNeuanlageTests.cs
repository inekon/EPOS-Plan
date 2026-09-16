using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Assistent;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// DER EINSTIEG „NEUES PROJEKT" (Anwenderentscheid vom 16.09.2026, drei
/// Bildschirmfotos): „Der Dialog Projekt-Erstellungskonfiguration soll bei
/// Neuanlage nicht erscheinen (irrelevant). Es soll gleich der Projektassistent
/// mit der Projektkonfiguration starten und dann mit Weiter … auf die Kachel
/// Wärmebedarf springen."
///
/// <para>Geprüft wird der ganze Weg in seinen drei Stücken: die Kachel meldet den
/// Einstieg (<c>Startseite</c>), der Assistent läuft im Modus
/// <see cref="AssistentEinstieg.Neuanlage"/> (ein Schritt, drei Knöpfe, „Weiter ▶"
/// legt an und nennt sein Ziel), und die <c>AppWurzel</c> übersetzt das Ziel in den
/// Ansichtswechsel auf den Reiter „Wärmebedarf".</para>
///
/// <para>Was NICHT geprüft wird, weil es unverändert bleibt: der vollständige Lauf
/// mit dem Komponentenschritt (<c>AssistentTests</c>) — er steht hier nur als
/// Gegenprobe, damit der neue Modus nicht heimlich zum einzigen wird.</para>
/// </summary>
public class AssistentNeuanlageTests : EposBunitContext
{
    public AssistentNeuanlageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Der Assistent im Modus NEUANLAGE
    // =====================================================================

    /// <summary>
    /// Ein Gabensatz je Seite — wie in <c>AssistentTests</c>: dreizehn leere
    /// Kacheln für den Komponentenschritt, ein leeres Wörterbuch für alle anderen.
    /// Die Projektkonfiguration zeichnet auch ohne Gaben (Hausregel EPOS.UI).
    /// </summary>
    private static IReadOnlyDictionary<string, object> Gaben(int nr)
    {
        if (nr != 0) return new Dictionary<string, object>();

        var zeilen = new List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile>();
        for (int k = 0; k < 13; k++)
            zeilen.Add(new EPOS.UI.Dialoge.Bedarf.KomponentenZeile
            {
                Kennung = k,
                Titel = "Kachel " + k,
                SeitenIndex = k < 11 ? k + 2 : EPOS.UI.Dialoge.Bedarf.KomponentenZeile.OHNE_SEITE
            });

        return new Dictionary<string, object> { ["Zeilen"] = zeilen };
    }

    private IRenderedComponent<AssistentSeite> Neuanlage(
        Func<(string Text, string Titel)?>? speichern = null,
        Func<int, string?>? seitePruefen = null,
        Action<string>? angelegt = null,
        Action<bool>? geschlossen = null,
        Func<bool>? geaendert = null,
        AssistentEinstieg einstieg = AssistentEinstieg.Neuanlage)
    {
        return Render<AssistentSeite>(p => p
            .Add(x => x.Betriebsart, AssistentCtrl.BETRIEBSART_NEU)
            .Add(x => x.Einstieg, einstieg)
            .Add(x => x.SeiteGaben, new Func<int, IReadOnlyDictionary<string, object>?>(Gaben))
            .Add(x => x.SeiteAktiv, nr => true)
            .Add(x => x.SeitePruefen, seitePruefen)
            .Add(x => x.Speichern, speichern ?? (() => null))
            .Add(x => x.HatAenderungen, geaendert ?? (() => false))
            .Add(x => x.Angelegt, (string ziel) => angelegt?.Invoke(ziel))
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IHtmlCollection<IElement> Fussknoepfe(IRenderedComponent<AssistentSeite> cut)
        => cut.Find(".epos-assistent-fuss").QuerySelectorAll("button");

    private static IElement Abbrechen(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[0];
    private static IElement Zurueck(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[1];
    private static IElement Weiter(IRenderedComponent<AssistentSeite> cut) => Fussknoepfe(cut)[2];

    /// <summary>
    /// <b>Der Lauf beginnt auf der PROJEKTKONFIGURATION</b> — und der
    /// Komponentenschritt wird nicht gezeichnet. Er ist nicht gelöscht, nur
    /// übersprungen: Für ein Projekt, das es noch nicht gibt, wählt er nichts aus.
    /// </summary>
    [Fact]
    public void Die_Neuanlage_beginnt_auf_der_Projektkonfiguration()
    {
        var cut = Neuanlage();

        Assert.Equal(AssistentSeite.PROJEKTKOPF, cut.Instance.Schritt);
        Assert.Single(cut.FindAll(".epos-projektkopf"));
        Assert.Empty(cut.FindAll(".epos-kachel"));
    }

    /// <summary>
    /// <b>Die Gegenprobe:</b> Der vollständige Lauf ist unverändert — er beginnt auf
    /// dem Komponentenschritt mit seinen dreizehn Kacheln, und „Zurück" ist dort
    /// gesperrt, weil es aus ihm keinen Rückweg gibt.
    /// </summary>
    [Fact]
    public void Der_vollstaendige_Lauf_beginnt_weiterhin_bei_den_Komponenten()
    {
        var cut = Neuanlage(einstieg: AssistentEinstieg.Vollstaendig);

        Assert.Equal(0, cut.Instance.Schritt);
        Assert.Equal(13, cut.FindAll(".epos-kachel").Count);
        Assert.True(Zurueck(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// Die Knopfleiste des Bildschirmfotos: <b>Abbrechen · ◀ Zurück · Weiter ▶</b>.
    /// „Weiter ▶" steht dort, wo im vollständigen Lauf „Speichern" stünde — der
    /// Ablauf hört hier nicht auf —, und „Zurück" ist bedienbar, obwohl es keinen
    /// vorigen Schritt gibt: Es führt aus dem Assistenten heraus.
    /// </summary>
    [Fact]
    public void Die_Knopfleiste_heisst_Abbrechen_Zurueck_Weiter()
    {
        var cut = Neuanlage();

        Assert.Equal(3, Fussknoepfe(cut).Length);
        Assert.Equal("Abbrechen", Abbrechen(cut).TextContent);
        Assert.Equal("◀ Zurück", Zurueck(cut).TextContent);
        Assert.Equal("Weiter ▶", Weiter(cut).TextContent);

        Assert.False(Zurueck(cut).HasAttribute("disabled"));
        Assert.Contains("epos-knopf--primaer", Weiter(cut).ClassList);
    }

    /// <summary>
    /// <b>„Weiter" prüft die Pflichtfelder</b> — dieselbe Prüfung wie „Speichern"
    /// (Projektname, Klimaregion). Sie meldet EINMAL, der Assistent bleibt stehen,
    /// und weder der Speicherweg noch das Ziel werden gerufen.
    /// </summary>
    [Fact]
    public void Weiter_ohne_Projektnamen_meldet_und_bleibt_stehen()
    {
        int gespeichert = 0;
        string? ziel = null;
        bool? ende = null;

        var cut = Neuanlage(speichern: () => { gespeichert++; return null; },
                            seitePruefen: nr => "Bitte einen Projektnamen eingeben!",
                            angelegt: z => ziel = z,
                            geschlossen: ok => ende = ok);

        Weiter(cut).Click();

        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.Contains("Bitte einen Projektnamen eingeben!", cut.Markup);
        Assert.Equal(AssistentSeite.PROJEKTKOPF, cut.Instance.Schritt);
        Assert.Equal(0, gespeichert);
        Assert.Null(ziel);
        Assert.Null(ende);
    }

    /// <summary>
    /// <b>„Weiter" legt an und nennt sein ZIEL:</b> derselbe Speicherweg wie
    /// „Speichern" und danach die Meldung „weiter zum Wärmebedarf". Der Lauf endet
    /// dabei NICHT über <c>Geschlossen</c> — den Wechsel macht der Wirt.
    /// </summary>
    [Fact]
    public void Weiter_legt_an_und_meldet_das_Ziel_Waermebedarf()
    {
        int gespeichert = 0;
        string? ziel = null;
        bool? ende = null;

        var cut = Neuanlage(speichern: () => { gespeichert++; return null; },
                            angelegt: z => ziel = z,
                            geschlossen: ok => ende = ok);

        Weiter(cut).Click();

        Assert.Equal(1, gespeichert);
        Assert.Equal(Reiterschluessel.Waermebedarf, ziel);
        Assert.Null(ende);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    /// <summary>
    /// Scheitert das Anlegen, bleibt der Assistent stehen und meldet — wie im
    /// vollständigen Lauf (Entscheid E-4). Ein Ziel wird dann nicht genannt.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Anlegen_meldet_und_nennt_kein_Ziel()
    {
        string? ziel = null;
        var cut = Neuanlage(speichern: () => ("Bitte eine Klimazone auswählen!", "Klimazone fehlt"),
                            angelegt: z => ziel = z);

        Weiter(cut).Click();

        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.Contains("Klimazone fehlt", cut.Markup);
        Assert.Null(ziel);
        Assert.Equal(AssistentSeite.PROJEKTKOPF, cut.Instance.Schritt);
    }

    /// <summary>
    /// <b>Ohne Rückruf endet der Lauf wie bisher</b> (Hausregel „jede Seite zeichnet
    /// auch ohne Gaben"): Wer kein Ziel entgegennimmt, bekommt das gewöhnliche
    /// <c>Geschlossen(true)</c> eines gespeicherten Laufs.
    /// </summary>
    [Fact]
    public void Ohne_Rueckruf_endet_die_Neuanlage_wie_ein_gespeicherter_Lauf()
    {
        bool? ende = null;
        var cut = Render<AssistentSeite>(p => p
            .Add(x => x.Betriebsart, AssistentCtrl.BETRIEBSART_NEU)
            .Add(x => x.Einstieg, AssistentEinstieg.Neuanlage)
            .Add(x => x.SeiteGaben, new Func<int, IReadOnlyDictionary<string, object>?>(Gaben))
            .Add(x => x.Speichern, () => ((string Text, string Titel)?)null)
            .Add(x => x.Geschlossen, (bool ok) => ende = ok));

        Weiter(cut).Click();

        Assert.True(ende);
    }

    /// <summary>
    /// <b>„Zurück" führt aus dem Assistenten heraus</b> — es gibt keinen vorigen
    /// Schritt, also ist es der Rückweg auf die Startansicht. Ohne Rückfrage, auch
    /// mit Eingaben: Der Knopf ist der Rückschritt eines Ablaufs, nicht sein
    /// Abbruch.
    /// </summary>
    [Fact]
    public void Zurueck_meldet_den_Rueckweg_ohne_Rueckfrage()
    {
        bool? ende = null;
        var cut = Neuanlage(geschlossen: ok => ende = ok, geaendert: () => true);

        Zurueck(cut).Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.False(ende);
    }

    /// <summary>
    /// „Abbrechen" bleibt dagegen, was es war: Mit ungespeicherten Eingaben kommt
    /// die Rückfrage aus 62b-E-1 mit ihren drei Wegen.
    /// </summary>
    [Fact]
    public void Abbrechen_fragt_weiterhin_nach_ungespeicherten_Eingaben()
    {
        var cut = Neuanlage(geaendert: () => true);

        Abbrechen(cut).Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
    }

    // =====================================================================
    //  Die Kachel „Neues Projekt" meldet den Einstieg
    // =====================================================================

    private static IReadOnlyList<StartKachel> Startkacheln() => new[]
    {
        new StartKachel
        {
            Schluessel = Kachelschluessel.ProjektNeu,
            Reiter = Reiterschluessel.Projekt,
            Titel = "Neues Projekt"
        },
        new StartKachel
        {
            Schluessel = Kachelschluessel.ProjektZuletzt,
            Reiter = Reiterschluessel.Projekt,
            Titel = "Zuletzt geöffnet"
        },
        new StartKachel
        {
            Schluessel = Kachelschluessel.Gebaeude,
            Reiter = Reiterschluessel.Waermebedarf,
            Titel = "Gebäudedaten eingeben"
        }
    };

    /// <summary>
    /// Die Kachel meldet den EINSTIEG — und zwar VOR dem Kachelweg der Hülle: Der
    /// Wirt muss wissen, was für ein Lauf gleich aufgeht, bevor er aufgeht.
    /// Jede andere Kachel meldet ihn nicht.
    /// </summary>
    [Fact]
    public void Nur_die_Kachel_Neues_Projekt_meldet_den_Einstieg()
    {
        List<string> reihenfolge = new List<string>();

        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Startkacheln())
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Geklickt, s => reihenfolge.Add("weg:" + s))
            .Add(x => x.ProjektNeuGewaehlt, () => reihenfolge.Add("einstieg")));

        cut.FindAll(".epos-kachel")[0].Click();
        cut.FindAll(".epos-kachel")[1].Click();

        Assert.Equal(new[] { "einstieg", "weg:" + Kachelschluessel.ProjektNeu,
                             "weg:" + Kachelschluessel.ProjektZuletzt }, reihenfolge);
    }

    /// <summary>
    /// Der REITERWUNSCH des Wirtes holt den Reiter „Wärmebedarf" nach vorn — das
    /// Ziel nach dem Anlegen. Ohne Wunsch steht der Reiter „Projekt" vorn.
    /// </summary>
    [Fact]
    public void Der_Reiterwunsch_holt_den_Waermebedarf_nach_vorn()
    {
        var ohne = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Startkacheln())
            .Add(x => x.ProjektId, () => 1030));

        Assert.Equal("true", ohne.FindAll("[role='tab']")[0].GetAttribute("aria-selected"));

        var mit = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Startkacheln())
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Reiterwunsch, Reiterschluessel.Waermebedarf));

        Assert.Equal("true", mit.FindAll("[role='tab']")[1].GetAttribute("aria-selected"));
    }

    /// <summary>
    /// Ein Wunsch hebt keine SPERRE auf: Ohne offenes Projekt sind die Reiter 2 bis
    /// 6 gesperrt, und die Seite bleibt auf „Projekt".
    /// </summary>
    [Fact]
    public void Ohne_offenes_Projekt_bleibt_der_Reiterwunsch_wirkungslos()
    {
        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Startkacheln())
            .Add(x => x.ProjektId, () => 0)
            .Add(x => x.Reiterwunsch, Reiterschluessel.Waermebedarf));

        Assert.Equal("true", cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected"));
    }

    // =====================================================================
    //  Der ganze Weg in der AppWurzel
    // =====================================================================

    /// <summary>
    /// <b>Der Weg des Anwenders, Stück für Stück:</b> Kachel „Neues Projekt" →
    /// Assistent im Modus NEUANLAGE (Projektkonfiguration, kein Komponentenschritt)
    /// → „Weiter ▶" legt an → die Startseite steht wieder da, und zwar auf dem
    /// Reiter „Wärmebedarf".
    /// </summary>
    [Fact]
    public void Die_Kachel_fuehrt_ueber_die_Projektkonfiguration_zum_Waermebedarf()
    {
        int projektId = 0;
        int gespeichert = 0;
        AppWurzel? wurzel = null;

        var startseite = new Dictionary<string, object>
        {
            ["ProjektId"] = new Func<int>(() => projektId),
            ["Kacheln"] = new Func<IReadOnlyList<StartKachel>>(Startkacheln),

            // Der Kachelweg der Huelle: Er merkt sich das Projekt dieses Laufs und
            // oeffnet den Assistenten in der Betriebsart NEU - unveraendert.
            ["Geklickt"] = new Action<string>(s =>
            {
                if (s == Kachelschluessel.ProjektNeu)
                    wurzel?.OeffneMaske(Seitenschluessel.Assistent, AssistentCtrl.BETRIEBSART_NEU);
            })
        };

        var assistent = new Dictionary<string, object>
        {
            ["Betriebsart"] = AssistentCtrl.BETRIEBSART_NEU,
            ["SeiteAktiv"] = new Func<int, bool>(nr => true),
            ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(Gaben),
            ["Speichern"] = new Func<(string Text, string Titel)?>(() =>
            {
                gespeichert++;
                projektId = 1030;          // der Nachzug der Huelle: das Projekt ist aktiv
                return null;
            }),
            ["HatAenderungen"] = new Func<bool>(() => false)
        };

        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(Array.Empty<ProjektZeile>()));
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Startseite)
            .Add(x => x.StartseiteGaben, startseite)
            .Add(x => x.AssistentGaben,
                 new Func<int, IReadOnlyDictionary<string, object>?>(_ => assistent)));
        wurzel = cut.Instance;

        Assert.Single(cut.FindAll(".epos-startseite"));

        // (1) Die Kachel - der Assistent geht auf der Projektkonfiguration auf.
        cut.FindAll(".epos-kachel")[0].Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Single(cut.FindAll(".epos-projektkopf"));
        Assert.Empty(cut.FindAll(".epos-kachel"));
        Assert.Equal("Weiter ▶",
                     cut.Find(".epos-assistent-fuss").QuerySelectorAll("button")[2].TextContent);

        // (2) „Weiter ▶" legt an und die Wurzel wechselt auf den Waermebedarf.
        cut.Find(".epos-assistent-fuss").QuerySelectorAll("button")[2].Click();
        cut.Render();

        Assert.Equal(1, gespeichert);
        Assert.Empty(cut.FindAll(".epos-assistentseite"));
        Assert.Single(cut.FindAll(".epos-startseite"));

        // Der Reiter „Waermebedarf" steht vorn - und zwar wirklich: Seine Kachel ist
        // zu sehen, die des Reiters „Projekt" nicht.
        Assert.Equal("true", cut.FindAll("[role='tab']")[1].GetAttribute("aria-selected"));
        Assert.Contains("Gebäudedaten eingeben", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Neues Projekt", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Jeder andere Einstieg bleibt vollständig.</b> Der Menüweg „Projekt →
    /// Neu…" und der Ruf des Kerns über <c>Masken.Assistent</c> öffnen den
    /// Assistenten unverändert auf dem Komponentenschritt — der Modus hängt an der
    /// KACHEL, nicht an der Betriebsart.
    /// </summary>
    [Theory]
    [InlineData("PROJEKT_NEU")]
    [InlineData("ASSISTENT")]
    public void Ohne_die_Kachel_bleibt_es_beim_vollstaendigen_Lauf(string schluessel)
    {
        var assistent = new Dictionary<string, object>
        {
            ["Betriebsart"] = AssistentCtrl.BETRIEBSART_NEU,
            ["SeiteAktiv"] = new Func<int, bool>(nr => true),
            ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(Gaben),
            ["HatAenderungen"] = new Func<bool>(() => false)
        };

        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(Array.Empty<ProjektZeile>()));
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.AssistentGaben,
                 new Func<int, IReadOnlyDictionary<string, object>?>(_ => assistent)));

        Assert.True(cut.Instance.OeffneMaske(schluessel, AssistentCtrl.BETRIEBSART_NEU));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Equal(13, cut.FindAll(".epos-kachel").Count);
        Assert.Empty(cut.FindAll(".epos-projektkopf"));
    }
}
