using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „Projekt Speichern unter" (iU9-W15a.4) — Soll ist die Feldkarte von
/// <c>Form_ProjektSpeichernUnter</c>: Liste, vier Eingabefelder, Fortschritt,
/// OK/Abbrechen und Hilfeknopf.
///
/// <para>Geprueft werden vor allem die vier Angleichungen der Welle: die RICHTIGE
/// Dublettenpruefung (A-4), der abbrechbare Kopierlauf (A-2), der Doppelklick, der
/// nur noch markiert (A-6), und die Fertig-Anzeige (A-5).</para>
/// </summary>
public class ProjektKopieDialogTests : BunitContext
{
    private static readonly ProjektKopfZeile[] ZWEI =
    {
        new ProjektKopfZeile(1030, "Musterprojekt", "Stadtwerke", "Kaskade", new DateTime(2026, 3, 1)),
        new ProjektKopfZeile(1007, "Zweitprojekt", "Kirchengemeinde", "Denkmal", new DateTime(2026, 5, 4))
    };

    public ProjektKopieDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    /// <summary>Der Kern-Ersatz: er merkt sich, womit er gerufen wurde.</summary>
    private sealed class Kern
    {
        public string Quelle = "", Neu = "";
        public int Ergebnis = 4711;
        public bool Abgebrochen;
        public List<KopierStand> Meldungen = new();
        public string FelderNeu = "", FelderBeschreibung = "", FelderKunde = "", FelderBearbeiter = "";
        public VerwaltungsfelderBefund FelderBefund = VerwaltungsfelderBefund.Ok;
        public string FelderFehler = "";
        public bool Gerufen;

        public DuplizierBefund Pruefung { get; set; } = DuplizierBefund.Ok;

        public DuplizierBefund Pruefen(string quelle, string neu) => Pruefung;

        public Task<int> Duplizieren(string quelle, string neu,
                                     IProgress<KopierStand> melder, CancellationToken abbruch)
        {
            Gerufen = true;
            Quelle = quelle;
            Neu = neu;
            melder?.Report(new KopierStand(0, 3, "Tab_Projekt"));
            if (abbruch.IsCancellationRequested) { Abgebrochen = true; return Task.FromResult(-1); }
            return Task.FromResult(Ergebnis);
        }

        public VerwaltungsfelderErgebnis Felder(string neu, string beschreibung, string kunde, string bearbeiter)
        {
            FelderNeu = neu; FelderBeschreibung = beschreibung;
            FelderKunde = kunde; FelderBearbeiter = bearbeiter;
            return new VerwaltungsfelderErgebnis(FelderBefund, FelderFehler);
        }
    }

