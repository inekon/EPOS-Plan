using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rechenproben des Rückkühlwerks ohne Datenbank</b> (K-F1; Entwurf Split/VRF/Rückkühlwerk 5.3, 5.6): Mit leeren
    /// Feldern rechnet jede Bauart Bit für Bit wie die Rückkühlart, als die sie gilt
    /// (<see cref="Kaeltemaschine.Rueckkuehltemperatur(string, double, double?, out bool)"/>) — über einen Temperatur- und
    /// Feuchtebereich und ohne Feuchte — adiabat und hybrid unterhalb ihrer Schaltgrenze und ohne Feuchte (K-F2 befeuchtet
    /// darüber). Eine gepflegte Annäherung wirkt; offen benannt bleibt nur die Reihe (K-F3).
    /// </summary>
    public class RueckkuehlwerkTests
    {
        private static IEnumerable<double> Temperaturen()
        {
            for (double t = -20.0; t <= 45.0; t += 0.7) yield return t;
            yield return 0.0;
            yield return -0.0;
            yield return 12.345678901234;
        }

        private static IEnumerable<double?> Feuchten() => new double?[] { null, double.NaN, 0, 3, 5, 17.5, 50, 83.3, 99, 100 };

        public static IEnumerable<object[]> Bauarten() => RueckkuehlwerkSchema.BAUARTEN.Select(b => new object[] { b });

        /// <summary>Befeuchtet die Bauart oberhalb der Schaltgrenze (K-F2)? Dann ist sie nur darunter bzw. ohne Feuchte bitgleich.</summary>
        private static bool Befeuchtend(string bauart) =>
            bauart is RueckkuehlwerkSchema.BAUART_ADIABAT or RueckkuehlwerkSchema.BAUART_HYBRID;

        private static Rueckkuehlwerk Leer(string bauart) =>
            Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Id = 7, Bezeichner = "RK", Bauart = bauart });

        [Theory]
        [MemberData(nameof(Bauarten))]
        public void Leere_Felder_rechnen_bitgleich_zur_Rueckkuehlart(string bauart)
        {
            Rueckkuehlwerk r = Leer(bauart);
            string art = bauart is RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN or RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN
                ? KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER : KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER;
            Assert.Equal(art, r.Rueckkuehlart);
            int faelle = 0;
            foreach (double t in Temperaturen())
                foreach (double? f in Feuchten())
                {
                    if (Befeuchtend(bauart) && t > KaelteFestwerte.RKW_BEFEUCHTUNG_AB_C && f.HasValue && !double.IsNaN(f.Value)) continue;
                    double heute = Kaeltemaschine.Rueckkuehltemperatur(art, t, f, out bool ohneHeute);
                    double neu = r.Rueckkuehltemperatur(t, f, out bool ohneNeu);
                    Assert.Equal(BitConverter.DoubleToInt64Bits(heute), BitConverter.DoubleToInt64Bits(neu));
                    Assert.Equal(ohneHeute, ohneNeu);
                    faelle++;
                }
            Assert.True(faelle > 600);
        }

        [Theory]
        [MemberData(nameof(Bauarten))]
        public void Die_Jahresreihe_ist_bitgleich_zur_Maschine_ohne_Rueckkuehlwerk(string bauart)
        {
            var aussen = new double[8760];
            var feuchte = new double[8760];
            for (int h = 0; h < aussen.Length; h++)
            {
                aussen[h] = 10.0 + 18.0 * Math.Sin(h * 2.0 * Math.PI / 8760.0) + 4.0 * Math.Sin(h * 2.0 * Math.PI / 24.0);
                feuchte[h] = h % 97 == 0 ? double.NaN : 40.0 + 45.0 * Math.Abs(Math.Cos(h * 0.01));
            }
            Rueckkuehlwerk r = Leer(bauart);
            var ohne = new Kaeltemaschine { Rueckkuehlart = r.Rueckkuehlart };
            var mit = new Kaeltemaschine { Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_WASSER };
            mit.RueckkuehlwerkSetzen(r);
            Assert.Equal(ohne.Rueckkuehlart, mit.Rueckkuehlart);
            Assert.Equal(ohne.FreieKuehlungMoeglich, mit.FreieKuehlungMoeglich);
            foreach (double[] f in Befeuchtend(bauart) ? new double[][] { null } : new[] { feuchte, null })
            {
                double[] a = ohne.RueckkuehltemperaturenBilden(aussen, f, out int na);
                double[] b = mit.RueckkuehltemperaturenBilden(aussen, f, out int nb);
                Assert.Equal(na, nb);
                Assert.Equal(a.Select(BitConverter.DoubleToInt64Bits), b.Select(BitConverter.DoubleToInt64Bits));
            }
        }

        [Fact]
        public void Ohne_Rueckkuehlwerk_bleibt_die_Maschine_unveraendert()
        {
            var k = new Kaeltemaschine { Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_LUFT };
            k.RueckkuehlwerkSetzen(null);
            Assert.Null(k.Rueckkuehlwerk);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_LUFT, k.Rueckkuehlart);
            double[] r = k.RueckkuehltemperaturenBilden(new[] { 20.0 }, null, out int ohne);
            Assert.Equal(20.0 + KaelteFestwerte.GRAEDIGKEIT_LUFT_K, r[0]);
            Assert.Equal(0, ohne);
            Assert.Null(Rueckkuehlwerk.AusModell(null));
        }

        [Fact]
        public void Leere_oder_unbekannte_Bauart_rechnet_trocken()
        {
            Assert.Equal(RueckkuehlwerkSchema.BAUART_TROCKEN, Leer(null).Bauart);
            Assert.Equal(RueckkuehlwerkSchema.BAUART_TROCKEN, Leer("UNBEKANNT").Bauart);
            Assert.Equal(KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K, Leer(null).AnnaeherungK);
            Assert.Equal(KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K, Leer(RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN).AnnaeherungK);
            Assert.Empty(Leer(RueckkuehlwerkSchema.BAUART_TROCKEN).NichtGerechnet);
            Assert.Empty(Leer(RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN).NichtGerechnet);
        }

        [Fact]
        public void Eine_gepflegte_Annaeherung_wirkt()
        {
            var trocken = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Bauart = "TROCKEN", Annaeherung_Nenn_K = 12.0 });
            Assert.Equal(32.0, trocken.Rueckkuehltemperatur(20.0, 50.0, out bool o1));
            Assert.False(o1);

            var nass = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Bauart = "KUEHLTURM_OFFEN", Annaeherung_Nenn_K = 3.5 });
            double tw = Kaeltemaschine.Feuchtkugeltemperatur(25.0, 60.0);
            Assert.Equal(tw + 3.5, nass.Rueckkuehltemperatur(25.0, 60.0, out bool o2));
            Assert.False(o2);
            // Ohne Feuchte verschiebt die Annäherung den Ausweichweg um ihren Abstand zur Vorgabe (3,5 − 5 = −1,5 K).
            Assert.Equal(25.0 - KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K - 1.5, nass.Rueckkuehltemperatur(25.0, null, out bool o3), 12);
            Assert.True(o3);

            // Eine nicht endliche Annäherung gilt als leer.
            var nan = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Bauart = "TROCKEN", Annaeherung_Nenn_K = double.NaN });
            Assert.Null(nan.AnnaeherungNennK);
            Assert.Equal(30.0, nan.Rueckkuehltemperatur(20.0, null, out _));
        }

        public static IEnumerable<object[]> Gerechnete_Merkmale() => new[]
        {
            new object[] { new RueckkuehlwerkModel { Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100 } },
            new object[] { new RueckkuehlwerkModel { Ventilator_Nenn_kW = 2.5 } },
            new object[] { new RueckkuehlwerkModel { Ventilator_Regelung = "STUFEN", Ventilator_Stufen = 3 } },
            new object[] { new RueckkuehlwerkModel { Bauart = "ADIABAT" } },
            new object[] { new RueckkuehlwerkModel { Bauart = "HYBRID", Befeuchtung_Ab_C = 24 } },
            new object[] { new RueckkuehlwerkModel { Bauart = "KUEHLTURM_OFFEN", Verdunstung_Faktor = 1.1, Eindickung = 3 } },
        };

        [Theory]
        [MemberData(nameof(Gerechnete_Merkmale))]
        public void Was_K_F2_rechnet_ist_kein_offenes_Merkmal(RueckkuehlwerkModel m)
        {
            Assert.Empty(Rueckkuehlwerk.AusModell(m).NichtGerechnet);
        }

        [Fact]
        public void Offen_bleibt_nur_die_Reihe_in_einer_Aufzaehlung()
        {
            Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel
            {
                Bauart = "HYBRID", Annaeherung_Weg = "LASTABHAENGIG", Ventilator_Nenn_kW = 3, Drift_Anteil = 0.001,
                Freikuehlung_Schaltung = "REIHE"
            });
            Assert.Equal(new[] { Rueckkuehlwerk.MERKMAL_REIHE }, r.NichtGerechnet);
            string text = SimulationControl.RueckkuehlwerkMerkmale(r.NichtGerechnet);
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.DoesNotContain(";", text);
            // PARALLEL und FEST sind die heutigen Wege, kein Merkmal.
            Assert.Empty(Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Annaeherung_Weg = "FEST", Freikuehlung_Schaltung = "PARALLEL" }).NichtGerechnet);
        }
    }
}
