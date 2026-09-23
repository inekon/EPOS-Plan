using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der vierte Kanal</b> (Stufe KU1 der Kühlung, zweite Welle; Kühlkonzept 4.1–4.5,
    /// Festlegung F-K4) — ohne Datenbank.
    ///
    /// <para><b>Geprüft wird:</b> vier Kanäle und zwei Kanallisten, disjunkt und vollständig; der
    /// Persistenzwert „Kuehlung" und die Vorbelegung Heizung für alles Unbekannte; die beiden
    /// Ausnahmen (<c>Summe</c> und <c>NetzverlusteVerteilen</c> nur über die Wärmekanäle) samt
    /// <c>SummeKaelte</c>; der tolerante Knappheitsparser (jede gespeicherte Dreierfolge gilt
    /// weiter, ohne Warnung; Kühlung immer zuletzt, K4); die Stellen der Wärmeseite, die den
    /// Kühlkanal nie bedienen dürfen (Restsumme, Deckungsaufschlüsselung, Senken und Speicher);
    /// Anzeigename und Berichtsschlüssel des vierten Kanals (4.3 #24, #34).</para>
    ///
    /// <para>In der seriellen Sammlung, weil der Knappheitsparser in den prozessweiten
    /// Protokollkanal meldet; Kultur de-DE für die Ressourcentexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlkanalTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Kanal und Kanallisten (4.3 #1, #6, #7; 4.2)
        // =============================================================================

        [Fact]
        public void Vier_Kanaele_zwei_Listen_disjunkt_und_vollstaendig()
        {
            Assert.Equal(4, Kanal.ANZAHL);
            Assert.Equal(3, Kanal.KUEHLUNG);
            Assert.Equal(new[] { Kanal.HEIZUNG, Kanal.BRAUCHWASSER, Kanal.PROZESS }, Kanal.KANAELE_WAERME.ToArray());
            Assert.Equal(new[] { Kanal.KUEHLUNG }, Kanal.KANAELE_KAELTE.ToArray());

            // Jeder Kanal steht in genau einer der beiden Listen - ein fünfter fiele sonst still heraus.
            var alle = Kanal.KANAELE_WAERME.Concat(Kanal.KANAELE_KAELTE).OrderBy(k => k).ToArray();
            Assert.Equal(Enumerable.Range(0, Kanal.ANZAHL).ToArray(), alle);
            for (int k = 0; k < Kanal.ANZAHL; k++)
                Assert.NotEqual(Kanal.IstWaerme(k), Kanal.IstKaelte(k));
        }

        [Fact]
        public void Kuehlung_nur_ueber_den_ASCII_Persistenzwert_alles_Unbekannte_bleibt_Heizung()
        {
            Assert.Equal("Kuehlung", DbWerte.KANAL_KUEHLUNG);
            Assert.True(DbWerte.KANAL_KUEHLUNG.All(c => c < 128));
            Assert.Equal(Kanal.KUEHLUNG, Kanal.AusText(DbWerte.KANAL_KUEHLUNG));
            Assert.Equal(Kanal.KUEHLUNG, Kanal.AusText(" kuehlung "));
            Assert.Equal(DbWerte.KANAL_KUEHLUNG, Kanal.Name(Kanal.KUEHLUNG));

            // 4.3 #6: Ein unbekannter Wert fällt NIE in den Kühlkanal.
            Assert.Equal(Kanal.HEIZUNG, Kanal.AusText("Kühlung"));
            Assert.Equal(Kanal.HEIZUNG, Kanal.AusText("Kaelte"));
            Assert.Equal(Kanal.HEIZUNG, Kanal.AusText(null));
            Assert.Equal(Kanal.HEIZUNG, Kanal.AusText(""));
        }

        // =============================================================================
        //  Die zwei Ausnahmen (4.2 a und b) und die Summe der Kälteseite
        // =============================================================================

        [Fact]
        public void Summe_laeuft_ohne_Kuehlanteil_SummeKaelte_ist_der_Kuehlkanal()
        {
            Kanalsatz k = Beispiel(mitKaelte: true);
            double[] summe = k.Summe();
            double[] kaelte = k.SummeKaelte();

            for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
            {
                double w = k.Heizung[h];
                w = w + k.Brauchwasser[h];
                w = w + k.Prozess[h];
                Assert.Equal(w, summe[h]);                     // bitgleich, dieselbe Folge wie vorher
                Assert.Equal(k.Kuehlung[h], kaelte[h]);
            }

            // Eigene Vektoren (Aliasing-Regel B0-2).
            kaelte[0] = -1.0;
            Assert.NotEqual(-1.0, k.Kuehlung[0]);

            // Der Maßstab der Energieprobe bleibt bei fünf Rundungsschritten (drei Wärmekanäle).
            Assert.Equal(5, Kanalsatz.ERHALTUNG_SCHRITTE_SUMME);
            Assert.Equal(1, Kanalsatz.ERHALTUNG_SCHRITTE_SUMME_KAELTE);
        }

        [Fact]
        public void Netzverluste_gehen_nur_auf_die_Waermekanaele_und_der_Kuehlkanal_bleibt_unberuehrt()
        {
            Kanalsatz ohne = Beispiel(mitKaelte: false);
            Kanalsatz mit = Beispiel(mitKaelte: true);
            double[] kaelteVorher = (double[])mit.Kuehlung.Clone();

            ohne.NetzverlusteVerteilen(0.4713);
            mit.NetzverlusteVerteilen(0.4713);

            // Die Wärmekanäle sind BITGLEICH - mit oder ohne Kältebedarf in derselben Stunde.
            foreach (int k in Kanal.KANAELE_WAERME)
                Assert.Equal(ohne.Bedarf[k], mit.Bedarf[k]);
            Assert.Equal(kaelteVorher, mit.Kuehlung);

            // Randfall: nur Kältebedarf in der Stunde - der Netzverlust geht ganz auf Heizung.
            var rand = new Kanalsatz();
            rand.Kuehlung[7] = 5.0;
            rand.NetzverlusteVerteilen(2.0);
            Assert.Equal(2.0, rand.Heizung[7]);
            Assert.Equal(5.0, rand.Kuehlung[7]);
        }

        // =============================================================================
        //  Der tolerante Knappheitsparser (4.3 #10–#12, 4.5; K4, K14)
        // =============================================================================

        [Fact]
        public void Jede_gespeicherte_Dreierfolge_ergibt_dieselbe_Waermeordnung_ohne_Warnung()
        {
            string[] schluessel = { DbWerte.KNAPPHEIT_BRAUCHWASSER, DbWerte.KNAPPHEIT_PROZESS, DbWerte.KNAPPHEIT_HEIZUNG };
            int[] kanaele = { Kanal.BRAUCHWASSER, Kanal.PROZESS, Kanal.HEIZUNG };

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            foreach (int[] perm in Permutationen(3))
            {
                string spec = string.Join(";", perm.Select(i => schluessel[i]));
                int[] ordnung = Kanal.KnappheitsReihenfolge(spec);
                Assert.Equal(perm.Select(i => kanaele[i]).Concat(new[] { Kanal.KUEHLUNG }).ToArray(), ordnung);
            }
            Assert.Empty(protokoll.Warnungen);
        }

        [Fact]
        public void Die_Kuehlung_steht_immer_zuletzt_auch_wenn_die_Angabe_sie_vorn_nennt()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(new[] { Kanal.HEIZUNG, Kanal.PROZESS, Kanal.BRAUCHWASSER, Kanal.KUEHLUNG },
                         Kanal.KnappheitsReihenfolge("KUEHLUNG;HEIZUNG;PROZESS;BRAUCHWASSER"));
            Assert.Equal(new[] { Kanal.BRAUCHWASSER, Kanal.PROZESS, Kanal.HEIZUNG, Kanal.KUEHLUNG },
                         Kanal.KnappheitsReihenfolge(" brauchwasser , PROZESS;kuehlung; HEIZUNG "));
            Assert.Empty(protokoll.Warnungen);
        }

        [Fact]
        public void Die_Vorgabe_fuehrt_vier_Glieder_und_ist_die_Vorbelegung()
        {
            Assert.Equal("BRAUCHWASSER;PROZESS;HEIZUNG;KUEHLUNG", DbWerte.KNAPPHEIT_DEFAULT);
            Assert.Equal(Kanal.KnappheitVorgabe(), Kanal.KnappheitsReihenfolge(DbWerte.KNAPPHEIT_DEFAULT));
            Assert.Equal(new[] { Kanal.BRAUCHWASSER, Kanal.PROZESS, Kanal.HEIZUNG, Kanal.KUEHLUNG },
                         Kanal.KnappheitVorgabe());
        }

        [Theory]
        [InlineData("Unfug")]
        [InlineData("BRAUCHWASSER")]
        [InlineData("KUEHLUNG")]
        [InlineData("BRAUCHWASSER;HEIZUNG;KUEHLUNG")]
        [InlineData("BRAUCHWASSER;BRAUCHWASSER;HEIZUNG")]
        [InlineData("BRAUCHWASSER;PROZESS;HEIZUNG;KUEHLUNG;KUEHLUNG")]
        [InlineData("BRAUCHWASSER;PROZESS;HEIZUNG;KAELTE")]
        public void Eine_unbrauchbare_Angabe_faellt_auf_die_Vorbelegung_und_warnt_einmal(string spec)
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(Kanal.KnappheitVorgabe(), Kanal.KnappheitsReihenfolge(spec));
            Assert.Equal(Kanal.KnappheitVorgabe(), Kanal.KnappheitsReihenfolge(spec));
            string warnung = Assert.Single(protokoll.Warnungen);
            Assert.Contains(DbWerte.KNAPPHEIT_KUEHLUNG, warnung);
        }

        // =============================================================================
        //  Die Wärmeseite bedient den Kühlkanal nie (4.2, 4.4; Lehre aus #24 bis #27)
        // =============================================================================

        [Fact]
        public void Restsumme_und_Deckungsaufschluesselung_bleiben_auf_der_Waermeseite()
        {
            double[] rest = new double[Kanal.ANZAHL];
            rest[Kanal.HEIZUNG] = 1.5; rest[Kanal.BRAUCHWASSER] = 2.25; rest[Kanal.PROZESS] = 0.125;
            rest[Kanal.KUEHLUNG] = 100.0;
            Assert.Equal(1.5 + 2.25 + 0.125, Kaskadenschleife.RestSumme(rest));

            // Die Wärmedeckung wird über die Wärmekanäle aufgeschlüsselt; ein Kältewert im
            // Eigenanteil (den es nicht geben darf) geht in keinen Anteil ein.
            double[] eigen = { 1000.0, 500.0, 0.0, 700.0 };
            double[] ohne = SimulationRunner.DeckungJeKanal(new[] { 1000.0, 500.0, 0.0, 0.0 }, 10.0, 12.0);
            double[] mit = SimulationRunner.DeckungJeKanal(eigen, 10.0, 12.0);
            Assert.Equal(ohne, mit);
            Assert.Equal(0.0, mit[Kanal.KUEHLUNG]);
            Assert.Equal(12.0, mit.Sum(), 12);
        }

        [Fact]
        public void Waermesenken_und_Speicher_bedienen_den_Kuehlkanal_nicht()
        {
            var direkt = new Z_AnlageSenkeModel { Rang = 1, Ziel = DbWerte.WS_ZIEL_HEIZKREIS, Bedarfsart = DbWerte.WS_TYP_BEIDES };
            Assert.True(Warnkriterien.SenkeBedientKanal(direkt, Kanal.HEIZUNG, null));
            Assert.False(Warnkriterien.SenkeBedientKanal(direkt, Kanal.KUEHLUNG, null));

            var puffer = new Z_AnlageSenkeModel { Rang = 1, Ziel = DbWerte.WS_ZIEL_PUFFER_HEIZUNG, ID_Puffer = 1 };
            var set = new PufferSpCtrl.KlassenSet(true, true, true);
            Assert.True(Warnkriterien.SenkeBedientKanal(puffer, Kanal.PROZESS, set));
            Assert.False(Warnkriterien.SenkeBedientKanal(puffer, Kanal.KUEHLUNG, set));

            // Der Speicher im Lauf: weder über das Klassen-Set noch über die Verwendung.
            var sp = new SimulationPufferspeicher();
            Assert.False(sp.BedientKanal(Kanal.KUEHLUNG));
            sp.KlassenSetSetzen(true, true, true);
            Assert.False(sp.BedientKanal(Kanal.KUEHLUNG));
            Assert.True(sp.BedientKanal(Kanal.HEIZUNG));
        }

        // =============================================================================
        //  Anzeige und Berichtsschlüssel (4.3 #13, #24, #34)
        // =============================================================================

        [Fact]
        public void Der_Kuehlkanal_hat_seinen_Anzeigenamen_und_seinen_Berichtsschluessel()
        {
            Assert.Equal("Kühlung", Warnkriterien.KanalAnzeige(Kanal.KUEHLUNG));
            Assert.NotEqual(Warnkriterien.KanalAnzeige(Kanal.HEIZUNG), Warnkriterien.KanalAnzeige(Kanal.KUEHLUNG));

            Assert.Equal(Kanal.ANZAHL, ZeitreihenSatz.KANAL_SCHLUESSEL.Length);
            Assert.Equal("BEDARF_KUEHLUNG", ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG));
            Assert.Equal("DECKUNG_WAERMEPUMPE_KUEHLUNG", ZeitreihenSatz.DeckungSchluessel("WAERMEPUMPE", Kanal.KUEHLUNG));
            Assert.Equal("", ZeitreihenSatz.BedarfSchluessel(Kanal.ANZAHL));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Ein Jahr mit reinen, gemischten und leeren Stunden; auf Wunsch mit Kältebedarf.</summary>
        private static Kanalsatz Beispiel(bool mitKaelte)
        {
            var k = new Kanalsatz();
            for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
            {
                switch (h % 5)
                {
                    case 0: k.Heizung[h] = 12.34; break;
                    case 1: k.Brauchwasser[h] = 3.7; break;
                    case 2: k.Prozess[h] = 7.03; break;
                    case 3: k.Heizung[h] = 8.1; k.Brauchwasser[h] = 2.9; k.Prozess[h] = 1.7; break;
                }
                if (mitKaelte && h % 3 == 0) k.Kuehlung[h] = 4.4 + h % 7;
            }
            return k;
        }

        private static IEnumerable<int[]> Permutationen(int n)
        {
            if (n == 1) { yield return new[] { 0 }; yield break; }
            foreach (int[] p in Permutationen(n - 1))
                for (int i = 0; i <= p.Length; i++)
                {
                    var neu = new List<int>(p);
                    neu.Insert(i, n - 1);
                    yield return neu.ToArray();
                }
        }
    }
}
