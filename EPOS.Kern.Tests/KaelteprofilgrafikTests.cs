using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KBP-3b: die Grafik der Kältebedarfsprofile</b> (Konzept Kältebedarfsprofile, Abschnitt 5) — Kennzahlen (Summe,
    /// Spitze, Vollbenutzungsstunden, Grundlast), Monatssummen, Woche nach Tagesarten, Dauerlinie und die vier
    /// Zeichenmodelle; dazu der Weg des Controllers gegen die Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteprofilgrafikTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string KUEHLRAUM = "Kühlraum";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Werktags 7 bis 19 Uhr 10 kW, sonst 2 kW; Samstag 4 kW, Sonntag 2 kW, jeweils rund um die Uhr.</summary>
        private static double[] Reihe(int wochentagJan1)
        {
            var r = new double[8760];
            for (int d = 0; d < 365; d++)
            {
                int wt = (wochentagJan1 + d) % 7;
                for (int h = 0; h < 24; h++)
                    r[d * 24 + h] = wt == 6 ? 2 : wt == 5 ? 4 : (h >= 7 && h < 19 ? 10 : 2);
            }
            return r;
        }

        [Fact]
        public void Die_Kennzahlen_rechnen_Summe_Spitze_Vollbenutzungsstunden_und_Grundlast()
        {
            var r = Enumerable.Repeat(1.0, 8760).ToArray();
            r[100] = 51.0;                                  // Summe 8 810 kWh, Spitze 51 kW, Minimum 1 kW
            Kaelteprofilkennzahlen k = Kaelteprofilgrafik.Kennzahlen(r);
            Assert.Equal(8.81, k.JahressummeMwh, 9);
            Assert.Equal(51.0, k.SpitzeKw, 9);
            Assert.Equal(8810.0 / 51.0, k.VollbenutzungsstundenH, 9);
            Assert.Equal(1.0, k.GrundlastKw, 9);
            Assert.Equal(8760.0 / 8810.0 * 100.0, k.GrundlastAnteilProzent, 9);

            // Konstante Last: Grundlast trägt alles, Vollbenutzungsstunden = Stundenzahl.
            Kaelteprofilkennzahlen konstant = Kaelteprofilgrafik.Kennzahlen(Enumerable.Repeat(3.0, 8760).ToArray());
            Assert.Equal(8760.0, konstant.VollbenutzungsstundenH, 9);
            Assert.Equal(100.0, konstant.GrundlastAnteilProzent, 9);

            // Leer, null und lauter Nullen: keine Division durch null.
            Assert.Equal(new Kaelteprofilkennzahlen(0, 0, 0, 0, 0), Kaelteprofilgrafik.Kennzahlen(null));
            Kaelteprofilkennzahlen null0 = Kaelteprofilgrafik.Kennzahlen(new double[8760]);
            Assert.Equal(0.0, null0.VollbenutzungsstundenH);
            Assert.Equal(0.0, null0.GrundlastAnteilProzent);

            // Nicht endliche Werte zählen nicht.
            var loch = Enumerable.Repeat(2.0, 10).Append(double.NaN).ToArray();
            Assert.Equal(0.02, Kaelteprofilgrafik.Kennzahlen(loch).JahressummeMwh, 9);
        }

        [Fact]
        public void Die_Monatssummen_folgen_dem_Gemeinjahr_in_MWh()
        {
            double[] m = Kaelteprofilgrafik.Monatssummen(Enumerable.Repeat(1.0, 8760).ToArray());
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (int i = 0; i < 12; i++) Assert.Equal(tage[i] * 24 / 1000.0, m[i], 12);
            Assert.Equal(8.76, m.Sum(), 9);
        }

        [Fact]
        public void Die_Woche_folgt_den_Tagesarten_des_Kalenders()
        {
            const int W0 = 3;                               // 1. Januar ein Donnerstag (Testdatenbank)
            double[] r = Reihe(W0);

            byte[] ohne = Kaelteprofilgrafik.Tagesklassen(W0, null);
            Assert.Equal(Kaelteprofilgrafik.WERKTAG, ohne[0]);  // Donnerstag
            Assert.Equal(Kaelteprofilgrafik.SAMSTAG, ohne[2]);
            Assert.Equal(Kaelteprofilgrafik.SONNTAG, ohne[3]);
            Assert.DoesNotContain(Kaelteprofilgrafik.FERIEN, ohne);

            double[][] gaenge = Kaelteprofilgrafik.Tagesgaenge(r, ohne);
            Assert.Null(gaenge[Kaelteprofilgrafik.FERIEN]);
            double[] woche = Kaelteprofilgrafik.TypischeWoche(gaenge);
            Assert.Equal(168, woche.Length);
            Assert.Equal(10.0, woche[0 * 24 + 8], 9);       // Montag 8 Uhr
            Assert.Equal(2.0, woche[4 * 24 + 22], 9);       // Freitag 22 Uhr
            Assert.Equal(4.0, woche[5 * 24 + 8], 9);        // Samstag
            Assert.Equal(2.0, woche[6 * 24 + 8], 9);        // Sonntag
            Assert.Null(Kaelteprofilgrafik.Ferienwoche(gaenge));

            // Kalender: Ferien gehen Feiertag und Wochentag vor, ein Feiertag zählt als Sonntag.
            var arten = new byte[365];
            arten[0] = Betriebskalender.TAG_FEIERTAG;       // Neujahr, ein Donnerstag
            for (int d = 200; d < 214; d++) arten[d] = Betriebskalender.TAG_FERIEN;
            byte[] mit = Kaelteprofilgrafik.Tagesklassen(W0, arten);
            Assert.Equal(Kaelteprofilgrafik.SONNTAG, mit[0]);
            Assert.All(mit.Skip(200).Take(14), k => Assert.Equal(Kaelteprofilgrafik.FERIEN, k));

            double[][] mitGaengen = Kaelteprofilgrafik.Tagesgaenge(r, mit);
            // Zwei Wochen Ferien: zehn Werktage zu 10/2, je zwei Samstage und Sonntage.
            double ferienMittag = (10 * 10.0 + 2 * 4.0 + 2 * 2.0) / 14.0;
            Assert.Equal(ferienMittag, mitGaengen[Kaelteprofilgrafik.FERIEN][12], 9);
            double[] ferienwoche = Kaelteprofilgrafik.Ferienwoche(mitGaengen);
            Assert.Equal(168, ferienwoche.Length);
            Assert.Equal(ferienMittag, ferienwoche[6 * 24 + 12], 9);
            // Der Feiertag geht in den Sonntag ein: dort mittags ein Donnerstag zu 10 kW unter lauter 2 kW.
            Assert.True(mitGaengen[Kaelteprofilgrafik.SONNTAG][12] > 2.0);
        }

        [Fact]
        public void Die_Dauerlinie_ist_absteigend_sortiert()
        {
            double[] r = Reihe(0);
            r[17] = double.NaN;
            double[] dl = Kaelteprofilgrafik.Dauerlinie(r);
            Assert.Equal(8759, dl.Length);
            for (int i = 1; i < dl.Length; i++) Assert.True(dl[i - 1] >= dl[i]);
            Assert.Equal(10.0, dl[0]);
            Assert.Equal(2.0, dl[^1]);
        }

        [Fact]
        public void Die_Zeichenmodelle_tragen_Zeitachse_Stapel_Woche_und_Rang()
        {
            double[] a = Reihe(3);
            double[] b = a.Select(x => x / 2).ToArray();

            Kaelteprofilbilder eine = Kaelteprofilgrafik.Bilder(a, new[] { ("A", a) }, 3, null);
            Assert.Equal(Kaelteprofilgrafik.Kennzahlen(a), eine.Kennzahlen);

            // Jahresgang: eine Zeitachse über 8 760 Stunden - Tagesband und Achswahl der Oberfläche.
            Assert.NotNull(eine.Jahresgang.Flaeche);
            Assert.Equal(Achsenart.Stunden, eine.Jahresgang.Flaeche.X);
            Assert.Equal(1.0, Zeitachse.StundenJeStuetzstelle(eine.Jahresgang.Flaeche));
            Assert.True(Oberflaechenregel.Anwenden(eine.Jahresgang).Reihen.Single().Tagesband);

            // Dauerlinie: Rang, keine Zeitachse, kein Band.
            Assert.NotNull(eine.Dauerlinie.Flaeche);
            Assert.NotEqual(Achsenart.Stunden, eine.Dauerlinie.Flaeche.X);
            Assert.Equal(0.0, Zeitachse.StundenJeStuetzstelle(eine.Dauerlinie.Flaeche));
            Assert.All(Oberflaechenregel.Anwenden(eine.Dauerlinie).Reihen, r => Assert.False(r.Tagesband));
            double[] dl = eine.Dauerlinie.Reihen[0].Werte;
            for (int i = 1; i < dl.Length; i++) Assert.True(dl[i - 1] >= dl[i]);

            // Woche: 168 Stunden, keine Jahreszeitachse.
            Assert.Equal(168, eine.Woche.Reihen[0].Werte.Length);
            Assert.Equal(0.0, Zeitachse.StundenJeStuetzstelle(eine.Woche.Flaeche));

            // Monate: eine Zeile - zwölf Säulen ohne Zeichenfläche; zwei Zeilen - Stapel je Zeile.
            Assert.Null(eine.Monate.Flaeche);
            Kaelteprofilbilder zwei = Kaelteprofilgrafik.Bilder(a, new[] { ("A", a), ("B", b) }, 3, null);
            Assert.Null(zwei.Monate.Flaeche);
            Assert.Equal(new[] { "A", "B" }, zwei.Monate.Reihen.Select(r => r.Name).ToArray());
            Assert.Equal(Kaelteprofilgrafik.Monatssummen(b), zwei.Monate.Reihen[1].Werte);
        }

        [Fact]
        public void Der_Controller_rechnet_die_markierte_Zeile_mit_der_Jahressumme_des_Dialogs()
        {
            if (!_db.Vorhanden) return;
            var zeile = new Kaeltegrafikzeile(KUEHLRAUM, 30.0, null);
            Kaelteprofilbilder bilder = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { zeile }, zeile);
            Assert.NotNull(bilder);
            Assert.Equal(30.0, bilder.Kennzahlen.JahressummeMwh, 6);
            Assert.True(bilder.Kennzahlen.SpitzeKw > 0);
            Assert.True(bilder.Kennzahlen.GrundlastAnteilProzent > 0);        // Kühlraum: rund um die Uhr

            Assert.Null(KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { zeile }, null));
            var fremd = new Kaeltegrafikzeile("", 1, null);
            Assert.Null(KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { fremd }, fremd));
        }
    }
}
