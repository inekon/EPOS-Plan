using System;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Anwenderentscheid 04.10.2026: Die Flottenvorgabe eines Projektlaufs stammt immer aus
    /// einem vorliegenden Lauf. Die Ergebnishülle setzt dafür
    /// <see cref="SimulationControl.SpeicherflotteAusLaufVorbelegen"/>; der Lauf bildet die
    /// Vorgabe an seiner Speicherstufe aus dem eigenen Lastgang ohne Speicher.
    /// </summary>
    /// <remarks>
    /// Projekt 1017 der Testdatenbank führt einen Stromspeicher und keine gespeicherte Flotte.
    /// Ohne diesen Weg kam der Peak-Zielvorschlag beim ersten Lauf aus dem Rückfall und ab dem
    /// zweiten aus dem Lastgang des vorigen Laufs — zwei Starts, zwei Flottenvorgaben.
    /// </remarks>
    [Collection("Testdatenbank")]
    public class SpeicherVorbelegungAusLaufTests
    {
        private const int PROJEKT = 1017;

        private static SimulationRunner Lauf()
        {
            var runner = new SimulationRunner();
            runner.sim.SpeicherflotteAusLaufVorbelegen = true;
            string fehler;
            Assert.True(runner.Simuliere(PROJEKT, out fehler), "Lauf gescheitert: " + fehler);
            return runner;
        }

        private static double PeakZiel(SimulationControl sim)
            => sim.Speicherflottenkonfiguration.Optionen.WirtschaftlicherPeakZielwertKw.Value;

        /// <summary>
        /// Zwei Läufe auf denselben Eingaben: dieselbe Peak-Vorgabe aus dem Lastgang und
        /// dasselbe Flottenergebnis — schon der erste Lauf rechnet mit dem Lastgang.
        /// </summary>
        [Fact]
        public void Zwei_Laeufe_ohne_Flotte_liefern_dieselbe_Vorgabe_aus_dem_Lastgang()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationRunner erster = Lauf();
            SimulationRunner zweiter = Lauf();

            Assert.NotNull(erster.sim.SpeicherflottenEingaben);
            Assert.NotNull(erster.sim.Speicherflottenlauf);
            Assert.Equal(FlottenPeakZielHerkunft.Lastgang, erster.sim.SpeicherflottenPeakZielHerkunft);
            Assert.Equal(FlottenPeakZielHerkunft.Lastgang, zweiter.sim.SpeicherflottenPeakZielHerkunft);

            Assert.Equal(PeakZiel(erster.sim), PeakZiel(zweiter.sim));
            Assert.Equal(erster.sim.ReststromMwh, zweiter.sim.ReststromMwh);
            Assert.Equal(erster.sim.Speicherflottennetzbilanz.NetzbezugKwh,
                         zweiter.sim.Speicherflottennetzbilanz.NetzbezugKwh);

            // Dieselbe Vorgabe, die die Auslegungsseite NACH dem Lauf aus ihm herleitet.
            var auslegung = new StromspeicherAuslegungCtrl(PROJEKT);
            auslegung.LaufUebernehmen(erster.sim);
            SpeicherOptimierungVorgaben nachher = auslegung.Vorgaben();
            Assert.Equal(FlottenPeakZielHerkunft.Lastgang, nachher.PeakZielHerkunft);
            Assert.Equal(PeakZiel(erster.sim),
                         nachher.Eingaben.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw.Value);
        }

        /// <summary>
        /// Ein gespeicherter Flottenstand (<c>@Aktuell</c>) gilt, wie er ist: Der Lauf rechnet
        /// mit dessen eingefrorenem Peak-Ziel und meldet die Herkunft „gespeichert".
        /// </summary>
        [Fact]
        public void Gespeicherte_Flotte_behaelt_ihre_Vorgabe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const double EINGEFROREN = 123.4;
            var auslegung = new StromspeicherAuslegungCtrl(PROJEKT);
            SpeicherOptimierungEingaben stand = auslegung.Vorgaben().Eingaben;
            Assert.True(stand.Auslegung.Flotte.Einheiten.Count > 0);
            stand.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw = EINGEFROREN;
            Assert.Equal("", auslegung.EinstellungenSpeichern(stand));

            SimulationRunner lauf = Lauf();

            Assert.Equal(FlottenPeakZielHerkunft.Gespeichert, lauf.sim.SpeicherflottenPeakZielHerkunft);
            Assert.Equal(EINGEFROREN, PeakZiel(lauf.sim));
        }

        /// <summary>
        /// Ohne Lauf (Auslegungsseite vor dem ersten Start) bleibt der benannte Rückfall —
        /// und er ist als Rückfall gekennzeichnet.
        /// </summary>
        [Fact]
        public void Ohne_Lauf_ist_die_Vorgabe_der_benannte_Rueckfall()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var auslegung = new StromspeicherAuslegungCtrl(PROJEKT);
            auslegung.LaufUebernehmen(new SimulationControl());
            SpeicherOptimierungVorgaben vorgaben = auslegung.Vorgaben();

            Assert.Equal(FlottenPeakZielHerkunft.Rueckfall, vorgaben.PeakZielHerkunft);
            Assert.Equal(FlottenPeakZielHerkunft.Rueckfall,
                         auslegung.PeakZielVorschlag(vorgaben.Eingaben).Herkunft);
        }

        /// <summary>Ohne Anforderung bildet der Lauf keine Vorgabe — Runner und Referenzlauf rechnen unverändert.</summary>
        [Fact]
        public void Ohne_Anforderung_bildet_der_Lauf_keine_Vorgabe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var runner = new SimulationRunner();
            string fehler;
            Assert.True(runner.Simuliere(PROJEKT, out fehler), fehler);
            Assert.Null(runner.sim.SpeicherflottenEingaben);
            Assert.Null(runner.sim.SpeicherflottenPeakZielHerkunft);
        }
    }
}
