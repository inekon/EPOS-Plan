using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// BEFUND #185 — „Im Dialog fehlen die Investitionskoeffizienten“.
///
/// <para>Der Anwenderbefund vom 11.09.2026: Ein Projektlauf mit aktivierter Speicherflotte
/// bricht vollständig ab, weil <c>SpeicherAuslegungCtrl.KostenAufloesen</c> spezifische
/// Kostensätze verlangt — auch dort, wo sie niemand braucht. Die Prüffälle halten die drei
/// Ebenen der Behebung fest:</para>
/// <list type="number">
/// <item>Der PROJEKTLAUF rechnet ohne Kostensätze weiter und meldet „nicht bewertbar“.</item>
/// <item>Der STUDIENLAUF bricht weiterhin ab — aber mit dem Ausweg im Text.</item>
/// <item>Einheiten mit EIGENEN Kosten brauchen überhaupt keine Sätze.</item>
/// </list>
/// </summary>
[Collection("Testdatenbank")]
public sealed class SpeicherFlottenProjektKostenTests
{
    private const int Projekt = 987654322;

    // =====================================================================
    //  1. Die Reproduktion — der Weg des Anwenders
    // =====================================================================

    /// <summary>
    /// Die REPRODUKTION: Ein Lauf-Snapshot mit <c>Investitionsquelle = Dialog</c>, dessen
    /// Dialogsätze nicht gepflegt sind, warf bis #185 genau die gemeldete Ausnahme.
    /// Seither rechnet der Projektlauf, und der Hinweis nennt die fehlende Bewertung.
    /// </summary>
    [Fact]
    public void Projektlauf_ohne_gepflegte_Dialogsaetze_rechnet_und_meldet_nicht_bewertbar()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        SimulationControl sim = ProjektSimulation(7);
        SpeicherOptimierungEingaben eingaben = Eingaben();
        // Genau der Stand aus dem Befund: Quelle „Dialog“, aber keine Sätze — so legt ihn
        // SpeicherAuslegungCtrl.Vorbelegung an, wenn das Gerät keine Kostendaten trägt.
        eingaben.Auslegung.DirekteKosten = new SpeicherKostensaetze();
        sim.SpeicherflottenEingaben = eingaben;

        double[] rueckgabe = sim.SpeicherlaufAusfuehren(Projekt);

