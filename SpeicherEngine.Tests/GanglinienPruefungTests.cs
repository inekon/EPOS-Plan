using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Importpruefung (AP5, Fachkonzept 3.2): je ein Fall fuer Raster,
    /// Einheit, Schaltjahr, Minutenmittelung, Sommerzeit, Luecken/Dubletten,
    /// Plausibilitaet und Kultur.
    /// </summary>
    /// <remarks>
    /// Alle Reihen sind synthetisch und folgen einem festen Muster
    /// (<see cref="Muster"/>), damit jeder Lauf dieselben Zahlen prueft. Ein
    /// Referenzjahr mit bekannten Umstellterminen wird durchgaengig verwendet:
    /// 2023 (Normaljahr, 26.03. / 29.10.) und 2024 (Schaltjahr, 31.03. / 27.10.).
    /// </remarks>
    public sealed class GanglinienPruefungTests
    {
        private const int Normaljahr = 2023;
        private const int Schaltjahr = 2024;

        // ==================================================================
        // Synthetische Reihen
        // ==================================================================

        /// <summary>
        /// Festes, streng deterministisches Lastmuster in kW: Tagesgang plus
        /// Wochenwelle, immer positiv, ohne Zufall.
        /// </summary>
        private static double Muster(int i)
        {
            return 100.0 + (i % 24) * 2.0 + (i % 7) * 0.5;
        }

        private static double[] Reihe(int anzahl)
        {
            double[] w = new double[anzahl];
            for (int i = 0; i < anzahl; i++) w[i] = Muster(i);
            return w;
        }

        /// <summary>Lueckenlose Zeitachse ab 01.01. 00:00 (naive Ortszeit, ohne Umstellung).</summary>
        private static DateTime[] Zeitachse(int jahr, int anzahl, int schrittMinuten)
        {
            var z = new DateTime[anzahl];
            DateTime t = new DateTime(jahr, 1, 1);
            for (int i = 0; i < anzahl; i++) { z[i] = t; t = t.AddMinutes(schrittMinuten); }
            return z;
        }

        /// <summary>
        /// Zeitachse einer echten Ortszeitreihe <b>mit</b> Sommerzeitumstellung:
        /// am letzten Maerzsonntag fehlt die Stunde 02:00, am letzten
        /// Oktobersonntag steht sie doppelt.
        /// </summary>
        private static DateTime[] ZeitachseMitUmstellung(int jahr, int schrittMinuten)
        {
            DateTime maerzVon = GanglinienPruefung.LetzterSonntag(jahr, 3).AddHours(2);
            DateTime maerzBis = maerzVon.AddHours(1);
            DateTime oktVon = GanglinienPruefung.LetzterSonntag(jahr, 10).AddHours(2);
            DateTime oktBis = oktVon.AddHours(1);

            var liste = new List<DateTime>();
            DateTime t = new DateTime(jahr, 1, 1);
            DateTime ende = new DateTime(jahr + 1, 1, 1);
            while (t < ende)
            {
                if (t >= maerzVon && t < maerzBis) { t = t.AddMinutes(schrittMinuten); continue; }
                liste.Add(t);
                if (t >= oktVon && t < oktBis) liste.Add(t);     // Rueckstellung: Stunde doppelt
                t = t.AddMinutes(schrittMinuten);
            }
            return liste.ToArray();
        }

        private static GanglinienPruefErgebnis Pruefe(
            double[] werte,
            DateTime[]? zeit = null,
            GanglinienEinheit einheit = GanglinienEinheit.Kilowatt,
            GanglinienRaster deklariert = GanglinienRaster.Unbekannt,
            IntervallKonvention konvention = IntervallKonvention.Automatisch)
        {
            return GanglinienPruefung.Pruefe(new GanglinienPruefEingang
            {
                Rohwerte = werte,
                Zeitstempel = zeit,
                Einheit = einheit,
                DeklariertesRaster = deklariert,
                Konvention = konvention
            });
        }

        private static bool Hat(GanglinienPruefErgebnis e, string schluessel)
        {
            return e.Protokoll.Any(m => m.Schluessel == schluessel);
        }

        private static PruefMeldung Finde(GanglinienPruefErgebnis e, string schluessel)
        {
            return e.Protokoll.First(m => m.Schluessel == schluessel);
        }

        // ==================================================================
        // Raster
        // ==================================================================

        [Fact]
        public void Stundenreihe_ohne_Zeitstempel_wird_uebernommen()
        {
            var e = Pruefe(Reihe(8760));

            Assert.True(e.Erfolgreich);
            Assert.Equal(8760, e.Werte.Length);
            Assert.Equal(GanglinienRaster.Stunde, e.Zielraster);
            Assert.Equal(1, e.Zeitinterval);
            Assert.False(e.SchaltjahrNormalisiert);
            Assert.False(e.Gemittelt);
            for (int i = 0; i < e.Werte.Length; i++) Assert.Equal(Muster(i), e.Werte[i]);
        }

        [Fact]
        public void Viertelstundenreihe_ohne_Zeitstempel_wird_uebernommen()
        {
            var e = Pruefe(Reihe(35040));

            Assert.True(e.Erfolgreich);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(GanglinienRaster.Viertelstunde, e.Zielraster);
            Assert.Equal(4, e.Zeitinterval);
        }

        [Fact]
        public void Raster_wird_aus_dem_Zeitstempelabstand_erkannt()
        {
            var e = Pruefe(Reihe(35040), Zeitachse(Normaljahr, 35040, 15));

            Assert.True(e.Erfolgreich);
            Assert.Equal(GanglinienRaster.Viertelstunde, e.Zielraster);
            Assert.Equal("15", Finde(e, GanglinienPruefung.SchluesselRasterAusZeit).Werte[0]);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselKonventionAnfang));
        }

        [Fact]
        public void Widerspruch_zwischen_Zeitschritt_und_Anzahl_ist_ein_Fehler()
        {
            // 8.760 Werte, aber ein 15-Minuten-Zeitstempelabstand.
            var e = Pruefe(Reihe(8760), Zeitachse(Normaljahr, 8760, 15));

            Assert.False(e.Erfolgreich);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselRasterWiderspruch));
            Assert.Empty(e.Werte);
        }

        [Fact]
        public void Abweichende_Rasterdeklaration_ist_nur_eine_Warnung()
        {
            var e = Pruefe(Reihe(35040), deklariert: GanglinienRaster.Stunde);

            Assert.True(e.Erfolgreich);            // Erkennung gewinnt
            Assert.Equal(GanglinienRaster.Viertelstunde, e.Zielraster);
            var m = Finde(e, GanglinienPruefung.SchluesselRasterAbweichend);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "1", "4" }, m.Werte);
        }

        [Fact]
        public void Unbekannte_Anzahl_ist_ein_Fehler()
        {
            var e = Pruefe(Reihe(1000));

            Assert.False(e.Erfolgreich);
            Assert.Equal("1000", Finde(e, GanglinienPruefung.SchluesselRasterUnbekannt).Werte[0]);
        }

        // ==================================================================
        // Einheit
        // ==================================================================

        [Fact]
        public void KWh_je_Viertelstunde_wird_mit_vier_auf_kW_gebracht()
        {
            double[] arbeit = new double[35040];
            for (int i = 0; i < arbeit.Length; i++) arbeit[i] = Muster(i) / 4.0;   // kWh je 1/4 h

            var e = Pruefe(arbeit, einheit: GanglinienEinheit.KilowattstundeJeIntervall);

            Assert.True(e.Erfolgreich);
            Assert.Equal("4", Finde(e, GanglinienPruefung.SchluesselEinheitUmgerechnet).Werte[0]);
            for (int i = 0; i < e.Werte.Length; i++) Assert.Equal(Muster(i), e.Werte[i], 9);
        }

        [Fact]
        public void KWh_je_Stunde_bleibt_zahlengleich()
        {
            var e = Pruefe(Reihe(8760), einheit: GanglinienEinheit.KilowattstundeJeIntervall);

            Assert.True(e.Erfolgreich);
            Assert.Equal("1", Finde(e, GanglinienPruefung.SchluesselEinheitUmgerechnet).Werte[0]);
            for (int i = 0; i < e.Werte.Length; i++) Assert.Equal(Muster(i), e.Werte[i]);
        }

        // ==================================================================
        // Minutenmittelung
        // ==================================================================

        [Fact]
        public void Minutenreihe_wird_auf_Viertelstunden_gemittelt()
        {
            var e = Pruefe(Reihe(525600));

            Assert.True(e.Erfolgreich);
            Assert.True(e.Gemittelt);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(GanglinienRaster.Viertelstunde, e.Zielraster);
            Assert.Equal(4, e.Zeitinterval);          // Minutenreihen landen als 4

            var m = Finde(e, GanglinienPruefung.SchluesselMinutenGemittelt);
            Assert.Equal(new[] { "525600", "35040" }, m.Werte);

            // Stichprobe: arithmetisches Mittel ueber je 15 Rohwerte.
            for (int g = 0; g < 40; g++)
            {
                double summe = 0.0;
                for (int k = 0; k < 15; k++) summe += Muster(g * 15 + k);
                Assert.Equal(summe / 15.0, e.Werte[g], 9);
            }
        }

        [Fact]
        public void Minutenreihe_in_kWh_wird_zuerst_umgerechnet_dann_gemittelt()
        {
            double[] arbeit = new double[525600];
            for (int i = 0; i < arbeit.Length; i++) arbeit[i] = Muster(i) / 60.0;  // kWh je Minute

            var e = Pruefe(arbeit, einheit: GanglinienEinheit.KilowattstundeJeIntervall);

            Assert.True(e.Erfolgreich);
            Assert.Equal("60", Finde(e, GanglinienPruefung.SchluesselEinheitUmgerechnet).Werte[0]);
            for (int g = 0; g < 20; g++)
            {
                double summe = 0.0;
                for (int k = 0; k < 15; k++) summe += Muster(g * 15 + k);
                Assert.Equal(summe / 15.0, e.Werte[g], 8);
            }
        }

        // ==================================================================
        // Schaltjahr
        // ==================================================================

        [Fact]
        public void Schaltjahr_Stundenreihe_ohne_Zeitstempel_laesst_den_29_Februar_aus()
        {
            var e = Pruefe(Reihe(8784));

            Assert.True(e.Erfolgreich);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.Equal(8760, e.Werte.Length);

            var m = Finde(e, GanglinienPruefung.SchluesselSchaltjahr);
            Assert.Equal(new[] { "8784", "8760", "24" }, m.Werte);

            // Ohne Zeitstempel faellt der Block ab Tag 60 (Index 59*24 = 1416).
            var p = Finde(e, GanglinienPruefung.SchluesselSchaltjahrPosition);
            Assert.Equal(new[] { "1417", "1440" }, p.Werte);

            Assert.Equal(Muster(1415), e.Werte[1415]);      // 28.02. 23:00
            Assert.Equal(Muster(1440), e.Werte[1416]);      // direkt der 01.03. 00:00
        }

        [Fact]
        public void Schaltjahr_Viertelstundenreihe_ohne_Zeitstempel_laesst_96_Werte_aus()
        {
            var e = Pruefe(Reihe(35136));

            Assert.True(e.Erfolgreich);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(new[] { "35136", "35040", "96" },
                         Finde(e, GanglinienPruefung.SchluesselSchaltjahr).Werte);
            Assert.Equal(new[] { "5665", "5760" },
                         Finde(e, GanglinienPruefung.SchluesselSchaltjahrPosition).Werte);
            Assert.Equal(Muster(5760), e.Werte[5664]);
        }

        [Fact]
        public void Schaltjahr_mit_Zeitstempeln_entfernt_datumsgenau()
        {
            DateTime[] zeit = Zeitachse(Schaltjahr, 8784, 60);
            var e = Pruefe(Reihe(8784), zeit);

            Assert.True(e.Erfolgreich);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.Equal(8760, e.Werte.Length);
            Assert.False(Hat(e, GanglinienPruefung.SchluesselSchaltjahrPosition));  // Datum statt Position

            // Der 29.02.2024 ist im Schaltjahr ebenfalls Tag 60 -> gleicher Schnitt.
            Assert.Equal(Muster(1440), e.Werte[1416]);
        }

        // ==================================================================
        // Sommerzeit
        // ==================================================================

        [Fact]
        public void Ortszeitreihe_mit_Umstellung_wird_zur_glatten_Jahresreihe()
        {
            DateTime[] zeit = ZeitachseMitUmstellung(Normaljahr, 60);
            Assert.Equal(8760, zeit.Length);                       // 23 h im Maerz + 25 h im Oktober

            double[] werte = Reihe(zeit.Length);
            var e = Pruefe(werte, zeit);

            Assert.True(e.Erfolgreich);
            Assert.True(e.SommerzeitBehandelt);
            Assert.Equal(8760, e.Werte.Length);

            var luecke = Finde(e, GanglinienPruefung.SchluesselSommerzeitLuecke);
            Assert.Equal(PruefStufe.Info, luecke.Stufe);
            Assert.Equal("26.03.2023 02:00", luecke.Werte[0]);
            Assert.Equal("1", luecke.Werte[1]);

            var dublette = Finde(e, GanglinienPruefung.SchluesselSommerzeitDublette);
            Assert.Equal(PruefStufe.Info, dublette.Stufe);
            Assert.Equal("29.10.2023 02:00", dublette.Werte[0]);
            Assert.Equal("2", dublette.Werte[1]);

            // Maerzluecke: Wertwiederholung des Vorintervalls (Hauskonvention).
            int idxLuecke = (int)(new DateTime(Normaljahr, 3, 26, 2, 0, 0) - new DateTime(Normaljahr, 1, 1)).TotalHours;
            Assert.Equal(e.Werte[idxLuecke - 1], e.Werte[idxLuecke]);

            // Oktoberdublette: arithmetisches Mittel der beiden Rohwerte.
            int rohDoppelt = Array.IndexOf(zeit, new DateTime(Normaljahr, 10, 29, 2, 0, 0));
            int idxDublette = (int)(new DateTime(Normaljahr, 10, 29, 2, 0, 0) - new DateTime(Normaljahr, 1, 1)).TotalHours;
            Assert.Equal((werte[rohDoppelt] + werte[rohDoppelt + 1]) / 2.0, e.Werte[idxDublette], 12);
        }

        [Fact]
        public void Ortszeitreihe_mit_Umstellung_auch_viertelstuendlich()
        {
            DateTime[] zeit = ZeitachseMitUmstellung(Normaljahr, 15);
            Assert.Equal(35040, zeit.Length);

            var e = Pruefe(Reihe(zeit.Length), zeit);

            Assert.True(e.Erfolgreich);
            Assert.True(e.SommerzeitBehandelt);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal("4", Finde(e, GanglinienPruefung.SchluesselSommerzeitLuecke).Werte[1]);
            Assert.Equal(4, e.Protokoll.Count(m => m.Schluessel == GanglinienPruefung.SchluesselSommerzeitDublette));
        }

        [Fact]
        public void Schaltjahr_und_Sommerzeit_zusammen()
        {
            DateTime[] zeit = ZeitachseMitUmstellung(Schaltjahr, 60);
            Assert.Equal(8784, zeit.Length);

            var e = Pruefe(Reihe(zeit.Length), zeit);

            Assert.True(e.Erfolgreich);
            Assert.True(e.SommerzeitBehandelt);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.Equal(8760, e.Werte.Length);
            Assert.Equal("31.03.2024 02:00", Finde(e, GanglinienPruefung.SchluesselSommerzeitLuecke).Werte[0]);
            Assert.Equal("27.10.2024 02:00", Finde(e, GanglinienPruefung.SchluesselSommerzeitDublette).Werte[0]);
        }

        // ==================================================================
        // Luecken und Dubletten ausserhalb der Umstellung
        // ==================================================================

        [Fact]
        public void Echte_Luecke_ist_ein_Fehler()
        {
            var zeit = Zeitachse(Normaljahr, 8760, 60).ToList();
            var werte = Reihe(8760).ToList();
            int weg = (int)(new DateTime(Normaljahr, 6, 15, 10, 0, 0) - new DateTime(Normaljahr, 1, 1)).TotalHours;
            zeit.RemoveAt(weg);
            werte.RemoveAt(weg);

            var e = Pruefe(werte.ToArray(), zeit.ToArray());

            Assert.False(e.Erfolgreich);
            var m = Finde(e, GanglinienPruefung.SchluesselLuecke);
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Equal("15.06.2023 10:00", m.Werte[0]);
            Assert.Equal("1", m.Werte[1]);
        }

        [Fact]
        public void Echte_Dublette_ist_ein_Fehler()
        {
            var zeit = Zeitachse(Normaljahr, 8760, 60).ToList();
            var werte = Reihe(8760).ToList();
            int doppelt = (int)(new DateTime(Normaljahr, 6, 15, 10, 0, 0) - new DateTime(Normaljahr, 1, 1)).TotalHours;
            zeit.Insert(doppelt, zeit[doppelt]);
            werte.Insert(doppelt, werte[doppelt]);

            var e = Pruefe(werte.ToArray(), zeit.ToArray());

            Assert.False(e.Erfolgreich);
            Assert.Equal("15.06.2023 10:00", Finde(e, GanglinienPruefung.SchluesselDublette).Werte[0]);
        }

        [Fact]
        public void Rueckwaerts_laufende_Zeitstempel_sind_ein_Fehler()
        {
            var zeit = Zeitachse(Normaljahr, 8760, 60);
            zeit[100] = zeit[100].AddDays(-5);

            var e = Pruefe(Reihe(8760), zeit);

            Assert.False(e.Erfolgreich);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselNichtMonoton));
        }

        // ==================================================================
        // Intervallkonvention
        // ==================================================================

        [Fact]
        public void Intervallende_wird_erkannt_und_zurueckgesetzt()
        {
            // Reihe 01:00 ... 01.01. des Folgejahres 00:00 - klassischer Zaehlerexport.
            var zeit = new DateTime[8760];
            DateTime t = new DateTime(Normaljahr, 1, 1, 1, 0, 0);
            for (int i = 0; i < 8760; i++) { zeit[i] = t; t = t.AddHours(1); }

            var e = Pruefe(Reihe(8760), zeit);

            Assert.True(e.Erfolgreich);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselKonventionEnde));
            Assert.False(Hat(e, GanglinienPruefung.SchluesselJahresanfang));   // nach Verschiebung 01.01. 00:00
            Assert.Equal(8760, e.Werte.Length);
        }

        [Fact]
        public void Nicht_am_Jahresanfang_beginnende_Reihe_warnt()
        {
            var zeit = Zeitachse(Normaljahr, 8760, 60);
            for (int i = 0; i < zeit.Length; i++) zeit[i] = zeit[i].AddDays(1);

            var e = Pruefe(Reihe(8760), zeit, konvention: IntervallKonvention.Anfang);

            Assert.True(e.Erfolgreich);
            var m = Finde(e, GanglinienPruefung.SchluesselJahresanfang);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal("02.01.2023 00:00", m.Werte[0]);
        }

        // ==================================================================
        // Grundpruefung
        // ==================================================================

        [Fact]
        public void Leere_Reihe_ist_ein_Fehler()
        {
            var e = Pruefe(Array.Empty<double>());
            Assert.False(e.Erfolgreich);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselKeineWerte));
        }

        [Fact]
        public void NaN_ist_ein_Fehler_mit_Zeilennummer()
        {
            double[] w = Reihe(8760);
            w[4711] = double.NaN;

            var e = Pruefe(w);

            Assert.False(e.Erfolgreich);
            Assert.Equal("4712", Finde(e, GanglinienPruefung.SchluesselUngueltigerWert).Werte[0]);
        }

        [Fact]
        public void Unterschiedlich_lange_Spalten_sind_ein_Fehler()
        {
            var e = Pruefe(Reihe(8760), Zeitachse(Normaljahr, 8759, 60));

            Assert.False(e.Erfolgreich);
            Assert.Equal(new[] { "8760", "8759" },
                         Finde(e, GanglinienPruefung.SchluesselZeitstempelAnzahl).Werte);
        }

        [Fact]
        public void Pruefe_ohne_Eingang_wirft()
        {
            Assert.Throws<ArgumentNullException>(() => GanglinienPruefung.Pruefe(null!));
        }

        // ==================================================================
        // Plausibilitaet
        // ==================================================================

        [Fact]
        public void Negative_Werte_sind_nur_eine_Warnung()
        {
            double[] w = Reihe(8760);
            w[10] = -5.0;
            w[20] = -12.5;

            var e = Pruefe(w);

            Assert.True(e.Erfolgreich);                 // Warnung blockiert nicht
            Assert.True(e.HatWarnungen);
            Assert.Equal(new[] { "2", "-12.5" },
                         Finde(e, GanglinienPruefung.SchluesselNegativeWerte).Werte);
        }

        [Fact]
        public void Lange_Nullserie_wird_gemeldet()
        {
            double[] w = Reihe(8760);
            for (int i = 1000; i < 1000 + 30; i++) w[i] = 0.0;

            var e = Pruefe(w);

            Assert.True(e.Erfolgreich);
            Assert.Equal(new[] { "1001", "30" }, Finde(e, GanglinienPruefung.SchluesselNullserie).Werte);
        }

        [Fact]
        public void Kurze_Nullserie_wird_nicht_gemeldet()
        {
            double[] w = Reihe(8760);
            for (int i = 1000; i < 1000 + 5; i++) w[i] = 0.0;

            var e = Pruefe(w);

            Assert.False(Hat(e, GanglinienPruefung.SchluesselNullserie));
        }

        [Fact]
        public void Nullreihe_wird_gemeldet()
        {
            var e = Pruefe(new double[8760]);

            Assert.True(e.Erfolgreich);
            Assert.True(Hat(e, GanglinienPruefung.SchluesselAlleNull));
            Assert.False(Hat(e, GanglinienPruefung.SchluesselAusreisser));
        }

        [Fact]
        public void Ausreisser_oberhalb_des_Medianvielfachen_wird_gemeldet()
        {
            double[] w = Reihe(8760);
            w[5000] = 1_000_000.0;

            var e = Pruefe(w);

            Assert.True(e.Erfolgreich);
            var m = Finde(e, GanglinienPruefung.SchluesselAusreisser);
            Assert.Equal("1", m.Werte[0]);
            Assert.Equal("1000000", m.Werte[1]);
            Assert.Equal("20", m.Werte[3]);
        }

        [Fact]
        public void Ausreisserpruefung_laesst_sich_abschalten()
        {
            double[] w = Reihe(8760);
            w[5000] = 1_000_000.0;

            var e = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
            {
                Rohwerte = w,
                AusreisserFaktor = 0.0
            });

            Assert.False(Hat(e, GanglinienPruefung.SchluesselAusreisser));
        }

        // ==================================================================
        // Kultur
        // ==================================================================

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        [InlineData("tr-TR")]
        public void Kernlauf_ist_kulturunabhaengig(string kultur)
        {
            var vorher = Thread.CurrentThread.CurrentCulture;
            var vorherUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                var ci = new CultureInfo(kultur);
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;

                DateTime[] zeit = ZeitachseMitUmstellung(Schaltjahr, 15);
                double[] werte = Reihe(zeit.Length);
                werte[77] = -3.25;
                werte[9000] = 500_000.0;

                var e = Pruefe(werte, zeit, GanglinienEinheit.KilowattstundeJeIntervall);

                Assert.True(e.Erfolgreich);
                Assert.Equal(35040, e.Werte.Length);

                // Referenzwerte fest verdrahtet: bitgleich in jeder Kultur.
                Assert.Equal(400.0, e.Werte[0], 12);
                Assert.Equal(-13.0, e.Werte[77], 12);
                Assert.Equal(new[] { "1", "-13" },
                             Finde(e, GanglinienPruefung.SchluesselNegativeWerte).Werte);
                Assert.Equal("2000000", Finde(e, GanglinienPruefung.SchluesselAusreisser).Werte[1]);
                Assert.Equal("31.03.2024 02:00",
                             Finde(e, GanglinienPruefung.SchluesselSommerzeitLuecke).Werte[0]);
                Assert.Equal("4", Finde(e, GanglinienPruefung.SchluesselEinheitUmgerechnet).Werte[0]);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = vorher;
                Thread.CurrentThread.CurrentUICulture = vorherUi;
            }
        }

        [Fact]
        public void Zwei_Kulturen_liefern_bitgleiche_Reihe_und_Protokoll()
        {
            var (werteDe, protokollDe) = LaufUnter("de-DE");
            var (werteEn, protokollEn) = LaufUnter("en-US");

            Assert.Equal(werteDe.Length, werteEn.Length);
            for (int i = 0; i < werteDe.Length; i++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(werteDe[i]),
                             BitConverter.DoubleToInt64Bits(werteEn[i]));
            Assert.Equal(protokollDe, protokollEn);
        }

        private static (double[] Werte, string[] Protokoll) LaufUnter(string kultur)
        {
            var vorher = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(kultur);

                DateTime[] zeit = ZeitachseMitUmstellung(Normaljahr, 60);
                double[] werte = Reihe(zeit.Length);
                werte[123] = 0.1 + 0.2;                      // klassische Rundungsstelle
                var e = Pruefe(werte, zeit);
                return (e.Werte, e.Protokoll.Select(m => m.ToString()).ToArray());
            }
            finally { Thread.CurrentThread.CurrentCulture = vorher; }
        }

        // ==================================================================
        // Protokoll und Ergebnisobjekt
        // ==================================================================

        [Fact]
        public void Protokoll_meldet_Ergebnis_mit_Jahresarbeit()
        {
            var e = Pruefe(Reihe(8760));

            var m = Finde(e, GanglinienPruefung.SchluesselErgebnis);
            Assert.Equal("8760", m.Werte[0]);
            Assert.Equal("1", m.Werte[1]);
            double erwartet = 0.0;
            for (int i = 0; i < 8760; i++) erwartet += Muster(i);
            Assert.Equal(erwartet.ToString("0.###", CultureInfo.InvariantCulture), m.Werte[2]);
        }

        [Fact]
        public void Sauberer_Lauf_braucht_keine_Bestaetigung()
        {
            var e = Pruefe(Reihe(8760));

            Assert.True(e.Erfolgreich);
            Assert.False(e.HatWarnungen);
            Assert.False(e.BestaetigungNoetig);
        }

        [Fact]
        public void Eingriffe_erzwingen_eine_Bestaetigung()
        {
            var e = Pruefe(Reihe(8784));

            Assert.True(e.Erfolgreich);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.True(e.BestaetigungNoetig);
        }

        [Fact]
        public void PruefMeldung_ToString_ist_sprachneutral()
        {
            var m = new PruefMeldung(PruefStufe.Warnung, "IMPORT_PROT_TEST", "a", "b");
            Assert.Equal("IMPORT_PROT_TEST: a; b", m.ToString());
            Assert.Equal("IMPORT_PROT_LEER", new PruefMeldung(PruefStufe.Info, "IMPORT_PROT_LEER").ToString());
        }

        // ==================================================================
        // Hilfsfunktionen
        // ==================================================================

        [Theory]
        [InlineData(8760, GanglinienRaster.Stunde)]
        [InlineData(8784, GanglinienRaster.Stunde)]
        [InlineData(35040, GanglinienRaster.Viertelstunde)]
        [InlineData(35136, GanglinienRaster.Viertelstunde)]
        [InlineData(525600, GanglinienRaster.Minute)]
        [InlineData(527040, GanglinienRaster.Minute)]
        [InlineData(0, GanglinienRaster.Unbekannt)]
        [InlineData(8761, GanglinienRaster.Unbekannt)]
        public void RasterAusAnzahl_deckt_Normal_und_Schaltjahr_ab(int anzahl, GanglinienRaster erwartet)
        {
            Assert.Equal(erwartet, GanglinienPruefung.RasterAusAnzahl(anzahl));
        }

        [Theory]
        [InlineData(2023, 3, "26.03.2023")]
        [InlineData(2023, 10, "29.10.2023")]
        [InlineData(2024, 3, "31.03.2024")]
        [InlineData(2024, 10, "27.10.2024")]
        [InlineData(2026, 3, "29.03.2026")]
        [InlineData(2026, 10, "25.10.2026")]
        public void LetzterSonntag_trifft_die_EU_Umstelltermine(int jahr, int monat, string erwartet)
        {
            Assert.Equal(erwartet,
                GanglinienPruefung.LetzterSonntag(jahr, monat).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Mittelwerte_bildet_das_arithmetische_Mittel()
        {
            double[] w = { 1, 2, 3, 4, 10, 20, 30, 40 };
            Assert.Equal(new[] { 2.5, 25.0 }, GanglinienPruefung.Mittelwerte(w, 4));
            Assert.Throws<ArgumentException>(() => GanglinienPruefung.Mittelwerte(w, 3));
            Assert.Throws<ArgumentNullException>(() => GanglinienPruefung.Mittelwerte(null!, 4));
        }

        [Fact]
        public void SchrittMinuten_haelt_der_Zeitumstellung_stand()
        {
            DateTime[] zeit = ZeitachseMitUmstellung(Normaljahr, 60);
            Assert.Equal(60, GanglinienPruefung.SchrittMinuten(zeit));
            Assert.Equal(15, GanglinienPruefung.SchrittMinuten(ZeitachseMitUmstellung(Normaljahr, 15)));
            Assert.Equal(0, GanglinienPruefung.SchrittMinuten(new[] { DateTime.Now }));
        }
    }
}
