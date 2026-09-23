using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Auswertung der Bilanzreihen für die Vorschau</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.1, 5.6; Stufe Z1, Gruppe 3): größter Monat und Tag, mittlerer
    /// Tagesgang je Tagtyp und die Woche mit dem größten Tagesbedarf. Die Reihen sind erfunden
    /// und so gebaut, dass jede Stunde ihren Tag und ihre Stunde verrät.
    /// </summary>
    public sealed class ZapfauswertungTests
    {
        /// <summary>Ein Kalender mit Montag als 1. Januar und Wochenendkennzeichen an Sa/So.</summary>
        private static bool[] WeSaSo(int wochentagJan1)
        {
            var we = new bool[Zapfkalender.TAGE];
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
            {
                int wt = Zapfkalender.Wochentag(wochentagJan1, d);
                we[d - 1] = wt == Zapfkalender.SAMSTAG || wt == Zapfkalender.SONNTAG;
            }
            return we;
        }

        /// <summary>Eine Reihe, deren Stundenwert nur vom Tagtyp und der Stunde abhängt.</summary>
        private static Bilanzreihe NachTagtyp(IReadOnlyList<ZapfTagtyp> kalender)
        {
            var h = new double[Bilanzreihe.STUNDEN];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
                for (int s = 0; s < 24; s++)
                    h[d * 24 + s] = (int)kalender[d] * 10.0 + s * 0.1;
            return new Bilanzreihe(h);
        }

        private static Bilanzreihe Konstant(double wert)
            => new Bilanzreihe(Enumerable.Repeat(wert, Bilanzreihe.STUNDEN).ToArray());

        [Fact]
        public void Der_groesste_Monat_und_Tag_sind_die_ersten_Hoechstwerte()
        {
            var h = new double[Bilanzreihe.STUNDEN];
            // Tag 40 (Februar) und Tag 200 (Juli) tragen je 48 kWh; der frühere gewinnt.
            for (int s = 0; s < 24; s++) { h[39 * 24 + s] = 2.0; h[199 * 24 + s] = 2.0; }
            var r = new Bilanzreihe(h);
            Assert.Equal(40, Zapfauswertung.GroessterTag(r));
            Assert.Equal(2, Zapfauswertung.GroessterMonat(r));

            // Ohne Zapfung: Januar und Tag 1.
            Assert.Equal(1, Zapfauswertung.GroessterMonat(Bilanzreihe.Null()));
            Assert.Equal(1, Zapfauswertung.GroessterTag(Bilanzreihe.Null()));
        }

        [Fact]
        public void Der_Tagesgang_mittelt_je_Tagtyp_und_zaehlt_die_Tage()
        {
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(0, WeSaSo(0), null);
            Tagesgangmittel t = Zapfauswertung.Tagesgang(NachTagtyp(kalender), Konstant(1.5), 1, kalender);

            // Januar mit Montag als 1. Januar: 23 Werktage, 4 Samstage, 4 Sonntage.
            Assert.Equal(1, t.Monat);
            Assert.Equal(new[] { 23, 4, 4 }, t.TageJeTagtyp.ToArray());
            for (int s = 0; s < 24; s++)
            {
                Assert.Equal(10.0 + s * 0.1, t.WerktagKw[s], 12);
                Assert.Equal(20.0 + s * 0.1, t.SamstagKw[s], 12);
                Assert.Equal(30.0 + s * 0.1, t.SonnFeiertagKw[s], 12);
                Assert.Equal(1.5, t.ZirkulationKw[s], 12);
            }
        }

        /// <summary>
        /// Die drei Tagesgänge, mit ihren Tagen gewichtet, ergeben die Monatssumme der Reihe —
        /// die Vorschau zeigt dieselbe Energie, die der Lauf verbucht (2.4).
        /// </summary>
        [Fact]
        public void Gewichtet_ergeben_die_Tagesgaenge_die_Monatssumme()
        {
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(3, WeSaSo(3), null);
            var h = new double[Bilanzreihe.STUNDEN];
            for (int i = 0; i < h.Length; i++) h[i] = 1.0 + Math.Sin(i * 0.37) * 0.8;
            var zapfung = new Bilanzreihe(h);

            for (int monat = 1; monat <= 12; monat++)
            {
                Tagesgangmittel t = Zapfauswertung.Tagesgang(zapfung, Bilanzreihe.Null(), monat, kalender);
                double summe = 0.0;
                IReadOnlyList<double>[] gaenge = { t.WerktagKw, t.SamstagKw, t.SonnFeiertagKw };
                for (int k = 0; k < 3; k++)
                    if (gaenge[k] != null) summe += gaenge[k].Sum() * t.TageJeTagtyp[k];
                Assert.Equal(zapfung.MonatssummenKwh[monat - 1], summe, 9);
            }
        }

        [Fact]
        public void Ruhetage_gehen_in_keinen_Tagesgang_ein()
        {
            // Ferien vom 1. bis 10. Januar: diese Tage zählen zu keinem der drei Tagtypen.
            var ferien = new[] { new Ferienfenster(1, 10) };
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(0, WeSaSo(0), ferien);
            Tagesgangmittel t = Zapfauswertung.Tagesgang(NachTagtyp(kalender), Bilanzreihe.Null(), 1, kalender);

            Assert.Equal(31 - 10, t.TageJeTagtyp.Sum());
            Assert.Equal(10.0, t.WerktagKw[0], 12);

            // Ein Kalender ohne Wochenende (Altkonvention) kennt weder Samstag noch Sonntag.
            ZapfTagtyp[] ohneWe = Zapfkalender.Bilden(0, new bool[Zapfkalender.TAGE], null);
            Tagesgangmittel w = Zapfauswertung.Tagesgang(NachTagtyp(ohneWe), Bilanzreihe.Null(), 1, ohneWe);
            Assert.Null(w.SamstagKw);
            Assert.Null(w.SonnFeiertagKw);
            Assert.Equal(31, w.TageJeTagtyp[0]);
        }

        [Fact]
        public void Die_Woche_beginnt_am_Montag_des_groessten_Tages()
        {
            var h = new double[Bilanzreihe.STUNDEN];
            for (int i = 0; i < h.Length; i++) h[i] = i;          // jede Stunde verrät sich
            var zapfung = new Bilanzreihe(h);
            var zirk = new Bilanzreihe(h.Select(x => x * 2).ToArray());

            // Der größte Tag ist der 365.; die Woche ragte über das Jahresende und wird
            // hineingeschoben: Beginn am Tag 359.
            Wochenausschnitt w = Zapfauswertung.Woche(zapfung, zirk, 2);
            Assert.Equal(365, w.GroessterTag);
            Assert.Equal(359, w.Starttag);
            Assert.Equal(Zapfkalender.Wochentag(2, 359), w.WochentagStarttag);
            Assert.Equal(168, w.ZapfungKw.Count);
            Assert.Equal(358 * 24.0, w.ZapfungKw[0]);
            Assert.Equal(2 * (358 * 24.0 + 167), w.ZirkulationKw[167]);

            // Mitten im Jahr: Tag 100 bei Montag = 1. Januar ist ein Dienstag, die Woche
            // beginnt am Montag, dem 99. Tag.
            var mitte = new double[Bilanzreihe.STUNDEN];
            for (int s = 0; s < 24; s++) mitte[99 * 24 + s] = 5.0;
            Wochenausschnitt m = Zapfauswertung.Woche(new Bilanzreihe(mitte), Bilanzreihe.Null(), 0);
            Assert.Equal(100, m.GroessterTag);
            Assert.Equal(99, m.Starttag);
            Assert.Equal(0, m.WochentagStarttag);
            Assert.Equal(5.0, m.ZapfungKw[24]);

            // Am Jahresanfang: Tag 2 bei Mittwoch = 1. Januar - der Montag läge vor dem Jahr.
            var anfang = new double[Bilanzreihe.STUNDEN];
            anfang[24] = 9.0;
            Wochenausschnitt a = Zapfauswertung.Woche(new Bilanzreihe(anfang), Bilanzreihe.Null(), 2);
            Assert.Equal(1, a.Starttag);
            Assert.Equal(2, a.WochentagStarttag);
        }

        [Fact]
        public void Ungueltige_Eingaben_werden_benannt_abgelehnt()
        {
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(0, WeSaSo(0), null);
            Assert.Throws<ArgumentNullException>(() => Zapfauswertung.Tagesgang(null, Bilanzreihe.Null(), 1, kalender));
            Assert.Throws<ArgumentException>(() => Zapfauswertung.Tagesgang(Bilanzreihe.Null(), Bilanzreihe.Null(), 1, new ZapfTagtyp[3]));
            Assert.Throws<ArgumentOutOfRangeException>(() => Zapfauswertung.Tagesgang(Bilanzreihe.Null(), Bilanzreihe.Null(), 13, kalender));
            Assert.Throws<ArgumentOutOfRangeException>(() => Zapfauswertung.Woche(Bilanzreihe.Null(), Bilanzreihe.Null(), 7));
        }
    }
}
