using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests des Preis- und Verguetungsmodells (Arbeitspaket AP4, Fachkonzept
    /// 4.1/4.2): Jahresprofil aus Monats- und Wochenwerten, Rasterwechsel und die
    /// additive Aufschlagsrechnung.
    /// </summary>
    public sealed class PreisModellTests
    {
        private static readonly int[] TageProMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Zwoelf unterscheidbare Monatswerte - jeder Monat muss identifizierbar bleiben.</summary>
        private static double[] Monatswerte()
        {
            double[] m = new double[12];
            for (int i = 0; i < 12; i++) m[i] = 20.0 + i;   // 20 ... 31 ct/kWh
            return m;
        }

        /// <summary>168 unterscheidbare Wochenwerte.</summary>
        private static double[] Wochenwerte()
        {
            double[] w = new double[168];
            for (int i = 0; i < 168; i++) w[i] = i * 0.01;   // 0,00 ... 1,67 ct/kWh
            return w;
        }

        // =================================================================
        // Jahresprofil
        // =================================================================

        [Fact]
        public void Profil_Hat_Genau_8760_Stundenwerte()
        {
            double[] profil = PreisModell.AusMonatsUndWochenwerten(Monatswerte(), Wochenwerte());
            Assert.Equal(RasterAdapter.StundenJahr, profil.Length);
        }

        /// <summary>
        /// Ohne Wochenwerte ist das Profil je Monat konstant, und die Jahressumme ist
        /// exakt <c>Sigma monat[m] * Tage[m] * 24</c> - die Kernaussage der
        /// Monatswert-Semantik (additiv, NICHT auf eine Menge normiert).
        /// </summary>
        [Fact]
        public void Ohne_Wochenwerte_Ist_Jahressumme_Gleich_Monatswertlogik()
        {
            double[] monat = Monatswerte();
            double[] profil = PreisModell.AusMonatsUndWochenwerten(monat, null);

            double erwartet = 0.0;
            for (int m = 0; m < 12; m++) erwartet += monat[m] * TageProMonat[m] * 24;

            Assert.Equal(erwartet, Numerik.SummeSequenziell(profil), 9);

            // Stichproben: erste Stunde Januar, letzte Stunde Dezember, erste Stunde Juli.
            Assert.Equal(monat[0], profil[0], 12);
            Assert.Equal(monat[11], profil[RasterAdapter.StundenJahr - 1], 12);

            int julianfang = 0;
            for (int m = 0; m < 6; m++) julianfang += TageProMonat[m] * 24;
            Assert.Equal(monat[6], profil[julianfang], 12);
        }

        /// <summary>
        /// Mit Wochenwerten gilt dieselbe Jahressumme plus der Summe aller
        /// Wochenanteile - der Wochenwert ist eine ABWEICHUNG, er skaliert nichts.
        /// </summary>
        [Fact]
        public void Mit_Wochenwerten_Addiert_Sich_Der_Wochenanteil_Auf_Die_Monatssumme()
        {
            double[] monat = Monatswerte();
            double[] woche = Wochenwerte();

            double[] ohne = PreisModell.AusMonatsUndWochenwerten(monat, null);
            double[] mit = PreisModell.AusMonatsUndWochenwerten(monat, woche);

            // Die Differenzreihe enthaelt ausschliesslich Wochenwerte.
            double summeDifferenz = 0.0;
            for (int i = 0; i < mit.Length; i++) summeDifferenz += mit[i] - ohne[i];

            // 365 Tage: 52 volle Wochen (364 Tage) plus ein Einzeltag.
            double summeWoche = 0.0;
            for (int i = 0; i < 168; i++) summeWoche += woche[i];

            int letzterTag = (PreisModell.WOCHENTAG_JAHRESANFANG + 364) % 7;
            double summeLetzterTag = 0.0;
            for (int h = 0; h < 24; h++) summeLetzterTag += woche[letzterTag * 24 + h];

            Assert.Equal(52.0 * summeWoche + summeLetzterTag, summeDifferenz, 8);
        }

        /// <summary>
        /// Kalenderausrichtung: Der 1. Januar ist ein Sonntag (Index 6), so wie ihn
        /// <c>BhkwPlan.StromWocheToJahr</c> vor die 52 vollen Wochen stellt.
        /// </summary>
        [Fact]
        public void Jahresanfang_Liegt_Auf_Sonntag_Wie_Im_Rechenkern()
        {
            Assert.Equal(6, PreisModell.WOCHENTAG_JAHRESANFANG);

            double[] monat = new double[12];          // Niveau 0, damit nur die Woche wirkt
            double[] woche = Wochenwerte();
            double[] profil = PreisModell.AusMonatsUndWochenwerten(monat, woche);

            // Tag 0 = Sonntag -> woche[6*24 + h]
            for (int h = 0; h < 24; h++) Assert.Equal(woche[6 * 24 + h], profil[h], 12);

            // Tag 1 = Montag -> woche[0*24 + h]
            for (int h = 0; h < 24; h++) Assert.Equal(woche[h], profil[24 + h], 12);
        }

        /// <summary>
        /// Der Wochentag des Jahresanfangs ist frei waehlbar - und mit Montag (0)
        /// entsteht genau die einfache Kachelung woche[0..167] ab Index 0.
        /// </summary>
        [Fact]
        public void Frei_Gewaehlter_Wochentag_Verschiebt_Das_Wochenmuster()
        {
            double[] monat = new double[12];
            double[] woche = Wochenwerte();
            double[] profil = PreisModell.AusMonatsUndWochenwerten(monat, woche, 0);

            for (int i = 0; i < 168; i++) Assert.Equal(woche[i], profil[i], 12);
        }

        /// <summary>
        /// Ein reines Tages- bzw. HT/NT-Profil ist der Sonderfall "alle sieben Tage
        /// gleich" (Fachkonzept 4.1) - dann ist die Kalenderlage ohne Bedeutung.
        /// </summary>
        [Fact]
        public void Gleiche_Tage_Machen_Die_Kalenderlage_Wirkungslos()
        {
            double[] monat = Monatswerte();
            double[] woche = new double[168];
            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                    woche[t * 24 + h] = h < 6 || h >= 22 ? -3.0 : 2.0;   // NT / HT

            double[] a = PreisModell.AusMonatsUndWochenwerten(monat, woche, 0);
            double[] b = PreisModell.AusMonatsUndWochenwerten(monat, woche, 6);

            Assert.Equal(a, b);
        }

        [Fact]
        public void Profil_Weist_Falsche_Vektorlaengen_Zurueck()
        {
            Assert.Throws<ArgumentNullException>(() => PreisModell.AusMonatsUndWochenwerten(null!, null));
            Assert.Throws<ArgumentException>(() => PreisModell.AusMonatsUndWochenwerten(new double[11], null));
            Assert.Throws<ArgumentException>(() => PreisModell.AusMonatsUndWochenwerten(new double[12], new double[167]));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PreisModell.AusMonatsUndWochenwerten(new double[12], null, 7));
        }

        // =================================================================
        // Rasterwechsel
        // =================================================================

        /// <summary>
        /// Der Rasterwechsel ist Wertwiederholung - wertgleich zum Adapter des
        /// Bestands, aber ohne dessen <c>float</c>-Rundung.
        /// </summary>
        [Fact]
        public void Viertelstundenexpansion_Wiederholt_Den_Stundenwert()
        {
            double[] stunden = new double[RasterAdapter.StundenJahr];
            for (int i = 0; i < stunden.Length; i++) stunden[i] = i * 0.001 - 4.0;

            double[] viertel = PreisModell.ZuViertelstunden(stunden);

            Assert.Equal(RasterAdapter.ViertelstundenJahr, viertel.Length);
            for (int i = 0; i < stunden.Length; i++)
                for (int k = 0; k < 4; k++)
                    Assert.Equal(stunden[i], viertel[i * 4 + k]);   // bitgenau, nicht gerundet
        }

        /// <summary>
        /// Die Expansion bleibt bitgenau, wo der Umweg ueber <c>float</c> rundet -
        /// die Begruendung fuer die eigene <c>double</c>-Ueberladung.
        /// </summary>
        [Fact]
        public void Doubleweg_Ist_Genauer_Als_Der_Floatweg()
        {
            double[] stunden = new double[RasterAdapter.StundenJahr];
            for (int i = 0; i < stunden.Length; i++) stunden[i] = 0.001;   // in float nicht exakt

            double[] direkt = PreisModell.ZuViertelstunden(stunden);
            double[] ueberFloat = RasterAdapter.ZuViertelstundenDouble(RasterAdapter.ZuFloat(stunden));

            Assert.Equal(0.001, direkt[0]);
            Assert.NotEqual(0.001, ueberFloat[0]);
        }

        [Fact]
        public void Viertelstundenreihe_Wird_Unveraendert_Kopiert()
        {
            double[] viertel = new double[RasterAdapter.ViertelstundenJahr];
            for (int i = 0; i < viertel.Length; i++) viertel[i] = i;

            double[] kopie = PreisModell.ZuViertelstunden(viertel);

            Assert.Equal(viertel, kopie);
            Assert.NotSame(viertel, kopie);
        }

        [Fact]
        public void Viertelstundenexpansion_Weist_Falsche_Laengen_Zurueck()
        {
            Assert.Throws<ArgumentNullException>(() => PreisModell.ZuViertelstunden(null!));
            Assert.Throws<ArgumentException>(() => PreisModell.ZuViertelstunden(new double[100]));
            Assert.Throws<ArgumentException>(() => PreisModell.ZuViertelstunden(new double[8784]));
        }

        // =================================================================
        // Aufschlag
        // =================================================================

        [Fact]
        public void Aufschlag_Ist_Additiv_Und_Laesst_Die_Eingangsreihe_Unberuehrt()
        {
            double[] roh = { -1.5, 0.0, 7.25, 100.0 };
            double[] mit = PreisModell.MitAufschlag(roh, 11.746);

            Assert.Equal(new double[] { -1.5, 0.0, 7.25, 100.0 }, roh);      // unveraendert
            Assert.Equal(-1.5 + 11.746, mit[0], 12);
            Assert.Equal(11.746, mit[1], 12);
            Assert.Equal(7.25 + 11.746, mit[2], 12);
            Assert.Equal(100.0 + 11.746, mit[3], 12);
            Assert.NotSame(roh, mit);
        }

        /// <summary>
        /// Negative Spotpreise werden NICHT bei 0 abgeschnitten: Netzentgelt und
        /// Steuern fallen auch bei negativem Boersenpreis an (Fachkonzept 4.2).
        /// </summary>
        [Fact]
        public void Negativer_Spotpreis_Bleibt_Nach_Aufschlag_Rechenbar()
        {
            double[] roh = { -13.545 };                                       // Minimum der 2024er Datei
            double[] mit = PreisModell.MitAufschlag(roh, 11.746);
            Assert.Equal(-1.799, mit[0], 9);                                  // bleibt negativ, kein Clipping
        }

        [Fact]
        public void Aufschlag_Null_Ist_Wertgleich()
        {
            double[] roh = { 1.0, 2.0, 3.0 };
            Assert.Equal(roh, PreisModell.MitAufschlag(roh, 0.0));
        }

        [Fact]
        public void Aufschlag_Auf_Leere_Reihe_Liefert_Leere_Reihe()
        {
            Assert.Empty(PreisModell.MitAufschlag(Array.Empty<double>(), 5.0));
            Assert.Throws<ArgumentNullException>(() => PreisModell.MitAufschlag(null!, 5.0));
        }

        // =================================================================
        // Aufschlagssatz (Fachkonzept 4.2)
        // =================================================================

        /// <summary>Der Regelfall des Fachkonzepts 4.2: Summe 11,746 ct/kWh.</summary>
        private static Aufschlagssatz Regelfall(AufschlagsModus modus = AufschlagsModus.Aufgeschluesselt,
                                                double overrideWert = 0.0)
        {
            return new Aufschlagssatz(new[]
            {
                new Aufschlagskomponente("NETZENTGELT",  6.440, true),
                new Aufschlagskomponente("UMLAGEN",      2.946, true),
                new Aufschlagskomponente("STROMSTEUER",  2.050, true),
                new Aufschlagskomponente("KONZESSION",   0.110, true),
                new Aufschlagskomponente("VERTRIEB",     0.200, true)
            }, modus, overrideWert);
        }

        [Fact]
        public void Komponentensumme_Trifft_Den_Regelfall_Des_Fachkonzepts()
        {
            Assert.Equal(11.746, Regelfall().SummeAktivCtKwh, 9);
            Assert.Equal(11.746, Regelfall().WirksamCtKwh, 9);
            Assert.Equal(0.0, Regelfall().NichtAufgeschluesselterRestCtKwh);
        }

        /// <summary>
        /// Reduzierte Stromsteuer (0,05 statt 2,05 ct/kWh) ergibt die zweite
        /// Summe des Fachkonzepts: 9,746 ct/kWh.
        /// </summary>
        [Fact]
        public void Reduzierte_Stromsteuer_Ergibt_Die_Zweite_Summe()
        {
            Aufschlagssatz satz = new Aufschlagssatz(new[]
            {
                new Aufschlagskomponente("NETZENTGELT",  6.440, true),
                new Aufschlagskomponente("UMLAGEN",      2.946, true),
                new Aufschlagskomponente("STROMSTEUER",  0.050, true),
                new Aufschlagskomponente("KONZESSION",   0.110, true),
                new Aufschlagskomponente("VERTRIEB",     0.200, true)
            });

            Assert.Equal(9.746, satz.SummeAktivCtKwh, 9);
        }

        [Fact]
        public void Inaktive_Komponenten_Gehen_Nicht_In_Die_Summe_Ein()
        {
            Aufschlagssatz satz = new Aufschlagssatz(new[]
            {
                new Aufschlagskomponente("NETZENTGELT", 6.440, true),
                new Aufschlagskomponente("UMLAGEN",     2.946, false),
                new Aufschlagskomponente("STROMSTEUER", 2.050, false)
            });

            Assert.Equal(6.440, satz.SummeAktivCtKwh, 9);
            Assert.Equal(0.0, satz.Komponenten[1].BeitragCtKwh);
            Assert.Equal(2.946, satz.Komponenten[1].WertCtKwh, 9);   // Wert bleibt sichtbar
        }

        /// <summary>
        /// Override-Modus: 20 ct/kWh der V7-Mappe gegen 11,746 ct/kWh
        /// aufgeschluesselt ergibt 8,254 ct/kWh nicht aufgeschluesselten Rest -
        /// die Zahl aus Fachkonzept 4.2.
        /// </summary>
        [Fact]
        public void Override_Weist_Den_Nicht_Aufgeschluesselten_Rest_Aus()
        {
            Aufschlagssatz satz = Regelfall(AufschlagsModus.Gesamtwert, 20.0);

            Assert.Equal(20.0, satz.WirksamCtKwh, 9);
            Assert.Equal(11.746, satz.SummeAktivCtKwh, 9);
            Assert.Equal(8.254, satz.NichtAufgeschluesselterRestCtKwh, 9);
        }

        /// <summary>Reduzierter Fall: 20 - 9,746 = 10,254 ct/kWh Rest.</summary>
        [Fact]
        public void Override_Rest_Im_Reduzierten_Stromsteuerfall()
        {
            Aufschlagssatz satz = new Aufschlagssatz(new[]
            {
                new Aufschlagskomponente("NETZENTGELT",  6.440, true),
                new Aufschlagskomponente("UMLAGEN",      2.946, true),
                new Aufschlagskomponente("STROMSTEUER",  0.050, true),
                new Aufschlagskomponente("KONZESSION",   0.110, true),
                new Aufschlagskomponente("VERTRIEB",     0.200, true)
            }, AufschlagsModus.Gesamtwert, 20.0);

            Assert.Equal(10.254, satz.NichtAufgeschluesselterRestCtKwh, 9);
        }

        /// <summary>
        /// Ein Gesamtwert UNTER der Komponentensumme ergibt einen negativen Rest -
        /// er wird ausgewiesen, nicht verschwiegen.
        /// </summary>
        [Fact]
        public void Override_Unter_Der_Summe_Ergibt_Negativen_Rest()
        {
            Aufschlagssatz satz = Regelfall(AufschlagsModus.Gesamtwert, 5.0);
            Assert.Equal(-6.746, satz.NichtAufgeschluesselterRestCtKwh, 9);
            Assert.Equal(5.0, satz.WirksamCtKwh, 9);
        }

        [Fact]
        public void Aufschlagssatz_Legt_Den_Wirksamen_Wert_Auf_Die_Reihe()
        {
            double[] roh = { 0.0, 10.0 };
            double[] mit = Regelfall().AufReihe(roh);

            Assert.Equal(11.746, mit[0], 9);
            Assert.Equal(21.746, mit[1], 9);
        }

        [Fact]
        public void Leerer_Aufschlagssatz_Ist_Null()
        {
            Aufschlagssatz satz = new Aufschlagssatz(Array.Empty<Aufschlagskomponente>());
            Assert.Equal(0.0, satz.SummeAktivCtKwh);
            Assert.Equal(0.0, satz.WirksamCtKwh);
        }

        [Fact]
        public void Aufschlagssatz_Weist_Unbrauchbare_Eingaben_Zurueck()
        {
            Assert.Throws<ArgumentNullException>(() => new Aufschlagssatz(null!));
            Assert.Throws<ArgumentNullException>(
                () => new Aufschlagssatz(new Aufschlagskomponente[] { null! }));
            Assert.Throws<ArgumentException>(() => new Aufschlagskomponente("", 1.0, true));
        }

        // =================================================================
        // Kennzahlen des Validierungsprotokolls
        // =================================================================

        [Fact]
        public void Spannweite_Und_Negativzaehlung_Beschreiben_Die_Reihe()
        {
            double[] reihe = { -2.0, 0.0, 4.0, 10.0 };

            double min, max, mittel;
            PreisModell.Spannweite(reihe, out min, out max, out mittel);

            Assert.Equal(-2.0, min);
            Assert.Equal(10.0, max);
            Assert.Equal(3.0, mittel, 12);
            Assert.Equal(1, PreisModell.AnzahlNegativ(reihe));
        }

        [Fact]
        public void Spannweite_Weist_Leere_Reihen_Zurueck()
        {
            double min, max, mittel;
            Assert.Throws<ArgumentNullException>(() => PreisModell.Spannweite(null!, out min, out max, out mittel));
            Assert.Throws<ArgumentException>(
                () => PreisModell.Spannweite(Array.Empty<double>(), out min, out max, out mittel));
        }
    }
}
