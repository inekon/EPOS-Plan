using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// DIE ZWEI BLÖCKE, DIE MIT AUFTRAG #224 AUS DEM EINHEITENEDITOR AUSGEZOGEN SIND
/// (Anwenderentscheid SD‑E‑9, Option A; Konzept „Stromspeicher-Dialoge" 7.4):
///
/// <list type="bullet">
/// <item><c>SpeicherFlottenNetzBlock</c> — „Netz und Planung", jetzt Schritt 3
/// „Betriebsführung". Bezugs- und Einspeisegrenze sind HARTE Grenzen des Anschlusses,
/// die Prognoseplanung sagt, mit welchem Wissen ein Fahrplan entsteht: Beides
/// beschreibt den Betrieb der Flotte, nicht eine Einheit.</item>
/// <item><c>SpeicherFlottenWirtschaftBlock</c> — die wirtschaftliche Jahresprojektion,
/// jetzt Schritt 2 „Daten &amp; Kosten" (SD‑Q12). Die Wiki-Seite ordnet sie seit jeher
/// dorthin; Programm und Doku widersprachen sich (Konzept 7.2).</item>
/// </list>
///
/// <para>Die Prüffälle hier sind die MITGEWANDERTEN aus
/// <c>SpeicherFlottenEditorTests</c> — dieselben Felder, geprüft an ihrem neuen Ort.</para>
/// </summary>
public sealed class SpeicherFlottenBloeckeTests : EposBunitContext
{
    // =====================================================================
    //  Netz und Planung
    // =====================================================================

    /// <summary>Die zwei Anschlussgrenzen schreiben in die Betriebsoptionen.</summary>
    [Fact]
    public void Die_Anschlussgrenzen_schreiben_in_die_Optionen()
    {
        var optionen = new FlottenSimulationOptionen();
        int gemeldet = 0;
        var cut = Render<SpeicherFlottenNetzBlock>(p => p
            .Add(x => x.Wert, optionen)
            .Add(x => x.Geaendert, () => gemeldet++));

        Eingabe(cut, Resource.FLOTTE_ED_BEZUGSGRENZE).Input("120");
        Eingabe(cut, Resource.FLOTTE_ED_EINSPEISEGRENZE).Input("80");

        Assert.Equal(120.0, optionen.NetzbezugGrenzeKw!.Value, 9);
        Assert.Equal(80.0, optionen.NetzeinspeisungGrenzeKw!.Value, 9);
        Assert.Equal(2, gemeldet);
    }

    /// <summary>
    /// Die PROGNOSEPLANUNG erscheint nur bei einem planenden Betriebsziel — ohne Planer
    /// gibt es keinen Planungshorizont, und ein Feld ohne Wirkung ist eine Behauptung.
    /// (Der mitgewanderte Fall aus <c>MultiUse_zeigt_Peak_und_Prognoseparameter</c>.)
    /// </summary>
    [Fact]
    public void MultiUse_zeigt_die_Prognoseparameter_ein_reaktives_Ziel_nicht()
    {
        var optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.MultiUse
        };
        var cut = Render<SpeicherFlottenNetzBlock>(p => p.Add(x => x.Wert, optionen));

        Assert.True(cut.Instance.HatPrognoseplanung);
        Assert.Contains(Resource.FLOTTE_ED_INFORMATIONSSTAND, cut.Markup);

        Auswahl(cut, Resource.FLOTTE_ED_INFORMATIONSSTAND)
            .Change(((int)PrognoseArt.Oracle).ToString());
        Eingabe(cut, Resource.FLOTTE_ED_PLANUNGSHORIZONT).Input("96");

        Assert.Equal(PrognoseArt.Oracle, optionen.PrognoseArt);
        Assert.Equal(96, optionen.PlanungshorizontIntervalle);
        Assert.Contains("Vergleichslauf", cut.Markup);

