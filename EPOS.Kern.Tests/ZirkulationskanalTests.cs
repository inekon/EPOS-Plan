using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schicht S5 — der Zirkulationskanal</b> (Umsetzungskonzept Zapfprofilgenerator 4.3;
    /// A4/ZU5): Methoden, Jahresverlust, Laufzeitfenster, Zonenanteil, Bilanzgrenzen der Quelle.
    /// Erfundene Parameter; Toleranz relativ 1e-12.
    /// </summary>
    public sealed class ZirkulationskanalTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Zonenanteil[] EineZone = { new Zonenanteil("Zone A", 3650.0, true, null) };

        private static Zirkulationsansatz Ansatz(ProjektStand p, IReadOnlyList<Zonenanteil> zonen = null,
                                                 List<ZapfHinweis> h = null, Parametersatz ps = null)
            => Zirkulationskanal.Ansetzen(p, zonen ?? EineZone, ps ?? Parameter(), new Herkunftsprotokoll(), h);

        [Fact]
        public void Die_drei_Methoden_liefern_bei_gleichen_Eingaben_denselben_Jahresverlust()
        {
            // α = 1, Q̄_d = 10 kWh/d, t_Lauf = 10 h: P = 0,5 kW in allen drei Methoden -> Q = 1825 kWh/a.
            ProjektStand basis = Projekt() with { ZirkLaufzeitH = 10.0 };
            Zirkulationsansatz anteil = Ansatz(basis with { ZirkMethode = ZapfZirkulationsmethode.Anteil, ZirkAnteil = 0.5 });
            Zirkulationsansatz laenge = Ansatz(basis with
            {
                ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = 50.0, ZirkVerlustWJeM = 10.0
            });
            Zirkulationsansatz flaeche = Ansatz(basis with
            {
                ZirkMethode = ZapfZirkulationsmethode.Flaechenkennwert, ZirkFlaecheM2 = 365.0, ZirkKennwert = 5.0
            });
            foreach (Zirkulationsansatz a in new[] { anteil, laenge, flaeche })
            {
                Assert.True(Relativ(a.LeistungKw, 0.5) < 1e-12);
                Assert.True(Relativ(a.JahresverlustKwh, 1825.0) < 1e-12);
            }
            Zirkulationsansatz manuell = Ansatz(basis with { ZirkAuto = false, ZirkManuellKw = 0.5 });
            Assert.Null(manuell.Methode);
            Assert.Equal(1825.0, manuell.JahresverlustKwh);
        }

        [Fact]
        public void Die_Reihe_erhaelt_den_Jahresverlust_jeder_Methode()
        {
            ProjektStand basis = Projekt();
            var ansaetze = new[]
            {
                Ansatz(basis with { ZirkMethode = ZapfZirkulationsmethode.Anteil }),
                Ansatz(basis with { ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = 37.0 }),
                Ansatz(basis with { ZirkMethode = ZapfZirkulationsmethode.Flaechenkennwert, ZirkFlaecheM2 = 250.0 }),
                Ansatz(basis with { ZirkAuto = false, ZirkManuellKw = 0.3, ZirkLaufzeitH = 13.25 }),
            };
            foreach (Zirkulationsansatz a in ansaetze)
            {
                Assert.True(a.JahresverlustKwh > 0);
                double[] fenster = Zirkulationskanal.Laufzeitfenster(a.LaufzeitH, 11.7);
                Assert.True(Relativ(fenster.Sum(), a.LaufzeitH) < 1e-12);
                Bilanzreihe r = Zirkulationskanal.Reihe(a.JahresverlustKwh, a.LaufzeitH, fenster);
                Assert.True(Relativ(r.JahressummeKwh, a.JahresverlustKwh) < 1e-12);
                Assert.True(Relativ(r.StundenKwh.Sum(), a.JahresverlustKwh) < 1e-12);
            }
        }

        [Fact]
        public void Kennwertreproduktion()
        {
            // α = 1: der Flächenkennwert gibt k_A · A_N als Jahresverlust zurück (Lage 2 aus dem Projekt).
            Zirkulationsansatz a = Ansatz(Projekt() with { ZirkFlaecheM2 = 400.0, ZirkLage = ZapfLeitungslage.AusserhalbHuelle });
            Assert.True(Relativ(a.JahresverlustKwh, 20.0 * 400.0) < 1e-12);
            Zirkulationsansatz b = Ansatz(Projekt() with { ZirkFlaecheM2 = 400.0 });   // Lage 1 aus dem Parameter
            Assert.True(Relativ(b.JahresverlustKwh, 5.0 * 400.0) < 1e-12);
        }

        [Fact]
        public void Einfach_liefert_eine_Zirkulation_ungleich_null()
        {
            // Vorgabe (Flächenkennwert) ohne jede Fläche -> Methode Anteil mit Hinweis.
            var h = new List<ZapfHinweis>();
            Zirkulationsansatz a = Ansatz(Projekt(), h: h);
            Assert.Equal(ZapfZirkulationsmethode.Anteil, a.Methode);
            Assert.Contains(h, x => x.Code == "ZIRKULATION_OHNE_FLAECHE");
            // 0,25 · 10 kWh/d / 18 h · 18 h · 365 = 912,5 kWh/a
            Assert.True(Relativ(a.JahresverlustKwh, 0.25 * 10.0 * 365.0) < 1e-12);

            // Mit Zonenfläche: Flächenkennwert, A_N = Summe der Zonenflächen.
            Zirkulationsansatz f = Ansatz(Projekt(), new[] { new Zonenanteil("Zone A", 3650.0, true, 300.0) });
            Assert.Equal(ZapfZirkulationsmethode.Flaechenkennwert, f.Methode);
            Assert.True(Relativ(f.JahresverlustKwh, 5.0 * 300.0) < 1e-12);
        }

        [Fact]
        public void Grenze_zwei_und_drei_tragen_keinen_Anteil()
        {
            var zonen = new[]
            {
                new Zonenanteil("Zone A", 1000.0, true, null),
                new Zonenanteil("Zone B", 3000.0, false, null),   // Grenze 2 oder Zirkulation „nein"
            };
            var h = new List<ZapfHinweis>();
            Zirkulationsansatz a = Ansatz(Projekt() with
            {
                ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = 100.0, ZirkLaufzeitH = 20.0
            }, zonen, h);
            Assert.Equal(0.25, a.Gewicht, 15);
            Assert.True(Relativ(a.LeistungKw, 0.25 * 100.0 * 10.0 / 1000.0) < 1e-12);
            Assert.Equal(a.JahresverlustKwh, a.AnteilJeZoneKwh[0], 9);
            Assert.Equal(0.0, a.AnteilJeZoneKwh[1]);
            Assert.Contains(h, x => x.Code == "ZIRKULATION_NICHT_IN_Z1" && x.Zone == "Zone B");

            // Alle Zonen außerhalb Z1 -> α = 0, kein Verlust nach Leitungslänge.
            var aussen = new[] { new Zonenanteil("Zone B", 3000.0, false, null) };
            Zirkulationsansatz b = Ansatz(Projekt() with
            {
                ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = 100.0
            }, aussen);
            Assert.Equal(0.0, b.Gewicht);
            Assert.Equal(0.0, b.JahresverlustKwh);
        }

        [Fact]
        public void Der_Zonenanteil_ist_mengengewichtet_und_mit_Flaechen_flaechengewichtet()
        {
            var zonen = new[]
            {
                new Zonenanteil("Zone A", 1000.0, true, 100.0),
                new Zonenanteil("Zone B", 3000.0, true, 100.0),
                new Zonenanteil("Zone C", 4000.0, false, 200.0),
            };
            Zirkulationsansatz a = Ansatz(Projekt() with { ZirkFlaecheM2 = 400.0 }, zonen);
            Assert.Equal(0.5, a.Gewicht, 15);                         // flächengewichtet: 200 / 400
            Assert.True(Relativ(a.JahresverlustKwh, 0.5 * 5.0 * 400.0) < 1e-12);
            Assert.Equal(0.25 * a.JahresverlustKwh, a.AnteilJeZoneKwh[0], 9);
            Assert.Equal(0.75 * a.JahresverlustKwh, a.AnteilJeZoneKwh[1], 9);
            Assert.Equal(0.0, a.AnteilJeZoneKwh[2]);
            Assert.Equal(a.JahresverlustKwh, a.AnteilJeZoneKwh.Sum(), 9);

            // Eine Zone ohne Fläche -> mengengewichtet: 4000 / 8000.
            zonen[2] = new Zonenanteil("Zone C", 4000.0, false, null);
            Assert.Equal(0.5, Ansatz(Projekt() with { ZirkFlaecheM2 = 400.0 }, zonen).Gewicht, 15);
            Assert.Equal(0.25, Zirkulationskanal.Gewicht(4000.0, 1000.0, false, 0.0, 0.0), 15);
        }

        [Fact]
        public void Das_Laufzeitfenster_liegt_zusammenhaengend_um_die_Tagesmitte()
        {
            double[] f = Zirkulationskanal.Laufzeitfenster(6.0, 12.3);   // Beginn ⌊12,3 − 3 + 0,5⌋ = 9
            for (int h = 0; h < 24; h++) Assert.Equal(h >= 9 && h < 15 ? 1.0 : 0.0, f[h]);

            double[] frueh = Zirkulationskanal.Laufzeitfenster(8.0, 1.0);  // an den Tagesbeginn geschoben
            Assert.Equal(8.0, frueh.Take(8).Sum());
            double[] spaet = Zirkulationskanal.Laufzeitfenster(8.0, 23.0); // an das Tagesende geschoben
            Assert.Equal(8.0, spaet.Skip(16).Sum());

            double[] gebrochen = Zirkulationskanal.Laufzeitfenster(6.5, 12.0);
            Assert.Equal(6.5, gebrochen.Sum(), 12);
            Assert.Equal(0.5, gebrochen.First(x => x > 0 && x < 1));

            Assert.All(Zirkulationskanal.Laufzeitfenster(24.0, 7.0), x => Assert.Equal(1.0, x));
            Assert.Throws<ZapfprofilEingabeException>(() => Zirkulationskanal.Laufzeitfenster(25.0, 12.0));
            Assert.Throws<ZapfprofilEingabeException>(() => Zirkulationskanal.Laufzeitfenster(0.0, 12.0));
        }

        [Fact]
        public void Die_Tagesmitte_ist_der_Schwerpunkt_der_Zapfung()
        {
            var r = new double[8760];
            for (int d = 0; d < 365; d++) { r[d * 24 + 6] = 1.0; r[d * 24 + 18] = 3.0; }
            // (6,5 · 1 + 18,5 · 3) / 4 = 15,5
            Assert.Equal(15.5, Zirkulationskanal.Tagesmitte(new[] { r }), 12);
            Assert.Equal(12.0, Zirkulationskanal.Tagesmitte(new[] { new double[8760] }));
        }

        [Fact]
        public void Unvollstaendige_Zirkulation_wird_benannt_abgelehnt()
        {
            Assert.Equal(ZapfEingabefehler.ZirkulationUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Ansatz(Projekt() with { ZirkAuto = false })).Fehler);
            Assert.Equal(ZapfEingabefehler.ZirkulationUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(
                    () => Ansatz(Projekt() with { ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge })).Fehler);
            Assert.Equal(ZapfEingabefehler.ZirkulationUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Ansatz(Projekt() with { ZirkLaufzeitH = 30.0 })).Fehler);
            Assert.Equal(ZapfEingabefehler.ProjektFehlt,
                Assert.Throws<ZapfprofilEingabeException>(() => Ansatz(null)).Fehler);
            Assert.Throws<ParametersatzException>(
                () => Ansatz(Projekt(), ps: Parameter(null, ZapfParameter.ZIRKULATION_LAUFZEIT)));
        }
    }
}
