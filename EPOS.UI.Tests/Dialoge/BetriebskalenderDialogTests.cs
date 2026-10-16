using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Betriebskalender in der Oberfläche</b> (Entscheidungsvorlage Modellgrenzen PW2, BW2): die
/// Verwaltung (<see cref="BetriebskalenderDialog"/>) mit Anlegen, Prüfen, Speichern, Löschen und
/// Abbrechen, der Fall ohne Gaben, und die Wahl je Zuordnung im Bedarfsprofil-Dialog — in den
/// Arbeitsstand der Zeile, geschrieben mit OK; ohne Liste der Hülle keine Gruppe.
/// </summary>
public class BetriebskalenderDialogTests : EposBunitContext
{
    public BetriebskalenderDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =============================================================================
    //  Verwaltung
    // =============================================================================

    private sealed class Ablage
    {
        public readonly List<BetriebskalenderDaten> Saetze = new();
        public int Gespeichert;
        public int Geloescht;

        public IReadOnlyList<BetriebskalenderDaten> Laden() => Saetze.Select(s => s.Kopie()).ToList();

        public string? Speichern(BetriebskalenderDaten d)
        {
            Gespeichert++;
            if (d.Id <= 0) d.Id = Saetze.Count == 0 ? 1 : Saetze.Max(s => s.Id) + 1;
            Saetze.RemoveAll(s => s.Id == d.Id);
            Saetze.Add(d.Kopie());
            return null;
        }

        public string? Loeschen(int id)
        {
            Geloescht++;
            Saetze.RemoveAll(s => s.Id == id);
            return null;
        }
    }

    private static string? Pruefen(BetriebskalenderDaten d)
        => string.IsNullOrWhiteSpace(d.Bezeichner) ? "Name fehlt" : null;

