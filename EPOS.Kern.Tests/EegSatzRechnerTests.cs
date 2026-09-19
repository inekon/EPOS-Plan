using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — <b>der <see cref="EegSatzRechner"/></b> (Analysepapier
    /// 2026-09-19, Befund N3: „keine Testklasse"; die 16 BNetzA-Werte waren als
    /// Soll dokumentiert und warteten auf ihren Test).
    ///
    /// <para><b>Das Abnahmekriterium der Katalogsaat</b> war von Anfang an: „Alle 16
    /// BNetzA-Werte 08/2026 sind Unit-Test-Soll" (<c>GesetzKatalog.cs:1522</c>).
    /// Diese Klasse löst das ein — sie rechnet alle sechzehn aus den fünf
    /// Basiswerten und den fünf Voll-Zuschlägen nach.</para>
    ///
    /// <para><b>Degression auf UNRUNDETER Basis.</b> Die Basis wird mit 0,99ⁿ
    /// fortgeschrieben, gerundet wird nur der ANZUWENDENDE Wert: 8,60 × 0,99⁵ =
    /// 8,17851 (Fenster 02–07/2026), × 0,99⁶ = 8,09679 → 8,10 (ab 08/2026).
    /// Schrittweises Runden lieferte an Zwischenstichtagen andere Werte.</para>
    ///
    /// <para>Rein — Katalogsätze kommen als Delegat aus
    /// <see cref="GesetzKatalog.Vorbelegung"/>, keine Datenbank.</para>
    /// </summary>
    public class EegSatzRechnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Der Stichtag der BNetzA-Tabelle, auf die alles bezogen ist.</summary>
        private static readonly DateTime IBN_08_2026 = new DateTime(2026, 8, 1);

        private static Func<string, int, double?> Katalog()
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            return (s, j) => vor.Where(p => p.Schluessel == s && p.JahrVon <= j)
                                .OrderByDescending(p => p.JahrVon)
                                .FirstOrDefault()?.Wert;
        }

        // =====================================================================
        //  1 — Die Degressionsschritte
        // =====================================================================

        /// <summary>
        /// Gezählt wird ab dem 01.02.2024, dann je Halbjahresstichtag (1.2. und
        /// 1.8.). Vor dem ersten Stichtag sind es null Schritte.
        /// </summary>
        [Theory]
        [InlineData(2023, 12, 31, 0)]
        [InlineData(2024, 1, 31, 0)]
        [InlineData(2024, 2, 1, 1)]
        [InlineData(2024, 7, 31, 1)]
        [InlineData(2024, 8, 1, 2)]
        [InlineData(2025, 2, 1, 3)]
        [InlineData(2025, 8, 1, 4)]
        [InlineData(2026, 2, 1, 5)]
        [InlineData(2026, 8, 1, 6)]
        public void Die_Degressionsschritte_zaehlen_die_Halbjahresstichtage(
            int jahr, int monat, int tag, int erwartet)
        {
            Assert.Equal(erwartet, EegSatzRechner.Degressionsschritte(new DateTime(jahr, monat, tag)));
        }

        /// <summary>
        /// Die Basis wird UNRUNDET fortgeschrieben — das ist der Nachtrag N1 und der
        /// Grund, warum die BNetzA-Tabelle an jedem Stichtag getroffen wird.
        /// </summary>
        [Fact]
        public void Die_Degression_laeuft_auf_unrundeter_Basis()
        {
            Func<string, int, double?> kat = Katalog();

            // Fenster 02–07/2026: fünf Schritte.
            double? fuenf = EegSatzRechner.AwKlasseUnrundet(10.0, new DateTime(2026, 2, 1), false, kat);
            Assert.NotNull(fuenf);
            Assert.Equal(8.60 * Math.Pow(0.99, 5), fuenf.Value, 9);
            Assert.Equal(8.17851, fuenf.Value, 5);

            // Ab 08/2026: sechs Schritte — der unrundete Wert, der auf 8,10 rundet.
            //
            // ABWEICHUNG ZUR DOKUMENTATION. Der Klassenkommentar des Rechners
            // (EegSatzRechner.cs:74) nennt für diesen Wert „8,09679". Nachgerechnet
            // sind es 8,60 × 0,99⁶ = 8,0967292848…, auf fünf Nachkommastellen also
            // 8,09673. Die dokumentierte Ziffernfolge ist ein Zahlendreher; am
            // ANZUWENDENDEN Wert 8,10 ändert das nichts, weshalb er nie aufgefallen
            // ist. Gepinnt ist die Rechnung.
            double? sechs = EegSatzRechner.AwKlasseUnrundet(10.0, IBN_08_2026, false, kat);
            Assert.NotNull(sechs);
            Assert.Equal(8.60 * Math.Pow(0.99, 6), sechs.Value, 9);
            Assert.Equal(8.09673, sechs.Value, 5);
            Assert.Equal(8.10, EegSatzRechner.AwKlasse(10.0, IBN_08_2026, false, kat).Value, 2);
        }

        // =====================================================================
        //  2 — Die 16 BNetzA-Werte 08/2026
        // =====================================================================

        /// <summary>
        /// Die zehn anzulegenden Werte der BNetzA-Tabelle für IBN ab 01.08.2026 —
        /// fünf Klassen mal Überschuss und Volleinspeisung.
        /// </summary>
        [Theory]
        // Klasse | Volleinspeisung | anzulegender Wert [ct/kWh]
        [InlineData(10.0, false, 8.10)]
        [InlineData(40.0, false, 7.06)]
        [InlineData(100.0, false, 5.84)]
        [InlineData(400.0, false, 5.84)]
        [InlineData(1000.0, false, 5.84)]
        [InlineData(10.0, true, 12.62)]
        [InlineData(40.0, true, 10.64)]
        [InlineData(100.0, true, 10.64)]
        [InlineData(400.0, true, 8.85)]
        [InlineData(1000.0, true, 7.63)]
        public void Die_zehn_anzulegenden_Werte_der_BNetzA_Tabelle(
            double klasseKw, bool voll, double erwartetCt)
        {
            double? ist = EegSatzRechner.AwKlasse(klasseKw, IBN_08_2026, voll, Katalog());

            Assert.NotNull(ist);
            Assert.Equal(erwartetCt, ist.Value, 2);
        }

        /// <summary>
        /// Die sechs festen Einspeisevergütungen derselben Tabelle: anzulegender
        /// Wert abzüglich 0,40 ct/kWh, und zwar nur bis 100 kW — darüber gibt es
        /// keine feste Vergütung mehr.
        /// </summary>
        [Theory]
        [InlineData(10.0, false, 7.70)]
        [InlineData(40.0, false, 6.66)]
        [InlineData(100.0, false, 5.44)]
        [InlineData(10.0, true, 12.22)]
        [InlineData(40.0, true, 10.24)]
        [InlineData(100.0, true, 10.24)]
        public void Die_sechs_festen_Einspeiseverguetungen_der_BNetzA_Tabelle(
            double klasseKw, bool voll, double erwartetCt)
        {
            double? aw = EegSatzRechner.AwKlasse(klasseKw, IBN_08_2026, voll, Katalog());

            Assert.NotNull(aw);
            Assert.Equal(erwartetCt, Math.Round(aw.Value - 0.40, 2), 2);
        }

        /// <summary>
        /// Eine Leistung, die keine Klassengrenze ist, hat keinen Klassenwert — der
        /// Rechner rät nicht, er gibt <c>null</c>.
        /// </summary>
        [Theory]
        [InlineData(50.0)]
        [InlineData(0.0)]
        [InlineData(300.0)]
        public void Eine_Leistung_ausserhalb_der_Klassengrenzen_hat_keinen_Klassenwert(double kw)
        {
            Assert.Null(EegSatzRechner.AwKlasse(kw, IBN_08_2026, false, Katalog()));
        }

        // =====================================================================
        //  3 — Die leistungsanteilige Mischrechnung (§ 23c EEG)
        // =====================================================================

        /// <summary>
        /// Die Klassen sind MARGINALE Tranchen, kein Stufentarif. Das Beispiel des
        /// Rechenwegs: 300 kWp Überschuss =
        /// (10×8,10 + 30×7,06 + 60×5,84 + 200×5,84) / 300 = 6,04 ct/kWh.
        /// </summary>
        [Fact]
        public void Der_gemischte_Wert_von_300_kWp_ist_6_04_ct()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                300.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.False(r.Unvollstaendig);
            Assert.Equal(6.04, r.AwMixCt, 2);
            Assert.Equal((10 * 8.10 + 30 * 7.06 + 60 * 5.84 + 200 * 5.84) / 300.0,
                         r.AwMixCtUnrundet, 6);

            // Die Zerlegung zeigt die vier Tranchen — sie ist die Auskunft dazu.
            Assert.Equal(4, r.Zerlegung.Count);
            Assert.Equal(new[] { 10.0, 30.0, 60.0, 200.0 },
                         r.Zerlegung.Select(z => z.AnteilKw).ToArray());
            Assert.Equal(300.0, r.Zerlegung.Sum(z => z.AnteilKw), 6);
            Assert.Equal(6, r.DegressionsSchritte);
        }

        /// <summary>
        /// Die Marginalität von einer Klasse zur nächsten: bis 10 kWp ist der Mix der
        /// Klassenwert selbst, ab 10 kWp mischt die nächste Tranche hinein — der Mix
        /// sinkt monoton, springt aber nicht.
        /// </summary>
        [Theory]
        [InlineData(10.0, 8.10)]
        [InlineData(40.0, 7.32)]     // (10×8,10 + 30×7,06)/40
        [InlineData(100.0, 6.43)]    // + 60×5,84
        [InlineData(300.0, 6.04)]
        [InlineData(400.0, 5.99)]
        public void Der_Mix_folgt_den_marginalen_Tranchen(double kwp, double erwartet)
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                kwp, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.Equal(erwartet, r.AwMixCt, 2);
        }

        /// <summary>Volleinspeisung mischt dieselben Tranchen mit den Voll-Werten.</summary>
        [Fact]
        public void Volleinspeisung_mischt_die_Voll_Werte()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                300.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_VOLL, Katalog(), DE);

            Assert.Equal((10 * 12.62 + 30 * 10.64 + 60 * 10.64 + 200 * 8.85) / 300.0,
                         r.AwMixCtUnrundet, 6);
            Assert.Equal(9.51, r.AwMixCt, 2);
        }

        // =====================================================================
        //  4 — Die beiden Abschläge und ihre Grenzen
        // =====================================================================

        /// <summary>
        /// Feste Einspeisevergütung <c>EV_mix = max(0, AW_mix − 0,40)</c>, zulässig
        /// nur bis 100 kW (§ 21 Abs. 1 Nr. 1 EEG). Über der Grenze rechnet der
        /// Rechner sie zwar weiter aus, kennzeichnet sie aber als unzulässig.
        /// </summary>
        [Theory]
        [InlineData(10.0, true)]
        [InlineData(100.0, true)]
        [InlineData(100.5, false)]
        [InlineData(300.0, false)]
        public void Die_feste_Einspeiseverguetung_gilt_nur_bis_100_kW(double kwp, bool zulaessig)
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                kwp, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.Equal(zulaessig, r.EvZulaessig);
            Assert.Equal(Math.Max(0.0, Math.Round(r.AwMixCt - 0.40, 2)), r.EvMixCt, 2);
        }

        /// <summary>
        /// Ausfallvergütung = AW × (1 − 20 %), also 80 % des anzulegenden Wertes —
        /// zulässig nur ÜBER 100 kW (§ 53 Abs. 3 EEG). Die beiden Grenzen sind
        /// komplementär: was die feste Vergütung bekommt, bekommt keine
        /// Ausfallvergütung und umgekehrt.
        /// </summary>
        [Theory]
        [InlineData(10.0, false)]
        [InlineData(100.0, false)]
        [InlineData(100.5, true)]
        [InlineData(300.0, true)]
        public void Die_Ausfallverguetung_gilt_nur_ueber_100_kW(double kwp, bool zulaessig)
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                kwp, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.Equal(zulaessig, r.AusfallvergZulaessig);

            // Gerechnet wird auf der UNRUNDETEN Mischung, nicht auf dem gerundeten
            // Ausweiswert: bei 100 kWp sind 6,432 × 0,8 = 5,1456 → 5,15, während
            // 6,43 × 0,8 = 5,144 → 5,14 ergäbe. Dieselbe Regel wie bei der
            // Degression — gerundet wird nur der Ausgabewert.
            Assert.Equal(Math.Round(r.AwMixCtUnrundet * 0.8, 2), r.AusfallvergCt, 2);

            // Komplementär zur festen Vergütung.
            Assert.NotEqual(r.EvZulaessig, r.AusfallvergZulaessig);
        }

        /// <summary>Der Beleg des Rechenwegs: 300 kWp → 6,04 × 0,8 = 4,83 ct/kWh.</summary>
        [Fact]
        public void Die_Ausfallverguetung_von_300_kWp_ist_4_83_ct()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                300.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.Equal(4.83, r.AusfallvergCt, 2);
        }

        // =====================================================================
        //  5 — Über der Ausschreibungsgrenze
        // =====================================================================

        /// <summary>
        /// Über 1.000 kW gilt die Ausschreibung (§ 22 EEG) — der Rechner kann den
        /// Wert dieses Anteils nicht kennen und sagt das BENANNT, statt zu raten.
        /// </summary>
        [Fact]
        public void Ueber_der_Ausschreibungsgrenze_bleibt_der_Satz_unvollstaendig()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                1500.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.True(r.Unvollstaendig);
            Assert.Equal(500.0, r.AusschreibungsAnteilKw, 6);
        }

        /// <summary>Genau auf der Grenze ist noch nichts auszuschreiben.</summary>
        [Fact]
        public void Genau_auf_der_Ausschreibungsgrenze_ist_der_Satz_vollstaendig()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                1000.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, Katalog(), DE);

            Assert.False(r.Unvollstaendig);
            Assert.Equal(0.0, r.AusschreibungsAnteilKw, 6);
        }

        // =====================================================================
        //  6 — Der Wert ist über die Laufzeit fest
        // =====================================================================

        /// <summary>
        /// Inbetriebnahmeprinzip: Die Degression bestimmt NUR den Stichtagswert zur
        /// Inbetriebnahme. Zwei Anlagen desselben Stichtags tragen denselben Wert,
        /// zwei verschiedener Stichtage nicht.
        /// </summary>
        [Fact]
        public void Der_anzulegende_Wert_haengt_allein_an_der_Inbetriebnahme()
        {
            Func<string, int, double?> kat = Katalog();

            double frueh = EegSatzRechner.AnzulegenderWert(
                300.0, new DateTime(2026, 2, 1), EegSatzRechner.EINSPEISEART_UEBERSCHUSS, kat, DE).AwMixCt;
            double spaet = EegSatzRechner.AnzulegenderWert(
                300.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, kat, DE).AwMixCt;

            Assert.True(frueh > spaet, "Die Degression senkt den Wert mit jedem Stichtag.");

            // Derselbe Stichtag, ein anderer Tag im Fenster — derselbe Wert.
            double gleich = EegSatzRechner.AnzulegenderWert(
                300.0, new DateTime(2026, 12, 31), EegSatzRechner.EINSPEISEART_UEBERSCHUSS, kat, DE).AwMixCt;
            Assert.Equal(spaet, gleich, 6);
        }

        /// <summary>Ohne Katalog rechnet der Rechner nicht — er sagt es.</summary>
        [Fact]
        public void Ohne_Katalog_bleibt_der_Satz_unvollstaendig()
        {
            EegSatzErgebnis r = EegSatzRechner.AnzulegenderWert(
                300.0, IBN_08_2026, EegSatzRechner.EINSPEISEART_UEBERSCHUSS, (s, j) => null, DE);

            Assert.True(r.Unvollstaendig);
        }
    }
}
