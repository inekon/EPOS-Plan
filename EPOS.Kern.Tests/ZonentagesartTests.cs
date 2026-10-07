using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Tagesbetriebsart je Zone und Kalenderfreigabe</b> (Entwurf AK3-K, Abschnitt 3, Festlegungen 1–7; Welle KZ) —
    /// die Proben des Kernschalters <see cref="Zonensperre"/>:
    /// <list type="number">
    /// <item>die Regel der Freigabe (<see cref="Zonenfreigabe"/>, 3.1, 3.3);</item>
    /// <item><b>ohne Kühlung bitgleich</b> — Einzone und Mehrzonen, Schalter ein gegen aus;</item>
    /// <item><b>nur eine Freigabe bitgleich</b> — ein Kalender, der je Tag nur eine Seite freigibt, rechnet wie heute;</item>
    /// <item><b>der Mischtag wird neu gerechnet</b> — kein Tag heizt und kühlt, die Sperre steht als „aus" in der Reihe,
    /// bis zum ersten Mischtag Bit für Bit der Bestand;</item>
    /// <item>Mehrzonen: jede Zone ihre eigene Tagesart;</item>
    /// <item>die Erzeugertagesart (K8a) aus den Kanälen nach der Sperre;</item>
    /// <item><b>Brauchwasser und Prozess frei</b> — im Projektlauf bleiben ihre Kanäle Bit für Bit.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonentagesartTests : IDisposable
    {
        /// <summary>
        /// Das Probeklima mit großem Tagesgang (±9 K um das Tagesmittel): Im Frühjahr und Herbst sind die Nächte kalt und die
        /// Nachmittage warm — die Zone heizt und kühlt am selben Tag (der Jahresgang der übrigen Proben zeigt keinen Mischtag).
        /// </summary>
        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(
            h => 12.0 - 10.0 * Math.Cos(2.0 * Math.PI * (h - 360.0) / 8760.0) - 9.0 * Math.Cos(2.0 * Math.PI * (h % 24 - 3) / 24.0));

        private static GebaeudeKlima Mehrzonenklima()
            => new GebaeudeKlima(Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);
        private readonly ITestOutputHelper _aus;

        public ZonentagesartTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => Zonensperre.An = false;

        private static GebaeudeModellEingang Eingang(bool kuehlbetrieb)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 24.0;
            return GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb);
        }

        private static GebaeudeModellErgebnis Lauf(GebaeudeModellEingang e, bool an)
        {
            using (Zonensperre.Schalten(an)) return Vdi6007Rechenweg.Laufen(e, 0, 1);
        }

        private static void Bitgleich(string wer, GebaeudeModellEingang e1, GebaeudeModellErgebnis r1,
                                      GebaeudeModellEingang e2, GebaeudeModellErgebnis r2)
        {
            List<(string Name, double[] Werte)> a = GebaeudeEinzonennetzTests.Reihen(e1, r1);
            List<(string Name, double[] Werte)> b = GebaeudeEinzonennetzTests.Reihen(e2, r2);
            Assert.Equal(a.Select(x => x.Name), b.Select(x => x.Name));
            for (int i = 0; i < a.Count; i++)
                Assert.True(GebaeudeEinzonennetzTests.Bilden(a[i].Werte).Sha256 == GebaeudeEinzonennetzTests.Bilden(b[i].Werte).Sha256,
                            wer + ": Reihe " + a[i].Name + " weicht ab.");
            Assert.Equal(r1.StundenMitUmschaltung, r2.StundenMitUmschaltung);
            Assert.Equal(r1.StundenHeizenUndKuehlen, r2.StundenHeizenUndKuehlen);
            Assert.Equal(r1.StundenMitSommerlueftung, r2.StundenMitSommerlueftung);
        }

        /// <summary>Die Tage, an denen eine Zone heizt und kühlt (Heizlast W, Kühlbedarf kWh).</summary>
        private static List<int> Mischtage(GebaeudeModellErgebnis r)
        {
            var tage = new List<int>();
            for (int d = 0; d < 365; d++)
            {
                double h = 0.0, k = 0.0;
                for (int s = d * 24; s < d * 24 + 24; s++)
                {
                    h += r.HeizlastW[s];
                    k += r.KuehlbedarfKwh?[s] ?? 0.0;
                }
                if (h > 0.0 && k > 0.0) tage.Add(d);
            }
            return tage;
        }

        // =====================================================================
        //  Die Regel der Freigabe
        // =====================================================================

        [Fact]
        public void Die_Freigabe_liest_das_aus_der_Sollwertreihen_je_Tag()
        {
            var heiz = Enumerable.Repeat(20.0, 8760).ToArray();
            var kuehl = Enumerable.Repeat(double.PositiveInfinity, 8760).ToArray();
            for (int h = 24; h < 48; h++) heiz[h] = double.NaN;     // Tag 1: Heizen aus
            kuehl[2 * 24 + 13] = 26.0;                              // Tag 2: eine Kühlstunde
            for (int h = 3 * 24; h < 4 * 24; h++) { heiz[h] = double.NaN; kuehl[h] = 25.0; }
            for (int h = 4 * 24; h < 5 * 24; h++) heiz[h] = double.NaN;

            Assert.Equal(Freigabeart.Heizen, Zonenfreigabe.Tag(heiz, kuehl, true, 0));
            Assert.Equal(Freigabeart.Keine, Zonenfreigabe.Tag(heiz, kuehl, true, 1));
            Assert.Equal(Freigabeart.Beides, Zonenfreigabe.Tag(heiz, kuehl, true, 2));
            Assert.Equal(Freigabeart.Heizen, Zonenfreigabe.Tag(heiz, kuehl, false, 2));      // ohne wirksame Kühlung
            Assert.Equal(Freigabeart.Kuehlen, Zonenfreigabe.Tag(heiz, kuehl, true, 3));
            Assert.Equal(Freigabeart.Keine, Zonenfreigabe.Tag(heiz, kuehl, true, 4));
            // Ohne Kalender gibt der Bestand die Seite frei; NaN als Kühl-„aus" zählt wie +∞.
            Assert.Equal(Freigabeart.Beides, Zonenfreigabe.Tag(null, null, true, 100));
            Assert.Equal(Freigabeart.Heizen, Zonenfreigabe.Tag(null, Enumerable.Repeat(double.NaN, 8760).ToArray(), true, 100));
            Freigabeart[] jahr = Zonenfreigabe.Jahr(heiz, kuehl, true);
            Assert.Equal(365, jahr.Length);
            Assert.Equal(Freigabeart.Kuehlen, jahr[3]);
        }

        // =====================================================================
        //  Bitgleich, wo keine Zone beide Freigaben hat
        // =====================================================================

        [Fact]
        public void Ohne_Kuehlung_rechnet_der_Schalter_bitgleich()
        {
            GebaeudeModellEingang e1 = Eingang(false), e2 = Eingang(false);
            Assert.False(e1.KuehlungWirksam);
            GebaeudeModellErgebnis aus = Lauf(e1, false), ein = Lauf(e2, true);
            Bitgleich("Einzone", e1, aus, e2, ein);
            Assert.Null(ein.Zonensperre);

            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            Mehrzonenergebnis m1, m2;
            using (Zonensperre.Schalten(false)) m1 = Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), false, null, 0, g.ID_Gebaeude);
            using (Zonensperre.Schalten(true)) m2 = Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), false, null, 0, g.ID_Gebaeude);
            for (int z = 0; z < m1.Zonen.Count; z++)
            {
                Assert.Equal(ZonenschleifeTests.Abdruck(m1.Zonen[z].HeizlastW), ZonenschleifeTests.Abdruck(m2.Zonen[z].HeizlastW));
                Assert.Equal(ZonenschleifeTests.Abdruck(m1.Zonen[z].Raumtemperatur), ZonenschleifeTests.Abdruck(m2.Zonen[z].Raumtemperatur));
                Assert.Null(m2.Zonen[z].Zonensperre);
            }
            Assert.Equal(m1.Schleife.IterierteStunden, m2.Schleife.IterierteStunden);
        }

        [Fact]
        public void Nur_eine_Freigabe_je_Tag_rechnet_bitgleich()
        {
            GebaeudeModellEingang e1 = Eingang(true), e2 = Eingang(true);
            foreach (GebaeudeModellEingang e in new[] { e1, e2 })
                for (int h = 0; h < 8760; h++)
                {
                    // Heizperiode im Winterhalbjahr, Kühlperiode im Sommerhalbjahr (Tag 120 bis 272).
                    int d = h / 24;
                    if (d >= 120 && d < 273) e.ThetaSoll[h] = double.NaN;
                    else e.ThetaMax[h] = double.PositiveInfinity;
                }
            Assert.True(e1.KuehlungWirksam);
            GebaeudeModellErgebnis aus = Lauf(e1, false), ein = Lauf(e2, true);
            Bitgleich("eine Freigabe", e1, aus, e2, ein);
            Assert.NotNull(ein.Zonensperre);
            Assert.Equal(0, ein.Zonensperre.TageBeides);
            Assert.Equal(0, ein.Zonensperre.Tage);
            Assert.True(ein.KuehlbedarfKwh.Sum() > 0.0, "Die Probe kühlt nicht.");
        }

        // =====================================================================
        //  Der Mischtag
        // =====================================================================

        [Fact]
        public void Der_Mischtag_wird_mit_gesperrter_Gegenseite_neu_gerechnet()
        {
            GebaeudeModellEingang e1 = Eingang(true), e2 = Eingang(true);
            GebaeudeModellErgebnis aus = Lauf(e1, false), ein = Lauf(e2, true);
            List<int> mischAus = Mischtage(aus);
            _aus.WriteLine("Mischtage ohne Sperre: " + mischAus.Count + ", Sperre: " + ein.Zonensperre);
            Assert.NotEmpty(mischAus);
            Assert.Empty(Mischtage(ein));

            Zonensperrkennzahl k = ein.Zonensperre;
            Assert.NotNull(k);
            Assert.Equal(365, k.TageBeides);
            Assert.True(k.Tage > 0 && k.Stunden > 0 && k.GesperrtKwh > 0.0);

            // Die Sperre steht als „aus" in den Reihen des Eingangs und des Ergebnisses — ganze Tage.
            int heizAus = 0, kuehlAus = 0;
            for (int d = 0; d < 365; d++)
            {
                bool h = Enumerable.Range(d * 24, 24).All(s => double.IsNaN(e2.ThetaSoll[s]));
                bool c = Enumerable.Range(d * 24, 24).All(s => double.IsPositiveInfinity(e2.ThetaMax[s]));
                if (h) heizAus++;
                if (c) kuehlAus++;
                Assert.False(h && c, "Tag " + d + " ohne jede Freigabe.");
            }
            Assert.Equal(k.Kuehltage, heizAus);
            Assert.Equal(k.Heiztage, kuehlAus);
            Assert.Equal(heizAus, Enumerable.Range(0, 365).Count(d => double.IsNaN(ein.Heizsollwert[d * 24])));

            // Bis zum ersten Mischtag rechnet der Lauf Bit für Bit wie ohne Sperre.
            int erste = mischAus[0] * 24;
            for (int s = 0; s < erste; s++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(aus.HeizlastW[s]), BitConverter.DoubleToInt64Bits(ein.HeizlastW[s]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(aus.Raumtemperatur[s]), BitConverter.DoubleToInt64Bits(ein.Raumtemperatur[s]));
            }
            // Die Tagesart folgt den Tagessummen des Probetags: Kühltag ⇔ Σ Kühlen > Σ Heizen (Gleichstand: Heiztag).
            int d0 = mischAus[0];
            double heiz = 0.0, kuehl = 0.0;
            for (int s = d0 * 24; s < d0 * 24 + 24; s++) { heiz += aus.HeizlastW[s]; kuehl += aus.KuehlbedarfKwh[s] * 1000.0; }
            bool kuehltag = kuehl > heiz;
            for (int s = d0 * 24; s < d0 * 24 + 24; s++)
                Assert.True(kuehltag ? ein.HeizlastW[s] == 0.0 : ein.KuehlbedarfKwh[s] == 0.0);
            // Deterministisch.
            GebaeudeModellErgebnis noch = Lauf(Eingang(true), true);
            Assert.Equal(ZonenschleifeTests.Abdruck(ein.HeizlastW), ZonenschleifeTests.Abdruck(noch.HeizlastW));
            Assert.Equal(ZonenschleifeTests.Abdruck(ein.KuehlbedarfKwh), ZonenschleifeTests.Abdruck(noch.KuehlbedarfKwh));
        }

        [Fact]
        public void Mehrzonen_tragen_je_Zone_ihre_Tagesart()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 22.0;
            Mehrzonenergebnis aus, ein;
            using (Zonensperre.Schalten(false)) aus = Zonenrechnung.Rechnen(g, Mehrzonenklima(), true, null, 0, g.ID_Gebaeude);
            using (Zonensperre.Schalten(true)) ein = Zonenrechnung.Rechnen(g, Mehrzonenklima(), true, null, 0, g.ID_Gebaeude);
            int mischAus = aus.Zonen.Sum(z => Mischtage(z).Count);
            _aus.WriteLine("Zonen: " + string.Join("; ", aus.Zonen.Select(z => z.HeizlastW.Sum().ToString("F0") + " W·h / " +
                                                                              (z.KuehlbedarfKwh?.Sum() ?? -1).ToString("F1") + " kWh")));
            Assert.True(mischAus > 0, "Die Mehrzonenprobe zeigt keinen Mischtag.");
            int gegenlaeufig = 0;
            for (int z = 0; z < ein.Zonen.Count; z++)
            {
                Assert.Empty(Mischtage(ein.Zonen[z]));
                // Kennzahlen trägt jede Zone mit wirksamer Kühlung; die unbeheizte, ungekühlte Zone keine.
                Assert.Equal(ein.Zonen[z].KuehlbedarfKwh != null, ein.Zonen[z].Zonensperre != null);
            }
            // Verschiedene Zonen dürfen am selben Tag verschiedene Betriebsarten haben.
            for (int d = 0; d < 365; d++)
            {
                bool heizt = false, kuehlt = false;
                foreach (GebaeudeModellErgebnis z in ein.Zonen)
                {
                    double h = 0.0, k = 0.0;
                    for (int s = d * 24; s < d * 24 + 24; s++) { h += z.HeizlastW[s]; k += z.KuehlbedarfKwh?[s] ?? 0.0; }
                    heizt |= h > 0.0;
                    kuehlt |= k > 0.0;
                }
                if (heizt && kuehlt) gegenlaeufig++;
            }
            _aus.WriteLine("Mischtage ohne Sperre (Summe der Zonen): " + mischAus + ", Tage mit heizender und kühlender Zone: " +
                           gegenlaeufig + ", Sperrtage je Zone: " + string.Join(", ", ein.Zonen.Select(z => z.Zonensperre?.Tage)));
            Assert.True(ein.Zonen.Sum(z => z.Zonensperre?.Tage ?? 0) > 0);
        }

        [Fact]
        public void Die_Erzeugertagesart_folgt_der_Zone_nach_der_Sperre()
        {
            GebaeudeModellErgebnis ein = Lauf(Eingang(true), true);
            var heiz = new double[8760];
            for (int h = 0; h < 8760; h++) heiz[h] = ein.HeizlastW[h] / 1000.0;
            bool[] kuehltag = Kaeltekaskade.TagesbetriebsartBestimmen(heiz, ein.KuehlbedarfKwh);
            for (int d = 0; d < 365; d++)
            {
                double h = 0.0, k = 0.0;
                for (int s = d * 24; s < d * 24 + 24; s++) { h += heiz[s]; k += ein.KuehlbedarfKwh[s]; }
                Assert.False(h > 0.0 && k > 0.0, "Tag " + d);
                Assert.Equal(k > 0.0, kuehltag[d]);
            }
        }

        // =====================================================================
        //  Brauchwasser und Prozess bleiben frei (Festlegungen 4, 5)
        // =====================================================================

        [Fact]
        public void Brauchwasser_und_Prozess_bleiben_im_Projektlauf_frei()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int PROJEKT = 1007;   // Bürobau mit Brauchwasserzuordnung; hier mit Kühlung
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Kuehlbetrieb = 1 WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24 WHERE ID_Projekt = ?",
                                           new DbParam("@p", PROJEKT));

            Kanalsatz aus = Kanaele(PROJEKT, false), ein = Kanaele(PROJEKT, true);
            Assert.Equal(ZonenschleifeTests.Abdruck(aus.Brauchwasser), ZonenschleifeTests.Abdruck(ein.Brauchwasser));
            Assert.Equal(ZonenschleifeTests.Abdruck(aus.Prozess), ZonenschleifeTests.Abdruck(ein.Prozess));
            Assert.True(aus.Brauchwasser.Sum() > 0.0, "Projekt 1007 trägt kein Brauchwasser.");
            Assert.True(aus.Kuehlung.Sum() > 0.0, "Projekt 1007 kühlt nicht.");
            _aus.WriteLine("Raumheizung " + aus.Heizung.Sum().ToString("F1") + " → " + ein.Heizung.Sum().ToString("F1") +
                           " kWh, Kühlung " + aus.Kuehlung.Sum().ToString("F1") + " → " + ein.Kuehlung.Sum().ToString("F1") + " kWh");
            Assert.NotEqual(ZonenschleifeTests.Abdruck(aus.Heizung), ZonenschleifeTests.Abdruck(ein.Heizung));
        }

        private static Kanalsatz Kanaele(int projekt, bool an)
        {
            using (Zonensperre.Schalten(an))
            {
                SimulationProtokoll.NeuStarten();
                var lauf = new SimulationRunner();
                int kopf = lauf.SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(kopf > 0, fehler);
                return lauf.simulation_Waermebedarf.KanaeleDrei();
            }
        }
    }
}
