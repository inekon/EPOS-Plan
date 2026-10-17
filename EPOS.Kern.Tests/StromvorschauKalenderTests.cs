using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Classes.Simulation;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Betriebskalender in der Vorschau „Simulation…“ des Bedarfsprofildialogs — Strom</b>
    /// (<see cref="BedarfsVorschauCtrl.ProjektVorschau"/>, <see cref="BedarfsVorschauCtrl.KalenderDesDialogs"/>).
    ///
    /// <para><b>Befund der Sichtprüfung:</b> Ein im Dialog gewählter oder im Blatt „Betriebskalender…“ angelegter
    /// Kalender wirkte in der Vorschau nicht — sie las nur die gespeicherte Zuordnung, die Hülle reichte die Wahl der
    /// Zeile nicht herein. Der Lauf nahm den gespeicherten Kalender schon (<c>BetriebskalenderTests</c>).</para>
    ///
    /// <para><b>Geprüft wird</b> auf Projekt 1041 (eine Stromzuordnung): Ferien 01.07.–01.09. mit Faktor 0 und
    /// „Ferien kürzen“ — Juli und August fallen auf 0, der September um den 1.; ohne Kürzen bleibt jede Monatsmenge;
    /// „Feiertag wie Sonntag“ — ein bundeseinheitlicher Feiertag trägt den Stundengang eines Sonntags desselben
    /// Monats; und die Vorschau mit dem Dialogkalender ist stundengenau der Lauf mit dem gespeicherten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class StromvorschauKalenderTests : IDisposable
    {
        private const int PROJEKT = 1041;

        /// <summary>Jahrestage 1. Juli bis 1. September im Gemeinjahr.</summary>
        private const int FERIEN_VON = 182, FERIEN_BIS = 244;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Ferien_mit_Kuerzen_senken_Juli_und_August_und_die_Jahressumme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            List<string> namen = Namen();
            (int[] a, int[] e) = Monate();

            (double[] ohne, double[] mOhne) = Vorschau(namen, null, a, e);
            Assert.True(mOhne.Sum() > 0);

            int k = Kalender(kuerzen: true);
            (double[] mit, double[] mMit) = Vorschau(namen, Dialog(namen, k), a, e);

            Assert.Equal(0.0, mMit[6]);
            Assert.Equal(0.0, mMit[7]);
            Assert.True(mMit[8] < mOhne[8] && mMit[8] > 0.9 * mOhne[8], "Der 1. September ist Ferientag.");
            for (int m = 0; m < 12; m++)
                if (m < 6 || m > 8) Assert.True(Math.Abs(mMit[m] - mOhne[m]) <= 1e-9 * Math.Max(1, mOhne[m]), "Monat " + (m + 1));
            double erwartet = mOhne.Sum() - mOhne[6] - mOhne[7] - (mOhne[8] - mMit[8]);
            Assert.True(Math.Abs(mMit.Sum() - erwartet) <= 1e-9 * erwartet);
            Assert.True(mit.Sum() < ohne.Sum());
            for (int h = a[6]; h <= e[7]; h++) Assert.Equal(0.0, mit[h]);
        }

        [Fact]
        public void Ferien_ohne_Kuerzen_erhalten_die_Monatsmenge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            List<string> namen = Namen();
            (int[] a, int[] e) = Monate();

            (double[] ohne, double[] mOhne) = Vorschau(namen, null, a, e);
            int k = Kalender(kuerzen: false);
            (double[] mit, double[] mMit) = Vorschau(namen, Dialog(namen, k), a, e);

            for (int m = 0; m < 12; m++)
                Assert.True(Math.Abs(mMit[m] - mOhne[m]) <= 1e-9 * Math.Max(1, mOhne[m]), "Monat " + (m + 1));
            // Der 1. September steht still, der Rest des Monats trägt seine Menge.
            for (int h = a[8]; h < a[8] + 24; h++) Assert.Equal(0.0, mit[h]);
            double restMit = 0, restOhne = 0;
            for (int h = a[8] + 24; h <= e[8]; h++) { restMit += mit[h]; restOhne += ohne[h]; }
            Assert.True(restMit > restOhne);
        }

        [Fact]
        public void Feiertag_traegt_den_Sonntag_des_Wochenprofils()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            List<string> namen = Namen();
            (int[] a, int[] e) = Monate();

            int k = BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = "Nur Feiertage", FeiertagWieSonntag = true });
            Betriebskalender kal = BetriebskalenderCtrl.Lies(k);
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            byte[] arten = kal.Tagesarten(raster);

            (double[] ohne, _) = Vorschau(namen, null, a, e);
            (double[] mit, _) = Vorschau(namen, Dialog(namen, k), a, e);

            int geprueft = 0, abweichend = 0;
            for (int d = 0; d < 365; d++)
            {
                if (arten[d] != Betriebskalender.TAG_FEIERTAG || Wochentag(raster, d) == 6) continue;
                int monat = MonatDesTages(d, a, e);
                int sonntag = Enumerable.Range(0, 365).First(t => MonatDesTages(t, a, e) == monat && Wochentag(raster, t) == 6 &&
                                                                  arten[t] == Betriebskalender.TAG_NORMAL);
                for (int s = 0; s < 24; s++)
                    Assert.True(Math.Abs(mit[d * 24 + s] - mit[sonntag * 24 + s]) <= 1e-12 * Math.Max(1, mit[sonntag * 24 + s]),
                                "Tag " + (d + 1) + ", Stunde " + s);
                for (int s = 0; s < 24; s++)
                    if (Math.Abs(mit[d * 24 + s] - ohne[d * 24 + s]) > 1e-9) { abweichend++; break; }
                geprueft++;
            }
            Assert.True(geprueft >= 5, "Bundeseinheitliche Feiertage außerhalb eines Sonntags: " + geprueft);
            Assert.True(abweichend > 0, "Ohne Kalender trägt ein Feiertag den Gang seines Wochentags.");
        }

        [Fact]
        public void Vorschau_mit_Dialogkalender_ist_der_Lauf_mit_gespeichertem()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            List<string> namen = Namen();
            (int[] a, int[] e) = Monate();
            int k = Kalender(kuerzen: true);

            // Ungespeichert: allein der Dialog kennt den Kalender.
            (double[] vorschau, _) = Vorschau(namen, Dialog(namen, k), a, e);
            (double[] gespeichertOhne, _) = Vorschau(namen, null, a, e);
            Assert.NotEqual(gespeichertOhne.Sum(), vorschau.Sum());

            var zeile = Z_ProjektStromverbraucherCtrl.LiesProjekt(PROJEKT).Single();
            Assert.True(BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zeile.m_ID_Z, k));
            double[] lauf = new SimulationStrombedarf { m_ID_Projekt = PROJEKT }.Stromprofil_Strombedarf_berechnen();
            Assert.NotNull(lauf);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(lauf[h]), BitConverter.DoubleToInt64Bits(vorschau[h]));

            // Gespeichert, ohne Arbeitsstand: dieselbe Reihe; die Wahl „keiner“ im Dialog geht der Zuordnung vor.
            (double[] gespeichert, _) = Vorschau(namen, null, a, e);
            Assert.Equal(vorschau, gespeichert);
            (double[] keiner, _) = Vorschau(namen, Dialog(namen, null), a, e);
            Assert.Equal(gespeichertOhne, keiner);
        }

        // ---- Hilfen ---------------------------------------------------------------------------------------------

        private static List<string> Namen()
        {
            var zeilen = Z_ProjektStromverbraucherCtrl.LiesProjekt(PROJEKT);
            Assert.Single(zeilen);
            return zeilen.Select(z => z.m_szVerbraucher).ToList();
        }

        private static int Kalender(bool kuerzen) => BetriebskalenderCtrl.Speichern(new Betriebskalender
        {
            Bezeichner = "testkalender", Bundesland = null, FeiertagWieSonntag = true, FerienWieSonntag = false,
            Ferienfaktor = 0.0, FerienKuerzen = kuerzen, Ferien = { new Ferienzeitraum(FERIEN_VON, FERIEN_BIS) }
        });

        private static IReadOnlyDictionary<string, ProfilArbeitsstand> Dialog(List<string> namen, int? kalender)
            => BedarfsVorschauCtrl.KalenderDesDialogs(namen.Select(n => (n, kalender)));

        private static (double[] stunden, double[] monate) Vorschau(List<string> namen,
                                                                     IReadOnlyDictionary<string, ProfilArbeitsstand> arbeit,
                                                                     int[] a, int[] e)
        {
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Stromverbraucher, PROJEKT, namen,
                                                                   arbeitsstaende: arbeit);
            Assert.True(v.Erfolgreich);
            double[] stunden = v.Strom.Strombedarf_viertelStundenwerte.Take(8760).ToArray();
            var monate = new double[12];
            for (int m = 0; m < 12; m++)
                for (int h = a[m]; h <= e[m]; h++) monate[m] += stunden[h];
            return (stunden, monate);
        }

        private static (int[] a, int[] e) Monate()
        {
            var a = new int[12];
            var e = new int[12];
            new Init().Monatswerte_berechnen(a, e);
            return (a, e);
        }

        /// <summary>Wochentag des Tages <paramref name="d"/> (0 = 1. Januar), 0 = Montag … 6 = Sonntag.</summary>
        private static int Wochentag(Gemeinjahrkalender raster, int d) => (((raster.W0 % 7) + 7) % 7 + d) % 7;

        private static int MonatDesTages(int d, int[] a, int[] e)
        {
            for (int m = 0; m < 12; m++) if (d * 24 >= a[m] && d * 24 <= e[m]) return m;
            return -1;
        }
    }
}
