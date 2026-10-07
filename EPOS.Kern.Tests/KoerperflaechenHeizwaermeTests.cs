using System;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Abstimmung G5, A6 — die Heizwärme der Probe ohne Mengensätze auf beiden Wegen</b>: Die Probe
    /// <c>ifc4_g5_kleinhaus_ohne_mengen.ifc</c> wird einmal über den Körperweg der Bauteilflächen (G5-3) und einmal mit
    /// ausgeschaltetem Körperweg (Rückfall ohne Zuordnung zu den Räumen: Bauteile an der Zone ihres Geschosses, Trennflächen
    /// nur aus den Raumkörpern) als Zonenvorschlag in eine Arbeitskopie der Testdatenbank geschrieben und mit der
    /// Mehrzonenrechnung gerechnet. Beide Läufe rechnen fehlerfrei; die Jahresheizwärme beider Wege steht in der Ausgabe.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KoerperflaechenHeizwaermeTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public KoerperflaechenHeizwaermeTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _kultur.Dispose();

        /// <summary>Schreibt den Zonenvorschlag in eine frische Arbeitskopie und rechnet; <c>null</c> = keine Testdatenbank.</summary>
        private static (double Mwh, int Zonen, double FlaecheM2)? Rechnen(GebaeudeImportAblauf ablauf)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return null;
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(PROJEKT);
                ProjektGebaeudeModel g = ctrl.items[0];
                GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null);
                Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
                using (DbVorgang vorgang = DataRepository.Vorgang())
                {
                    GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                    Assert.True(e.Ok, e.Meldung);
                    vorgang.Commit();
                }
                ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(PROJEKT);
                ProjektGebaeudeModel mit = ctrl.items[0];
                mit.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
                Assert.All(mit.Zonen, z => Assert.Null(z.Lesefehler));
                var projekt = new ProjektCtrl();
                projekt.ReadSingle(PROJEKT);
                var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
                sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
                var werte = new double[8760];
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, werte), string.Join(" | ", p.Hinweise));
                Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
                return (sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh, mit.Zonen.Count,
                        v.Zeilen.Where(z => z.Bauteil.Bauteilart != DbWerte.BAUTEILART_FENSTER).Sum(z => z.Bauteil.Flaeche));
            }
        }

        [Fact]
        public void Heizwaerme_ueber_den_Koerperweg_gegen_den_Rueckfall_ohne_Raumzuordnung()
        {
            (double Mwh, int Zonen, double FlaecheM2)? koerper = Rechnen(KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE));
            if (!koerper.HasValue) return;
            (double Mwh, int Zonen, double FlaecheM2)? rueckfall = Rechnen(KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE, koerperflaechenAus: true));
            Assert.True(rueckfall.HasValue);
            string F(double w) => w.ToString("0.###", CultureInfo.InvariantCulture);
            _ausgabe.WriteLine("A6 Körperweg: " + F(koerper.Value.Mwh) + " MWh/a, " + koerper.Value.Zonen + " Zonen, opake Flächen " + F(koerper.Value.FlaecheM2) + " m²");
            _ausgabe.WriteLine("A6 Rückfall: " + F(rueckfall.Value.Mwh) + " MWh/a, " + rueckfall.Value.Zonen + " Zonen, opake Flächen " + F(rueckfall.Value.FlaecheM2) + " m²");
            Assert.True(koerper.Value.Mwh > 0.0);
            Assert.True(rueckfall.Value.Mwh > 0.0);
            Assert.Equal(3, koerper.Value.Zonen);
        }
    }
}
