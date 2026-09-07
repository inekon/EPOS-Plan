using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Spaltenfilter der ZEITREIHENKATALOGE</b> (Anwenderentscheid
    /// <b>W14a-E-10</b> vom 07.09.2026, Stufe <b>S3.2</b> des
    /// <c>Konzept_Katalogfilter_EPOS-Plan.md</c>, Abschnitte 2.10 und 4.10) —
    /// Waermebedarf-Lastgang (4), Stromganglinie (3), Solarthermieganglinie (1).
    ///
    /// <para><b>Der Kern der Stufe ist EINE Gruppenabfrage je Katalog.</b> Jahresarbeit
    /// und Spitze stehen nicht am Kopfsatz, sondern in der Wertetabelle (78 840 bzw.
    /// 35 040 Zeilen in der Testdatenbank); je Zeile zu fragen hiesse, die ganze
    /// Tabelle so oft zu lesen, wie der Katalog Saetze hat. Und die Zahlen muessen
    /// DIESELBEN sein, die die Grafik der Dialoge seit W9-E-3/W12-E-2 zeigt — genau
    /// das ist hier die Gegenprobe: <see cref="GanglinienAuswertungCtrl.Kennzahlen"/>
    /// gegen <see cref="GanglinienAuswertungCtrl.AusKatalog"/>, Satz fuer Satz.</para>
    ///
    /// <para>Kultur gepinnt (Hausregel seit iU9-W8).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogfilterZeitreihenTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;
        private readonly CultureInfo _vorher = CultureInfo.DefaultThreadCurrentCulture;

        public KatalogfilterZeitreihenTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;

            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _vorher;
            CultureInfo.DefaultThreadCurrentUICulture = _vorher;
        }

        // =================================================================
        //  1 - Die Auspraegung, ohne Datenbank
        // =================================================================

        /// <summary>
        /// <b>Die drei Zeitreihenkataloge unterscheiden sich in genau zwei Spalten</b>
        /// (Konzept 4.10): Das Zeitintervall fuehrt nur die Stromganglinie, die
        /// Beschreibung nur die Solarganglinie — die anderen zwei Kopftabellen haben
        /// die Spalte gar nicht.
        /// </summary>
        [Fact]
        public void Die_Spalten_der_drei_Zeitreihenkataloge()
        {
            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner,
                Katalogfilterprofil.SpJahresarbeitMwh,
                Katalogfilterprofil.SpSpitzeKw
            }, Schluessel(Zeitreihenart.Waermebedarf));

            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner,
                Katalogfilterprofil.SpZeitintervall,
                Katalogfilterprofil.SpJahresarbeitMwh,
                Katalogfilterprofil.SpSpitzeKw
            }, Schluessel(Zeitreihenart.Stromganglinie));

            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner,
                Katalogfilterprofil.SpBeschreibung,
                Katalogfilterprofil.SpJahresarbeitMwh,
                Katalogfilterprofil.SpSpitzeKw
            }, Schluessel(Zeitreihenart.Solarganglinie));
        }

        /// <summary>Jahresarbeit und Spitze sind ZAHLENspalten und nennen ihre Einheit.</summary>
        [Fact]
        public void Jahresarbeit_und_Spitze_tragen_ihre_Einheit()
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Stromganglinie);

            Katalogspalte arbeit = p.Spalte(Katalogfilterprofil.SpJahresarbeitMwh);
            Assert.Equal(Katalogspaltenart.Zahl, arbeit.Art);
            Assert.Equal("MWh", arbeit.Einheit);

            Katalogspalte spitze = p.Spalte(Katalogfilterprofil.SpSpitzeKw);
            Assert.Equal(Katalogspaltenart.Zahl, spitze.Art);
            Assert.Equal("kW", spitze.Einheit);
        }

        /// <summary>
        /// Jeder Zeitreihenkatalog fuehrt seinen eigenen Filterstand, und keiner faellt
        /// mit einem Bedarfs- oder Anlagenkatalog zusammen.
        /// </summary>
        [Fact]
        public void Jeder_Zeitreihenkatalog_hat_seinen_eigenen_Schluessel()
        {
            var alle = new List<string>();
            foreach (Zeitreihenart a in new[] { Zeitreihenart.Waermebedarf,
                                                Zeitreihenart.Stromganglinie,
                                                Zeitreihenart.Solarganglinie })
                alle.Add(Katalogfilterprofil.FuerZeitreihe(a).Schluessel);

            foreach (BedarfsArt b in new[] { BedarfsArt.Brauchwasser, BedarfsArt.Prozesswaerme,
                                             BedarfsArt.Stromverbraucher })
                alle.Add(Katalogfilterprofil.FuerBedarf(b).Schluessel);

            foreach (Anlagenart art in Katalogfilterprofil.AlleArten)
                alle.Add(Katalogfilterprofil.Finde(art).Schluessel);

            Assert.Equal(alle.Count, alle.Distinct().Count());
        }

        // =================================================================
        //  2 - Die gemessenen Zahlen (Kenndaten_Test.sqlite)
        // =================================================================

        /// <summary>
        /// Die Zeilenzahlen der drei Kataloge, gemessen am 07.09.2026 — dieselben
        /// Zahlen wie im Befund 1.2 des Konzepts.
        /// </summary>
        [Theory]
        [InlineData(Zeitreihenart.Waermebedarf, 4)]
        [InlineData(Zeitreihenart.Stromganglinie, 3)]
        [InlineData(Zeitreihenart.Solarganglinie, 1)]
        public void Die_Zeilenzahl_je_Zeitreihenkatalog(Zeitreihenart art, int erwartet)
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(erwartet, ZeitreihenKatalogCtrl.Katalogfilterzeilen(art).Count);
        }

        /// <summary>
        /// <b>Die EINE Wahrheit im Kern:</b> Die Gruppenabfrage liefert Satz fuer Satz
        /// dieselben zwei Kennzahlen wie <see cref="GanglinienAuswertungCtrl.AusKatalog"/>
        /// — der Weg, aus dem die Grafik der Dialoge ihre drei Zahlen nimmt
        /// (W9-E-3 / W12-E-2). Ohne diese Gegenprobe stuende in der Liste eine andere
        /// Zahl als im Bild darunter.
        ///
        /// <para><b>Der Fall, an dem es haengt, ist die VIERTELSTUNDENREIHE.</b> Die
        /// Spitze ist dort der Hoechstwert der STUNDENMITTEL und nicht der der
        /// Viertelstundenwerte; ein blankes <c>MAX(Wert)</c> haette 4 590 statt
        /// 1 513,5 kW angezeigt. Zwei der drei Stromganglinien der Testdatenbank
        /// fuehren 35 040 Werte — der Fall ist also wirklich getroffen.</para>
        /// </summary>
        [Theory]
        [InlineData(Zeitreihenart.Waermebedarf)]
        [InlineData(Zeitreihenart.Stromganglinie)]
        [InlineData(Zeitreihenart.Solarganglinie)]
        public void Die_Gruppenabfrage_liefert_dieselben_Zahlen_wie_die_Grafik(Zeitreihenart art)
        {
            if (!_db.Vorhanden) return;

            GanglinienQuelle quelle = GanglinienQuelle.Zu(art);

            foreach (Katalogfilterzeile z in ZeitreihenKatalogCtrl.Katalogfilterzeilen(art))
            {
                GanglinienAuswertung einzeln =
                    GanglinienAuswertungCtrl.AusKatalog(quelle, z.Bezeichner);
                if (!einzeln.Erfolgreich) continue;

                double? arbeit = z.Zahl(Katalogfilterprofil.SpJahresarbeitMwh);
                double? spitze = z.Zahl(Katalogfilterprofil.SpSpitzeKw);

                Assert.NotNull(arbeit);
                Assert.NotNull(spitze);
                Assert.Equal(einzeln.JahresarbeitMwh, arbeit.Value, 6);
                Assert.Equal(einzeln.SpitzeKw, spitze.Value, 6);
            }
        }

        /// <summary>
        /// <b>Die zwei Viertelstundenreihen sind wirklich in der Probe.</b> Ohne diesen
        /// Zeugen liefe der Fall oben gruen durch, ohne den Fall je gesehen zu haben,
        /// fuer den er da ist. Gemessen am 07.09.2026: Ganglinie 21 traegt 35 040
        /// Werte, ihre Viertelstundenspitze ist 4 590 kW, ihre STUNDENspitze 1 513,5 kW.
        /// </summary>
        [Fact]
        public void Die_Stundenspitze_einer_Viertelstundenreihe_ist_die_kleinere_Zahl()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen =
                ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.Stromganglinie);

            Katalogfilterzeile viertel = zeilen.FirstOrDefault(
                z => Math.Abs((z.Zahl(Katalogfilterprofil.SpZeitintervall) ?? 0) - 4) < 0.5);
            Assert.NotNull(viertel);

            double? spitze = viertel.Zahl(Katalogfilterprofil.SpSpitzeKw);
            Assert.NotNull(spitze);

            // Die Viertelstundenspitze derselben Reihe - der Wert, den ein blankes
            // MAX(Wert) geliefert haette.
            object roh = StilleDb.Scalar(
                "SELECT MAX(Wert) FROM " + StromganglinieStammCtrl.DATA_STAMM +
                " WHERE ID_Ganglinie = ?",
                new DbParam("@g", DbParamTyp.Integer) { Wert = viertel.Id });
            double viertelspitze = Convert.ToDouble(roh, CultureInfo.InvariantCulture);

            Assert.True(spitze.Value < viertelspitze,
                        "Die Stundenspitze (" + spitze.Value + ") muss unter der " +
                        "Viertelstundenspitze (" + viertelspitze + ") liegen.");
        }

        /// <summary>
        /// <b>Die Laufzeit der Gruppenabfrage je Katalog</b> — die Zahl, die der
        /// Bericht der Stufe S3.2 nennt. Geprueft wird nur, dass sie ueberhaupt
        /// endlich ist; die Messung selbst steht in der Mitschrift, weil eine feste
        /// Millisekundengrenze auf einem geteilten Laeufer flatterhaft waere.
        ///
        /// <para><b>Die Gegenprobe ist der Vergleich mit dem Weg JE ZEILE</b>: Er
        /// liest dieselbe Tabelle einmal je Katalogsatz und ist deshalb bei drei
        /// Saetzen rund dreimal so teuer.</para>
        /// </summary>
        [Fact]
        public void Die_Laufzeit_der_Gruppenabfrage_je_Katalog()
        {
            if (!_db.Vorhanden) return;

            foreach (Zeitreihenart art in new[] { Zeitreihenart.Waermebedarf,
                                                  Zeitreihenart.Stromganglinie,
                                                  Zeitreihenart.Solarganglinie })
            {
                GanglinienQuelle quelle = GanglinienQuelle.Zu(art);

                // Einmal warmlaufen, damit nicht der erste Seitenzugriff gemessen wird.
                GanglinienAuswertungCtrl.Kennzahlen(quelle);

                var uhr = Stopwatch.StartNew();
                IReadOnlyList<Katalogfilterzeile> zeilen =
                    ZeitreihenKatalogCtrl.Katalogfilterzeilen(art);
                uhr.Stop();
                long gruppe = uhr.ElapsedMilliseconds;

                uhr.Restart();
                foreach (Katalogfilterzeile z in zeilen)
                    GanglinienAuswertungCtrl.AusKatalog(quelle, z.Bezeichner);
                uhr.Stop();

                _aus.WriteLine(art + ": Liste " + gruppe + " ms (" + zeilen.Count +
                               " Zeilen), je Zeile " + uhr.ElapsedMilliseconds + " ms");

                Assert.NotEmpty(zeilen);
            }
        }

        /// <summary>
        /// <b>Eine unbrauchbare Reihe zeigt Leerwerte, keinen erfundenen Wert</b>
        /// (W6-E-1). Der Fall entsteht, sobald eine Wertetabelle weder 8 760 noch
        /// 35 040 Zeilen fuehrt — dieselbe Regel, nach der
        /// <c>GanglinienAuswertungCtrl</c> die Reihe als unbrauchbar meldet.
        /// </summary>
        [Fact]
        public void Eine_Reihe_ausserhalb_des_Rasters_zeigt_Leerwerte()
        {
            if (!_db.Vorhanden) return;

            const string probe = "ZZZ Probe ohne Werte";

            // Ein Kopfsatz ohne Werte - die Gruppenabfrage kennt seine Id gar nicht.
            // Er wird am Ende wieder weggeraeumt: Die Arbeitskopie gehoert der ganzen
            // Testklasse (IClassFixture), und Die_Zeilenzahl_je_Zeitreihenkatalog
            // zaehlt denselben Katalog.
            DataRepository.ExecuteSQL(
                "INSERT INTO " + SolarganglinieStammCtrl.HEAD_STAMM +
                " (Bezeichner, Beschreibung, ReadOnly) VALUES (?, ?, 0)",
                new DbParam("@b", probe),
                new DbParam("@d", "Probe"));
            try
            {
                Katalogfilterzeile zeile = ZeitreihenKatalogCtrl
                    .Katalogfilterzeilen(Zeitreihenart.Solarganglinie)
                    .First(z => z.Bezeichner == probe);

                Assert.Null(zeile.Zahl(Katalogfilterprofil.SpJahresarbeitMwh));
                Assert.Null(zeile.Zahl(Katalogfilterprofil.SpSpitzeKw));
                Assert.Equal(ParameterVerwendung.LEER,
                             zeile.Text(Katalogfilterprofil.SpJahresarbeitMwh));
            }
            finally
            {
                DataRepository.ExecuteSQL(
                    "DELETE FROM " + SolarganglinieStammCtrl.HEAD_STAMM +
                    " WHERE Bezeichner = ?", new DbParam("@b", probe));
            }
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        private static string[] Schluessel(Zeitreihenart art)
            => Katalogfilterprofil.FuerZeitreihe(art).Spalten.Select(s => s.Schluessel).ToArray();
    }
}
