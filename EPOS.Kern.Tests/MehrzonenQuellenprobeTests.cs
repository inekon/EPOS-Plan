using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrzonengebäude aus Quelldateien, gerechnet wie die Auskunft des Gebäudedialogs</b>
    /// (<see cref="GebaeudeBedarfCtrl.Rechnen(int, int, ProjektGebaeudeModel, string)"/>): ein Projektpaket in
    /// <c>Quellen/</c> mit einem importierten Gebäude in elf Zonen und, als Gegenprobe, die IFC-Datei der Produktion mit
    /// Verwaltung, nach der Vorgabe der Datei bzw. nach Raumtemperatur und Nutzung zoniert und als Zonen mit Bauteilen
    /// an das Gebäude eines Referenzprojekts geschrieben.
    ///
    /// <para>Beide Gebäude führen Stunden, in denen das festgehaltene Muster des ersten Durchlaufs die Abschnittsregel
    /// bricht; die Zonenschleife rechnet sie dann frei (Mehrzonenkonzept 2.4). Das ist ein regulärer Ausgang und darf
    /// keine Ausnahme werfen — auch keine gefangene: Ein Debugger, der bei geworfenen Ausnahmen hält, stand sonst mitten
    /// in der Rechnung mit einer Meldung, die wie ein Rechenfehler aussieht. Geprüft wird deshalb, dass die Rechnung
    /// gelingt und während ihr keine <see cref="GebaeudeModellException"/> geworfen wird.</para>
    ///
    /// <para>Fehlt eine Quelldatei (nicht jeder Klon trägt sie), wird der Fall übersprungen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class MehrzonenQuellenprobeTests : IDisposable
    {
        private const string PROJEKTPAKET = "Pajunk WP D30.wpx";
        private const string IFC = "Produktion_groß_mit_Verwaltung_EG55-2026.ifc";

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public MehrzonenQuellenprobeTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static string Quelle(string name)
            => Path.GetFullPath(Path.Combine(IfcProbenTests.Ordner(), "..", "..", "Quellen", name));

        private bool Fehlt(string pfad)
        {
            if (File.Exists(pfad) && new FileInfo(pfad).Length >= 1000) return false;
            _aus.WriteLine("Quelldatei fehlt: " + pfad);
            return true;
        }

        /// <summary>Rechnet wie die Auskunft und zählt dabei jede geworfene Ausnahme des Gebäudemodells.</summary>
        private static (GebaeudeBedarfErgebnis Ergebnis, List<string> Geworfen, IList<string> Hinweise) Rechnen(
            int projekt, int klimaregion, ProjektGebaeudeModel gebaeude)
        {
            var geworfen = new List<string>();
            EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> zaehler = (_, e) =>
            {
                if (e.Exception is GebaeudeModellException) lock (geworfen) geworfen.Add(e.Exception.Message);
            };
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            AppDomain.CurrentDomain.FirstChanceException += zaehler;
            try
            {
                return (GebaeudeBedarfCtrl.Rechnen(projekt, klimaregion, gebaeude), geworfen, p.Hinweise);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= zaehler;
            }
        }

        [Fact]
        public void Projektpaket_mit_elf_Zonen_rechnet_ohne_geworfene_Abschnittsregel()
        {
            if (!_db.Vorhanden) return;
            string pfad = Quelle(PROJEKTPAKET);
            if (Fehlt(pfad)) return;

            int projekt = new ProjektExportImportCtrl().Importieren(pfad, "Quellenprobe Mehrzonen",
                ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.True(projekt > 0, "Import fehlgeschlagen: " + fehler);
            var projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle(projekt);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projekt);
            ProjektGebaeudeModel g = Assert.Single(ctrl.items);
            Assert.Equal(11, g.Zonen.Count);

            var (r, geworfen, hinweise) = Rechnen(projekt, projektCtrl.m_ID_Klimaregion, g);
            _aus.WriteLine("Heizwärme " + r.HeizwaermeMwh + " MWh, geworfen " + geworfen.Count);
            Assert.True(r.Erfolgreich, r.Befund);
            Assert.True(r.HeizwaermeMwh > 0.0);
            // Der Fall, um den es geht, tritt auf: Muster, die nicht hielten, frei gerechnet.
            Assert.Contains(hinweise, h => h.Contains("nicht haltbar", StringComparison.Ordinal));
            Assert.True(geworfen.Count == 0, geworfen.Count + " geworfen, zuerst: " + geworfen.FirstOrDefault());
        }

        [Theory]
        [InlineData(1045, null)]
        [InlineData(1045, IfcImportProfil.ZONENREGEL_Z6)]
        [InlineData(1047, null)]
        public void IFC_Produktion_mit_Verwaltung_rechnet_als_Mehrzonengebaeude(int projekt, string regel)
        {
            if (!_db.Vorhanden) return;
            string pfad = Quelle(IFC);
            if (Fehlt(pfad)) return;

            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                ablauf.Lesen(s, pfad, GebaeudeImportProfil.FuerDatei(IFC));
            Assert.True(ablauf.Abbild != null, string.Join(" | ", ablauf.Meldungen.Select(m => m.ToString())));

            // Wie im Dialog: die Zonierung nach der Regel, „Als Zonen mit Bauteilen übernehmen".
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null, regel);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projekt);
            int idGebaeude = ctrl.items[0].ID_Gebaeude;
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(idGebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                vorgang.Commit();
            }

            var projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle(projekt);
            var neu = new ProjektGebaeudeCtrl();
            neu.ReadAll(projekt);
            ProjektGebaeudeModel mit = neu.items.First(x => x.ID_Gebaeude == idGebaeude);
            mit.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            Assert.Equal(v.Zonen.Count, mit.Zonen.Count);
            Assert.True(mit.Zonen.Count > 1);

            var (r, geworfen, _) = Rechnen(projekt, projektCtrl.m_ID_Klimaregion, mit);
            Assert.True(r.Erfolgreich, r.Befund);
            Assert.True(r.HeizwaermeMwh > 0.0);
            Assert.True(geworfen.Count == 0, geworfen.Count + " geworfen, zuerst: " + geworfen.FirstOrDefault());
        }
    }
}
