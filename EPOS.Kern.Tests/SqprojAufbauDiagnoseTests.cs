using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose BA-4b an der Anwenderdatei</b> (Sportheim: IFC und Projektdatei unter <c>Quellen/</c>, nicht im
    /// Repositorium): Deckung der Zuordnung je Bauteilart samt abgeleiteter Stücke, Verteilung der Rangstufen E98 (Zahl,
    /// Fläche), Zuordnungsstufen A/B/C ohne und mit den Aufbauten der Projektdatei und — über die Testdatenbank — Jahresheizwärme
    /// und Spitze des Einzonenwegs vorher/nachher. Zur Auskunft: Die Zahlen stehen im Protokoll; geprüft wird nur, was die
    /// Übernahme tragen muss. Fehlt eine Datei, endet der Fall ohne Prüfung.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojAufbauDiagnoseTests : IDisposable
    {
        private const string IFC = "Sportheim_1970_unsaniert.ifc";
        private const int PROJEKT = 1045;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public SqprojAufbauDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static string Z(double w) => w.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>IFC gelesen, Projektdatei dazugeladen; <c>null</c>, wenn eine der Dateien fehlt.</summary>
        private GebaeudeImportAblauf Lesen()
        {
            string ifc = IfcQuelldateienDiagnoseTests.Pfad(IFC);
            string pd = ifc == null ? null : Path.ChangeExtension(ifc, ".sqproj");
            if (ifc == null || !File.Exists(pd)) { _aus.WriteLine(IFC + " oder Projektdatei fehlt — übersprungen."); return null; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(ifc))
                a.Lesen(s, ifc, new IfcImportProfil());
            SqprojStand stand = a.ProjektdateiLesen(pd, 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.ToString());
            Assert.True(stand.Abbild.BauteileGelesen);
            // IFC und Projektdatei stammen aus demselben Projektstand: Die Standprüfung (E108) schlägt nicht an, es gilt ohne Wahl
            // die Rangfolge E98 („IFC“), die diese Diagnose festhält. Die Prüfung selbst hält SqprojStandpruefungDiagnoseTests.
            Assert.False(stand.Standpruefung?.Angeschlagen == true);
            Assert.Equal(Aufbauquelle.Ifc, stand.Aufbauquelle);
            return a;
        }

        /// <summary>Der Vorschlag mit (<paramref name="mitAufbauten"/>) bzw. ohne die Aufbauten der Projektdatei.</summary>
        private static GebaeudeBauteilvorschlag Vorschlag(GebaeudeImportAblauf a, bool mitAufbauten)
        {
            a.Projektdatei.Abbild.BauteileGelesen = mitAufbauten;
            try
            {
                return GebaeudeBauteilvorschlag.Bilden(a, 0, null, null, new Baustoffabgleich(BaustoffabgleichDaten.AusSaat()));
            }
            finally { a.Projektdatei.Abbild.BauteileGelesen = true; }
        }

        private string Stufen(GebaeudeBauteilvorschlag v)
            => string.Join(", ", Bauteilzuordnung.Summen(v.Zeilen).Select(s => s.Stufe + " " + s.Zahl + " (" + Z(Math.Round(s.Flaeche_M2, 1)) + " m²)"));

        [Fact]
        public void Sportheim_Zuordnung_und_Rangstufen()
        {
            GebaeudeImportAblauf a = Lesen();
            if (a == null) return;
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            SqprojAufbauwahl w = SqprojAufbauwahl.Bilden(a.Projektdatei.Abbild, a.Projektdatei.Abgleich, g);
            _aus.WriteLine("Räume über GUID " + a.Projektdatei.Abgleich.UeberGuid + " von " + a.Projektdatei.Abbild.Raeume.Count
                           + "; Bauteile über GUID " + w.UeberGuid + ", über GlobalId " + w.UeberGlobalId + ", ohne Gegenstück "
                           + w.OhneGegenstueck.Count + ", mehrdeutig " + w.Mehrdeutig.Count);
            foreach (IGrouping<Bauteilart, AbbildBauteil> art in g.Bauteile.Where(b => SqprojAufbauwahl.Opak(b.Art)).GroupBy(b => b.Art).OrderBy(x => x.Key))
            {
                int abgeleitet = art.Count(b => b.Trenndeckenherkunft != null);
                _aus.WriteLine("DECKUNG " + art.Key + ": " + art.Count(b => w.Entscheid(b) != null) + " von " + art.Count()
                               + " zugeordnet; abgeleitete Stücke " + art.Count(b => b.Trenndeckenherkunft != null && w.Entscheid(b) != null)
                               + " von " + abgeleitet + "; Rang 1/2/keiner "
                               + art.Count(b => w.Entscheid(b)?.Rang == Aufbaurang.Projektdatei) + "/"
                               + art.Count(b => w.Entscheid(b)?.Rang == Aufbaurang.Projektkatalog) + "/"
                               + art.Count(b => w.Entscheid(b) != null && w.Entscheid(b).Rang == Aufbaurang.Keiner)
                               + "; Katalog mehrdeutig " + art.Count(b => w.Entscheid(b)?.KatalogMehrdeutig == true)
                               + "; U abweichend " + art.Count(b => w.Entscheid(b)?.UAbweichend == true)
                               + "; Richtung angenommen " + art.Count(b => w.Entscheid(b)?.RichtungAngenommen == true));
            }

            GebaeudeBauteilvorschlag vorher = Vorschlag(a, false), nachher = Vorschlag(a, true);
            Assert.False(nachher.Abgelehnt, string.Join(" | ", nachher.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            _aus.WriteLine("STUFEN vorher: " + Stufen(vorher) + "; Innenweg " + vorher.Innenweg);
            _aus.WriteLine("STUFEN nachher: " + Stufen(nachher) + "; Innenweg " + nachher.Innenweg);
            foreach (IGrouping<Aufbaurang, GebaeudeBauteilzeile> r in nachher.Zeilen.Where(z => z.Aufbaurang != Aufbaurang.Keiner)
                                                                            .GroupBy(z => z.Aufbaurang).OrderBy(x => x.Key))
                _aus.WriteLine("RANG " + (int)r.Key + " " + r.Key + ": " + r.Count() + " Zeilen, " + Z(Math.Round(r.Sum(z => z.Bauteil.Flaeche), 1)) + " m² ("
                               + string.Join(", ", r.GroupBy(z => z.Bauteil.Bauteilart).OrderBy(x => x.Key, StringComparer.Ordinal)
                                                    .Select(x => x.Key + " " + x.Count() + "/" + Z(Math.Round(x.Sum(z => z.Bauteil.Flaeche), 1)))) + ")");
            foreach (PruefMeldung m in nachher.Meldungen.Where(m => m.Schluessel.StartsWith("IMP_BAUTEIL_PROT_PD_", StringComparison.Ordinal)))
                _aus.WriteLine("  " + m.Stufe + " " + m);
            int pd = nachher.Aufbauten.Count(x => x.AusProjektdatei);
            _aus.WriteLine("Aufbauten aus der Projektdatei " + pd + ", Projektstoffe " + nachher.Aufbauten.SelectMany(x => x.Projektstoffe).Count(s => s != null)
                           + ", weggelassene Schichten " + nachher.Aufbauten.Where(x => x.AusProjektdatei).Sum(x => x.Weggelassen.Count));

            Assert.True(w.UeberGuid > 0);
            Assert.True(pd > 0);
            // Derselbe Projektstand: Jedes opake Bauteil der IFC findet sein Gegenstück, jede Zuordnung trifft Rang 1 (Aufbau der
            // Projektdatei, U stimmt) — keine fällt auf den Projektkatalog (Rang 2) zurück, keine trägt eine U-Abweichung.
            List<AbbildBauteil> opak = g.Bauteile.Where(b => SqprojAufbauwahl.Opak(b.Art)).ToList();
            Assert.Empty(w.OhneGegenstueck);
            Assert.All(opak, b => Assert.Equal(Aufbaurang.Projektdatei, w.Entscheid(b)?.Rang));
            Assert.DoesNotContain(opak, b => w.Entscheid(b).UAbweichend);
            Assert.DoesNotContain(nachher.Zeilen, z => z.Aufbaurang == Aufbaurang.Projektkatalog);
            Assert.Contains(nachher.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.PD_ZUORDNUNG);
            // Mit der Projektdatei bleibt kein Bauteil, das vorher Stufe A hatte, darunter.
            double aVorher = Bauteilzuordnung.Summen(vorher.Zeilen).Where(s => s.Stufe == Bauteilzuordnungsstufe.A).Sum(s => s.Flaeche_M2);
            double aNachher = Bauteilzuordnung.Summen(nachher.Zeilen).Where(s => s.Stufe == Bauteilzuordnungsstufe.A).Sum(s => s.Flaeche_M2);
            Assert.True(aNachher >= aVorher - 1e-6, "Stufe A: vorher " + Z(aVorher) + " m², nachher " + Z(aNachher) + " m²");
        }

        /// <summary>Jahresheizwärme und Spitze des Einzonenwegs, je Fall in einer eigenen Arbeitskopie (Projekt 1045).</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Sportheim_Heizwaerme_vorher_nachher(bool mitAufbauten)
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportAblauf a = Lesen();
            if (a == null) return;
            GebaeudeBauteilvorschlag v = Vorschlag(a, mitAufbauten);
            Assert.False(v.Abgelehnt);
            ProjektGebaeudeModel g = GebaeudeBauteilvorschlagDatenbankTests.Zeile(PROJEKT);
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);

            SimulationWaermebedarf sim = GebaeudeBauteilvorschlagDatenbankTests.NeueRechnung(PROJEKT);
            ProjektGebaeudeModel x = GebaeudeBauteilvorschlagDatenbankTests.Zeile(PROJEKT);
            GebaeudeBauteilvorschlagDatenbankTests.MitImport(x, v.Satz, v.Innenflaechenfaktor);
            var werte = new double[8760];
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(x, 0, werte));
            Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Fehler));
            double q = sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh;
            _aus.WriteLine("HEIZWAERME " + (mitAufbauten ? "nachher (mit Aufbauten der Projektdatei)" : "vorher (ohne)") + ": Q " + Z(Math.Round(q, 3))
                           + " MWh/a, Spitze " + Z(Math.Round(werte.Max() / 1000.0, 2)) + " kW; Stufen " + Stufen(v) + "; Innenweg " + v.Innenweg);
            Assert.True(q > 0.0);
        }
    }
}