        SpeicherFlottenProjektLauf lauf = Assert.IsType<SpeicherFlottenProjektLauf>(sim.Speicherflottenlauf);
        Assert.Equal(35040, rueckgabe.Length);
        Assert.True(lauf.Studie.Variante.Zulaessig);
        Assert.False(lauf.KostenBewertbar);
        Assert.Contains("nicht bewertbar", lauf.Hinweis);
        Assert.True(lauf.Eingaben.Auslegung.VerwendeteKosten.NichtBewertbar);
        Assert.Equal(0.0, lauf.Eingaben.Auslegung.VerwendeteKosten.InvestEurProKwh);
        // Das Betriebsergebnis ist vollständig da — daran hängt der übrige Projektlauf.
        Assert.Equal(35040, lauf.NetzleistungKw.Length);
        Assert.Equal(7, lauf.Studie.ReferenzOhneSpeicher.Intervalle[0].LastKw, 10);
    }

    /// <summary>Gegenprobe: Mit gepflegten Sätzen bleibt der Lauf bewertbar.</summary>
    [Fact]
    public void Projektlauf_mit_gepflegten_Saetzen_bleibt_bewertbar()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        SimulationControl sim = ProjektSimulation(7);
        sim.SpeicherflottenEingaben = Eingaben();

        sim.SpeicherlaufAusfuehren(Projekt);

        SpeicherFlottenProjektLauf lauf = Assert.IsType<SpeicherFlottenProjektLauf>(sim.Speicherflottenlauf);
        Assert.True(lauf.KostenBewertbar);
        Assert.DoesNotContain("nicht bewertbar", lauf.Hinweis);
        Assert.True(lauf.Eingaben.Auslegung.VerwendeteKosten.InvestVorhanden);
        Assert.False(lauf.Eingaben.Auslegung.VerwendeteKosten.NichtBewertbar);
    }

    // =====================================================================
    //  2a. KostenAufloesen — nur verlangen, was gebraucht wird
    // =====================================================================

    /// <summary>Der Studienlauf bricht weiterhin ab — jetzt mit dem Ausweg im Text.</summary>
    [Fact]
    public void Studienlauf_ohne_gebrauchte_Saetze_wirft_mit_Ausweg()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.DirekteKosten = new SpeicherKostensaetze();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Vorbereiten(e, KostenPflicht.Studienlauf));

        Assert.Contains("Im Dialog fehlen die Investitionskoeffizienten", ex.Message);
        Assert.Contains("eigene Kosten", ex.Message);
        Assert.Contains("Daten, Kosten", ex.Message);
    }

    /// <summary>Auch die Betriebskosten nennen ihren Ausweg.</summary>
    [Fact]
    public void Studienlauf_ohne_Betriebssaetze_wirft_mit_Ausweg()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.DirekteKosten.BetriebVorhanden = false;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Vorbereiten(e, KostenPflicht.Studienlauf));

        Assert.Contains("Betriebskostenkoeffizienten", ex.Message);
        Assert.Contains("eigene Kosten", ex.Message);
    }

    /// <summary>
    /// Trägt JEDE Einheit eigene Kosten, braucht der Lauf keine spezifischen Sätze —
    /// auch der Studienlauf nicht. <c>SpeicherFlottenStudieCtrl.Konfiguration</c>
    /// überschreibt genau diese Einheiten nicht.
    /// </summary>
    [Fact]
    public void Einheiten_mit_eigenen_Kosten_brauchen_keine_Saetze()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.DirekteKosten = new SpeicherKostensaetze();
        e.Auslegung.Flotte.Einheiten[0].EigeneKosten = true;
        e.Auslegung.Flotte.Einheiten[0].InvestitionEuro = 15000;

        StromspeicherOptimierungVorbereitung v = Vorbereiten(e, KostenPflicht.Studienlauf);

        SpeicherKostensaetze k = v.Eingaben.Auslegung.VerwendeteKosten;
        Assert.False(k.NichtBewertbar);
        Assert.Equal(0.0, k.InvestEurProKw);
        Assert.Contains("je Einheit", k.Herkunft);
        // Und die eigene Pauschale der Einheit überlebt die Kostenzuordnung.
        FlottenStudieKonfiguration config = SpeicherFlottenStudieCtrl.Konfiguration(v.Eingaben);
        Assert.Equal(15000, config.Einheiten[0].InvestitionEuro, 10);
    }

    /// <summary>
    /// Eine EINZELANLAGE ohne Flotte braucht die Sätze weiterhin: sie werden zu
    /// <c>CCapEurProKwh</c>/<c>CPowEurProKw</c> des Parametersatzes.
    /// </summary>
    [Fact]
    public void Ohne_Flotte_bleiben_die_Saetze_Pflicht()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.Flotte = null;
        e.Auslegung.DirekteKosten = new SpeicherKostensaetze();

        Assert.True(SpeicherAuslegungCtrl.SpezifischeSaetzeGebraucht(e.Auslegung));
        Assert.Throws<InvalidOperationException>(() => Vorbereiten(e, KostenPflicht.Studienlauf));
    }

    /// <summary>Der Kostenmodul-Zweig behält seinen bisherigen Wortlaut samt Ausweg.</summary>
    [Fact]
    public void Fehlendes_Kostenmodul_nennt_weiterhin_die_fehlende_Kategorie()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.Betriebsquelle = SpeicherKostenQuelle.Kostenmodul;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                Epos(), Basis(), new StromspeicherLaufKontext { Parameter = Basis() },
                e, new SpeicherKostensaetze { InvestVorhanden = true, BetriebVorhanden = false },
                0.0, null, null, KostenPflicht.Studienlauf));

        Assert.Contains("keine verwendbaren Betriebskosten", ex.Message);
    }

    // =====================================================================
    //  2b. Aktivieren — der Stand @Projektflotte trägt die Sätze
    // =====================================================================

    /// <summary>
    /// Ein Aufrufer, der den ROHEN Dialogstand übergibt (ohne aufgelöste
    /// <c>VerwendeteKosten</c>), hinterließ bis #185 einen unvollständigen
    /// <c>@Projektflotte</c>. Seither zieht <c>Aktivieren</c> die Sätze selbst nach.
    /// </summary>
    [Fact]
    public void Aktivieren_traegt_die_Kostensaetze_in_den_Projektflottenstand()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        Sql("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)",
            new DbParam("@p", Projekt), new DbParam("@n", "Befund 185"));

        SpeicherFlottenErgebnis ergebnis = Aktivierungsergebnis(mitAufgeloestenKosten: false);
        SpeicherFlottenProjektCtrl.Aktivieren(Projekt, ergebnis);

        SpeicherAuslegungKonfiguration stand = SpeicherAuslegungCtrl.Profile(Projekt, 0)
            .Single(x => x.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand)
            .Eingaben.Auslegung;
        Assert.True(stand.FlotteImProjektAktiv);
        Assert.Equal(SpeicherKostenQuelle.Dialog, stand.Investitionsquelle);
        Assert.NotNull(stand.VerwendeteKosten);
        Assert.True(stand.VerwendeteKosten.InvestVorhanden);
        Assert.True(stand.VerwendeteKosten.BetriebVorhanden);
        Assert.False(stand.VerwendeteKosten.NichtBewertbar);
        Assert.Equal(200.0, stand.VerwendeteKosten.InvestEurProKw, 10);
        Assert.Equal(350.0, stand.VerwendeteKosten.InvestEurProKwh, 10);
    }

    /// <summary>Bereits aufgelöste Sätze bleiben EINGEFROREN — sie werden nicht neu gelesen.</summary>
    [Fact]
    public void Aktivieren_friert_bereits_aufgeloeste_Saetze_ein()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        Sql("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)",
            new DbParam("@p", Projekt), new DbParam("@n", "Befund 185 eingefroren"));

        SpeicherFlottenErgebnis ergebnis =
            Aktivierungsergebnis(mitAufgeloestenKosten: true, investProKw: 42);
        SpeicherFlottenProjektCtrl.Aktivieren(Projekt, ergebnis);

        SpeicherAuslegungKonfiguration stand = SpeicherAuslegungCtrl.Profile(Projekt, 0)
            .Single(x => x.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand)
            .Eingaben.Auslegung;
        Assert.Equal(42.0, stand.VerwendeteKosten.InvestEurProKw, 10);
    }

    // =====================================================================
    //  2c. Pruefe — die vollständige Liste statt des ersten Problems
    // =====================================================================

    [Fact]
    public void Pruefe_nennt_alle_Probleme_und_den_Ausweg()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.FlotteImProjektAktiv = false;
        e.Auslegung.Lastquelle = SpeicherAuslegungQuelle.Datei;
        e.Auslegung.Flotte.Einheiten.Clear();

        FlottenProjektPruefung pruefung = SpeicherFlottenProjektCtrl.Pruefe(e, 0, true);

        Assert.False(pruefung.Rechenbar);
        Assert.Equal(3, pruefung.Probleme.Count);
        Assert.Contains(pruefung.Probleme, x => x.Contains("nicht aktiviert"));
        Assert.Contains(pruefung.Probleme, x => x.Contains("keine physische Speichereinheit"));
        Assert.Contains(pruefung.Probleme, x => x.Contains("externe Lastdatei"));
        Assert.Contains("Ausweg", pruefung.Meldung);
    }

    [Fact]
    public void Pruefe_meldet_den_fehlenden_Stand()
    {
        FlottenProjektPruefung pruefung = SpeicherFlottenProjektCtrl.Pruefe(null, 0, true);

        Assert.False(pruefung.Rechenbar);
        Assert.Single(pruefung.Probleme);
        Assert.Contains("@Projektflotte", pruefung.Probleme[0]);
    }

    /// <summary>
    /// Fehlende Kostensätze sind ein HINWEIS, kein Problem: Der Projektlauf rechnet.
    /// </summary>
    [Fact]
    public void Pruefe_stuft_fehlende_Kostensaetze_als_Hinweis_ein()
    {
        SpeicherOptimierungEingaben e = Eingaben();
        e.Auslegung.DirekteKosten = new SpeicherKostensaetze();

        FlottenProjektPruefung pruefung = SpeicherFlottenProjektCtrl.Pruefe(e, Projekt, false);

        Assert.True(pruefung.Rechenbar);
        Assert.Single(pruefung.Hinweise);
        Assert.Contains("nicht bewertbar", pruefung.Hinweise[0]);
    }

    // =====================================================================
    //  Hilfsmittel
    // =====================================================================

    private static void Sql(string sql, params DbParam[] parameter)
    {
        using var db = DataRepository.Vorgang();
        db.Ausfuehren(sql, parameter);
        db.Commit();
    }

    private static SimulationControl ProjektSimulation(double ersteLastKw)
    {
        var bedarf = new SimulationStrombedarf
        {
            Strombedarf_viertelStundenwerte = new double[35040]
        };
        bedarf.Strombedarf_viertelStundenwerte[0] = ersteLastKw;
        return new SimulationControl
        {
            simulation_Strombedarf = bedarf,
            Rest_Strombedarf_viertelstuendlich =
                (double[])bedarf.Strombedarf_viertelStundenwerte.Clone()
        };
    }

    private static SpeicherParameter Basis() => new SpeicherParameter
    {
        CNomKwh = 100,
        PKw = 50,
        SoCMinKwh = 0,
        SoCMaxKwh = 100,
        RoundTripWirkungsgrad = 0.9,
        Kapitalzins = 0.04,
        NutzungsdauerA = 15
    };

    private static StromspeicherOptimierungVorbereitung Epos()
    {
        double[] l = Enumerable.Repeat(4.0, 35040).ToArray();
        double[] p = Enumerable.Repeat(2.0, 35040).ToArray();
        double[] k = Enumerable.Repeat(30.0, 35040).ToArray();
        SpeicherEingang eingang = new SpeicherEingang(l, p, k, null, null, null);
        return new StromspeicherOptimierungVorbereitung
        {
            Basis = Basis(),
            Eingang = eingang,
            Kontext = new StromspeicherLaufKontext
            {
                Parameter = Basis(),
                Eingang = eingang,
                Bezeichner = "Speicher",
                Preisversion = "EPOS"
            }
        };
    }

    /// <summary>Die Naht ohne Datenbank — genau der Weg, den der Projektlauf nimmt.</summary>
    private static StromspeicherOptimierungVorbereitung Vorbereiten(
        SpeicherOptimierungEingaben e, KostenPflicht pflicht)
        => SpeicherAuslegungCtrl.AusQuellenVorbereiten(
            Epos(), Basis(), new StromspeicherLaufKontext { Parameter = Basis() },
            e, null, 0.0, null, null, pflicht);

    private static SpeicherOptimierungEingaben Eingaben() => new()
    {
        Auslegung = new SpeicherAuslegungKonfiguration
        {
            Investitionsquelle = SpeicherKostenQuelle.Dialog,
            Betriebsquelle = SpeicherKostenQuelle.Dialog,
            DirekteKosten = new SpeicherKostensaetze
            {
                InvestEurProKw = 200,
                InvestEurProKwh = 350,
                BetriebEurProKwJahr = 5,
                BetriebEurProKwhJahr = 2.5,
                BetriebEurProKwhEntladen = 0.01,
                InvestVorhanden = true,
                BetriebVorhanden = true,
                Herkunft = "Dialog"
            },
            Lastquelle = SpeicherAuslegungQuelle.Epos,
            PvQuelle = SpeicherAuslegungQuelle.Keine,
            Preisquelle = SpeicherAuslegungQuelle.Epos,
            FlotteImProjektAktiv = true,
            Flotte = new FlottenStudieKonfiguration
            {
                Einheiten = new List<FlottenEinheit>
                {
                    new()
                    {
                        Id = "s185", Name = "Befundspeicher",
                        KapazitaetKWh = 10, LadeleistungKw = 4, EntladeleistungKw = 4,
                        Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
                        SocMin = 0, SocStart = 0, SocMax = 1
                    }
                },
                Optionen = new FlottenSimulationOptionen
                {
                    Betriebsziel = FlottenBetriebsziel.PvGreedy,
                    EnergieAusgleichEuroProKWh = 0
                },
                Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
                {
                    ProjektjahreBeiWiederholung = 1
                }
            }
        }
    };

    /// <summary>Ein übernahmefähiger Flottenlauf — wahlweise mit oder ohne aufgelöste Kosten.</summary>
    private static SpeicherFlottenErgebnis Aktivierungsergebnis(
        bool mitAufgeloestenKosten, double investProKw = 200)
    {
        SpeicherOptimierungEingaben eingaben = Eingaben();
        eingaben.Auslegung.FlotteImProjektAktiv = false;
        eingaben.Auslegung.VerwendeteKosten = mitAufgeloestenKosten
            ? new SpeicherKostensaetze
            {
                InvestEurProKw = investProKw, InvestEurProKwh = 350,
                BetriebEurProKwJahr = 5, BetriebEurProKwhJahr = 2.5,
                BetriebEurProKwhEntladen = 0.01,
                InvestVorhanden = true, BetriebVorhanden = true, Herkunft = "Lauf"
            }
            : new SpeicherKostensaetze();

        FlottenStudieKonfiguration flotte =
            SpeicherAuslegungKopie.Von(eingaben.Auslegung.Flotte);
        string konfigurationId = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(eingaben.Auslegung)));
        FlottenSimulationErgebnis Simulation() => new()
        {
            Zulaessig = true,
            KonfigurationId = konfigurationId,
            DatenId = "befund-185",
            Intervalle = new List<FlottenIntervallErgebnis> { new() }
        };
        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Eingaben = eingaben,
            Konfiguration = flotte,
            Studie = new FlottenStudienErgebnis
            {
                ReferenzOhneSpeicher = Simulation(),
                Variante = Simulation()
            }
        };
    }
}
