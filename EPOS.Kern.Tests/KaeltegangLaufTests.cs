using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Anwenderbefund 04.10.2026</b> (Projekt 1017, Simulation → Ergebnis → „Kälte Produktion Chart"): Die
    /// Übersicht zeigte 98,2 % Kältedeckung durch die Wärmepumpe, das Bild dagegen nur „Ungedeckte Kälte" und
    /// „Kältebedarf" — die Säule der Wärmepumpe fehlte, der ganze Bedarf stand grau als ungedeckt da.
    ///
    /// <para><b>Die Ursache.</b> Der Lauf der Ergebnishülle rechnete seinen Bedarf in die zwei Bedarfsobjekte des
    /// PROJEKTS (<see cref="BedarfsZustand"/>), die er mit der Startseite teilt, und hängte seine Kältekaskade an
    /// deren Kälteseite. Die Zusammenfassung der Startseite (Reiter „Simulation") rechnet dieselben Objekte bei jedem
    /// Betreten neu — <c>SimulationKaeltebedarf.Beginnen</c> setzt dabei die Kaskade auf <c>null</c>. Der Lauf blieb
    /// gültig, das Bild las eine Kälteseite ohne Kaskade. Der Lauf rechnet deshalb in eigene Objekte.</para>
    ///
    /// <para>Gemessen wird am Weg der Seite: <see cref="SimulationAnsichtQuelle"/> → Ergebnisdienste →
    /// <c>Laufen</c> → <c>Modell(Bilder.Kaeltegang)</c>, einmal gleich nach dem Lauf und einmal, nachdem die
    /// Startseite den geteilten Bedarf neu gerechnet hat.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltegangLaufTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KaeltegangLaufTests(TestDatenbank db) { _db = db; }

        /// <summary>„WP_PV-Speicher": Kälte über eine Wärmepumpe im Kühlbetrieb.</summary>
        private const int PROJEKT_KAELTE = 1017;

        /// <summary>Kopie von 1017 mit Anlagenkopplung AK1.</summary>
        private const int PROJEKT_GEKOPPELT = 1047;

        [Theory]
        [InlineData(PROJEKT_KAELTE)]
        [InlineData(PROJEKT_GEKOPPELT)]
        public async Task Das_Kaeltebild_traegt_die_Saeule_der_Waermepumpe(int projekt)
        {
            if (!_db.Vorhanden) return;
            var bedarf = new BedarfsZustand();
            SimulationErgebnisDienste dienste = await Gerechnet(bedarf, projekt);

            Pruefe(dienste, projekt);
        }

        /// <summary>
        /// DER BEFUND SELBST: Die Startseite rechnet den geteilten Bedarf des Projekts neu (ihr Weg in
        /// <c>StartseiteHuelle.Zusammenfassen</c>) — das gezeigte Ergebnis des Laufs bleibt, wie es war.
        /// </summary>
        [Theory]
        [InlineData(PROJEKT_KAELTE)]
        [InlineData(PROJEKT_GEKOPPELT)]
        public async Task Eine_Bedarfsrechnung_der_Startseite_nimmt_dem_Lauf_seine_Kaeltekaskade_nicht(int projekt)
        {
            if (!_db.Vorhanden) return;
            var bedarf = new BedarfsZustand();
            SimulationErgebnisDienste dienste = await Gerechnet(bedarf, projekt);

            var projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle(projekt);
            bedarf.FuerProjekt(projekt);
            bedarf.Strom.Berechnung(projekt);
            bedarf.Waerme.Waermebedarf_berechnen(projekt, projektCtrl.m_ID_Klimaregion);
            Assert.True(bedarf.Waerme.Kaelteseite.Gerechnet);
            Assert.Null(bedarf.Waerme.Kaelteseite.Kaskade);   // die Bedarfsrechnung kennt keine Kaskade

            Pruefe(dienste, projekt);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static async Task<SimulationErgebnisDienste> Gerechnet(BedarfsZustand bedarf, int projekt)
        {
            var quelle = new SimulationAnsichtQuelle(bedarf, null);
            var ansicht = (SimulationAnsichtDienste)quelle.AnsichtGaben(projekt, "")["Dienste"];
            var dienste = (SimulationErgebnisDienste)ansicht.Ergebnis["Dienste"];
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            return dienste;
        }

        /// <summary>
        /// Die Jahressummen der Basis [MWh/a] — Kältebedarf, Deckung der Wärmepumpe, ungedeckter Rest (1017: Übersicht
        /// des Anwenderbefunds 4,08 / 4,01 / 0,08). Gehalten in einem Band: Der Befund war gedeckt = 0.
        /// </summary>
        private static (double Bedarf, double Gedeckt, double Ungedeckt) Soll(int projekt)
            => projekt == PROJEKT_KAELTE ? (4.0826, 4.0075, 0.0751) : (3.8498, 3.7822, 0.0677);

        /// <summary>
        /// Das Zeichenmodell trägt die Säule der Wärmepumpe (1017: gedeckt ≈ 4,01 MWh/a), darauf den Rest (≈ 0,08 MWh/a),
        /// darüber die Bedarfslinie; je Stunde ist die Stapelhöhe der Kältebedarf, und Rest und Bedarf sind die der
        /// Übersicht.
        /// </summary>
        private static void Pruefe(SimulationErgebnisDienste dienste, int projekt)
        {
            Zeichenmodell m = dienste.Modell(new Bildauftrag(Bilder.Kaeltegang));
            Assert.NotNull(m);

            List<Datenreihe> flaechen = m.Reihen.Where(r => r.Art == Reihenart.Flaeche).ToList();
            Datenreihe bedarfslinie = Assert.Single(m.Reihen, r => r.Art == Reihenart.Linie);
            Assert.Equal(2, flaechen.Count);                     // Wärmepumpe, ungedeckte Kälte
            Datenreihe wp = flaechen[0];
            Datenreihe rest = flaechen[1];
            Assert.NotNull(rest.Unten);

            double[] bedarf = bedarfslinie.Werte;
            Assert.Equal(bedarf.Length, wp.Werte.Length);
            Assert.Equal(bedarf.Length, rest.Werte.Length);
            double gedecktMwh = 0, restMwh = 0, bedarfMwh = 0;
            for (int h = 0; h < bedarf.Length; h++)
            {
                double unten = wp.Unten != null ? wp.Unten[h] : 0.0;
                Assert.Equal(0.0, unten, 9);
                Assert.Equal(wp.Werte[h], rest.Unten[h], 9);     // der Rest liegt auf der Säule
                Assert.True(Math.Abs(rest.Werte[h] - bedarf[h]) <= 1e-6 * Math.Max(1.0, bedarf[h]),
                            $"Projekt {projekt}, Stunde {h}: Stapel {rest.Werte[h]} gegen Bedarf {bedarf[h]}");
                gedecktMwh += wp.Werte[h] / 1000.0;
                restMwh += (rest.Werte[h] - rest.Unten[h]) / 1000.0;
                bedarfMwh += bedarf[h] / 1000.0;
            }

            (double sollBedarf, double sollGedeckt, double sollRest) = Soll(projekt);
            Assert.InRange(bedarfMwh, 0.98 * sollBedarf, 1.02 * sollBedarf);
            Assert.InRange(gedecktMwh, 0.98 * sollGedeckt, 1.02 * sollGedeckt);
            Assert.InRange(restMwh, 0.5 * sollRest, 1.5 * sollRest);

            // Dieselben Zahlen wie in der Übersicht der Seite.
            SimulationErgebnisDaten daten = dienste.Laden(projekt);
            Assert.NotNull(daten.Bedarf.Kaelte);
            Assert.Equal(daten.Bedarf.Kaelte.KaelterestbedarfMwh, restMwh, 6);
            Assert.Equal(daten.Bedarf.Kaelte.KaeltebedarfMwh, bedarfMwh, 6);
        }
    }
}