    private IRenderedComponent<BetriebskalenderDialog> Verwaltung(Ablage a, Action<bool>? geschlossen = null)
        => Render<BetriebskalenderDialog>(p => p
            .Add(x => x.Laden, a.Laden)
            .Add(x => x.Laender, new[] { new Bundeslandeintrag("BY", "Bayern"), new Bundeslandeintrag("NW", "Nordrhein-Westfalen") })
            .Add(x => x.Pruefen, Pruefen)
            .Add(x => x.Speichern, a.Speichern)
            .Add(x => x.Loeschen, a.Loeschen)
            .Add(x => x.Verwendungen, _ => 2)
            .Add(x => x.Herleitung, d => "Herleitung " + d.Bezeichner)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<BetriebskalenderDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Ohne_Gaben_zeichnet_die_Verwaltung()
    {
        var cut = Render<BetriebskalenderDialog>();
        Assert.Contains("Betriebskalender", cut.Markup);
        Assert.Contains("noch kein Kalender angelegt", cut.Markup);
        Assert.Equal(8, cut.FindAll(".epos-gemeinjahrdatum").Count);
    }

    [Fact]
    public void Anlegen_mit_OK_schreibt_und_schliesst()
    {
        var a = new Ablage();
        bool? ergebnis = null;
        var cut = Verwaltung(a, b => ergebnis = b);

        cut.Find("input[type=text]").Input("Werk Süd");
        cut.FindAll("select")[1].Change("1");                    // Bayern
        var daten = cut.FindAll(".epos-gemeinjahrdatum input");
        daten[0].Change("01.08.");
        cut.FindAll(".epos-gemeinjahrdatum input")[1].Change("21.08.");
        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
        BetriebskalenderDaten s = Assert.Single(a.Saetze);
        Assert.Equal("Werk Süd", s.Bezeichner);
        Assert.Equal("BY", s.Bundesland);
        Assert.Equal(213, s.Von[0]);
        Assert.Equal(233, s.Bis[0]);
        Assert.True(s.FeiertagWieSonntag);
    }

    /// <summary>KBP-5d: „Ferien wie Sonntag“ steht neben „Feiertag wie Sonntag“, sperrt den Ferienfaktor und nennt es.</summary>
    [Fact]
    public void Ferien_wie_Sonntag_sperrt_den_Ferienfaktor_und_wird_gespeichert()
    {
        var a = new Ablage();
        var cut = Verwaltung(a);
        var schalter = cut.FindAll("label.epos-schalter").Select(l => l.TextContent.Trim()).ToList();
        int feiertag = schalter.IndexOf("Feiertag wie Sonntag");
        Assert.True(feiertag >= 0);
        Assert.Equal(feiertag + 1, schalter.IndexOf("Ferien wie Sonntag"));
        Assert.Empty(cut.FindAll("input[disabled]"));
        Assert.DoesNotContain("Der Ferienfaktor ist gesperrt", cut.Markup);

        cut.Find("input[type=text]").Input("Werk Nord");
        cut.FindAll("label.epos-schalter").First(l => l.TextContent.Trim() == "Ferien wie Sonntag")
           .QuerySelector("input")!.Change(true);

        var gesperrt = Assert.Single(cut.FindAll("input[disabled]"));
        Assert.NotEqual("checkbox", gesperrt.GetAttribute("type"));
        Assert.Contains("Der Ferienfaktor ist gesperrt", cut.Markup);
        Knopf(cut, "OK").Click();
        Assert.True(Assert.Single(a.Saetze).FerienWieSonntag);
    }

    [Fact]
    public void Ein_halber_Zeitraum_und_ein_fehlender_Name_halten_den_Dialog_offen()
    {
        var a = new Ablage();
        bool? ergebnis = null;
        var cut = Verwaltung(a, b => ergebnis = b);

        cut.FindAll(".epos-gemeinjahrdatum input")[0].Change("01.08.");
        Knopf(cut, "OK").Click();
        Assert.Null(ergebnis);
        Assert.Contains("Ferienzeitraum", cut.Instance.Meldung);

        cut.FindAll(".epos-gemeinjahrdatum input")[1].Change("21.08.");
        Knopf(cut, "OK").Click();
        Assert.Null(ergebnis);
        Assert.Equal("Name fehlt", cut.Instance.Meldung);
        Assert.Empty(a.Saetze);
    }

    [Fact]
    public void Loeschen_fragt_mit_der_Zahl_der_Zuordnungen()
    {
        var a = new Ablage();
        a.Saetze.Add(new BetriebskalenderDaten { Id = 7, Bezeichner = "Sommer" });
        var cut = Verwaltung(a);

        Assert.Equal("Sommer", cut.Instance.Satz.Bezeichner);
        Knopf(cut, "Löschen").Click();
        Assert.Contains("„Sommer“", cut.Instance.OffeneFrage);
        Assert.Contains("2 Profilzuordnung", cut.Instance.OffeneFrage);
        Knopf(cut, "Ja").Click();
        Assert.Equal(1, a.Geloescht);
        Assert.Empty(a.Saetze);
        Assert.Equal("Betriebskalender gelöscht.", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_schreibt_nicht()
    {
        var a = new Ablage();
        a.Saetze.Add(new BetriebskalenderDaten { Id = 1, Bezeichner = "A" });
        bool? ergebnis = null;
        var cut = Verwaltung(a, b => ergebnis = b);
        cut.Find("input[type=text]").Input("B");
        Knopf(cut, "Abbrechen").Click();

        Assert.False(ergebnis);
        Assert.Equal(0, a.Gespeichert);
        Assert.Equal("A", a.Saetze[0].Bezeichner);
    }

    // =============================================================================
    //  Wahl je Zuordnung im Bedarfsprofil-Dialog
    // =============================================================================

    private IRenderedComponent<BedarfsProfileDialog> Projektdialog(
        BedarfsArt art, List<BedarfsProfilZeile> zeilen, IReadOnlyList<(int, string)>? kalender, Action? geaendert = null)
        => Render<BedarfsProfileDialog>(p =>
        {
            p.Add(x => x.Art, art)
             .Add(x => x.Zeilen, zeilen)
             .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung", "Typ 1"))
             .Add(x => x.Geaendert, geaendert);
            if (kalender is not null) p.Add(x => x.Betriebskalender, kalender);
        });

    [Theory]
    [InlineData(BedarfsArt.Brauchwasser)]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Stromverbraucher)]
    public void Die_Zuordnung_waehlt_ihren_Kalender(BedarfsArt art)
    {
        var zeile = new BedarfsProfilZeile { IdZ = 1, IdStamm = 3, Name = "Profil", Summe = 10 };
        int geaendert = 0;
        var cut = Projektdialog(art, new List<BedarfsProfilZeile> { zeile },
                                new[] { (4, "Werk"), (9, "Sommer") }, () => geaendert++);

        cut.Find("tbody button, tbody input").Click();              // die Projektzeile wählen
        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();   // AW3b: Bearbeiten… am Satz; Stufe 4: Satz-Überlagerung
        Assert.Contains("Kalender der Zuordnung", cut.Markup);
        IElement wahl = cut.FindAll("select").First(s => s.InnerHtml.Contains("ohne Kalender"));
        wahl.Change("9");
        Assert.Equal(9, zeile.KalenderId);
        Assert.Equal(1, geaendert);

        cut.FindAll("select").First(s => s.InnerHtml.Contains("ohne Kalender")).Change("0");
        Assert.Null(zeile.KalenderId);
    }

    [Fact]
    public void Ohne_Liste_der_Huelle_keine_Gruppe()
    {
        var cut = Projektdialog(BedarfsArt.Prozesswaerme,
                                new List<BedarfsProfilZeile> { new() { IdZ = 1, Name = "P" } }, null);
        Assert.DoesNotContain("Kalender der Zuordnung", cut.Markup);
    }

    // =============================================================================
    //  KBP-1: Aus Gebäude übernehmen (Konzept Kältebedarfsprofile 7.2, OE-6)
    // =============================================================================

    private IRenderedComponent<BetriebskalenderDialog> MitGebaeude(Ablage a, IReadOnlyList<BetriebskalenderGebaeude>? gebaeude,
                                                                   bool titel = true)
        => Render<BetriebskalenderDialog>(p =>
        {
            p.Add(x => x.Laden, a.Laden)
             .Add(x => x.Laender, new[] { new Bundeslandeintrag("BY", "Bayern"), new Bundeslandeintrag("NW", "Nordrhein-Westfalen") })
             .Add(x => x.Pruefen, Pruefen)
             .Add(x => x.Speichern, a.Speichern)
             .Add(x => x.TitelAnzeigen, titel);
            if (gebaeude is not null) p.Add(x => x.Gebaeude, () => gebaeude);
        });

    private static readonly BetriebskalenderGebaeude SCHULE =
        new("Schule", "BY", new[] { (10, 20), (100, 110) });

    private static readonly BetriebskalenderGebaeude HALLE =
        new("Halle", "NW", new[] { (1, 5), (30, 35), (60, 65), (90, 95), (200, 210), (355, 3) });

    [Fact]
    public void Ohne_Gabe_kein_Knopf_aus_Gebaeude()
    {
        var cut = MitGebaeude(new Ablage(), null);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Aus Gebäude übernehmen…");
    }

    [Fact]
    public void Ohne_Gebaeude_ist_der_Knopf_weich_gesperrt_und_nennt_den_Grund()
    {
        var cut = MitGebaeude(new Ablage(), Array.Empty<BetriebskalenderGebaeude>());
        IElement knopf = Knopf(cut, "Aus Gebäude übernehmen…");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Das Projekt hat kein Gebäude — es gibt nichts zu übernehmen.", knopf.GetAttribute("title"));
        Assert.False(knopf.HasAttribute("disabled"));   // weiche Sperre: der Tooltip erscheint

        knopf.Click();
        Assert.False(cut.Instance.GebaeudewahlOffen);
        Assert.Equal("Das Projekt hat kein Gebäude — es gibt nichts zu übernehmen.", cut.Instance.Meldung);
    }

    [Fact]
    public void Aus_Gebaeude_uebernimmt_Land_und_Ferien_als_Kopie_in_den_Arbeitsstand()
    {
        var a = new Ablage();
        a.Saetze.Add(new BetriebskalenderDaten { Id = 1, Bezeichner = "Werk", Bundesland = "NW",
                                                 Von = new int?[] { 300, 50, 70, 80 }, Bis = new int?[] { 310, 55, 75, 85 } });
        var cut = MitGebaeude(a, new[] { HALLE, SCHULE });
        Knopf(cut, "Aus Gebäude übernehmen…").Click();
        Assert.True(cut.Instance.GebaeudewahlOffen);
        Assert.Contains("Schule — Bayern, 2 Ferienzeiträume", cut.Markup);

        // OK ohne Wahl nennt den Weg und lässt die Wahl offen
        cut.FindAll(".epos-kalender-gebaeudewahl button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.Equal("Bitte ein Gebäude wählen.", cut.Instance.Meldung);

        cut.FindAll(".epos-kalender-gebaeudeliste button")[1].Click();      // Schule
        cut.FindAll(".epos-kalender-gebaeudewahl button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.GebaeudewahlOffen);
        BetriebskalenderDaten s = cut.Instance.Satz;
        Assert.Equal("BY", s.Bundesland);
        Assert.Equal(new int?[] { 10, 100, null, null }, s.Von);
        Assert.Equal(new int?[] { 20, 110, null, null }, s.Bis);
        Assert.Equal(1, s.Id);                                   // derselbe Kalender, keine Herkunft am Satz
        Assert.Contains("„Schule“ übernommen", cut.Instance.Meldung);
        Assert.Equal(0, a.Gespeichert);                          // geschrieben wird erst mit Speichern oder OK
        Assert.Equal("NW", a.Saetze[0].Bundesland);

        Knopf(cut, "OK").Click();
        Assert.Equal(1, a.Gespeichert);
        Assert.Equal("BY", a.Saetze[0].Bundesland);
        Assert.Equal(new int?[] { 10, 100, null, null }, a.Saetze[0].Von);
    }

    [Fact]
    public void Mehr_als_vier_Zeitraeume_uebernehmen_die_ersten_vier_und_nennen_die_fehlenden()
    {
        var cut = MitGebaeude(new Ablage(), new[] { HALLE });
        Knopf(cut, "Aus Gebäude übernehmen…").Click();
        // ein einziges Gebäude ist vorgewählt
        cut.FindAll(".epos-kalender-gebaeudewahl button").First(b => b.TextContent.Trim() == "OK").Click();

        BetriebskalenderDaten s = cut.Instance.Satz;
        Assert.Equal("NW", s.Bundesland);
        Assert.Equal(new int?[] { 1, 30, 60, 90 }, s.Von);
        Assert.Equal(new int?[] { 5, 35, 65, 95 }, s.Bis);
        Assert.Contains("Das Gebäude führt 6 Ferienzeiträume", cut.Instance.Meldung);
        Assert.Contains("2 fehlen", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_und_Esc_der_Gebaeudewahl_lassen_den_Arbeitsstand_stehen()
    {
        bool? geschlossen = null;
        var a = new Ablage();
        var cut = Render<BetriebskalenderDialog>(p => p
            .Add(x => x.Laden, a.Laden)
            .Add(x => x.Gebaeude, () => new[] { SCHULE })
            .Add(x => x.Geschlossen, b => geschlossen = b));
        Knopf(cut, "Aus Gebäude übernehmen…").Click();
        cut.FindAll(".epos-kalender-gebaeudewahl button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(cut.Instance.GebaeudewahlOffen);
        Assert.Null(cut.Instance.Satz.Von[0]);

        Knopf(cut, "Aus Gebäude übernehmen…").Click();
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.False(cut.Instance.GebaeudewahlOffen);
        Assert.Null(geschlossen);                                // Esc schloss nur die Wahl
        Assert.Null(cut.Instance.Satz.Bundesland);
    }

    [Fact]
    public void Im_Blatt_traegt_der_Dialog_weder_Titel_noch_Kreuz()
    {
        var cut = MitGebaeude(new Ablage(), null, titel: false);
        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));
        Assert.Contains("epos-dialog-kopf--ohnetitel", cut.Find(".epos-dialog-kopf").ClassName);
        Assert.Empty(cut.FindAll(".epos-dialog-kopf .epos-schliesskreuz"));
    }

    // =============================================================================
    //  KBP-1: Baustein Betriebskalenderwahl im Bedarfsprofil-Dialog, Blatt „Betriebskalender…“
    // =============================================================================

    private IRenderedComponent<BedarfsProfileDialog> MitBlatt(BedarfsArt art, List<BedarfsProfilZeile> zeilen, Ablage a)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung", "Typ 1"))
            .Add(x => x.Betriebskalender, (IReadOnlyList<(int, string)>)a.Saetze.Select(s => (s.Id, s.Bezeichner)).ToList())
            .Add(x => x.BetriebskalenderNeuLesen, () => a.Saetze.OrderBy(s => s.Bezeichner).Select(s => (s.Id, s.Bezeichner)).ToList())
            .Add(x => x.BetriebskalenderGaben, () => new Dictionary<string, object>
            {
                ["Laden"] = new Func<IReadOnlyList<BetriebskalenderDaten>>(a.Laden),
                ["Pruefen"] = new Func<BetriebskalenderDaten, string?>(Pruefen),
                ["Speichern"] = new Func<BetriebskalenderDaten, string?>(a.Speichern)
            }));

    private static void SatzOeffnen(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        cut.Find("tbody button, tbody input").Click();
        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();
    }

    [Theory]
    [InlineData(BedarfsArt.Kaelte)]
    [InlineData(BedarfsArt.Brauchwasser)]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Stromverbraucher)]
    public void Das_Blatt_legt_einen_Kalender_an_und_waehlt_ihn_fuer_die_Zeile_vor(BedarfsArt art)
    {
        var a = new Ablage();
        a.Saetze.Add(new BetriebskalenderDaten { Id = 4, Bezeichner = "Werk" });
        var zeile = new BedarfsProfilZeile { IdZ = 1, IdStamm = 3, Name = "Profil", Summe = 10 };
        var cut = MitBlatt(art, new List<BedarfsProfilZeile> { zeile }, a);
        SatzOeffnen(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Betriebskalender…").Click();
        Assert.True(cut.Instance.KalenderblattOffen);
        Assert.NotNull(cut.Find(".epos-blatt"));
        Assert.Empty(cut.FindAll(".epos-zweispalten"));          // der Inhalt ist getauscht, keine dritte Ebene

        cut.FindAll(".epos-blatt button").First(b => b.TextContent.Trim() == "Neu").Click();
        cut.Find(".epos-blatt input[type=text]").Input("Sommerpause");
        cut.FindAll(".epos-blatt button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.KalenderblattOffen);
        Assert.Contains((5, "Sommerpause"), cut.Instance.KalenderListe!);
        Assert.Equal(5, zeile.KalenderId);                       // vorgewählt, in den Arbeitsstand
        cut.WaitForAssertion(() => Assert.True(cut.Instance.SatzUeberlagerungOffen));
        Assert.Contains("Sommerpause", cut.FindAll("select").First(s => s.InnerHtml.Contains("ohne Kalender")).InnerHtml);
    }

    [Fact]
    public void Rueckknopf_des_Blattes_liest_neu_und_waehlt_nichts_vor()
    {
        var a = new Ablage();
        var zeile = new BedarfsProfilZeile { IdZ = 1, IdStamm = 3, Name = "Profil", Summe = 10 };
        var cut = MitBlatt(BedarfsArt.Kaelte, new List<BedarfsProfilZeile> { zeile }, a);
        SatzOeffnen(cut);
        Assert.Contains("Noch kein Betriebskalender angelegt.", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Betriebskalender…").Click();
        cut.Find(".epos-blatt input[type=text]").Input("Neu angelegt");
        cut.FindAll(".epos-blatt button").First(b => b.TextContent.Trim() == "Speichern").Click();
        cut.Find(".epos-blatt-zurueck").Click();

        Assert.False(cut.Instance.KalenderblattOffen);
        Assert.Single(cut.Instance.KalenderListe!);
        Assert.Null(zeile.KalenderId);
    }
}
