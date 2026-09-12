using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „SPEICHER HINZUFÜGEN" AUS EINER QUELLE — Anwenderrückmeldung 12.09.2026 (Auftrag
/// <b>#239</b>): „Stromspeicher hinzufügen geht nicht für Speicher aus der Datenbank — nur
/// für duplizierung des vorhandenen. Bei mehreren angelegten Stromspeichern wird nur einer
/// angezeigt, nach löschen steht er nicht mehr zur Auswahl."
///
/// <para><b>Was der Editor seither kann</b> — und was diese Fälle halten: Der Knopf
/// „+ Speicher hinzufügen" öffnet eine Überlagerung mit DREI Quellen (Speicheranlage des
/// Projekts, Speicherkatalog, leere Einheit), und über der Einheitenliste steht eine leise
/// Zeile, sobald eine Speicheranlage des Projekts keine Einheit hat. <b>Ohne die neuen
/// Parameter bleibt alles beim Alten</b>: ein Klick, eine leere Einheit — das prüft
/// <see cref="Ohne_Quellen_legt_der_Knopf_wie_bisher_eine_leere_Einheit_an"/>, und die
/// bestehenden Fälle in <c>SpeicherFlottenEditorTests</c> fahren genau so.</para>
///
/// <para><b>Der gespeicherte Stand bleibt unberührt</b> (SP‑O‑8): Der Nachzug ist eine
/// Anwenderhandlung; der Editor meldet wie immer nur seinen Arbeitsstand nach außen.</para>
/// </summary>
public sealed class SpeicherFlottenQuellenTests : EposBunitContext
{
    private const string ANLAGE_HALLE = "14935";
    private const string ANLAGE_VERWALTUNG = "14936";

    public SpeicherFlottenQuellenTests()
    {
        // Die Ueberlagerung setzt den Fokus in OnAfterRenderAsync.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =================================================================================
    // Prüfdaten
    // =================================================================================

    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Stromspeicher,
            s => Resource.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat> Kandidaten() => new[]
    {
        new SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat(ANLAGE_HALLE, "Speicher Halle", 129.0, 100.0),
        new SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat(ANLAGE_VERWALTUNG, "Speicher Verwaltung", 30.0, 15.0)
    };

    private static FlottenEinheit? AusAnlage(string anlageId) => anlageId switch
    {
        ANLAGE_HALLE => new FlottenEinheit
        {
            Id = "a1", Name = "Speicher Halle", AnlageId = ANLAGE_HALLE,
            KapazitaetKWh = 129.0, LadeleistungKw = 100.0, EntladeleistungKw = 100.0,
            Ladewirkungsgrad = 0.9487, Entladewirkungsgrad = 0.9487,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.1
        },
        ANLAGE_VERWALTUNG => new FlottenEinheit
        {
            Id = "a2", Name = "Speicher Verwaltung", AnlageId = ANLAGE_VERWALTUNG,
            KapazitaetKWh = 30.0, LadeleistungKw = 15.0, EntladeleistungKw = 15.0,
            Ladewirkungsgrad = 0.9487, Entladewirkungsgrad = 0.9487,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.1
        },
        _ => null
    };

    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        new Katalogfilterzeile(41, "Speicher 10")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 10")
            .MitText(Katalogfilterprofil.SpHersteller, "BYD")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 10.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 10.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.9, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 6000, 0),