        var reaktiv = Render<SpeicherFlottenNetzBlock>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen
            { Betriebsziel = FlottenBetriebsziel.PeakShaving }));

        Assert.False(reaktiv.Instance.HatPrognoseplanung);
        Assert.DoesNotContain(Resource.FLOTTE_ED_INFORMATIONSSTAND, reaktiv.Markup);
    }

    /// <summary>
    /// Die ENDENERGIEZIELE tragen die Namen der Einheiten — deshalb bekommt der Block
    /// sie herein.
    /// </summary>
    [Fact]
    public void Die_Endenergieziele_nennen_die_Einheiten_beim_Namen()
    {
        var optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.Arbitrage,
            Endbedingung = FlottenEndbedingung.JeSpeicherZiel,
            EndenergieZielKWh = new List<double> { 10, 20 }
        };
        var cut = Render<SpeicherFlottenNetzBlock>(p => p
            .Add(x => x.Wert, optionen)
            .Add(x => x.Einheiten, new List<FlottenEinheit>
            {
                new() { Id = "a", Name = "Halle" }, new() { Id = "b", Name = "Werkstatt" }
            }));

        Assert.Contains("Halle", cut.Markup);
        Assert.Contains("Werkstatt", cut.Markup);
        Assert.Equal(2, cut.FindAll(".epos-flotte-endziele label").Count);
    }

    // =====================================================================
    //  Die wirtschaftliche Jahresprojektion
    // =====================================================================

    /// <summary>
    /// Der mitgewanderte Fall <c>Projektlaufzeit_ist_immer_sichtbar</c>: Zins,
    /// Projektionsart und Laufzeit stehen da und schreiben in den Studienstand.
    /// </summary>
    [Fact]
    public void Zins_Projektionsart_und_Laufzeit_schreiben_in_die_Studie()
    {
        var flotte = new FlottenStudieKonfiguration();
        var cut = Render<SpeicherFlottenWirtschaftBlock>(p => p.Add(x => x.Wert, flotte));

        Assert.Contains("Ein bereitgestelltes Jahr kann einmal bewertet", cut.Markup);

        Eingabe(cut, Resource.FLOTTE_ED_PROJEKTJAHRE).Input("1");
        Assert.False(flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(1, flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);

        Auswahl(cut, Resource.FLOTTE_ED_PROJEKTIONSART).Change("1");
        Eingabe(cut, Resource.FLOTTE_ED_PROJEKTJAHRE).Input("15");
        Eingabe(cut, Resource.FLOTTE_ED_ZINS).Input("4,5");

        Assert.True(flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(15, flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        Assert.Equal(0.045, flotte.Wirtschaftlichkeit.Kalkulationszins, 9);
    }

    /// <summary>
    /// DIE DREI EXPERTENFELDER tragen je eine Erklärzeile aus Konzept 7.3 — bis #224
    /// standen sie ohne jede Erklärung neben Zins und Laufzeit.
    /// </summary>
    [Fact]
    public void Die_drei_Expertenfelder_tragen_je_eine_Erklaerzeile()
    {
        var cut = Render<SpeicherFlottenWirtschaftBlock>(p => p
            .Add(x => x.Wert, new FlottenStudieKonfiguration()));

        Assert.Equal(3, cut.FindAll("p.epos-herleitung").Count);
        Assert.Contains(Resource.FLOTTE_ED_AUSGLEICH_ERL, cut.Markup);
        Assert.Contains(Resource.FLOTTE_ED_RESTWERT_ERL, cut.Markup);
        Assert.Contains(Resource.FLOTTE_ED_KANDIDATEN_ERL, cut.Markup);
    }

    /// <summary>
    /// Der Ausgleichswert und die Kandidatengrenze schreiben in die Optionen bzw. den
    /// Suchraum — dieselben Modellfelder wie bis #224 im Einheiteneditor.
    /// </summary>
    [Fact]
    public void Ausgleichswert_und_Kandidatengrenze_schreiben_in_ihre_Felder()
    {
        var flotte = new FlottenStudieKonfiguration();
        var cut = Render<SpeicherFlottenWirtschaftBlock>(p => p.Add(x => x.Wert, flotte));

        Eingabe(cut, Resource.FLOTTE_ED_AUSGLEICH).Input("0,28");
        Eingabe(cut, Resource.FLOTTE_ED_RESTWERT_GESAMT).Input("5000");
        Eingabe(cut, Resource.FLOTTE_ED_MAX_KANDIDATEN).Input("500");

        Assert.Equal(0.28, flotte.Optionen.EnergieAusgleichEuroProKWh!.Value, 9);
        Assert.Equal(5000.0, flotte.Wirtschaftlichkeit.RestwertEuro, 9);
        Assert.Equal(500, flotte.Auslegung.MaximaleKandidaten);
    }

    // ================================================================= Prüfstand

    private static AngleSharp.Dom.IElement Eingabe<T>(IRenderedComponent<T> cut, string label)
        where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Auswahl<T>(IRenderedComponent<T> cut, string label)
        where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;
}
