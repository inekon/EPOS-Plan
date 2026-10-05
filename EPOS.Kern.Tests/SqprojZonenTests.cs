using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 36 — Zonenplan</b> (Datenaustauschkonzept 16.7): die Zonen der Projektdatei werden freie Zonen mit der Nutzung
    /// aus der Profilnummer; die Räume gehören der gewählten Zonierung (Vorgabe DIN-V-18599-Zonen, wählbar Simulationszonen,
    /// ohne die gewählte die vorhandene — E87, F1); leere Zonen sind gemeldet und nicht angelegt.
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

        private (SqprojAbbild Projekt, SqprojRaumabgleich Abgleich) Lesen(SqprojProbenErzeuger e)
        {
            string p = SqprojProbenErzeuger.TempPfad("zonen");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(e.Schreiben(p));
            return (projekt, SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]));
        }

        private (Zonenplan Plan, SqprojZonenergebnis Ergebnis) Uebernehmen(SqprojProbenErzeuger e, SqprojZonierung zonierung = SqprojZonierung.Din18599)
        {
            (SqprojAbbild projekt, SqprojRaumabgleich abgleich) = Lesen(e);
            Zonenplan plan = Zonenplan.Vorschlag(SqprojProbenErzeuger.IfcAbbild(), 0);
            Assert.NotNull(plan);
            return (plan, SqprojZonen.Uebernehmen(plan, projekt, abgleich, zonierung));
        }

        [Fact]
        public void Vorgabe_sind_die_DIN_Zonen_mit_der_Gruppe_der_Simulationszone()
        {
            (Zonenplan plan, SqprojZonenergebnis e) = Uebernehmen(SqprojProbenErzeuger.Standard());
            Assert.Equal(SqprojZonierung.Din18599, e.Zonierung);
            Assert.Equal(new[] { "Nutzung EG [BUERO]", "Nutzung OG [WOHNEN]" }, plan.Zonen.Select(z => z.Name + " [" + z.Nutzung + "]"));
            Assert.Empty(e.LeereZonen);   // die leere Simulationszone gehört nicht zur wirksamen Zonierung
            string eg = plan.Zonen.Single(z => z.Name == "Nutzung EG").Schluessel;
            Assert.Equal(new[] { "Raum A", "raum b " }, plan.RaeumeVon(eg).Select(r => r.Name));
            // Nutzungsprofil der DIN-Zone selbst, Ganglinie aus der Gruppe der Simulationszone mit den meisten geteilten Räumen.
            Zonenkonditionierung k = plan.Zone(eg).Projektdatei;
            Assert.Equal(1, k.Profilnummer);
            Assert.Equal(Konditionierungsherkunft.Ganglinie, k.Groesse(Konditionierungsgroesse.Heizsoll).Herkunft);
            Assert.All(plan.Zonen, z => Assert.Equal(SqprojZonierung.Din18599, z.Projektdatei.Zonierung));
        }

        [Fact]
        public void Zonen_mit_Nutzung_Simulationszonen_gewaehlt_leere_Zonen_gemeldet()
        {
            (Zonenplan plan, SqprojZonenergebnis e) = Uebernehmen(SqprojProbenErzeuger.Standard(), SqprojZonierung.Simulation);
            Assert.Equal(SqprojZonierung.Simulation, e.Zonierung);
            Assert.Equal(new[] { "Simulation EG [BUERO]", "Simulation OG [WOHNEN]" },
                         plan.Zonen.Select(z => z.Name + " [" + z.Nutzung + "]"));
            Assert.Equal(2, e.Uebernommen);
            Assert.Equal(new[] { "Leer" }, e.LeereZonen);   // nur die Zonen der gewählten Zonierung zählen
            Assert.Equal(1, e.Meldungen.Count(m => m.Schluessel == SqprojProtokoll.ZONE_LEER));
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
            // Gewählt sind die DIN-Zonen (Vorgabe), keine trägt einen Raum: ohne Wahl gelten die Simulationszonen.
            Assert.Equal(SqprojZonierung.Simulation, e.Zonierung);
            Assert.Empty(e.LeereZonen);
        }

        [Fact]
        public void Zwei_Zonierungen_Vorgabe_DIN_und_Wahl_Simulation()
        {
            (Zonenplan din, SqprojZonenergebnis ed) = Uebernehmen(SqprojProbenErzeuger.ZweiZonierungen());
            Assert.Equal(new[] { "DIN Büro [BUERO]", "DIN Wohnen [WOHNEN]" }, din.Zonen.Select(z => z.Name + " [" + z.Nutzung + "]"));
            Assert.Equal(new[] { "Raum C" }, din.RaeumeVon(din.Zonen[1].Schluessel).Select(r => r.Name));
            // Beide DIN-Zonen nehmen die Ganglinie der Simulationszone, mit der sie Räume teilen.
            Assert.All(din.Zonen, z => Assert.Equal(Konditionierungsherkunft.Ganglinie,
                                                    z.Projektdatei.Groesse(Konditionierungsgroesse.Heizsoll).Herkunft));
            Assert.Equal(2, ed.Uebernommen);

            (Zonenplan sim, SqprojZonenergebnis es) = Uebernehmen(SqprojProbenErzeuger.ZweiZonierungen(), SqprojZonierung.Simulation);
            Planzone haus = Assert.Single(sim.Zonen);
            Assert.Equal("Simulation Haus", haus.Name);
            Assert.Equal(new[] { "Raum A", "raum b ", "Raum C" }, sim.RaeumeVon(haus.Schluessel).Select(r => r.Name));
            // Die Simulationszone nimmt das Nutzungsprofil der DIN-Zone mit den meisten geteilten Räumen (Büro: 2 von 3).
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, haus.Nutzung);
            Assert.Equal(1, haus.Projektdatei.Profilnummer);
            Assert.Equal(SqprojZonierung.Simulation, haus.Projektdatei.Zonierung);
            Assert.Equal(SqprojZonierung.Simulation, es.Zonierung);
        }

        [Fact]
        public void Nur_DIN_Zonen_gelten_ohne_Wahl()
        {
            (Zonenplan plan, SqprojZonenergebnis e) = Uebernehmen(SqprojProbenErzeuger.NurDinZonen(), SqprojZonierung.Simulation);
            Assert.Equal(SqprojZonierung.Din18599, e.Zonierung);
            Planzone z = Assert.Single(plan.Zonen);
            Assert.Equal("Wohnen", z.Name);
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, z.Nutzung);
            (SqprojAbbild projekt, SqprojRaumabgleich abgleich) = Lesen(SqprojProbenErzeuger.NurDinZonen());
            Assert.Equal(2, projekt.ZonentypenUebersprungen.Count);   // Typ 2 und 10 gezählt und übersprungen
            Assert.Equal(1, SqprojZonen.Belegte(projekt, abgleich, SqprojZonierung.Din18599));
            Assert.Equal(0, SqprojZonen.Belegte(projekt, abgleich, SqprojZonierung.Simulation));
            PruefMeldung m = SqprojZonen.Zonierungsmeldung(projekt, abgleich, SqprojZonen.Wirksam(projekt, abgleich, SqprojZonierung.Simulation));
            Assert.Equal(SqprojProtokoll.ZONIERUNG_EINE, m.Schluessel);
            Assert.Equal(new[] { "DIN-V-18599-Zonen", "1" }, m.Werte);
        }

        [Fact]
        public void Der_Protokollsatz_nennt_beide_Zonierungen()
        {
            (SqprojAbbild projekt, SqprojRaumabgleich abgleich) = Lesen(SqprojProbenErzeuger.ZweiZonierungen());
            PruefMeldung din = SqprojZonen.Zonierungsmeldung(projekt, abgleich, SqprojZonierung.Din18599);
            Assert.Equal(SqprojProtokoll.ZONIERUNG, din.Schluessel);
            Assert.Equal(new[] { "DIN-V-18599-Zonen", "2", "Simulationszonen", "1" }, din.Werte);
            Assert.Equal("Zonierung: DIN-V-18599-Zonen (2 Zonen); Simulationszonen vorhanden (1).",
                         string.Format(System.Globalization.CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.IMP_SQ_PROT_ZONIERUNG, din.Werte));
            PruefMeldung sim = SqprojZonen.Zonierungsmeldung(projekt, abgleich, SqprojZonierung.Simulation);
            Assert.Equal(new[] { "Simulationszonen", "1", "DIN-V-18599-Zonen", "2" }, sim.Werte);
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