    private IRenderedComponent<ProjektKopieDialog> Aufbauen(
        Kern kern, Action<ComponentParameterCollectionBuilder<ProjektKopieDialog>>? mehr = null)
        => Render<ProjektKopieDialog>(p =>
        {
            p.Add(x => x.Zeilen, ZWEI);
            p.Add(x => x.Quellfelder, (Func<string, ProjektKopfDaten?>)(name =>
                new ProjektKopfDaten { Name = name, Beschreibung = "B von " + name, Kunde = "K", Bearbeiter = "M" }));
            p.Add(x => x.Pruefen, (Func<string, string, DuplizierBefund>)kern.Pruefen);
            p.Add(x => x.Duplizieren,
                  (Func<string, string, IProgress<KopierStand>, CancellationToken, Task<int>>)kern.Duplizieren);
            p.Add(x => x.Verwaltungsfelder,
                  (Func<string, string, string, string, VerwaltungsfelderErgebnis>)kern.Felder);
            p.Add(x => x.FertigAnzeigeMs, 0);
            mehr?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement Ok(IRenderedComponent<ProjektKopieDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste .epos-knopf--primaer")[0];

    private static AngleSharp.Dom.IElement Feld(IRenderedComponent<ProjektKopieDialog> cut, int nummer)
        => cut.FindAll(".epos-projektkopie-felder input, .epos-projektkopie-felder textarea")[nummer];

    [Fact]
    public void Der_Dialog_zeigt_die_Liste_und_die_vier_Felder()
    {
        var cut = Aufbauen(new Kern());

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Equal(4, cut.FindAll(".epos-projektkopie-felder input, .epos-projektkopie-felder textarea").Count);
        Assert.Single(cut.FindAll(".epos-projektkopie-felder textarea"));   // Beschreibung mehrzeilig
        Assert.Empty(cut.FindAll(".epos-fortschritt"));
    }

    [Fact]
    public void Die_Auswahl_belegt_Beschreibung_Kunde_und_Bearbeiter_der_QUELLE_vor()
    {
        var cut = Aufbauen(new Kern());

        // Vorauswahl: die erste Zeile (sortiert: Musterprojekt).
        Assert.Equal("B von Musterprojekt", cut.Find(".epos-projektkopie-felder textarea").TextContent);

        cut.FindAll("tbody .epos-anlagenwahl")[1].Click();                 // Zweitprojekt
        Assert.Equal("B von Zweitprojekt", cut.Find(".epos-projektkopie-felder textarea").TextContent);
    }

    [Fact]
    public void Ein_leerer_Name_meldet_und_startet_nichts()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);

        Ok(cut).Click();

        Assert.False(kern.Gerufen);
        Assert.Contains("Bitte einen neuen Projektnamen eingeben.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_belegter_Name_meldet_ueber_die_KERN_Pruefung_nicht_ueber_eine_Praefixsuche()
    {
        var kern = new Kern { Pruefung = DuplizierBefund.ZielExistiert };
        var cut = Aufbauen(kern);

        Feld(cut, 0).Input("Zweitprojekt");
        Ok(cut).Click();

        Assert.False(kern.Gerufen);
        Assert.Contains("Projektname bereits vorhanden!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_Name_der_nur_der_Anfang_eines_vorhandenen_ist_wird_ZUGELASSEN()
    {
        // Befund W15a-B10: "Muster" traf im Vorlaeufer "Musterprojekt" (FindItemWithText,
        // Praefix-Semantik) und wurde abgelehnt, obwohl der Name frei ist.
        var kern = new Kern { Pruefung = DuplizierBefund.Ok };
        bool geschlossen = false;
        var cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => geschlossen = b));

        Feld(cut, 0).Input("Muster");
        Ok(cut).Click();

        Assert.True(kern.Gerufen);
        Assert.Equal("Muster", kern.Neu);
        Assert.True(geschlossen);
    }

    [Fact]
    public void Der_Kopierlauf_meldet_seinen_Fortschritt_und_schreibt_danach_die_drei_Felder()
    {
        var kern = new Kern();
        bool geschlossen = false;
        var cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => geschlossen = b));

        Feld(cut, 0).Input("Kopie");
        Feld(cut, 1).Input("Neue Beschreibung");
        Feld(cut, 2).Input("Neuer Kunde");
        Feld(cut, 3).Input("Neuer Bearbeiter");
        Ok(cut).Click();

        Assert.Equal("Musterprojekt", kern.Quelle);
        Assert.Equal("Kopie", kern.Neu);
        Assert.Equal("Kopie", kern.FelderNeu);
        Assert.Equal("Neue Beschreibung", kern.FelderBeschreibung);
        Assert.Equal("Neuer Kunde", kern.FelderKunde);
        Assert.Equal("Neuer Bearbeiter", kern.FelderBearbeiter);
        Assert.True(geschlossen);
    }

    [Fact]
    public void Ein_Fehler_beim_Schreiben_der_Felder_meldet_und_schliesst_trotzdem()
    {
        // Die Fehlerpolitik des Vorlaeufers, woertlich: gemeldet, aber NICHT
        // zurueckgerollt - die Kopie steht bereits (R-W15a-11).
        var kern = new Kern { FelderBefund = VerwaltungsfelderBefund.NichtGespeichert };
        bool geschlossen = false;
        var cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => geschlossen = b));

        Feld(cut, 0).Input("Kopie");
        Ok(cut).Click();

        Assert.True(geschlossen);
    }

    [Fact]
    public void Ein_gescheiterter_Lauf_laesst_den_Dialog_offen()
    {
        var kern = new Kern { Ergebnis = -1 };
        bool gerufen = false;
        var cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => gerufen = true));

        Feld(cut, 0).Input("Kopie");
        Ok(cut).Click();

        Assert.False(gerufen);
        Assert.Empty(cut.FindAll(".epos-fortschritt"));
    }

    [Fact]
    public void Ein_Doppelklick_markiert_nur_und_startet_keinen_Lauf()
    {
        // A-6: Der Vorlaeufer rief button_Open.PerformClick() (Befund W15a-B13).
        var kern = new Kern();
        var cut = Aufbauen(kern);

        cut.FindAll("tbody tr")[1].DoubleClick();

        Assert.False(kern.Gerufen);
        Assert.Equal("Zweitprojekt", cut.Instance.Quelle);
        Assert.Equal("B von Zweitprojekt", cut.Find(".epos-projektkopie-felder textarea").TextContent);
    }

    [Fact]
    public void Abbrechen_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(new Kern(), p => p.Add(x => x.Geschlossen, (bool b) => ergebnis = b));

        cut.FindAll(".epos-dialog > .epos-leiste button")
           .First(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();

        Assert.False(ergebnis);
    }

    [Fact]
    public void Ohne_Kopierdelegat_geschieht_nichts()
    {
        var cut = Render<ProjektKopieDialog>(p => p.Add(x => x.Zeilen, ZWEI));

        Feld(cut, 0).Input("Kopie");
        Ok(cut).Click();

        Assert.Empty(cut.FindAll(".epos-fortschritt"));
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Die vier Eingabefelder stehen im Formularraster, einspaltig: Sie sitzen in der schmalen rechten Spalte neben der Quellliste.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Die_Eingabefelder_stehen_im_einspaltigen_Formularraster()
    {
        var cut = Aufbauen(new Kern());

        Assert.Single(cut.FindAll(
            ".epos-projektkopie-felder .epos-formularraster--einspaltig"));
        Assert.Equal(4, cut.FindAll(".epos-formularraster .epos-feld").Count);
    }
}
