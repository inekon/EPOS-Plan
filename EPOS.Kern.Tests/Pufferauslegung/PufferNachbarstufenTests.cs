using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Nutzen-Aufwand-Zeile</b> der Pufferspeicher-Auslegung (Konzept 6, V47, E-P32): Nachbarstufen der
    /// Nenninhaltsliste mit D1/D2 bei festem Volumen, Verlust K11 und JAZ-Hinweis; die Speicher-gegen-Leistung-
    /// Kurve der Brauchwasserzone. Ohne Datenbank.
    /// </summary>
    public class PufferNachbarstufenTests
    {
        private static double[] Jahresreihe(double spitzeKw)
        {
            var r = new double[8760];
            for (int i = 0; i < 8760; i++)
            {
                double tag = i / 24;
                double aussen = 8 - 10 * Math.Cos(2 * Math.PI * (tag - 15) / 365.0) + 3 * Math.Sin(2 * Math.PI * (i % 24 - 9) / 24.0);
                r[i] = Math.Max(0, (15 - aussen) / 27.0 * spitzeKw);
            }
            return r;
        }

        /// <summary>Eine Zapfreihe mit Spitzen morgens (7 Uhr) und abends (19 Uhr), Tagessumme 60 kWh.</summary>
        private static double[] Zapfreihe()
        {
            double[] tag = { 0.5, 0.5, 0.5, 0.5, 0.5, 1, 3, 9, 6, 2, 1.5, 1.5, 2, 1.5, 1, 1, 1.5, 3, 6, 8, 4, 2.5, 1.5, 1 };
            double summe = tag.Sum();
            return Enumerable.Range(0, 8760).Select(i => tag[i % 24] * 60.0 / summe).ToArray();
        }

        private static PufferAuslegungEingang Waermepumpe() => new PufferAuslegungEingang
        {
            KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO,
            Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = false },
            VorlaufC = 50, RuecklaufC = 40, Uebergabeart = "FLAECHE", HeizgrenzeC = 15,
            ReiheHeizung = Jahresreihe(40), Sperrfenster = PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI")
        };

        [Fact]
        public void Empfehlung_steht_in_der_Mitte_der_Nenninhalte()
        {
            PufferAuslegungEingang e = Waermepumpe();
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            PufferNachbarstufen n = PufferAuslegung.Nachbarstufen(e, r);
            Assert.Equal(5, n.Stufen.Count);
            Assert.Equal(r.EmpfehlungL, n.Stufen[2].VolumenL);
            Assert.True(n.Stufen[2].Empfehlung);
            Assert.Equal(new[] { -2, -1, 0, 1, 2 }, n.Stufen.Select(s => s.Abstand).ToArray());
            int i = PufferAuslegung.NENNINHALTE_VORGABE.ToList().IndexOf(r.EmpfehlungL);
            Assert.True(i >= 2, "Empfehlung " + r.EmpfehlungL);
            Assert.Equal(PufferAuslegung.NENNINHALTE_VORGABE[i - 1], n.Stufen[1].VolumenL);
            Assert.Equal(PufferAuslegung.NENNINHALTE_VORGABE[i + 1], n.Stufen[3].VolumenL);
            Assert.Equal(n.Stufen[3].VolumenL - r.EmpfehlungL, n.Stufen[3].MehrvolumenL);
            Assert.Equal(PufferZone.Heizung, n.Simulationszone);
            // Die Empfehlung (aufgerundet) taktet höchstens so oft wie das Betriebsbild mit dem Zonenvolumen.
            Assert.True(n.Stufen[2].StartsJeTag.Value <= r.Zone(PufferZone.Heizung).Betriebsbild.StartsJeTag + 1e-9);
        }

        [Fact]
        public void Mehr_Volumen_heisst_weniger_Starts_und_mehr_Verlust()
        {
            PufferAuslegungEingang e = Waermepumpe();
            PufferNachbarstufen n = PufferAuslegung.Nachbarstufen(e, PufferAuslegung.Rechnen(e));
            for (int i = 1; i < n.Stufen.Count; i++)
            {
                PufferNachbarstufe a = n.Stufen[i - 1], b = n.Stufen[i];
                Assert.True(b.VolumenL > a.VolumenL);
                Assert.True(b.StartsJeTag <= a.StartsJeTag + 1e-9, $"Starts {a.StartsJeTag} → {b.StartsJeTag}");
                Assert.True(b.Deckungsgrad >= a.Deckungsgrad - 1e-12);
                Assert.True(b.Verlust.KwhJeJahr > a.Verlust.KwhJeJahr);
            }
            Assert.True(n.Stufen[0].StartsJeTag > n.Stufen[4].StartsJeTag);
            Assert.Equal("PAUS_JAZ_GROESSER", n.Stufen[4].JazHinweis.Schluessel);
            Assert.Equal("PAUS_JAZ_KLEINER", n.Stufen[0].JazHinweis.Schluessel);
            Assert.Equal("PAUS_JAZ_EMPFEHLUNG", n.Stufen[2].JazHinweis.Schluessel);
            Assert.Contains("JAZ sinkt", n.Stufen[4].JazHinweis.Klartext);
        }

        [Fact]
        public void Rasterstufen_am_Listenrand_und_ueber_dem_Ende()
        {
            Assert.Equal(new double[] { 100, 150, 200 }, PufferAuslegung.Rasterstufen(100, PufferAuslegung.NENNINHALTE_VORGABE, 1000, 2));
            Assert.Equal(new double[] { 8000, 10000, 12000, 13000, 14000 },
                         PufferAuslegung.Rasterstufen(12000, PufferAuslegung.NENNINHALTE_VORGABE, 1000, 2));
            Assert.Equal(new double[] { 5000, 8000, 10000, 11000, 12000 },
                         PufferAuslegung.Rasterstufen(10000, PufferAuslegung.NENNINHALTE_VORGABE, 1000, 2));
        }

        [Fact]
        public void Ohne_Empfehlung_keine_Stufen_ohne_Reihe_nur_Volumen_und_Verlust()
        {
            PufferAuslegungEingang e = Waermepumpe();
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            Assert.Empty(PufferAuslegung.Nachbarstufen(e, r with { EmpfehlungL = 0 }).Stufen);

            PufferNachbarstufen ohne = PufferAuslegung.Nachbarstufen(e with { ReiheHeizung = Array.Empty<double>() }, r);
            Assert.Null(ohne.Simulationszone);
            Assert.All(ohne.Stufen, s => { Assert.Null(s.StartsJeTag); Assert.Null(s.Deckungsgrad); Assert.True(s.Verlust.KwhJeJahr > 0); });
        }

        [Fact]
        public void Kurve_faellt_mit_der_Leistung_und_trifft_Dmax_an_der_Ladeleistung()
        {
            PufferAuslegungEingang e = Waermepumpe() with
            {
                KlasseBrauchwasser = true, ReiheBrauchwasser = Zapfreihe(),
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 20, LadeleistungKw = 3.5, TagesbedarfKwh = 60 }
            };
            PufferNachbarstufen n = PufferAuslegung.Nachbarstufen(e, PufferAuslegung.Rechnen(e));
            Assert.Equal(5, n.Kurve.Count);
            for (int i = 1; i < n.Kurve.Count; i++)
            {
                Assert.True(n.Kurve[i].LeistungKw > n.Kurve[i - 1].LeistungKw);
                Assert.True(n.Kurve[i].VolumenL <= n.Kurve[i - 1].VolumenL + 1e-9);
            }
            Assert.True(n.Kurve[0].VolumenL > n.Kurve[4].VolumenL);
            PufferLeistungspunkt mitte = n.Kurve[2];
            Assert.Equal(3.5, mitte.LeistungKw, 9);
            Assert.Equal(20, mitte.LaufvolumenKwh, 9);
            // V = D_max · 1000 / (c · ΔT_B · η_s), ΔT_B = 65 − 25 K, η_s = 0,85.
            Assert.Equal(20 * 1000 / (1.16 * 40 * 0.85), mitte.VolumenL, 6);
            // Laufzeit 60 kWh / 3,5 kW = 17,1 h liegt im Band 16–20 h.
            Assert.Equal(60 / 3.5, mitte.LaufzeitH.Value, 9);
            Assert.True(mitte.ImLaufzeitband);
            Assert.False(n.Kurve[0].ImLaufzeitband);
        }

        [Fact]
        public void Ohne_Zapfprofil_oder_Ladeleistung_keine_Kurve()
        {
            PufferAuslegungEingang e = Waermepumpe();
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            PufferNachbarstufen a = PufferAuslegung.Nachbarstufen(e, r);
            Assert.Empty(a.Kurve);
            Assert.Equal("PAUS_KURVE_KEIN_ZAPFPROFIL", a.KurveHinweis.Schluessel);

            PufferAuslegungEingang b = e with
            {
                KlasseBrauchwasser = true, ReiheBrauchwasser = Zapfreihe(),
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 20 }
            };
            PufferNachbarstufen nb = PufferAuslegung.Nachbarstufen(b, PufferAuslegung.Rechnen(b));
            Assert.Empty(nb.Kurve);
            Assert.Equal("PAUS_KURVE_OHNE_LADELEISTUNG", nb.KurveHinweis.Schluessel);

            PufferAuslegungEingang c = b with { ReiheBrauchwasser = Array.Empty<double>(), Zapfprofil = b.Zapfprofil with { LadeleistungKw = 4 } };
            PufferNachbarstufen nc = PufferAuslegung.Nachbarstufen(c, PufferAuslegung.Rechnen(c));
            Assert.Empty(nc.Kurve);
            Assert.Equal("PAUS_KURVE_OHNE_REIHE", nc.KurveHinweis.Schluessel);
        }

        [Fact]
        public void Laufvolumen_ist_das_groesste_Defizit_der_Spitze()
        {
            Assert.Equal(6, PufferAuslegung.Laufvolumen(new double[] { 1, 5, 4, 1, 0, 6 }, 2), 9);
            Assert.Equal(0, PufferAuslegung.Laufvolumen(new double[] { 1, 1 }, 2), 9);
        }
    }
}
