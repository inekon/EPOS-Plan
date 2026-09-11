using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Pinnt die Kultur (Auftrag #230, Befund „Windows-CI rot seit Lauf 262"):
/// <c>UngueltigerSnapshot_WirftImFlottenzweig_StattAufLegacyZurueckzufallen</c> hält die
/// deutsche Ausnahmemeldung („Speicherflotte", „externe Lastdatei") fest, die aus dem
/// Kern über <c>CurrentUICulture</c> kommt — auf dem Windows-Läufer (en-US) sonst
/// englisch.
/// </summary>
[Collection("Testdatenbank")]
public sealed class SpeicherFlottenLaufSnapshotTests : IDisposable
{
    private const int Projekt = 987654321;

    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose() => _kultur.Dispose();

    [Fact]
    public void Snapshot_IstNurAnSeineSimulationControlInstanzGebunden()
    {
        var erste = new SimulationControl();
        var zweite = new SimulationControl();
        SpeicherOptimierungEingaben eingaben = GueltigeEingaben();

        erste.SpeicherflottenEingaben = eingaben;

        Assert.Same(eingaben, erste.SpeicherflottenEingaben);
        Assert.Null(zweite.SpeicherflottenEingaben);
        Assert.Null(erste.Speicherflottenlauf);
        Assert.Null(zweite.Speicherflottenlauf);
    }

    [Fact]
    public void UngueltigerSnapshot_WirftImFlottenzweig_StattAufLegacyZurueckzufallen()
    {
        var sim = ProjektSimulation(3);
        SpeicherOptimierungEingaben eingaben = GueltigeEingaben();
        eingaben.Auslegung.Lastquelle = SpeicherAuslegungQuelle.Datei;
        sim.SpeicherflottenEingaben = eingaben;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => sim.SpeicherlaufAusfuehren(Projekt));

        Assert.Contains("Speicherflotte", ex.Message);
        Assert.Contains("externe Lastdatei", ex.Message);
        Assert.Null(sim.Speicherflottenlauf);
        Assert.Null(sim.Speicherergebnis);
    }

    [Fact]
    public void ErfolgreicherSnapshot_NutztFrischeProjektquellen_UndIstDirektUebernehmbar()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        var sim = ProjektSimulation(7);
        SpeicherOptimierungEingaben eingaben = GueltigeEingaben();
        sim.SpeicherflottenEingaben = eingaben;

        double[] rueckgabe = sim.SpeicherlaufAusfuehren(Projekt);

        SpeicherFlottenProjektLauf lauf = Assert.IsType<SpeicherFlottenProjektLauf>(sim.Speicherflottenlauf);
        Assert.Equal(35040, rueckgabe.Length);
        Assert.Equal(7, lauf.Studie.ReferenzOhneSpeicher.Intervalle[0].LastKw, 10);
        Assert.Equal(0, lauf.Studie.ReferenzOhneSpeicher.Intervalle[1].LastKw, 10);
        Assert.False(lauf.Eingaben.Auslegung.FlottenGroessenOptimieren);
        Assert.False(lauf.Eingaben.Auslegung.FlotteImProjektAktiv);
        Assert.True(lauf.Eingaben.Auslegung.VerwendeteKosten.InvestVorhanden);
        Assert.NotSame(eingaben, lauf.Eingaben);

        eingaben.Auslegung.Flotte.Einheiten[0].KapazitaetKWh = 99;
        Assert.Equal(10, lauf.Eingaben.Auslegung.Flotte.Einheiten[0].KapazitaetKWh, 10);

        // Genau dieser Laufstand ist der nachfolgende Aktivierungsvertrag des Hosts.
        SpeicherFlottenProjektCtrl.PruefeUebernahme(new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Eingaben = lauf.Eingaben,
            Konfiguration = lauf.Konfiguration,
            Studie = lauf.Studie
        });
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

    private static SpeicherOptimierungEingaben GueltigeEingaben()
    {
        var kosten = new SpeicherKostensaetze
        {
            InvestVorhanden = true,
            BetriebVorhanden = true
        };
        return new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Investitionsquelle = SpeicherKostenQuelle.Dialog,
                Betriebsquelle = SpeicherKostenQuelle.Dialog,
                DirekteKosten = kosten,
                Lastquelle = SpeicherAuslegungQuelle.Epos,
                PvQuelle = SpeicherAuslegungQuelle.Keine,
                Preisquelle = SpeicherAuslegungQuelle.Epos,
                FlottenGroessenOptimieren = true,
                Flotte = new FlottenStudieKonfiguration
                {
                    Einheiten = new List<FlottenEinheit>
                    {
                        new()
                        {
                            Id = "lauf-s1",
                            Name = "Laufspeicher",
                            KapazitaetKWh = 10,
                            LadeleistungKw = 4,
                            EntladeleistungKw = 4,
                            Ladewirkungsgrad = 1,
                            Entladewirkungsgrad = 1,
                            SocMin = 0,
                            SocStart = 0,
                            SocMax = 1
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
    }
}
