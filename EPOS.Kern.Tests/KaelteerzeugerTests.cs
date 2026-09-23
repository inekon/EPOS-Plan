using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kälteerzeuger, Stufe KU2 Welle 2</b> — die reversible Wärmepumpe im Kern
    /// (Kühlkonzept 5.0–5.5, 6.1, 10.2, 10.3; Entscheide E15, E21, E27, E33).
    ///
    /// <para><b>Ohne Datenbank:</b> die Stützstellenprobe je Vorlauf und die Vorlaufwahl (K21),
    /// Laststufe, Dubletten und Achsenlage (K22), die Kennlinie außerhalb ihrer Stützstellen; die
    /// Tagesbetriebsart (K8a) und der Zeitanteil; die Rechenprobe der Kältekaskade gegen die
    /// Handrechnung JE VORLAUF samt Teillast, Hilfsstrom (K23) und Kaskadenreihenfolge; die
    /// Deckungsprobe Kälte (#31); der Deckungsgrad mit zwei Nennern (6.4); die Senke „Kältekreis"
    /// (#14–#16); die Importregel.</para>
    ///
    /// <para><b>Mit Datenbank (10.3):</b> die zwei Kennlinienprüfungen, die Sperrgründe im
    /// Schreibweg und im Lauf, der gekennzeichnete Bestandssatz und Läufe mit einer reversiblen
    /// Wärmepumpe — Heizkanal am Kühltag gesperrt, Brauchwasser bedient, Kälte gedeckt, Kältestrom
    /// in der Strombilanz, Deckungsspalte, Skalare für den Export.</para>
    ///
    /// <para><b>Phantasie-Kennlinien.</b> Keine Normzahl und kein Produktwert: Die Kennlinien der
    /// Fälle tragen runde, erfundene Werte (Kühlkonzept 11.3, Hausregel „keine Produktdaten").</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaelteerzeugerTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaelteerzeugerTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  Die Phantasie-Kennlinie: zwei Vorläufe, fünf Temperaturen, dazu eine Teillaststufe
        // =============================================================================

        private static readonly int[] TEMPERATUREN = { 20, 25, 30, 35, 40 };
        private static readonly double[] EER_7 = { 4.0, 3.6, 3.2, 2.8, 2.4 };
        private static readonly double[] PK_7 = { 12.0, 11.5, 11.0, 10.5, 10.0 };
        private static readonly double[] EER_18 = { 5.5, 5.0, 4.5, 4.0, 3.5 };
        private static readonly double[] PK_18 = { 15.0, 14.5, 14.0, 13.5, 13.0 };

        /// <summary>Die Zeilen der Phantasie-Kennlinie — Laststufe 100, dazu eine Teillast 50 mit anderen Werten.</summary>
        private static List<KuehlkennlinienZeile> Phantasiezeilen(bool mitTeillast = true)
        {
            var z = new List<KuehlkennlinienZeile>();
            int id = 1;
            for (int i = 0; i < TEMPERATUREN.Length; i++)
            {
                z.Add(new KuehlkennlinienZeile(id++, 7, TEMPERATUREN[i], EER_7[i], PK_7[i], 100));
                z.Add(new KuehlkennlinienZeile(id++, 18, TEMPERATUREN[i], EER_18[i], PK_18[i], 100));
                if (mitTeillast)
                {
                    z.Add(new KuehlkennlinienZeile(id++, 7, TEMPERATUREN[i], 9.9, 1.0, 50));
                    z.Add(new KuehlkennlinienZeile(id++, 18, TEMPERATUREN[i], 9.9, 1.0, 50));
                }
            }
            return z;
        }

        private static double Linear(double x0, double x1, double y0, double y1, double x)
            => y0 + (x - x0) * (y1 - y0) / (x1 - x0);

        // =============================================================================
        //  Teil 1 — Die Kühlkennlinie ohne Datenbank (5.1; K21, K22; 10.2)
        // =============================================================================

        /// <summary>
        /// <b>EER-Stützstellenprobe je Vorlauf</b> (10.2): Für JEDE Vorlauf-Stützstelle wird jede
        /// Temperatur-Stützstelle exakt getroffen und ein Zwischenwert linear interpoliert; zwei
        /// Vorläufe ergeben bei derselben Außentemperatur zwei verschiedene EER — der Kühl-Vorlauf
        /// wirkt und wird nicht übergangen. Gerechnet wird die höchste Laststufe (MAX(Last)).
        /// </summary>
        [Fact]
        public void Die_Stuetzstellenprobe_trifft_je_Vorlauf_jede_Stuetzstelle_exakt_und_interpoliert_linear()
        {
            List<KuehlkennlinienZeile> zeilen = Phantasiezeilen();
            foreach (int vorlauf in new[] { 7, 18 })
            {
                Kuehlkennlinie k = Kuehlkennlinie.Bilden(zeilen, vorlauf);
                double[] eer = vorlauf == 7 ? EER_7 : EER_18;
                double[] pk = vorlauf == 7 ? PK_7 : PK_18;

                Assert.True(k.Rechenbar);
                Assert.Equal(vorlauf, k.Vorlauf);
                Assert.False(k.VorlaufAusgewichen);
                Assert.Equal(100, k.Laststufe);
                Assert.Equal(TEMPERATUREN.Length, k.Punkte);
                Assert.Equal(new[] { 7, 18 }, k.Stuetzstellen);

                for (int i = 0; i < TEMPERATUREN.Length; i++)
                {
                    KennlinienPunkt p = k.Auswerten(TEMPERATUREN[i], true);
                    Assert.Equal(eer[i], p.Eer);        // exakt, nicht nur nahe
                    Assert.Equal(pk[i], p.Pkuehl);
                    Assert.Equal(KennlinienLage.Innen, p.Lage);
                }

                for (int i = 1; i < TEMPERATUREN.Length; i++)
                {
                    double t = (TEMPERATUREN[i - 1] + TEMPERATUREN[i]) / 2.0 + 1.25;
                    KennlinienPunkt p = k.Auswerten(t, true);
                    Assert.Equal(Linear(TEMPERATUREN[i - 1], TEMPERATUREN[i], eer[i - 1], eer[i], t), p.Eer, 12);
                    Assert.Equal(Linear(TEMPERATUREN[i - 1], TEMPERATUREN[i], pk[i - 1], pk[i], t), p.Pkuehl, 12);
                }
            }

            // Zwei Vorläufe, dieselbe Temperatur, zwei EER.
            double eer7 = Kuehlkennlinie.Bilden(zeilen, 7).Auswerten(27.5, true).Eer;
            double eer18 = Kuehlkennlinie.Bilden(zeilen, 18).Auswerten(27.5, true).Eer;
            Assert.NotEqual(eer7, eer18);
            Assert.True(eer18 > eer7, "Wärmeres Kaltwasser hat den besseren EER.");
        }

        /// <summary>
        /// K21: NULL wählt den kleinsten Stützwert; ein Wunsch, der keine Stützstelle ist, rechnet
        /// mit der nächsten — bei gleichem Abstand der kälteren — und ist als ausgewichen markiert.
        /// </summary>
        [Fact]
        public void Kuehl_Vorlauf_NULL_waehlt_den_kleinsten_Stuetzwert_und_ein_Zwischenwert_die_naechste()
        {
            List<KuehlkennlinienZeile> zeilen = Phantasiezeilen();

            Kuehlkennlinie ohne = Kuehlkennlinie.Bilden(zeilen, null);
            Assert.Equal(7, ohne.Vorlauf);
            Assert.False(ohne.VorlaufAusgewichen);

            Kuehlkennlinie zwoelf = Kuehlkennlinie.Bilden(zeilen, 12);
            Assert.Equal(7, zwoelf.Vorlauf);
            Assert.True(zwoelf.VorlaufAusgewichen);

            Assert.Equal(18, Kuehlkennlinie.Bilden(zeilen, 13).Vorlauf);
            Assert.Equal(18, Kuehlkennlinie.Bilden(zeilen, 40).Vorlauf);

            // Gleicher Abstand: die kältere Stützstelle.
            Assert.Equal(10, Kuehlkennlinie.VorlaufWaehlen(new[] { 10, 20 }, 15));
            Assert.Equal(10, Kuehlkennlinie.VorlaufWaehlen(new[] { 20, 10 }, null));
        }

        /// <summary>
        /// Laststufe (Festlegung 1): die höchste JE VORLAUF; Zeilen ohne Laststufe gelten nur, wenn der
        /// Vorlauf gar keine trägt.
        /// </summary>
        [Fact]
        public void Die_Laststufe_ist_die_hoechste_des_Vorlaufs_und_Zeilen_ohne_Stufe_gelten_nur_allein()
        {
            var z = new List<KuehlkennlinienZeile>
            {
                new KuehlkennlinienZeile(1, 7, 20, 3.0, 9.0, 60),
                new KuehlkennlinienZeile(2, 7, 30, 2.0, 8.0, 60),
                new KuehlkennlinienZeile(3, 7, 20, 3.5, 11.0, 90),
                new KuehlkennlinienZeile(4, 7, 30, 2.5, 10.0, 90),
                new KuehlkennlinienZeile(5, 7, 25, 9.0, 99.0, null),
                new KuehlkennlinienZeile(6, 18, 20, 4.0, 12.0, null),
                new KuehlkennlinienZeile(7, 18, 30, 3.0, 11.0, null),
            };

            Kuehlkennlinie sieben = Kuehlkennlinie.Bilden(z, 7);
            Assert.Equal(90, sieben.Laststufe);
            Assert.Equal(2, sieben.Punkte);
            Assert.Equal(11.0, sieben.Auswerten(20, true).Pkuehl);

            Kuehlkennlinie achtzehn = Kuehlkennlinie.Bilden(z, 18);
            Assert.Null(achtzehn.Laststufe);
            Assert.Equal(2, achtzehn.Punkte);
            Assert.Equal(12.0, achtzehn.Auswerten(20, true).Pkuehl);
        }

        /// <summary>
        /// Dubletten (Welle-1-Befund: jede Stützstelle der Testdatenbank steht doppelt): Gleiche
        /// Mehrfachzeilen werden zusammengefasst und gezählt; abweichende ebenfalls gezählt, und es
        /// gilt die zuerst gespeicherte (kleinste ID) — unabhängig von der Lesereihenfolge.
        /// </summary>
        [Fact]
        public void Dubletten_werden_deterministisch_zusammengefasst_und_benannt_gezaehlt()
        {
            var gleich = new List<KuehlkennlinienZeile>
            {
                new KuehlkennlinienZeile(11, 7, 20, 4.0, 12.0, 100),
                new KuehlkennlinienZeile(12, 7, 30, 3.0, 10.0, 100),
                new KuehlkennlinienZeile(21, 7, 20, 4.0, 12.0, 100),
                new KuehlkennlinienZeile(22, 7, 30, 3.0, 10.0, 100),
            };
            Kuehlkennlinie k = Kuehlkennlinie.Bilden(gleich, null);
            Assert.Equal(2, k.Punkte);
            Assert.Equal(2, k.Dubletten);
            Assert.Equal(0, k.DublettenAbweichend);

            var abweichend = new List<KuehlkennlinienZeile>
            {
                new KuehlkennlinienZeile(30, 7, 20, 5.0, 20.0, 100),     // später gespeichert
                new KuehlkennlinienZeile(12, 7, 30, 3.0, 10.0, 100),
                new KuehlkennlinienZeile(11, 7, 20, 4.0, 12.0, 100),     // zuerst gespeichert
            };
            Kuehlkennlinie a = Kuehlkennlinie.Bilden(abweichend, null);
            Kuehlkennlinie b = Kuehlkennlinie.Bilden(abweichend.AsEnumerable().Reverse(), null);
            Assert.Equal(1, a.DublettenAbweichend);
            Assert.Equal(0, a.Dubletten);
            Assert.Equal(4.0, a.Auswerten(20, true).Eer);
            Assert.Equal(12.0, a.Auswerten(20, true).Pkuehl);
            Assert.Equal(a.Auswerten(20, true).Eer, b.Auswerten(20, true).Eer);
        }

        /// <summary>
        /// K22: Heizlage (Vorlauf ab 30 °C oder jede Stützstelle kälter als der Vorlauf) und vertauschte
        /// Achsen (Temperatur = Vorlauf in jeder Zeile) werden erkannt — und nicht gerechnet.
        /// </summary>
        [Fact]
        public void Heizlage_und_vertauschte_Achsen_werden_erkannt_und_nicht_gerechnet()
        {
            Assert.Equal(KuehlblockBefund.Heizlage, Kuehlkennlinie.BlockBefund(35, new[] { -5, 0, 5, 10 }));
            Assert.Equal(KuehlblockBefund.Heizlage, Kuehlkennlinie.BlockBefund(45, new[] { 20, 30, 40 }));
            Assert.Equal(KuehlblockBefund.Heizlage, Kuehlkennlinie.BlockBefund(25, new[] { -10, -5, 0, 5, 10 }));
            Assert.Equal(KuehlblockBefund.AchsenVertauscht, Kuehlkennlinie.BlockBefund(7, new[] { 7 }));
            Assert.Equal(KuehlblockBefund.AchsenVertauscht, Kuehlkennlinie.BlockBefund(18, new[] { 18, 18 }));
            Assert.Equal(KuehlblockBefund.Gueltig, Kuehlkennlinie.BlockBefund(7, new[] { 20, 30, 35 }));
            Assert.Equal(KuehlblockBefund.Gueltig, Kuehlkennlinie.BlockBefund(13, new[] { -5, 0, 10, 20, 30 }));
            Assert.Equal(KuehlblockBefund.Gueltig, Kuehlkennlinie.BlockBefund(18, new[] { 35 }));

            var achsen = new List<KuehlkennlinienZeile>
            {
                new KuehlkennlinienZeile(1, 7, 7, 2.0, 11.0, 70),
                new KuehlkennlinienZeile(2, 18, 18, 2.5, 13.0, 70),
            };
            Kuehlkennlinie k = Kuehlkennlinie.Bilden(achsen, null);
            Assert.Equal(KuehlblockBefund.AchsenVertauscht, k.Befund);
            Assert.False(k.Rechenbar);
            Assert.Equal(0.0, k.Auswerten(25, true).Pkuehl);

            IReadOnlyList<KeyValuePair<int, KuehlblockBefund>> befunde = Kuehlkennlinie.Befunde(achsen);
            Assert.Equal(2, befunde.Count);
            Assert.All(befunde, b => Assert.Equal(KuehlblockBefund.AchsenVertauscht, b.Value));
        }

        /// <summary>
        /// Außerhalb der Stützstellen (5.1, „wie auf der Heizseite", gespiegelt): Zur günstigen, kalten
        /// Seite wird gekappt; zur ungünstigen, warmen Seite linear verlängert, wenn die
        /// Projekteinstellung es erlaubt, sonst gekappt; wer dabei auf 0 fällt, liefert keine Kälte.
        /// Eine Kennlinie mit einer Stützstelle gilt konstant.
        /// </summary>
        [Fact]
        public void Ausserhalb_der_Stuetzstellen_kalt_gekappt_warm_verlaengert_oder_gekappt()
        {
            Kuehlkennlinie k = Kuehlkennlinie.Bilden(Phantasiezeilen(false), 7);

            KennlinienPunkt kalt = k.Auswerten(12.0, true);
            Assert.Equal(KennlinienLage.KappungUnten, kalt.Lage);
            Assert.Equal(EER_7[0], kalt.Eer);
            Assert.Equal(PK_7[0], kalt.Pkuehl);

            KennlinienPunkt warm = k.Auswerten(45.0, true);
            Assert.Equal(KennlinienLage.ExtrapolationOben, warm.Lage);
            Assert.Equal(Linear(35, 40, EER_7[3], EER_7[4], 45.0), warm.Eer, 12);
            Assert.Equal(Linear(35, 40, PK_7[3], PK_7[4], 45.0), warm.Pkuehl, 12);

            KennlinienPunkt gekappt = k.Auswerten(45.0, false);
            Assert.Equal(KennlinienLage.KappungOben, gekappt.Lage);
            Assert.Equal(EER_7[4], gekappt.Eer);

            // Weit über der Kennlinie fällt der EER der Verlängerung auf 0: keine Kälte.
            KennlinienPunkt heiss = k.Auswerten(100.0, true);
            Assert.Equal(0.0, heiss.Pkuehl);

            var einzeln = new List<KuehlkennlinienZeile> { new KuehlkennlinienZeile(1, 7, 35, 2.5, 4.5, 70) };
            Kuehlkennlinie e = Kuehlkennlinie.Bilden(einzeln, null);
            Assert.Equal(KennlinienLage.EinzelneStuetzstelle, e.Auswerten(28.0, true).Lage);
            Assert.Equal(4.5, e.Auswerten(28.0, true).Pkuehl);
            Assert.Equal(KennlinienLage.Innen, e.Auswerten(35.0, true).Lage);
        }

        // =============================================================================
        //  Teil 2 — Die Kältekaskade ohne Datenbank (5.2, 5.5, 6.1; 10.2)
        // =============================================================================

        private static double[] Konstant(double wert)
        {
            var r = new double[Kaeltekaskade.STUNDEN];
            for (int h = 0; h < r.Length; h++) r[h] = wert;
            return r;
        }

        private static Kaelteerzeuger Erzeuger(int vorlauf, double temperatur, double hilfsstrom, bool[] kuehltage, double[] heizanteil = null)
        {
            return new Kaelteerzeuger
            {
                AnlagenID = 900 + vorlauf,
                IdWp = 9000 + vorlauf,
                Bezeichner = "Phantasie " + vorlauf,
                Modulindex = 0,
                Kennlinie = Kuehlkennlinie.Bilden(Phantasiezeilen(), vorlauf),
                Hilfsstromanteil = hilfsstrom,
                Quelltemperatur = Konstant(temperatur),
                Zeitanteil = Kaeltekaskade.ZeitanteilBilden(kuehltage, heizanteil, false, 0, 0)
            };
        }

        private static bool[] AlleKuehltage()
        {
            var k = new bool[Kaeltekaskade.TAGE];
            for (int d = 0; d < k.Length; d++) k[d] = true;
            return k;
        }

        /// <summary>
        /// K8a (5.2): Kühltag, wenn die Tagessumme im Kühlkanal die im Heizkanal ÜBERSTEIGT; Gleichstand
        /// und ein Tag ohne Bedarf sind Heiztage.
        /// </summary>
        [Fact]
        public void Die_Tagesbetriebsart_folgt_den_Tagessummen_von_Heiz_und_Kuehlkanal()
        {
            var heiz = new double[Kaeltekaskade.STUNDEN];
            var kuehl = new double[Kaeltekaskade.STUNDEN];
            // Tag 0: nur Heizung; Tag 1: mehr Kälte; Tag 2: Gleichstand; Tag 3: nichts;
            // Tag 4: kleine Kälte am Nachmittag, mehr Heizung am Morgen.
            for (int h = 0; h < 24; h++) heiz[h] = 1.0;
            for (int h = 24; h < 48; h++) { heiz[h] = 0.5; kuehl[h] = 0.6; }
            for (int h = 48; h < 72; h++) { heiz[h] = 1.0; kuehl[h] = 1.0; }
            heiz[4 * 24 + 6] = 5.0; kuehl[4 * 24 + 15] = 4.0;

            bool[] tag = Kaeltekaskade.TagesbetriebsartBestimmen(heiz, kuehl);
            Assert.Equal(365, tag.Length);
            Assert.False(tag[0]);
            Assert.True(tag[1]);
            Assert.False(tag[2]);
            Assert.False(tag[3]);
            Assert.False(tag[4]);
        }

        /// <summary>
        /// Der Zeitanteil (5.2): am Heiztag 0; am Kühltag 1 minus Heizzeitanteil (Brauchwasser zuerst,
        /// der Rest an die Kälte); in der Sperrzeit 0.
        /// </summary>
        [Fact]
        public void Der_Zeitanteil_gibt_der_Kaelte_den_Rest_der_Stunde_am_Kuehltag()
        {
            var tage = new bool[Kaeltekaskade.TAGE];
            tage[1] = true;
            var heiz = new double[Kaeltekaskade.STUNDEN];
            heiz[24 + 10] = 0.25;
            heiz[24 + 11] = 1.0;
            heiz[5] = 0.5;

            double[] a = Kaeltekaskade.ZeitanteilBilden(tage, heiz, true, 12, 14);
            Assert.Equal(0.0, a[5]);            // Heiztag
            Assert.Equal(1.0, a[24 + 9]);
            Assert.Equal(0.75, a[24 + 10]);
            Assert.Equal(0.0, a[24 + 11]);      // die ganze Stunde Brauchwasser
            Assert.Equal(0.0, a[24 + 12]);      // Sperrzeit
            Assert.Equal(0.0, a[24 + 13]);
            Assert.Equal(1.0, a[24 + 14]);
            Assert.Equal(0.0, a[2 * 24 + 10]);  // Heiztag

            Assert.Equal(0.0, SimulationWaermepumpe.Heizzeitanteil(0.0, 10.0));
            Assert.Equal(0.25, SimulationWaermepumpe.Heizzeitanteil(2.5, 10.0));
            Assert.Equal(1.0, SimulationWaermepumpe.Heizzeitanteil(12.0, 10.0));
            Assert.Equal(1.0, SimulationWaermepumpe.Heizzeitanteil(1.0, 0.0));
        }

        /// <summary>
        /// <b>Rechenprobe gegen die Handrechnung JE VORLAUF</b> (11.1, 10.2): eine Maschine, Kühltage,
        /// Außentemperatur 27,5 °C (zwischen zwei Stützstellen), Kältebedarf 13 kWh je Stunde.
        /// Vorlauf 7: EER 3,4, Pkuehl 11,25 → Deckung 11,25, Rest 1,75; Vorlauf 18: EER 4,75, Pkuehl
        /// 14,25 → Deckung 13, Rest 0. Kältestrom = Kälte / EER · (1 + Hilfsstromanteil).
        /// </summary>
        [Fact]
        public void Rechenprobe_der_Kaeltekaskade_gegen_die_Handrechnung_je_Vorlauf()
        {
            const double BEDARF = 13.0, T = 27.5, HILFS = 0.1;
            var faelle = new[]
            {
                new { Vorlauf = 7, Eer = Linear(25, 30, 3.6, 3.2, T), Pk = Linear(25, 30, 11.5, 11.0, T) },
                new { Vorlauf = 18, Eer = Linear(25, 30, 5.0, 4.5, T), Pk = Linear(25, 30, 14.5, 14.0, T) },
            };
            Assert.Equal(3.4, faelle[0].Eer, 12);
            Assert.Equal(11.25, faelle[0].Pk, 12);
            Assert.Equal(4.75, faelle[1].Eer, 12);
            Assert.Equal(14.25, faelle[1].Pk, 12);

            foreach (var f in faelle)
            {
                var kaskade = new Kaeltekaskade { Kuehltage = AlleKuehltage() };
                kaskade.Erzeuger.Add(Erzeuger(f.Vorlauf, T, HILFS, kaskade.Kuehltage));
                kaskade.Rechnen(Konstant(BEDARF), true);

                double deckung = Math.Min(BEDARF, f.Pk);
                double strom = deckung / f.Eer * (1.0 + HILFS);
                Kaelteerzeuger e = kaskade.Erzeuger[0];
                Assert.Equal(deckung, e.Kaelte_stuendlich[100], 12);
                Assert.Equal(strom, e.Strom_stuendlich[100], 12);
                Assert.Equal(BEDARF - deckung, kaskade.Rest_stuendlich[100], 12);
                Assert.Equal(deckung * 8760, kaskade.DeckungGesamtKwh, 6);
                Assert.Equal(strom * 8760, kaskade.StromGesamtKwh, 6);
                Assert.Equal(strom * 8760 - deckung / f.Eer * 8760, kaskade.HilfsstromGesamtKwh, 6);
                Assert.Equal(f.Eer / (1.0 + HILFS), kaskade.EerJahreswert, 9);
                Assert.Equal((BEDARF - deckung) * 8760, kaskade.RestGesamtKwh, 6);
                Assert.Equal(0.0, kaskade.RestAnHeiztagenKwh);
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Vorlauf {0} °C: EER {1:F3}, Pkuehl {2:F3} kW, Deckung {3:F3} kWh/h, Kältestrom {4:F4} kWh/h, EER-Jahreswert {5:F4}",
                    f.Vorlauf, f.Eer, f.Pk, deckung, strom, kaskade.EerJahreswert));
            }
        }

        /// <summary>
        /// Teillast (K8b): unter der Kapazität skaliert der Strom linear mit KONSTANTEM EER; der
        /// Hilfsstromanteil NULL (0) heißt kein Zuschlag (K23); der Heizzeitanteil kürzt die Kapazität.
        /// </summary>
        [Fact]
        public void Teillast_mit_konstantem_EER_und_Hilfsstrom_null_ohne_Zuschlag()
        {
            var kaskade = new Kaeltekaskade { Kuehltage = AlleKuehltage() };
            double[] heiz = Konstant(0.5);
            kaskade.Erzeuger.Add(Erzeuger(7, 30.0, 0.0, kaskade.Kuehltage, heiz));
            double[] bedarf = Konstant(0.0);
            bedarf[10] = 2.0;           // Teillast
            bedarf[11] = 20.0;          // über der halben Kapazität (0,5 · 11 = 5,5)
            kaskade.Rechnen(bedarf, true);

            Kaelteerzeuger e = kaskade.Erzeuger[0];
            Assert.Equal(2.0, e.Kaelte_stuendlich[10]);
            Assert.Equal(2.0 / 3.2, e.Strom_stuendlich[10], 12);
            Assert.Equal(5.5, e.Kaelte_stuendlich[11], 12);
            Assert.Equal(14.5, kaskade.Rest_stuendlich[11], 12);
            Assert.Equal(0.0, kaskade.HilfsstromGesamtKwh);
            Assert.Equal(2, e.StundenMitKaelte);
        }

        /// <summary>
        /// Kaskadenreihenfolge (5.5): Der erste Kälteerzeuger deckt zuerst, der zweite den Rest; am
        /// Heiztag kühlt keiner — der Rest steht als „an Heiztagen" ungedeckt.
        /// </summary>
        [Fact]
        public void Die_Kaltekaskade_deckt_in_Reihenfolge_und_am_Heiztag_nicht()
        {
            bool[] tage = AlleKuehltage();
            tage[0] = false;
            var kaskade = new Kaeltekaskade { Kuehltage = tage };
            kaskade.Erzeuger.Add(Erzeuger(7, 30.0, 0.0, tage));
            kaskade.Erzeuger.Add(Erzeuger(18, 30.0, 0.0, tage));
            kaskade.Rechnen(Konstant(20.0), true);

            Assert.Equal(11.0, kaskade.Erzeuger[0].Kaelte_stuendlich[30]);
            Assert.Equal(9.0, kaskade.Erzeuger[1].Kaelte_stuendlich[30]);
            Assert.Equal(0.0, kaskade.Rest_stuendlich[30]);
            for (int h = 0; h < 24; h++)
            {
                Assert.Equal(0.0, kaskade.Deckung_stuendlich[h]);
                Assert.Equal(20.0, kaskade.Rest_stuendlich[h]);
            }
            Assert.Equal(20.0 * 24, kaskade.RestAnHeiztagenKwh, 9);
            Assert.Equal(364, kaskade.AnzahlKuehltage);
        }

        /// <summary>
        /// <b>Deckungsprobe Kälte</b> (4.3 #31, 4.4): Sie schlägt an, wenn ein Wärmeerzeuger in den
        /// Kühlkanal gebucht hat, der Kühlkanal die Wärmekaskade verändert verlassen hat, ein Wärmekanal
        /// sich in der Kältekaskade verändert hat oder die Kältebilanz nicht schließt — und bleibt für
        /// eine saubere Kaskade still.
        /// </summary>
        [Fact]
        public void Die_Deckungsprobe_Kaelte_schlaegt_bei_jeder_Verletzung_an()
        {
            var kaskade = new Kaeltekaskade { Kuehltage = AlleKuehltage() };
            kaskade.Erzeuger.Add(Erzeuger(7, 30.0, 0.05, kaskade.Kuehltage));
            double[] bedarf = Konstant(8.0);
            kaskade.Rechnen(bedarf, true);

            Assert.True(Kaeltekaskade.Deckungsprobe(kaskade, new[] { 0.0, 0.0 }, bedarf, (double[])bedarf.Clone(), 0).Ok);

            DeckungsprobeKaelte waerme = Kaeltekaskade.Deckungsprobe(kaskade, new[] { 0.0, 1.5 }, bedarf, bedarf, 0);
            Assert.False(waerme.Ok);
            Assert.Equal(1, waerme.WaermeImKuehlkanal);

            double[] veraendert = (double[])bedarf.Clone();
            veraendert[100] = 7.0;
            Assert.Equal(1, Kaeltekaskade.Deckungsprobe(kaskade, null, bedarf, veraendert, 0).KuehlkanalVeraendert);

            Assert.Equal(3, Kaeltekaskade.Deckungsprobe(kaskade, null, bedarf, bedarf, 3).WaermekanalVeraendert);

            kaskade.Rest_stuendlich[200] += 0.5;
            DeckungsprobeKaelte bilanz = Kaeltekaskade.Deckungsprobe(kaskade, null, bedarf, bedarf, 0);
            Assert.False(bilanz.Ok);
            Assert.Equal(1, bilanz.BilanzVerletzt);
        }

        // =============================================================================
        //  Teil 3 — Der Deckungsgrad der Kälteseite: zwei Nenner (6.4, 10.2)
        // =============================================================================

        /// <summary>
        /// <c>DeckungKanalKaelte</c> rechnet mit <c>Kaeltebedarf_Gesamt</c> — Gegenprobe: Dieselbe
        /// Kältemenge über den Wärmenenner ergibt eine andere Zahl. Ohne erhobene Kälte „—" (null).
        /// </summary>
        [Fact]
        public void DeckungKanalKaelte_rechnet_mit_dem_Kaeltenenner_nicht_mit_dem_Waermenenner()
        {
            var v = new VariantenDaten { Ergebnis = new ErgebnisModel() };
            v.Ergebnis.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = 100.0,
                Kaeltebedarf_Gesamt = 20.0,
            };
            v.Ergebnis.Energiebedarf.Waermebedarf_Kanal[Kanal.KUEHLUNG] = 20.0;
            v.Ergebnis.Waermepumpe = new ErgebnisWaermepumpeModel();
            const double GEDECKT_MWH = 15.0;
            v.Ergebnis.Waermepumpe.Deckung_Kanal[Kanal.KUEHLUNG] = GEDECKT_MWH / 20.0 * 100.0;

            double? kaelte = KennzahlenKatalog.DeckungKanalKaelte(v);
            Assert.Equal(75.0, kaelte.Value, 12);

            double ueberWaermenenner = GEDECKT_MWH / v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 100.0;
            Assert.NotEqual(kaelte.Value, ueberWaermenenner);

            v.Ergebnis.Energiebedarf.Kaeltebedarf_Gesamt = null;
            Assert.Null(KennzahlenKatalog.DeckungKanalKaelte(v));
        }

        // =============================================================================
        //  Teil 4 — Die Senke „Kältekreis" (4.3 #14–#16)
        // =============================================================================

        /// <summary>
        /// #14–#16: Das Kälteziel bildet hin und zurück, ist weder Puffer- noch Wärmedirektsenke, fällt
        /// nie auf den Heizkreis und deckt in der Wärmekaskade nichts.
        /// </summary>
        [Fact]
        public void Das_Kaelteziel_bildet_hin_und_zurueck_und_deckt_keinen_Waermekanal()
        {
            Assert.Equal("Kaeltekreis", DbWerte.WS_ZIEL_KAELTEKREIS);
            Assert.Equal(Senke.Kaeltekreis, Senkenzuordnung.SenkeAusZiel(WaermesenkeClass.ZIEL_KAELTEKREIS));
            Assert.Equal(WaermesenkeClass.ZIEL_KAELTEKREIS, Senkenzuordnung.ZielAusSenke(Senke.Kaeltekreis));
            Assert.False(WaermesenkeClass.IstPufferZiel(WaermesenkeClass.ZIEL_KAELTEKREIS));
            Assert.Null(WaermesenkeClass.VerwendungZuZiel(WaermesenkeClass.ZIEL_KAELTEKREIS));
            Assert.False(Senkenzuordnung.IstPuffersenke(Senke.Kaeltekreis));
            Assert.Equal("Kältekreis", WaermesenkeClass.ZielAnzeigeVollstaendig(WaermesenkeClass.ZIEL_KAELTEKREIS));

            var zeile = new Senkenzeile { Ziel = Senke.Kaeltekreis };
            Assert.False(zeile.IstDirektsenke);
            Assert.True(zeile.IstKaeltesenke);
            Assert.False(zeile.IstPuffersenke);
            Assert.All(Kaskadenschleife.SenkenMaske(zeile), m => Assert.False(m));

            var liste = new Senkenliste { AnlagenID = 1 };
            liste.Zeilen.Add(zeile);
            Assert.False(liste.HatDirektsenke);
            var rest = new double[Kanal.ANZAHL];
            rest[Kanal.HEIZUNG] = 5.0;
            rest[Kanal.BRAUCHWASSER] = 2.0;
            Assert.Equal(0.0, Kaskadenschleife.SenkeAbziehen(liste, 10.0, rest, null));
            Assert.Equal(5.0, rest[Kanal.HEIZUNG]);
            Assert.Equal(2.0, rest[Kanal.BRAUCHWASSER]);

            // #16 auf der Altspalte: benannt vermerkt, nicht still verschluckt.
            var d = new WaermesenkeClass.SenkeDaten { Ziel = WaermesenkeClass.ZIEL_KAELTEKREIS };
            WaermesenkeClass.Normalisieren(d);
            Assert.True(d.KaeltezielVerworfen);

            // Die Kältekaskade bucht auf diese Senke.
            Assert.Equal(Senke.Kaeltekreis, new Kaelteerzeuger().Ziel);
        }

        // =============================================================================
        //  Teil 5 — Die Importregel (K22, E27)
        // =============================================================================

        /// <summary>Die Prüfung trennt Blöcke je Vorlauf und Laststufe, die Reihenfolge der Datei bleibt.</summary>
        [Fact]
        public void Die_Importregel_lehnt_Heizlage_und_vertauschte_Achsen_je_Block_ab()
        {
            var zeilen = new List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)>
            {
                (18, 20, 4.8, 12.0, 100), (18, 35, 3.3, 10.0, 100),
                (45, -5, 3.0, 6.0, 100), (45, 10, 4.0, 8.0, 100),
                (7, 7, 4.4, 11.0, 100),
                (18, 20, 6.0, 5.0, 50), (18, 35, 4.0, 4.0, 50),
            };
            List<KuehlblockPruefung.Abgelehnt> abgelehnt;
            var gut = KuehlblockPruefung.Pruefen(zeilen, out abgelehnt);

            Assert.Equal(4, gut.Count);
            Assert.All(gut, z => Assert.Equal(18, z.Vorlauf));
            Assert.Equal(100, gut[0].Last);
            Assert.Equal(2, abgelehnt.Count);
            Assert.Equal(45, abgelehnt[0].Vorlauf);
            Assert.Equal(KuehlblockBefund.Heizlage, abgelehnt[0].Befund);
            Assert.Equal(7, abgelehnt[1].Vorlauf);
            Assert.Equal(KuehlblockBefund.AchsenVertauscht, abgelehnt[1].Befund);
        }

        /// <summary>
        /// Der Importweg (VDI 3805 Blatt 22): Eine Datei mit einem gültigen Kühlblock, einem in Heizlage
        /// und einem mit vertauschten Achsen — übernommen wird allein der gültige, und das Leseprotokoll
        /// nennt die beiden anderen benannt, je Befund eine Warnung.
        /// </summary>
        [Fact]
        public void Der_Import_uebernimmt_nur_Kuehlbloecke_in_Kaltwasserlage_und_meldet_die_uebrigen()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ku2-import-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string datei = Path.Combine(ordner, "phantasie.vdi");
                string[] zeilen =
                {
                    "010;22;201903;Phantasie AG;20260101;;;;;;1;;DEU;DE;;",
                    "100;1;1;Heizung;;;;;35;",
                    "110;1;1;Luft-Wasser;;;;;1;",
                    "400;1;Kompakt;",
                    "450;1;innen;",
                    "700;1;1;Phantasie WP 10;10;2.5;2;6;;;;;;;;;;;;1;12;;;;;;;55;;;;4.5;;;",
                    "710.09;1;1;35;15;-5;;100;",
                    "710.91;1;-5;7;2;3.5;",
                    "710.91;2;7;9;2;4.5;",
                    "710.09;2;2;18;;;;100;",
                    "710.91;1;20;12;2.5;4.8;",
                    "710.91;2;35;10;3;3.3;",
                    "710.09;3;2;45;;;;100;",
                    "710.91;1;-5;6;2;3;",
                    "710.91;2;10;8;2;4;",
                    "710.09;4;2;7;;;;100;",
                    "710.91;1;7;11;2.5;4.4;",
                    "900;1;",
                };
                File.WriteAllLines(datei, zeilen, AnsiEncoding.Get());

                var imp = new WaermepumpenImport();
                imp.Import(datei);
                Assert.Single(imp._list);

                imp.KennlinienRoh(0, out var kennRoh, out var kuehlRoh);
                Assert.Equal(5, kuehlRoh.Count);

                imp.KennlinienZu(0, out var kenn, out var kuehl);
                Assert.Equal(2, kenn.Count);
                Assert.Equal(2, kuehl.Count);
                Assert.All(kuehl, z => Assert.Equal(18, z.Vorlauf));

                Assert.Equal(2, imp.Meldungen.Count);
                PruefMeldung heiz = Assert.Single(imp.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_HEIZLAGE");
                Assert.Equal(new[] { "Phantasie WP 10", "1", "45" }, heiz.Werte);
                PruefMeldung achse = Assert.Single(imp.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_ACHSE");
                Assert.Equal(new[] { "Phantasie WP 10", "1", "7" }, achse.Werte);
                Assert.Contains("Heizlage", R.IMP_KAT_PROT_KUEHLBLOCK_HEIZLAGE);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // =============================================================================
        //  Teil 6 — Mit der Testdatenbank (10.3)
        // =============================================================================

        /// <summary>1045: Wärmepumpe (Luft-Wasser, Anlage 14924) als erster Erzeuger, VDI-Gebäude 10651, Brauchwasser.</summary>
        private const int PROJEKT_WP = 1045, WP_1045 = 1672044, ANLAGE_1045 = 14924, GEBAEUDE_1045 = 10651;

        /// <summary>1017: das Referenzprojekt mit Kühlung; seine Wärmepumpe steht auf keinem Kaskadenplatz.</summary>
        private const int PROJEKT_1017 = 1017, WP_1017 = 1017033, ANLAGE_1017 = 10211;

        /// <summary>Ein Katalogsatz mit Kühlkennlinie und einer mit vertauschten Achsen (Testdatenbank, Kennzeichnung).</summary>
        private const int STAMM_MIT_KENNLINIE = 42, STAMM_ACHSEN = 47;

        private static void KennlinieSaeen(int idWp)
        {
            foreach (KuehlkennlinienZeile z in Phantasiezeilen())
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                    "VALUES ((SELECT COALESCE(MAX(ID), 0) + 1 FROM Tab_Kenndaten_Kuehlung), ?, ?, ?, ?, ?, ?)",
                    new DbParam("@wp", idWp), new DbParam("@v", z.Vorlauf), new DbParam("@t", z.Temperatur),
                    new DbParam("@e", z.Eer), new DbParam("@p", z.Pkuehl), new DbParam("@l", z.Last.Value)));
        }

        private static void GebaeudeKuehlen(int idGebaeude, double sollwert)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = ? WHERE ID = ?",
                new DbParam("@s", sollwert), new DbParam("@id", idGebaeude)));
        }

        private static object Skalar(string sql, params DbParam[] p) => DataRepository.ExecuteScalar(sql, p);

        /// <summary>
        /// 5.0.1: zwei Prüfungen — der Katalog (<c>HatKenndatenStamm</c>) und das Projekt
        /// (<c>HatKenndatenProjekt</c>); ein Gerät kann im Katalog eine Kennlinie haben und im Projekt
        /// keine.
        /// </summary>
        [Fact]
        public void HatKenndatenStamm_und_HatKenndatenProjekt_trennen_Katalog_und_Projekt()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KenndatenKuehlungCtrl.HatKenndatenStamm(STAMM_MIT_KENNLINIE));
            Assert.False(KenndatenKuehlungCtrl.HatKenndatenProjekt(STAMM_MIT_KENNLINIE));
            Assert.False(KenndatenKuehlungCtrl.HatKenndatenProjekt(WP_1045));

            KennlinieSaeen(WP_1045);
            Assert.True(KenndatenKuehlungCtrl.HatKenndatenProjekt(WP_1045));
            Assert.False(KenndatenKuehlungCtrl.HatKenndatenStamm(WP_1045));

            Kuehlkennlinie k = KenndatenKuehlungCtrl.KennlinieProjekt(WP_1045, null);
            Assert.True(k.Rechenbar);
            Assert.Equal(7, k.Vorlauf);
            Assert.Equal(100, k.Laststufe);
        }

        /// <summary>
        /// 10.3 „Kühlbetrieb nur mit Kennlinie im Projekt" und 8.2 „mit Quellspeicher gesperrt": Der
        /// Schreibweg lehnt benannt ab; mit Kennlinie lässt er sich setzen; ausschalten geht immer.
        /// </summary>
        [Fact]
        public void Der_Kuehlbetrieb_laesst_sich_nur_mit_Kennlinie_im_Projekt_und_ohne_Quellspeicher_setzen()
        {
            if (!_db.Vorhanden) return;

            WPCtrl.SpeicherErgebnis ohne = WPCtrl.KuehlkonfigurationSchreiben(WP_1045, PROJEKT_WP, true, null, null);
            Assert.False(ohne.Ok);
            Assert.Equal(R.WP_PROJ_MSG_KUEHL_OHNE_KENNLINIE, ohne.Meldung);
            Assert.Equal(0L, Convert.ToInt64(Skalar("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = ?", new DbParam("@id", WP_1045))));
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_1045, PROJEKT_WP, false, null, null).Ok);

            KennlinieSaeen(WP_1045);
            Assert.Null(WPCtrl.KuehlbetriebSperrgrund(WP_1045, PROJEKT_WP));
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_1045, PROJEKT_WP, true, 18, 0.05).Ok);

            // Quellspeicher: eine Sole-Maschine mit Pufferspeicher als Wärmequelle.
            KennlinieSaeen(WP_1017);
            Assert.Null(WPCtrl.KuehlbetriebSperrgrund(WP_1017, PROJEKT_1017));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET WQ_Typ = ? WHERE ID = ?",
                new DbParam("@wq", WaermequelleClass.TYP_PUFFER), new DbParam("@id", ANLAGE_1017)));
            Assert.Equal(R.WP_PROJ_MSG_KUEHL_QUELLSPEICHER, WPCtrl.KuehlbetriebSperrgrund(WP_1017, PROJEKT_1017));
            Assert.False(WPCtrl.KuehlkonfigurationSchreiben(WP_1017, PROJEKT_1017, true, null, null).Ok);
        }

        /// <summary>
        /// Bestandsdaten werden nicht still geändert (K22): Der Katalogsatz mit der Kaltwassertemperatur
        /// auf der Temperaturachse wird gekennzeichnet — beide Vorläufe „Achsen vertauscht" —, und seine
        /// Zeilen bleiben, wie sie sind. Dazu die Dubletten der Testdatenbank.
        /// </summary>
        [Fact]
        public void Der_Bestandssatz_mit_vertauschten_Achsen_wird_gekennzeichnet_nicht_geaendert()
        {
            if (!_db.Vorhanden) return;

            List<KuehlkennlinienZeile> zeilen = KenndatenKuehlungCtrl.ZeilenStamm(STAMM_ACHSEN);
            Assert.NotEmpty(zeilen);
            IReadOnlyList<KeyValuePair<int, KuehlblockBefund>> befunde = Kuehlkennlinie.Befunde(zeilen);
            Assert.Equal(new[] { 7, 18 }, befunde.Select(b => b.Key).ToArray());
            Assert.All(befunde, b => Assert.Equal(KuehlblockBefund.AchsenVertauscht, b.Value));
            Assert.Equal(zeilen.Count, KenndatenKuehlungCtrl.ZeilenStamm(STAMM_ACHSEN).Count);

            Kuehlkennlinie gut = Kuehlkennlinie.Bilden(KenndatenKuehlungCtrl.ZeilenStamm(STAMM_MIT_KENNLINIE), null);
            Assert.True(gut.Rechenbar);
            Assert.True(gut.Dubletten > 0, "Die Testdatenbank führt jede Stützstelle doppelt.");
            Assert.Equal(0, gut.DublettenAbweichend);
        }

        /// <summary>Richtet 1045 mit Kühlung und reversibler Wärmepumpe ein (Phantasie-Kennlinie).</summary>
        private void Projekt1045MitKuehlbetrieb(double hilfsstrom)
        {
            GebaeudeKuehlen(GEBAEUDE_1045, 24.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT_WP, true));
            KennlinieSaeen(WP_1045);
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(WP_1045, PROJEKT_WP, true, 18, hilfsstrom);
            Assert.True(e.Ok, e.Meldung);
        }

        /// <summary>
        /// <b>Ein Lauf mit reversibler Wärmepumpe</b> (5.1, 5.2, 5.5, 6.1; 4.3 #31): Die Kältekaskade
        /// deckt Kälte; am Kühltag ist der Heizkanal der Maschine gesperrt (keine Direktdeckung, kein
        /// Heizstab im Heizkanal), Brauchwasser bleibt bedient; am Heiztag kühlt sie nicht; der
        /// Kältestrom steht als eigene Reihe in der Strombilanz; die Deckungsprobe Kälte bleibt still;
        /// Deckungsspalte, Deckungsgrad und Exportskalare tragen die Kältedeckung.
        /// </summary>
        [Fact]
        public void Ein_Lauf_mit_reversibler_Waermepumpe_deckt_Kaelte_und_sperrt_am_Kuehltag_den_Heizkanal()
        {
            if (!_db.Vorhanden) return;
            const double HILFS = 0.08;

            GebaeudeKuehlen(GEBAEUDE_1045, 24.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT_WP, true));
            var ohne = new SimulationRunner();
            int kopfOhne = ohne.SimuliereUndSpeichere(PROJEKT_WP, out string fehlerOhne);
            Assert.True(kopfOhne > 0, fehlerOhne);
            Assert.Null(ohne.simulation_Kaeltebedarf.Kaskade);
            Assert.Empty(KaelteErgebnisexport.Skalare(ohne.simulation_Kaeltebedarf));

            Projekt1045MitKuehlbetrieb(HILFS);
            var mit = new SimulationRunner();
            int kopf = mit.SimuliereUndSpeichere(PROJEKT_WP, out string fehler);
            Assert.True(kopf > 0, fehler);

            SimulationKaeltebedarf kaelte = mit.simulation_Kaeltebedarf;
            Kaeltekaskade k = kaelte.Kaskade;
            Assert.NotNull(k);
            Kaelteerzeuger e = Assert.Single(k.Erzeuger);
            Assert.Equal(ANLAGE_1045, e.AnlagenID);
            Assert.Equal(18, e.Kennlinie.Vorlauf);
            Assert.True(k.DeckungGesamtKwh > 0);
            Assert.True(k.AnzahlKuehltage > 0);
            Assert.Equal(ohne.simulation_Kaeltebedarf.Kaeltebedarf_Gesamt, kaelte.Kaeltebedarf_Gesamt, 9);   // der Bedarf kennt keinen Erzeuger
            Assert.Equal(k.RestGesamtKwh / 1000.0, kaelte.Kaelterestbedarf, 9);
            Assert.True(kaelte.Kaelterestbedarf < kaelte.Kaeltebedarf_Gesamt);
            Assert.Equal(e.HilfsstromGesamtKwh, e.StromGesamtKwh - e.StromGesamtKwh / (1.0 + HILFS), 6);

            // Heiztag: keine Kälte. Kühltag: Heizkanal gesperrt, Brauchwasser bedient.
            SimulationWaermepumpe wp = mit.sim.simulation_wp;
            double[] heizDirekt = wp.Direktdeckung_KanalStuendlich.Zeile(Kanal.HEIZUNG);
            double[] heizStab = wp.Heizstab_KanalStuendlich.Zeile(Kanal.HEIZUNG);
            double[] wwDirekt = wp.Direktdeckung_KanalStuendlich.Zeile(Kanal.BRAUCHWASSER);
            double[] wwStab = wp.Heizstab_KanalStuendlich.Zeile(Kanal.BRAUCHWASSER);
            double wwKuehltag = 0, wwBedarfKuehltag = 0;
            double[] ww = mit.simulation_Waermebedarf.KanaeleDrei().Brauchwasser;
            for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
            {
                if (k.Kuehltage[h / 24])
                {
                    Assert.Equal(0.0, heizDirekt[h]);
                    Assert.Equal(0.0, heizStab[h]);
                    wwKuehltag += wwDirekt[h] + wwStab[h];
                    wwBedarfKuehltag += ww[h];
                }
                else Assert.Equal(0.0, k.Deckung_stuendlich[h]);
            }
            Assert.True(wwBedarfKuehltag > 0, "1045 hat Brauchwasser an Kühltagen.");
            Assert.True(wwKuehltag > 0 || wp.Speicherentladung_KanalStuendlich.Jahressumme(Kanal.BRAUCHWASSER) > 0,
                        "Am Kühltag bleibt das Brauchwasser bedienbar.");

            // Die Kälte liegt in eigenen Reihen - die Wärmepumpenreihen tragen keinen Kältestrom.
            Assert.Equal(0.0, wp.Direktdeckung_Kanal[Kanal.KUEHLUNG]);
            Assert.Equal(0.0, wp.Heizstab_Kanal[Kanal.KUEHLUNG]);

            // 6.1: der Kältestrom als eigene Reihe in der Strombilanz.
            Assert.True(k.StromGesamtKwh > 0);
            Assert.Equal(k.StromGesamtKwh, k.Stromverbrauch_Kuehlung_stuendlich.Sum(), 6);
            Assert.Equal(k.DeckungGesamtKwh / k.StromGesamtKwh, k.EerJahreswert, 12);

            // Deckungsspalte der Wärmepumpe, Deckungsgrad über den Kältenenner, Exportskalare.
            double deckungProzent = SimulationRunner.DeckungKuehlkanalProzent(kaelte);
            double spalte = Convert.ToDouble(Skalar("SELECT " + KuehlungSchema.SPALTE_DECKUNG_KUEHLUNG +
                " FROM Tab_ErgebnisWaermepumpe WHERE ID_Ergebnis = ?", new DbParam("@k", kopf)), CultureInfo.InvariantCulture);
            Assert.Equal(Math.Round(deckungProzent, 2, MidpointRounding.AwayFromZero), spalte, 2);
            IReadOnlyList<KeyValuePair<string, double>> skalare = KaelteErgebnisexport.Skalare(kaelte);
            Assert.Contains(skalare, s => s.Key == "Kaelte.StromMwh" && Math.Abs(s.Value - k.StromGesamtKwh / 1000.0) < 1e-12);
            Assert.Contains(skalare, s => s.Key == "Kaelte.EerJahreswert");
            Assert.Contains(skalare, s => s.Key == "Kaelte[0].Vorlauf" && s.Value == 18);

            Assert.DoesNotContain(mit.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));
            Assert.Contains(mit.Protokoll.Hinweise, h => h.StartsWith("Kälteerzeugung:", StringComparison.Ordinal));

            // Die Ergebnisseite erfährt die Deckung (der Satz „kein Kälteerzeuger" wäre hier falsch).
            SimulationErgebnisCtrl.KaelteErgebnis anzeige = SimulationErgebnisCtrl.Kaelte(mit.simulation_Waermebedarf);
            Assert.True(anzeige.MitKaelteerzeuger);
            Assert.Equal(k.DeckungGesamtKwh / 1000.0, anzeige.KaeltedeckungMwh, 12);
            Assert.False(SimulationErgebnisCtrl.Kaelte(ohne.simulation_Waermebedarf).MitKaelteerzeuger);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1045: Kältebedarf {0:F3} MWh, gedeckt {1:F3} MWh ({2:F1} %), Rest {3:F3} MWh, Kältestrom {4:F3} MWh " +
                "(Hilfsstrom {5:F3}), EER-Jahreswert {6:F2}, Kühltage {7}, Reststrom {8:F3} → {9:F3} MWh",
                kaelte.Kaeltebedarf_Gesamt, k.DeckungGesamtKwh / 1000.0, deckungProzent, kaelte.Kaelterestbedarf,
                k.StromGesamtKwh / 1000.0, k.HilfsstromGesamtKwh / 1000.0, k.EerJahreswert, k.AnzahlKuehltage,
                ohne.sim.ReststromMwh, mit.sim.ReststromMwh));
        }

        /// <summary>
        /// Anlagenschema (4.3 #26, #33): Mit einer Wärmepumpe im Kühlbetrieb steht der Abnehmer
        /// „Kältekreis" mit ihrer Versorgungskante da, und kein Speicher versorgt ihn; ohne
        /// Kälteerzeuger, aber mit Kältebedarf, steht er mit Warnzeichen da — ohne Kante.
        /// </summary>
        [Fact]
        public void Das_Anlagenschema_zeigt_den_Kaeltekreis_mit_seinem_Versorger()
        {
            if (!_db.Vorhanden) return;
            string erzeuger = SchemaModell.PRAEFIX_ERZEUGER + ANLAGE_1045;

            GebaeudeKuehlen(GEBAEUDE_1045, 24.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT_WP, true));
            var bedarf = new double[Kanal.ANZAHL];
            bedarf[Kanal.HEIZUNG] = 70.0;
            bedarf[Kanal.KUEHLUNG] = 1.0;

            SchemaModell ohne = SchemaModell.Aufbauen(PROJEKT_WP, null, bedarf);
            SchemaModell.Knoten warn = ohne.Finden(SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.NotNull(warn);
            Assert.True(warn.Warnung);
            Assert.DoesNotContain(ohne.Kantenliste, e => e.Nach == SchemaModell.ABNEHMER_KAELTEKREIS);

            KennlinieSaeen(WP_1045);
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_1045, PROJEKT_WP, true, null, null).Ok);
            SchemaModell mit = SchemaModell.Aufbauen(PROJEKT_WP, null, bedarf);
            SchemaModell.Knoten k = mit.Finden(SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.NotNull(k);
            Assert.False(k.Warnung);
            Assert.Equal(R.SIM_ZIEL_KAELTEKREIS, k.Titel);
            SchemaModell.Kante kante = Assert.Single(mit.Kantenliste, e => e.Nach == SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.Equal(erzeuger, kante.Von);

            // Ohne Projektschalter keine Kältesenke - auch nicht mit Kühlbetrieb am Gerät.
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT_WP, false));
            Assert.Null(SchemaModell.Aufbauen(PROJEKT_WP, null).Finden(SchemaModell.ABNEHMER_KAELTEKREIS));
        }

        /// <summary>
        /// Sperrgründe im Lauf (5.1 Festlegung 6, 8.5): Ein Gerät auf Kühlbetrieb ohne Kennlinie und
        /// eines mit Quellspeicher kühlen nicht — benannt als Warnung, und der Kältebedarf bleibt mit der
        /// Warnung „ohne Kälteerzeuger" ungedeckt.
        /// </summary>
        [Fact]
        public void Sperrgruende_im_Lauf_sind_benannt_und_der_Kaeltebedarf_bleibt_ungedeckt()
        {
            if (!_db.Vorhanden) return;

            GebaeudeKuehlen(GEBAEUDE_1045, 24.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT_WP, true));
            // Am Schreibweg vorbei: das Gerät auf Kühlbetrieb, aber ohne Kennlinie im Projekt.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_WP SET Kuehlbetrieb = 1 WHERE ID = ?", new DbParam("@id", WP_1045)));

            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT_WP, out string fehler);
            Assert.True(kopf > 0, fehler);
            Assert.Null(lauf.simulation_Kaeltebedarf.Kaskade);
            Assert.Equal(lauf.simulation_Kaeltebedarf.Kaeltebedarf_Gesamt, lauf.simulation_Kaeltebedarf.Kaelterestbedarf);
            Assert.Contains(lauf.Protokoll.Warnungen, w => w.Contains("ohne Kühlkennlinie im Projekt"));
            Assert.Contains(lauf.Protokoll.Warnungen, w => w.StartsWith("Kältebedarf ohne Kälteerzeuger", StringComparison.Ordinal));
            Assert.Null(lauf.sim.simulation_wp.KuehlModule);
        }

        /// <summary>
        /// <b>Probe an einem Testprojekt im Speicher — 1017 mit Wärmepumpen-Kühlbetrieb</b> (Auftrag
        /// KU2 Welle 2): Die Wärmepumpe von 1017 bekommt einen Kaskadenplatz, eine Phantasie-Kennlinie
        /// und den Kühlbetrieb (Vorlauf 18 °C, Hilfsstrom 5 %) — nur in der Arbeitskopie; das
        /// Referenzprojekt selbst und sein Einfrierschritt sind Welle 4.
        /// </summary>
        [Fact]
        public void Probe_1017_mit_Waermepumpen_Kuehlbetrieb()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Tool_3 = ? WHERE ID_Projekt = ?",
                new DbParam("@t", DbWerte.ERZEUGER_WAERMEPUMPE), new DbParam("@p", PROJEKT_1017)));
            KennlinieSaeen(WP_1017);
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(WP_1017, PROJEKT_1017, true, 18, 0.05);
            Assert.True(e.Ok, e.Meldung);

            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT_1017, out string fehler);
            Assert.True(kopf > 0, fehler);

            SimulationKaeltebedarf kaelte = lauf.simulation_Kaeltebedarf;
            Kaeltekaskade k = kaelte.Kaskade;
            Assert.NotNull(k);
            Assert.True(k.DeckungGesamtKwh > 0);
            Assert.True(k.StromGesamtKwh > 0);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1017 mit WP-Kühlbetrieb: Kältebedarf {0:F3} MWh (Spitze {1:F2} kW), gedeckt {2:F3} MWh ({3:F1} %), " +
                "ungedeckt {4:F3} MWh (Heiztage {5:F3}, Kühltage {6:F3}), Kältestrom {7:F3} MWh (Hilfsstrom {8:F3}), " +
                "EER-Jahreswert {9:F2}, Kühltage {10}, Stunden mit Kälte {11}",
                kaelte.Kaeltebedarf_Gesamt, kaelte.Kaeltebedarf_Max, k.DeckungGesamtKwh / 1000.0,
                SimulationRunner.DeckungKuehlkanalProzent(kaelte), kaelte.Kaelterestbedarf,
                k.RestAnHeiztagenKwh / 1000.0, k.RestAnKuehltagenKwh / 1000.0, k.StromGesamtKwh / 1000.0,
                k.HilfsstromGesamtKwh / 1000.0, k.EerJahreswert, k.AnzahlKuehltage, k.Erzeuger[0].StundenMitKaelte));
            foreach (string h in lauf.Protokoll.Hinweise.Where(x => x.StartsWith("Kälte", StringComparison.Ordinal) ||
                                                                     x.StartsWith("Tagesbetriebsart", StringComparison.Ordinal) ||
                                                                     x.StartsWith("Wärmepumpe", StringComparison.Ordinal)))
                _aus.WriteLine("Hinweis: " + h);
            foreach (string w in lauf.Protokoll.Warnungen.Where(x => x.Contains("Kälte") || x.Contains("Kühl") ||
                                                                     x.StartsWith("Tagesbetriebsart", StringComparison.Ordinal)))
                _aus.WriteLine("Warnung: " + w);
        }
    }
}
