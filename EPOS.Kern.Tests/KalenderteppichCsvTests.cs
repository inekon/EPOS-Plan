using System;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>CSV am Kalenderteppich</b> (CSV-4): Das Teppichbild führt seine Tafel 365 × 24 im Modell
    /// (ungezeichnet), und <see cref="ZeitreihenCsv.Kalenderteppich(Tagesstundentafel)"/> schreibt sie
    /// wie das Bild — je Tag des Gemeinjahrs eine Zeile mit Datum „TT.MM.“, Wochentag des Rasters und
    /// 24 Stundenwerten. Dazu die Jahreszählung ab 0 des Rasters <see cref="Zeitraster.Jahr"/>.
    /// Deutsche Texte und Zahlformate: die Kultur ist auf de-DE gepinnt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KalenderteppichCsvTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private const int DONNERSTAG = 3;
        private const long GEBAEUDE_1051 = 10657;
        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public KalenderteppichCsvTests(TestDatenbank db) => _db = db;

        public void Dispose() => _kultur.Dispose();

        /// <summary>Mo–Fr 7–18 Uhr 20 °C, sonst 16 °C, Sonntag „aus“.</summary>
        private static Konditionierungskalender Heizkalender()
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt == 6 ? double.NaN : wt < 5 && s >= 7 && s < 18 ? 20.0 : 16.0;
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(w), null,
                                                Array.Empty<Kalenderregel>());
        }

        private static string[][] Zeilen(string text)
            => text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(z => z.Split(';')).ToArray();

        [Fact]
        public void Das_Teppichmodell_fuehrt_seine_Tafel_365_mal_24()
        {
            Kalenderteppich t = Kalenderteppich.ImGemeinjahr(Heizkalender(), new Gemeinjahrkalender(DONNERSTAG));
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(t);

            Assert.NotNull(m.Tafel);
            Assert.Equal(365 * 24, m.Tafel.Werte.Length);
            Assert.Equal(DONNERSTAG, m.Tafel.WochentagDesErstenTags);
            Assert.Equal("°C", m.Tafel.Einheit);
            Assert.Empty(m.Reihen);
            Assert.True(m.Gleicht(ChartRenderer.KalenderteppichModell(t)));
            // Ohne Teppich (Leerhinweis) keine Tafel.
            Assert.Null(ChartRenderer.KalenderteppichModell(null).Tafel);
        }

        /// <summary>
        /// 365 Zeilen und Kopf, 26 Spalten (Datum, Wochentag, 0 h … 23 h); die erste Zeile ist der
        /// 1. Januar mit dem Wochentag des Rasters, die letzte der 31. Dezember; „aus“ bleibt leer.
        /// </summary>
        [Fact]
        public void Die_Tafel_schreibt_365_Tage_mit_Datum_Wochentag_und_24_Stunden()
        {
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(
                Kalenderteppich.ImGemeinjahr(Heizkalender(), new Gemeinjahrkalender(DONNERSTAG)));
            string[][] z = Zeilen(ZeitreihenCsv.Kalenderteppich(m.Tafel));

            Assert.Equal(366, z.Length);
            Assert.All(z, zeile => Assert.Equal(26, zeile.Length));
            Assert.Equal("Datum", z[0][0]);
            Assert.Equal("Wochentag", z[0][1]);
            Assert.Equal("0 h [°C]", z[0][2]);
            Assert.Equal("23 h [°C]", z[0][25]);

            Assert.Equal("01.01.", z[1][0]);
            Assert.Equal("Do", z[1][1]);
            Assert.Equal("16,0", z[1][2 + 6]);
            Assert.Equal("20,0", z[1][2 + 7]);
            // 4. Januar: Sonntag im Donnerstag-Raster — „aus“.
            Assert.Equal("04.01.", z[4][0]);
            Assert.Equal("So", z[4][1]);
            Assert.All(z[4].Skip(2), w => Assert.Equal("", w));
            Assert.Equal("28.02.", z[59][0]);
            Assert.Equal("01.03.", z[60][0]);
            Assert.Equal("31.12.", z[365][0]);
            Assert.Equal("Do", z[365][1]);
        }

        [Fact]
        public void Der_Kopf_kommt_aus_den_Ressourcen_auch_englisch()
        {
            using var en = new Kulturvorrichtung("en-US");
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(
                Kalenderteppich.ImGemeinjahr(Heizkalender(), new Gemeinjahrkalender(0)));
            string[][] z = Zeilen(ZeitreihenCsv.Kalenderteppich(m.Tafel));

            Assert.Equal(new[] { "Date", "Weekday", "0 h [°C]" }, z[0].Take(3));
            Assert.Equal("Mon", z[1][1]);
            // Das Zahlformat bleibt das des Schreibers (Dezimalkomma), wie bei jeder Zeitreihe.
            Assert.Equal("16,0", z[1][2]);
        }

        /// <summary>
        /// Der Arbeitsstand eines Gebäudes der Testdatenbank rechnet im Raster seiner Klimaregion — der
        /// 1. Januar ist ein Donnerstag; die erste Datumszelle der Datei trägt ihn.
        /// </summary>
        [Fact]
        public void Der_Teppich_der_Testdatenbank_beginnt_am_Donnerstag_dem_1_Januar()
        {
            if (!_db.Vorhanden) return;
            Konditionierungsarbeitsstand a = new KonditionierungCtrl().ArbeitsstandLesen(GEBAEUDE_1051, null, out string meldung);
            Assert.True(a != null, meldung);
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(Kalenderteppich.ImGemeinjahr(Heizkalender(), a.Kalender));
            string[][] z = Zeilen(ZeitreihenCsv.Kalenderteppich(m.Tafel));

            Assert.Equal(366, z.Length);
            Assert.Equal(new[] { "01.01.", "Do" }, z[1].Take(2));
        }

        /// <summary>Die Betrachtungsjahre zählen wie Tafel und Zahlungsstrombild ab dem Jahr 0.</summary>
        [Fact]
        public void Das_Raster_Jahr_zaehlt_ab_0_die_uebrigen_ab_1()
        {
            var spalten = new[] { new ZeitreihenSpalte("Netto", "€", new[] { -500.0, 120.0, 130.0 }) };
            string[][] jahr = Zeilen(ZeitreihenCsv.Text(Zeitraster.Jahr, spalten));
            Assert.Equal(new[] { "Jahr", "Netto [€]" }, jahr[0]);
            Assert.Equal(new[] { "0", "-500,0" }, jahr[1]);
            Assert.Equal(new[] { "2", "130,0" }, jahr[3]);

            Assert.Equal("1", Zeilen(ZeitreihenCsv.Text(Zeitraster.Monat, spalten))[1][0]);
            Assert.Equal(0, ZeitreihenCsv.Erste(Zeitraster.Jahr));
            Assert.Equal(1, ZeitreihenCsv.Erste(Zeitraster.Stunde));
        }
    }
}
