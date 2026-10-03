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
        daten[0].Input("01.08.");
        cut.FindAll(".epos-gemeinjahrdatum input")[1].Input("21.08.");
        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
        BetriebskalenderDaten s = Assert.Single(a.Saetze);
        Assert.Equal("Werk Süd", s.Bezeichner);
        Assert.Equal("BY", s.Bundesland);
        Assert.Equal(213, s.Von[0]);
        Assert.Equal(233, s.Bis[0]);
        Assert.True(s.FeiertagWieSonntag);
    }

    [Fact]
    public void Ein_halber_Zeitraum_und_ein_fehlender_Name_halten_den_Dialog_offen()
    {
        var a = new Ablage();
        bool? ergebnis = null;
        var cut = Verwaltung(a, b => ergebnis = b);

        cut.FindAll(".epos-gemeinjahrdatum input")[0].Input("01.08.");
        Knopf(cut, "OK").Click();
        Assert.Null(ergebnis);
        Assert.Contains("Ferienzeitraum", cut.Instance.Meldung);

        cut.FindAll(".epos-gemeinjahrdatum input")[1].Input("21.08.");
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

        Assert.Contains("Kalender der Zuordnung", cut.Markup);
        cut.Find("tbody button, tbody input").Click();              // die Projektzeile wählen
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
}
