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

            // Kalenderwochen nach DIN EN ISO 8601: Der 1. Januar ist ein Donnerstag - 53 Wochen, KW 1 beginnt am 29.12.
            Assert.Equal(53, Kaelteprofilgrafik.Wochenzahl(W0));
            Assert.Equal(52, Kaelteprofilgrafik.Wochenzahl(0));
            Assert.Equal(-3, Kaelteprofilgrafik.ErsterTag(1, W0));
            Assert.Equal(0, Kaelteprofilgrafik.ErsterTag(1, 0));
            Assert.Equal(3, Kaelteprofilgrafik.ErsterTag(1, 4));                // Freitag: KW 1 beginnt am 4. Januar
            Assert.Equal(1, Kaelteprofilgrafik.WocheDesTags(1, 4));             // der 2. Januar gehört zu keiner Woche des Jahres
            Assert.Equal(27, Kaelteprofilgrafik.WocheDesTags(180, W0));
            Assert.Equal(string.Format(WindowsFormsApplication1.MyResource.Resource.KBP_WOCHE_EINTRAG, 27, "29.06.", "05.07."), Kaelteprofilgrafik.Wochentext(27, W0));
            Assert.Equal(string.Format(WindowsFormsApplication1.MyResource.Resource.KBP_WOCHE_EINTRAG, 1, "01.01.", "04.01."), Kaelteprofilgrafik.Wochentext(1, W0));

            double[] woche = Kaelteprofilgrafik.Wochenwerte(r, 2, W0);         // KW 2: Montag, der 5. Januar
            Assert.Equal(168, woche.Length);
            Assert.Equal(10.0, woche[0 * 24 + 8], 9);       // Montag 8 Uhr
            Assert.Equal(2.0, woche[4 * 24 + 22], 9);       // Freitag 22 Uhr
            Assert.Equal(4.0, woche[5 * 24 + 8], 9);        // Samstag
            Assert.Equal(2.0, woche[6 * 24 + 8], 9);        // Sonntag
            double[] erste = Kaelteprofilgrafik.Wochenwerte(r, 1, W0);
            Assert.All(erste.Take(72), w => Assert.Equal(0.0, w));             // 29. bis 31.12. liegen außerhalb
            Assert.Equal(10.0, erste[3 * 24 + 8], 9);                          // Donnerstag, der 1. Januar
            Assert.Equal(new byte[] { Kaelteprofilgrafik.AUSSERHALB, Kaelteprofilgrafik.AUSSERHALB, Kaelteprofilgrafik.AUSSERHALB,
                                      Kaelteprofilgrafik.WERKTAG, Kaelteprofilgrafik.WERKTAG, Kaelteprofilgrafik.SAMSTAG,
                                      Kaelteprofilgrafik.SONNTAG }, Kaelteprofilgrafik.Wochentagesarten(ohne, 1, W0));
            byte[] letzte = Kaelteprofilgrafik.Wochentagesarten(ohne, 53, W0);    // 28.12. bis 03.01.
            Assert.Equal(new byte[] { Kaelteprofilgrafik.AUSSERHALB, Kaelteprofilgrafik.AUSSERHALB, Kaelteprofilgrafik.AUSSERHALB },
                         letzte.Skip(4).ToArray());

            // Vorgabewoche: die Woche der Spitze; ohne Spitze die erste ganze Woche.
            var spitze = (double[])r.Clone();
            spitze[180 * 24 + 12] = 99.0;
            Assert.Equal(27, Kaelteprofilgrafik.Vorgabewoche(spitze, W0));
            Assert.Equal(2, Kaelteprofilgrafik.Vorgabewoche(new double[8760], W0));
            Assert.Equal(1, Kaelteprofilgrafik.Vorgabewoche(new double[8760], 0));

            // Kalender: Ferien gehen Feiertag und Wochentag vor, ein Feiertag zählt als Sonntag.
            var arten = new byte[365];
            arten[0] = Betriebskalender.TAG_FEIERTAG;       // Neujahr, ein Donnerstag
            for (int d = 200; d < 214; d++) arten[d] = Betriebskalender.TAG_FERIEN;
            byte[] mit = Kaelteprofilgrafik.Tagesklassen(W0, arten);
            Assert.Equal(Kaelteprofilgrafik.SONNTAG, mit[0]);
            Assert.All(mit.Skip(200).Take(14), k => Assert.Equal(Kaelteprofilgrafik.FERIEN, k));

            // KW 30 beginnt am 20. Juli (Tag 200): sieben Ferientage im Hintergrund der Woche.
            Assert.All(Kaelteprofilgrafik.Wochentagesarten(mit, 30, W0), k => Assert.Equal(Kaelteprofilgrafik.FERIEN, k));
            Assert.Equal(Kaelteprofilgrafik.SONNTAG, Kaelteprofilgrafik.Wochentagesarten(mit, 1, W0)[3]);   // Neujahr
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

            // Woche: 168 Stunden der Vorgabewoche (die Spitze liegt im ersten Werktag 7 Uhr), keine Jahreszeitachse, EINE
            // Reihe (die Tagesarten sind Hintergrund), durchsichtig; dieselbe Referenz wie Wochenbild(Vorgabewoche).
            Assert.Equal(1, eine.Vorgabewoche);
            Assert.Equal(53, eine.Wochenzahl);
            Assert.Equal(53, eine.Wocheneintraege.Count);
            Assert.Equal(168, eine.Woche.Reihen.Single().Werte.Length);
            Assert.Equal(0.0, Zeitachse.StundenJeStuetzstelle(eine.Woche.Flaeche));
            Assert.Equal((byte)0, eine.Woche.Hintergrund.Deckung);
            Assert.Same(eine.Woche, eine.Wochenbild(1).Modell);
            Kaeltewochenbild kw27 = eine.Wochenbild(27);
            Assert.Same(kw27, eine.Wochenbild(27));
            Assert.Equal(179, kw27.ErsterTag);
            Assert.Equal(a.Skip(179 * 24).Take(168).ToArray(), kw27.Modell.Reihen.Single().Werte);
            Assert.Same(eine.Wochenbild(53), eine.Wochenbild(99));
            var lage = kw27.Flaechenlage;
            Assert.True(lage.Links > 0 && lage.Links + lage.Breite < 1 && lage.Oben > 0 && lage.Oben + lage.Hoehe < 1);

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
            var zeile = new Kaeltegrafikzeile(KUEHLRAUM, 30.0, null, null, null);
            Kaelteprofilbilder bilder = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { zeile }, zeile);
            Assert.NotNull(bilder);
            Assert.Equal(30.0, bilder.Kennzahlen.JahressummeMwh, 6);
            Assert.True(bilder.Kennzahlen.SpitzeKw > 0);
            Assert.True(bilder.Kennzahlen.GrundlastAnteilProzent > 0);        // Kühlraum: rund um die Uhr

            Assert.Null(KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { zeile }, null));
            var fremd = new Kaeltegrafikzeile("", 1, null);
            Assert.Null(KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { fremd }, fremd));
        }

        /// <summary>
        /// Nacharbeit KBP-3b: Ungespeicherte Monatswerte, Wochenprofil und Betriebskalender des Dialogs gehen in die Grafik
        /// ein — die Reihe gleicht der, die nach dem Speichern derselben Werte gerechnet wird; Woche und Stundenreihe kommen
        /// aus demselben Arbeitsstand (die Ferientage des ungespeicherten Kalenders stehen in beiden).
        /// </summary>
        [Fact]
        public void Die_Vorschau_rechnet_ungespeicherte_Monate_Woche_und_Kalender_wie_nach_dem_Speichern()
        {
            if (!_db.Vorhanden) return;
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("?", KUEHLRAUM)));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 50 }
            }));
            System.Data.DataTable z = DataRepository.GetDataTable(
                "SELECT z.ID AS Zeile, z.ID_Kaeltebedarf AS Kopie FROM Z_Projekt_Kaeltebedarf z JOIN Tab_Kaeltebedarf k " +
                "ON k.ID = z.ID_Kaeltebedarf WHERE z.ID_Projekt = ? AND k.Bezeichner = ? ORDER BY z.ID DESC",
                new DbParam("?", PROJEKT), new DbParam("?", KUEHLRAUM));
            int zeile = Convert.ToInt32(z.Rows[0]["Zeile"]);
            int kopie = Convert.ToInt32(z.Rows[0]["Kopie"]);

            double[] monate = { 2, 2, 4, 6, 10, 14, 18, 16, 12, 8, 5, 3 };
            var woche = new double[168];
            for (int i = 0; i < 168; i++) woche[i] = i / 24 < 5 && i % 24 >= 8 && i % 24 < 18 ? 3.0 : 0.5;
            int kalender = BetriebskalenderCtrl.Speichern(new Betriebskalender
            {
                Bezeichner = "Sommerpause", FerienKuerzen = true, Ferien = { new Ferienzeitraum(201, 214) }
            });
            Assert.True(kalender > 0);

            var gespeichert = new Kaeltegrafikzeile(KUEHLRAUM, 50, null);
            Kaelteprofilbilder vorher = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { gespeichert }, gespeichert);
            var offen = new Kaeltegrafikzeile(KUEHLRAUM, 50, kalender, monate, woche);
            Kaelteprofilbilder arbeit = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { offen }, offen);
            Assert.NotNull(vorher);
            Assert.NotNull(arbeit);
            Assert.NotEqual(vorher.StundeKwh, arbeit.StundeKwh);              // der Arbeitsstand wirkt ...
            Assert.True(arbeit.Kennzahlen.JahressummeMwh < 50.0);            // ... die Ferien kürzen die Menge

            // Dieselben Werte gespeichert: dieselbe Reihe ohne Arbeitsstand.
            Assert.True(KaeltebedarfStammCtrl.ProjektProfilSchreiben(kopie, monate, woche));
            DataRepository.ExecuteNonQuery("UPDATE Z_Projekt_Kaeltebedarf SET ID_Betriebskalender = ? WHERE ID = ?",
                                           new DbParam("?", kalender), new DbParam("?", zeile));
            var nachher = new Kaeltegrafikzeile(KUEHLRAUM, 50, kalender);
            BedarfsVorschau lauf = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, PROJEKT, new[] { KUEHLRAUM }, null,
                new Dictionary<string, double> { { KUEHLRAUM, 50 } });
            for (int h = 0; h < 8760; h++) Assert.Equal(lauf.KaelteStunde[h], arbeit.StundeKwh[h], 9);
            Kaelteprofilbilder gesp = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { nachher }, nachher);
            for (int h = 0; h < 8760; h++) Assert.Equal(gesp.StundeKwh[h], arbeit.StundeKwh[h], 9);

            // Kalender gespeichert, im Dialog auf „keiner“ gestellt: Stundenreihe UND Woche ohne Ferien.
            var ohneKalender = new Kaeltegrafikzeile(KUEHLRAUM, 50, null, monate, woche);
            Kaelteprofilbilder ohne = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { ohneKalender }, ohneKalender);
            int w0 = arbeit.WochentagJan1;
            int kw = Kaelteprofilgrafik.WocheDesTags(205, w0);
            Kaeltewochenbild mitFerien = arbeit.Wochenbild(kw), ohneFerien = ohne.Wochenbild(kw);
            Assert.Equal(arbeit.StundeKwh.Skip(mitFerien.ErsterTag * 24).Take(168).ToArray(), mitFerien.Modell.Reihen.Single().Werte);
            for (int t = 0; t < 7; t++)
            {
                int tag = mitFerien.ErsterTag + t;
                bool ferien = tag >= 200 && tag <= 213;
                Assert.Equal(ferien, mitFerien.Tagesarten[t] == Kaelteprofilgrafik.FERIEN);
                Assert.NotEqual(Kaelteprofilgrafik.FERIEN, ohneFerien.Tagesarten[t]);
                double tagMit = arbeit.StundeKwh.Skip(tag * 24).Take(24).Sum();
                double tagOhne = ohne.StundeKwh.Skip(tag * 24).Take(24).Sum();
                if (ferien) Assert.Equal(0.0, tagMit, 9);                   // Ferienfaktor 0: Stillstand, in Woche und Reihe
                else Assert.True(tagOhne > 0);
            }
        }
    }
}
