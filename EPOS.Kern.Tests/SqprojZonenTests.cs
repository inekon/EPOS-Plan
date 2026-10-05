using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 36 — Zonenplan</b> (Datenaustauschkonzept 16.7): die Zonen der Projektdatei werden freie Zonen mit der Nutzung
    /// aus der Profilnummer; Räume in mehreren Zonen folgen der Simulationszone; leere Zonen sind gemeldet und nicht angelegt.
    /// </summary>
    public sealed class SqprojZonenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _pfade = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
        }

        private (Zonenplan Plan, SqprojZonenergebnis Ergebnis) Uebernehmen(SqprojProbenErzeuger e)
        {
            string p = SqprojProbenErzeuger.TempPfad("zonen");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(e.Schreiben(p));
            GebaeudeAbbild ifc = SqprojProbenErzeuger.IfcAbbild();
            Zonenplan plan = Zonenplan.Vorschlag(ifc, 0);
            Assert.NotNull(plan);
            SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(projekt, ifc.Gebaeude[0]);
            return (plan, SqprojZonen.Uebernehmen(plan, projekt, abgleich));
        }

        [Fact]
        public void Zonen_mit_Nutzung_Simulationszone_vorn_leere_Zonen_gemeldet()
        {
            (Zonenplan plan, SqprojZonenergebnis e) = Uebernehmen(SqprojProbenErzeuger.Standard());
            Assert.Equal(new[] { "Simulation EG [BUERO]", "Simulation OG [WOHNEN]" },
                         plan.Zonen.Select(z => z.Name + " [" + z.Nutzung + "]"));
            Assert.Equal(2, e.Uebernommen);
            Assert.Equal(new[] { "Leer", "Nutzung EG", "Nutzung OG" }, e.LeereZonen);   // Typ 6 deckt alle Räume, Typ 5 bleibt leer
            Assert.Equal(3, e.Meldungen.Count(m => m.Schluessel == SqprojProtokoll.ZONE_LEER));
            string sim = plan.Zonen.Single(z => z.Name == "Simulation EG").Schluessel;
            Assert.Equal(new[] { "Raum A", "raum b " }, plan.RaeumeVon(sim).Select(r => r.Name));
            string og = plan.Zonen.Single(z => z.Name == "Simulation OG").Schluessel;
            Assert.Equal(new[] { "Raum C", "Raum X" }, plan.RaeumeVon(og).Select(r => r.Name));
            Assert.Equal(new[] { "Raum Y" }, plan.NichtZugeordnet.Select(r => r.Name));
            Assert.All(plan.Zonen, z => Assert.NotNull(z.Projektdatei));
            Assert.Equal(Konditionierungsherkunft.Ganglinie,
                         plan.Zone(sim).Projektdatei.Groesse(Konditionierungsgroesse.Heizsoll).Herkunft);
            Assert.Equal(2, e.Zonenzahl(Konditionierungsgroesse.Heizsoll, Konditionierungsherkunft.Ganglinie)
                            + e.Zonenzahl(Konditionierungsgroesse.Heizsoll, Konditionierungsherkunft.Nutzungsprofil));
            // Die Zonierung trägt Nutzung und Konditionierung weiter.
            GebaeudeZonierung zon = plan.Zonieren();
            Assert.All(zon.Zonen, iz => Assert.NotNull(iz.Projektdatei));
        }

        [Fact]
        public void Eine_unvollstaendige_Zonierung_laesst_Raeume_offen()
        {
            (Zonenplan plan, SqprojZonenergebnis e) = Uebernehmen(SqprojProbenErzeuger.Unvollstaendig());
            Planzone z = Assert.Single(plan.Zonen);
            Assert.Equal("Simulation", z.Name);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, z.Nutzung);   // aus der Profilnummer der Gruppe
            Assert.Equal(new[] { "Raum A" }, plan.RaeumeVon(z.Schluessel).Select(r => r.Name));
            Assert.Equal(new[] { "raum b ", "Raum C", "Raum X", "Raum Y" }, plan.NichtZugeordnet.Select(r => r.Name));
            Assert.Equal(new[] { "Nutzung" }, e.LeereZonen);
        }

        [Fact]
        public void Eine_Profilnummer_ohne_Entsprechung_laesst_die_Zone_ohne_Nutzung()
        {
            SqprojProbenErzeuger e = new SqprojProbenErzeuger().Geschoss("F1", "EG").Raum("R1", "Raum A", "F1", null, 20.0)
                .Zone("Z1", "Lagerzone", 5, null, "R1")
                .Nutzung("U1", "Profil Lager", "Z1", 20, SqprojProbenErzeuger.NULLZEIT, SqprojProbenErzeuger.NULLZEIT, null, 12.0,
                         null, null, null, null, null, null, null, null);
            (Zonenplan plan, SqprojZonenergebnis r) = Uebernehmen(e);
            Planzone z = Assert.Single(plan.Zonen);
            Assert.Null(z.Nutzung);
            Assert.Contains(r.Meldungen, m => m.Schluessel == SqprojProtokoll.NUTZUNG_OHNE_ABBILDUNG && m.Werte[1] == "20");
            Assert.Equal(20, z.Projektdatei.Profilnummer);
        }

        [Theory]
        [InlineData(1, "BUERO")]
        [InlineData(5, "BUERO")]
        [InlineData(8, "SCHULE")]
        [InlineData(29, "SCHULE")]
        [InlineData(70, "WOHNEN")]
        [InlineData(71, "WOHNEN")]
        [InlineData(6, null)]
        [InlineData(20, null)]
        public void Die_feste_Nutzungstabelle(int nummer, string nutzung) => Assert.Equal(nutzung, Din18599Nutzung.Nutzung(nummer));

        [Fact]
        public void Die_Nutzungstabelle_nennt_nur_Nutzungen_des_Zonenplans()
        {
            Assert.All(Din18599Nutzung.Tabelle.Values, n => Assert.Contains(n, Zonenplan.NUTZUNGEN));
            Assert.Null(Din18599Nutzung.Nutzung(null));
        }
    }
}
