using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KBP-4a — die <see cref="Typtagschicht"/></b> ohne Datenbank (Konzept Kältebedarfsprofile 3.4, 10): Summe
    /// erhalten, Monat × Tagesart, Feiertag = Sonntag, Ferienfaktor (umverteilen und kürzen), fehlende Zellen und leere
    /// Typtage benannt, kein Bedarf im Jahr. Raster wie die Testdatenbank: Der 1. Januar ist ein Donnerstag (w₀ = 3).
    /// </summary>
    public class TyptagschichtTests
    {
        private const int W0 = 3;
        private const double SUMME = 12.5;

        private static readonly int[] MO_ANFANG = Monatsanfaenge();

        [Fact]
        public void Die_Jahressumme_bleibt_erhalten()
        {
            KaeltetyptagEingabe e = Drei();
            var reihe = new double[8760];
            Typtagschicht.Ergebnis erg = Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, null, 1.0, false);
            Assert.True(erg.Gerechnet);
            Assert.Empty(erg.FehlendeZellen);
            Assert.Empty(erg.LeereTyptage);
            Assert.Equal(SUMME * 1000.0, reihe.Sum(), 6);
            Assert.All(reihe, w => Assert.True(w >= 0));
        }

        [Fact]
        public void Jeder_Tag_nimmt_den_Typtag_seiner_Matrixzelle()
        {
            KaeltetyptagEingabe e = Drei();
            // Juli, Werktag: ein eigener Typtag 4 mit Bedarf nur in den ersten sechs Stunden.
            e.Typtage.Add(new Kaeltetyptag { Nr = 4, Bezeichner = "Sommernacht", Stunden = Stunden(0, 6, 1.0), Tagesmenge = 3 });
            e.Matrix[6, 0] = 4;
            var reihe = new double[8760];
            Assert.True(Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, null, 1.0, false).Gerechnet);

            int montagJuli = TagImMonat(6, 0);
            int montagJuni = TagImMonat(5, 0);
            int samstagJuli = TagImMonat(6, 5);
            int sonntagJuli = TagImMonat(6, 6);
            double[] juli = Tag(reihe, montagJuli), juni = Tag(reihe, montagJuni);
            Assert.True(juli.Take(6).All(w => w > 0) && juli.Skip(6).All(w => w == 0), "Juli-Werktag hat nicht die Form von Typtag 4.");
            Assert.True(juni.Skip(8).Take(10).All(w => w > 0) && juni.Take(8).All(w => w == 0), "Juni-Werktag hat nicht die Form von Typtag 1.");
            // Tagesmengen: Typtag 4 (3) gegen Typtag 1 (2), Samstag (1), Sonntag (0,5) - dieselbe Normierung für alle Tage.
            Assert.Equal(1.5, juli.Sum() / juni.Sum(), 9);
            Assert.Equal(0.5, Tag(reihe, samstagJuli).Sum() / juni.Sum(), 9);
            Assert.Equal(0.25, Tag(reihe, sonntagJuli).Sum() / juni.Sum(), 9);
        }

        [Fact]
        public void Ein_Feiertag_rechnet_wie_Sonntag()
        {
            KaeltetyptagEingabe e = Drei();
            int mittwoch = TagImMonat(4, 2);   // ein Mittwoch im Mai
            int sonntag = TagImMonat(4, 6);
            byte[] arten = new byte[365];
            arten[mittwoch] = Betriebskalender.TAG_FEIERTAG;
            var reihe = new double[8760];
            Assert.True(Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, arten, 1.0, false).Gerechnet);
            Assert.Equal(Tag(reihe, sonntag), Tag(reihe, mittwoch));
            Assert.Equal(SUMME * 1000.0, reihe.Sum(), 6);
        }

        [Fact]
        public void Ein_Ferientag_ist_ein_Werktag_mit_Ferienfaktor()
        {
            KaeltetyptagEingabe e = Drei();
            int ferienMittwoch = TagImMonat(7, 2);           // August
            int ferienSonntag = TagImMonat(7, 6);
            int dienstag = TagImMonat(7, 1);
            byte[] arten = new byte[365];
            arten[ferienMittwoch] = Betriebskalender.TAG_FERIEN;
            arten[ferienSonntag] = Betriebskalender.TAG_FERIEN;

            // Umverteilen (Vorgabe): Summe bleibt, der Ferientag trägt f · Werktag - auch am Sonntag.
            var verteilt = new double[8760];
            Assert.True(Typtagschicht.TyptageZuJahr(e, SUMME, verteilt, MO_ANFANG, W0, arten, 0.4, false).Gerechnet);
            Assert.Equal(SUMME * 1000.0, verteilt.Sum(), 6);
            double[] werktag = Tag(verteilt, dienstag);
            Assert.Equal(werktag.Select(w => 0.4 * w).ToArray(), Tag(verteilt, ferienMittwoch), new Toleranz(1e-9));
            Assert.Equal(werktag.Select(w => 0.4 * w).ToArray(), Tag(verteilt, ferienSonntag), new Toleranz(1e-9));

            // Kürzen: die übrigen Tage bleiben wie ohne Ferien, die Menge sinkt um den Ausfall.
            var ohne = new double[8760];
            var gekuerzt = new double[8760];
            Typtagschicht.TyptageZuJahr(e, SUMME, ohne, MO_ANFANG, W0, new byte[365], 0.4, true);
            Assert.True(Typtagschicht.TyptageZuJahr(e, SUMME, gekuerzt, MO_ANFANG, W0, arten, 0.4, true).Gerechnet);
            Assert.Equal(Tag(ohne, dienstag), Tag(gekuerzt, dienstag));
            double ausfall = 0.6 * Tag(ohne, dienstag).Sum() + (Tag(ohne, ferienSonntag).Sum() - 0.4 * Tag(ohne, dienstag).Sum());
            Assert.Equal(SUMME * 1000.0 - ausfall, gekuerzt.Sum(), 6);
        }

        [Fact]
        public void Ferien_ohne_Bedarf_rechnen_das_Jahr_ohne_Ferien()
        {
            KaeltetyptagEingabe e = Drei();
            byte[] arten = Enumerable.Repeat(Betriebskalender.TAG_FERIEN, 365).ToArray();
            var reihe = new double[8760];
            Typtagschicht.Ergebnis erg = Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, arten, 0.0, false);
            Assert.True(erg.Gerechnet);
            Assert.True(erg.FerienOhneWirkung);
            Assert.Equal(SUMME * 1000.0, reihe.Sum(), 6);
        }

        [Fact]
        public void Fehlende_Zellen_werden_benannt_und_die_Summe_verteilt_sich()
        {
            KaeltetyptagEingabe e = Drei();
            e.Matrix[11, 2] = 0;      // Dezember, Sonntag: leer
            e.Matrix[0, 1] = 5;       // Januar, Samstag: zeigt auf einen Typtag, den es nicht gibt
            var reihe = new double[8760];
            Typtagschicht.Ergebnis erg = Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, null, 1.0, false);
            Assert.True(erg.Gerechnet);
            Assert.Equal(new[] { "1/samstag", "12/sonntag" }, erg.FehlendeZellen);
            Assert.Equal(5 + 4, erg.TageOhneTyptag);   // fünf Samstage im Januar, vier Sonntage im Dezember (w0 = 3)
            Assert.All(Tag(reihe, TagImMonat(11, 6)), w => Assert.Equal(0.0, w));
            Assert.All(Tag(reihe, TagImMonat(0, 5)), w => Assert.Equal(0.0, w));
            Assert.Equal(SUMME * 1000.0, reihe.Sum(), 6);
        }

        [Fact]
        public void Ein_Typtag_ohne_Stundenwerte_wird_benannt_eine_Tagesmenge_0_nicht()
        {
            KaeltetyptagEingabe e = Drei();
            e.Typtage.Add(new Kaeltetyptag { Nr = 5, Bezeichner = "leer", Stunden = new double[24], Tagesmenge = 1 });
            e.Typtag(3).Tagesmenge = 0;
            e.Matrix[2, 1] = 5;
            var reihe = new double[8760];
            Typtagschicht.Ergebnis erg = Typtagschicht.TyptageZuJahr(e, SUMME, reihe, MO_ANFANG, W0, null, 1.0, false);
            Assert.True(erg.Gerechnet);
            Assert.Equal(new[] { 5 }, erg.LeereTyptage);
            Assert.Empty(erg.FehlendeZellen);
            Assert.All(Tag(reihe, TagImMonat(4, 6)), w => Assert.Equal(0.0, w));   // Sonntag mit Tagesmenge 0
            Assert.Equal(SUMME * 1000.0, reihe.Sum(), 6);
        }

        [Fact]
        public void Ohne_Bedarf_im_Jahr_rechnet_die_Schicht_nichts()
        {
            var leer = new KaeltetyptagEingabe();
            var reihe = Enumerable.Repeat(7.0, 8760).ToArray();
            Typtagschicht.Ergebnis erg = Typtagschicht.TyptageZuJahr(leer, SUMME, reihe, MO_ANFANG, W0, null, 1.0, false);
            Assert.False(erg.Gerechnet);
            Assert.Equal(36, erg.FehlendeZellen.Count);
            Assert.Equal(365, erg.TageOhneTyptag);
            Assert.All(reihe, w => Assert.Equal(0.0, w));
        }

        [Fact]
        public void Die_Pruefung_nennt_Regelverstoesse()
        {
            Assert.Null(Drei().Pruefen());
            KaeltetyptagEingabe e = Drei();
            e.Typtage.Add(new Kaeltetyptag { Nr = 1, Bezeichner = "doppelt", Stunden = Stunden(0, 24, 1) });
            Assert.Contains("doppelt", e.Pruefen());
            e = Drei();
            e.Typtag(2).Stunden[3] = -1;
            Assert.Contains("Stundenwert", e.Pruefen());
            e = Drei();
            e.Typtag(2).Tagesmenge = double.NaN;
            Assert.Contains("Tagesmenge", e.Pruefen());
            e = Drei();
            e.Matrix[3, 0] = 6;
            Assert.Contains("fehlenden Typtag 6", e.Pruefen());
            e = Drei();
            e.Typtage.Add(new Kaeltetyptag { Nr = 7, Bezeichner = "sieben", Stunden = Stunden(0, 24, 1) });
            Assert.Contains("ausserhalb", e.Pruefen());
        }

        // ---- Helfer ---------------------------------------------------------------------------------------------------

        /// <summary>Drei Typtage: 1 Werktag (8–17 Uhr, Menge 2), 2 Samstag (flach, 1), 3 Sonntag (flach, 0,5), alle Monate.</summary>
        internal static KaeltetyptagEingabe Drei()
        {
            var e = new KaeltetyptagEingabe();
            e.Typtage.Add(new Kaeltetyptag { Nr = 1, Bezeichner = "Werktag", Stunden = Stunden(8, 18, 1.0), Tagesmenge = 2 });
            e.Typtage.Add(new Kaeltetyptag { Nr = 2, Bezeichner = "Samstag", Stunden = Stunden(0, 24, 1.0), Tagesmenge = 1 });
            e.Typtage.Add(new Kaeltetyptag { Nr = 3, Bezeichner = "Sonntag", Stunden = Stunden(0, 24, 2.0), Tagesmenge = 0.5 });
            for (int m = 0; m < 12; m++)
            {
                e.Matrix[m, 0] = 1;
                e.Matrix[m, 1] = 2;
                e.Matrix[m, 2] = 3;
            }
            return e;
        }

        internal static double[] Stunden(int von, int bis, double wert)
        {
            var s = new double[24];
            for (int h = von; h < bis; h++) s[h] = wert;
            return s;
        }

        private static double[] Tag(double[] reihe, int tag) => reihe.Skip(tag * 24).Take(24).ToArray();

        private static int[] Monatsanfaenge()
        {
            var a = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
            }
            return a;
        }

        /// <summary>Der erste Tag (0-basiert) des Monats mit dem Wochentag (Montag = 0).</summary>
        private static int TagImMonat(int monat, int wochentag)
        {
            for (int t = MO_ANFANG[monat] / 24; ; t++)
                if ((W0 + t) % 7 == wochentag) return t;
        }

        private sealed class Toleranz : System.Collections.Generic.IEqualityComparer<double>
        {
            private readonly double _eps;
            public Toleranz(double eps) { _eps = eps; }
            public bool Equals(double x, double y) => Math.Abs(x - y) <= _eps * Math.Max(1.0, Math.Abs(x));
            public int GetHashCode(double obj) => 0;
        }
    }
}