        new Katalogfilterzeile(42, "Speicher 20")
            .MitText(Katalogfilterprofil.SpBezeichner, "Speicher 20")
            .MitText(Katalogfilterprofil.SpHersteller, "VARTA")
            .MitText(Katalogfilterprofil.SpChemie, "Lithium-Ionen")
            .MitZahl(Katalogfilterprofil.SpEnergie, 20.0)
            .MitZahl(Katalogfilterprofil.SpLeistung, 20.0)
            .MitZahl(Katalogfilterprofil.SpCrate, 1.0, 2)
            .MitZahl(Katalogfilterprofil.SpEtaRt, 0.92, 3)
            .MitZahl(Katalogfilterprofil.SpZyklen, 8000, 0)
    };

    private static FlottenEinheit? AusKatalog(int id) => id == 42
        ? new FlottenEinheit
        {
            Id = "k42", Name = "Speicher 20", AnlageId = null,
            KapazitaetKWh = 20.0, LadeleistungKw = 20.0, EntladeleistungKw = 20.0,
            Ladewirkungsgrad = 0.9592, Entladewirkungsgrad = 0.9592,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.1,
            HilfsverbrauchKw = 0.035,
            EigeneKosten = true, InvestitionEuroProKWh = 480.0, InvestitionEuroProKw = 120.0
        }
        : null;

    // =================================================================================
    // Aufbau
    // =================================================================================

    private IRenderedComponent<SpeicherFlottenEditor> MitQuellen(
        FlottenStudieKonfiguration eingang,
        Action<FlottenStudieKonfiguration> gemeldet,
        bool anlagen = true, bool katalog = true)
    {
        return Render<SpeicherFlottenEditor>(p =>
        {
            p.Add(x => x.Wert, eingang)
             .Add(x => x.WertChanged, gemeldet)
             .Add(x => x.BetriebZeigen, false);

            if (anlagen)
                p.Add(x => x.Projektanlagen, Kandidaten)
                 .Add(x => x.EinheitAusProjektanlage, AusAnlage);

            if (katalog)
                p.Add(x => x.Katalogzeilen, Katalogzeilen)
                 .Add(x => x.Katalogprofil, Profil)
                 .Add(x => x.EinheitAusKatalog, AusKatalog);
        });
    }

    private static FlottenStudieKonfiguration Leer() => new();

    /// <summary>Eine Flotte, die GENAU die Anlage „Halle" führt — „Verwaltung" fehlt.</summary>
    private static FlottenStudieKonfiguration NurHalle()
    {
        var f = new FlottenStudieKonfiguration();
        f.Einheiten.Add(AusAnlage(ANLAGE_HALLE)!);
        return f;
    }

    private static void Hinzufuegen(IRenderedComponent<SpeicherFlottenEditor> cut)
        => cut.FindAll("button").First(x => x.TextContent.Contains("Speicher hinzufügen")).Click();

    private static void QuelleWaehlen(IRenderedComponent<SpeicherFlottenEditor> cut, int quelle)
        => cut.FindAll(".epos-flotte-quellenwahl .epos-option-kasten")
              .Single(x => x.GetAttribute("value") == quelle.ToString()).Change(true);

    private static void Uebernehmen(IRenderedComponent<SpeicherFlottenEditor> cut)
        => cut.FindAll(".epos-flotte-quellenwahl__leiste button")
              .Single(x => x.TextContent.Trim() == "Übernehmen").Click();

    // =================================================================================
    // 1 — Ohne Quellen bleibt alles, wie es war
    // =================================================================================

    /// <summary>
    /// <b>Kein Delegat, kein Zwischenschritt.</b> Der zweite Wirt des Editors — der Reiter
    /// „Stromspeicher" — kennt kein Projekt und keinen Katalog; dort soll der Knopf tun,
    /// was er immer tat.
    /// </summary>
    [Fact]
    public void Ohne_Quellen_legt_der_Knopf_wie_bisher_eine_leere_Einheit_an()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, Leer())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Hinzufuegen(cut);

        Assert.NotNull(gemeldet);
        Assert.Single(gemeldet!.Einheiten);
        Assert.Single(gemeldet.Auslegung.Achsen);
        Assert.Equal(100, gemeldet.Einheiten[0].KapazitaetKWh);
        Assert.Null(gemeldet.Einheiten[0].AnlageId);
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Empty(cut.FindAll(".epos-flotte-nachzug"));
    }

    // =================================================================================
    // 2 — Die Quellenwahl
    // =================================================================================

    [Fact]
    public void Mit_Quellen_oeffnet_der_Knopf_die_Wahl_mit_drei_Quellen()
    {
        var cut = MitQuellen(Leer(), _ => { });

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Hinzufuegen(cut);

        var dialog = cut.Find("[role=dialog]");
        Assert.Contains("Speicher hinzufügen", dialog.TextContent);
        Assert.Contains("Speicheranlage des Projekts", dialog.TextContent);
        Assert.Contains("Speicherkatalog (Datenbank)", dialog.TextContent);
        Assert.Contains("Leere Einheit", dialog.TextContent);
        Assert.Equal(3, cut.FindAll(".epos-flotte-quellenwahl .epos-option-kasten").Count);
    }

    /// <summary>
    /// <b>Die Anlagenwahl liefert eine Einheit MIT Anlagenbezug</b> — und wie jede andere
    /// Einheit ihre Suchachse (Kopf von <c>SpeicherFlottenEditor.razor</c>).
    /// </summary>
    [Fact]
    public void Die_Anlagenwahl_legt_eine_Einheit_mit_Anlagenbezug_und_Achse_an()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = MitQuellen(Leer(), x => gemeldet = x);

        Hinzufuegen(cut);
        QuelleWaehlen(cut, SpeicherFlottenEditor.QuelleAnlage);
        cut.FindAll(".epos-flotte-anlagenliste input[type=radio]")[1].Change(true);
        Uebernehmen(cut);

        Assert.NotNull(gemeldet);
        FlottenEinheit e = Assert.Single(gemeldet!.Einheiten);
        Assert.Equal("Speicher Verwaltung", e.Name);
        Assert.Equal(ANLAGE_VERWALTUNG, e.AnlageId);
        Assert.Equal(30.0, e.KapazitaetKWh);
        Assert.Single(gemeldet.Auslegung.Achsen);
        Assert.Equal(e.Id, gemeldet.Auslegung.Achsen[0].Vorlage!.Id);
        Assert.Empty(cut.FindAll("[role=dialog]"));
    }

    /// <summary>
    /// <b>Eine Anlage, die schon in der Flotte steht, ist gesperrt</b> — sie steht trotzdem
    /// da, mit dem Vermerk „bereits in der Flotte". Sie zu verbergen ließe genau die Frage
    /// offen, die der Anwender gestellt hat („wo ist mein zweiter Speicher?").
    /// </summary>
    [Fact]
    public void Eine_vertretene_Anlage_ist_gesperrt_und_benannt()
    {
        var cut = MitQuellen(NurHalle(), _ => { });

        Hinzufuegen(cut);
        QuelleWaehlen(cut, SpeicherFlottenEditor.QuelleAnlage);

        var zeilen = cut.FindAll(".epos-flotte-anlagenliste tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.NotNull(zeilen[0].QuerySelector("input[type=radio]")!.GetAttribute("disabled"));
        Assert.Contains("bereits in der Flotte", zeilen[0].TextContent);
        Assert.Null(zeilen[1].QuerySelector("input[type=radio]")!.GetAttribute("disabled"));
        Assert.DoesNotContain("bereits in der Flotte", zeilen[1].TextContent);
    }

    /// <summary>
    /// <b>Die Katalogwahl liefert die Gerätedaten des Satzes</b>, samt seiner Kosten und
    /// OHNE Anlagenbezug — der Satz gehört zu keiner Anlage des Projekts.
    /// </summary>
    [Fact]
    public void Die_Katalogwahl_legt_eine_Einheit_mit_Kosten_und_ohne_Anlagenbezug_an()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = MitQuellen(Leer(), x => gemeldet = x);

        Hinzufuegen(cut);
        QuelleWaehlen(cut, SpeicherFlottenEditor.QuelleKatalog);
        cut.FindAll(".epos-flotte-katalogwahl .epos-anlagenwahl")[1].Click();
        Uebernehmen(cut);

        Assert.NotNull(gemeldet);
        FlottenEinheit e = Assert.Single(gemeldet!.Einheiten);
        Assert.Equal("Speicher 20", e.Name);
        Assert.Null(e.AnlageId);
        Assert.Equal(20.0, e.KapazitaetKWh);
        Assert.True(e.EigeneKosten);
        Assert.Equal(480.0, e.InvestitionEuroProKWh);
        Assert.Equal(0.035, e.HilfsverbrauchKw);
        Assert.Single(gemeldet.Auslegung.Achsen);
    }

    /// <summary>Die dritte Quelle ist das alte Verhalten — eine Einheit mit Vorgabewerten.</summary>
    [Fact]
    public void Die_leere_Quelle_legt_die_Einheit_mit_Vorgabewerten_an()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = MitQuellen(Leer(), x => gemeldet = x);

        Hinzufuegen(cut);
        QuelleWaehlen(cut, SpeicherFlottenEditor.QuelleLeer);
        Uebernehmen(cut);

        Assert.NotNull(gemeldet);
        FlottenEinheit e = Assert.Single(gemeldet!.Einheiten);
        Assert.Null(e.AnlageId);
        Assert.Equal(100, e.KapazitaetKWh);
        Assert.Equal(50, e.LadeleistungKw);
    }

    /// <summary>Abbrechen legt nichts an und schließt die Wahl.</summary>
    [Fact]
    public void Abbrechen_legt_nichts_an()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = MitQuellen(Leer(), x => gemeldet = x);

        Hinzufuegen(cut);
        cut.FindAll(".epos-flotte-quellenwahl__leiste button")
           .Single(x => x.TextContent.Trim() == "Abbrechen").Click();

        Assert.Null(gemeldet);
        Assert.Empty(cut.FindAll("[role=dialog]"));
    }

    // =================================================================================
    // 3 — Der Nachzug: die Antwort auf „nur einer angezeigt"
    // =================================================================================

    /// <summary>
    /// <b>Der Befund selbst.</b> Die Flotte führt eine von zwei Speicheranlagen — die Zeile
    /// sagt welche, der Knopf holt sie, und danach ist die Zeile weg. Nach dem Entfernen
    /// derselben Einheit steht sie wieder da: Genau daran scheiterte „nach löschen steht er
    /// nicht mehr zur Auswahl".
    /// </summary>
    [Fact]
    public void Die_Nachzugszeile_nennt_die_fehlende_Anlage_und_holt_sie()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = MitQuellen(NurHalle(), x => gemeldet = x);

        var zeile = cut.Find(".epos-flotte-nachzug");
        Assert.Contains("Speicher Verwaltung", zeile.TextContent);
        Assert.DoesNotContain("Speicher Halle", zeile.QuerySelector(".epos-flotte-nachzug__text")!.TextContent);

        cut.Find(".epos-flotte-nachzug button").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(2, gemeldet!.Einheiten.Count);
        Assert.Equal(ANLAGE_VERWALTUNG, gemeldet.Einheiten[1].AnlageId);
        Assert.Equal(2, gemeldet.Auslegung.Achsen.Count);
        Assert.Empty(cut.FindAll(".epos-flotte-nachzug"));

        // … und nach dem Entfernen steht sie wieder da.
        cut.FindAll("button").Last(x => x.TextContent.Trim() == "Entfernen").Click();
        Assert.Single(gemeldet.Einheiten);
        Assert.Contains("Speicher Verwaltung", cut.Find(".epos-flotte-nachzug").TextContent);
    }

    /// <summary>
    /// Führt die Flotte beide Anlagen, steht keine Zeile da — ein Hinweis ohne Anlass wäre
    /// Rauschen.
    /// </summary>
    [Fact]
    public void Ohne_fehlende_Anlage_steht_keine_Nachzugszeile()
    {
        var f = new FlottenStudieKonfiguration();
        f.Einheiten.Add(AusAnlage(ANLAGE_HALLE)!);
        f.Einheiten.Add(AusAnlage(ANLAGE_VERWALTUNG)!);

        var cut = MitQuellen(f, _ => { });

        Assert.Empty(cut.FindAll(".epos-flotte-nachzug"));
    }

    /// <summary>
    /// <b>Ohne den Weg in den Kern keine Zeile.</b> Wer die Anlage nicht holen kann, soll
    /// nicht erfahren, dass sie fehlt — das wäre eine Zusage ohne Deckung (Hausregel „kein
    /// Delegat, kein Bedienelement").
    /// </summary>
    [Fact]
    public void Ohne_Anlagenweg_steht_keine_Nachzugszeile()
    {
        var cut = MitQuellen(NurHalle(), _ => { }, anlagen: false);

        Assert.Empty(cut.FindAll(".epos-flotte-nachzug"));
    }

    /// <summary>
    /// Ohne Anlagenquelle bleiben zwei Quellen — Katalog und leere Einheit; die
    /// Optionsgruppe zählt nur, was wirklich geht.
    /// </summary>
    [Fact]
    public void Ohne_Anlagenquelle_bleiben_zwei_Quellen()
    {
        var cut = MitQuellen(Leer(), _ => { }, anlagen: false);

        Hinzufuegen(cut);

        Assert.Equal(2, cut.FindAll(".epos-flotte-quellenwahl .epos-option-kasten").Count);
        Assert.DoesNotContain("Speicheranlage des Projekts", cut.Find("[role=dialog]").TextContent);
    }
}
