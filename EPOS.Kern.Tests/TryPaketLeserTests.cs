using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Leser des TRY-REGIONALPAKETS</b> (Auftrag KL1-B): Regionswahl aus dem
    /// Zentralverzeichnis, Szenario und Bezugsjahr, die 300-km-Grenze und der
    /// BEREICHSABRUF.
    ///
    /// <para><b>Kein Netz.</b> Der Prüfstand baut ein ZIP im Speicher und bedient
    /// <c>INetzbereich</c> daraus — er zählt dabei Abrufe und übertragene Bytes. Genau
    /// diese Zahl ist der Nachweis, dass das 892-MB-Paket NICHT heruntergeladen
    /// wird.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8).</para>
    /// </summary>
    public class TryPaketLeserTests
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Die drei Regionen des Prüfpakets: Nummer, Breite, Länge.</summary>
        private static readonly (int Nummer, double Lat, double Lon)[] REGIONEN =
        {
            (1, 53.5591, 8.5872),      // Nordseekueste
            (7, 50.9000, 7.0000),      // Rheinland
            (13, 48.1000, 11.5000)     // Alpenvorland
        };

        // =====================================================================
        //  Das Prüfpaket
        // =====================================================================

        /// <summary>Die zwölf Ziffern eines Dateinamens: <c>lat·1e4</c> und <c>lon·1e4</c>.</summary>
        private static string Ziffern(double lat, double lon)
            => ((int)Math.Round(lat * 10000)).ToString("D6", CultureInfo.InvariantCulture) +
               ((int)Math.Round(lon * 10000)).ToString("D6", CultureInfo.InvariantCulture);

        /// <summary>Eine vollständige, synthetische TRY-Reihe (34 Kopfzeilen, 8 760 Zeilen).</summary>
        private static string Reihe(int kennung)
        {
            var sb = new StringBuilder(700 * 1024);
            for (int i = 1; i <= 34; i++)
                sb.Append("Kopfzeile ").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("*** \r\n");

            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int index = 0;
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++, index++)
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "4321000 5678000 {0} {1} {2} {3} 1013 180 2.0 4 3.0 80 0 0 260 300 1\r\n",
                            m, tag, h, kennung * 100000 + index);

            return sb.ToString();
        }

        /// <summary>
        /// Baut das Prüfpaket: drei Regionen × zwei Jahre × drei Szenarien.
        /// </summary>
        /// <param name="fuellung">Byte einer UNKOMPRIMIERTEN Füllung am Anfang des
        /// Archivs. Sie bläht das Paket auf, ohne an der Struktur etwas zu ändern — so
        /// lässt sich zeigen, dass die übertragene Menge NICHT mit der Paketgröße
        /// wächst (das echte <c>data.zip</c> ist 892 MB groß).</param>
        private static byte[] Paket(int fuellung = 0)
        {
            var speicher = new MemoryStream();
            using (var archiv = new ZipArchive(speicher, ZipArchiveMode.Create, leaveOpen: true))
            {
                if (fuellung > 0)
                {
                    ZipArchiveEntry gross = archiv.CreateEntry(
                        "0_fuellung/gross.bin", CompressionLevel.NoCompression);
                    using (Stream s = gross.Open())
                    {
                        var block = new byte[64 * 1024];
                        for (int i = 0; i < block.Length; i++) block[i] = (byte)(i % 251);
                        for (int geschrieben = 0; geschrieben < fuellung; geschrieben += block.Length)
                            s.Write(block, 0, Math.Min(block.Length, fuellung - geschrieben));
                    }
                }

                foreach (var r in REGIONEN)
                {
                    foreach (int jahr in new[] { 2015, 2045 })
                    {
                        foreach (string sz in new[] { "Jahr", "Somm", "Wint" })
                        {
                            string name = string.Format(CultureInfo.InvariantCulture,
                                "1_raw-data/{0}/TRY{1}_{2}_{3}.dat",
                                r.Nummer, jahr, Ziffern(r.Lat, r.Lon), sz);

                            ZipArchiveEntry e = archiv.CreateEntry(name, CompressionLevel.Optimal);
                            using (Stream s = e.Open())
                            using (var w = new StreamWriter(s))
                            {
                                w.Write(Reihe(r.Nummer * 10 +
                                              (sz == "Jahr" ? 1 : sz == "Somm" ? 2 : 3)));
                            }
                        }
                    }
                }

                // Zwei Eintraege, die NICHT zum Muster passen - sie muessen uebergangen werden.
                archiv.CreateEntry("LIESMICH.txt");
                archiv.CreateEntry("2_data/uebersicht.csv");
            }
            return speicher.ToArray();
        }

        /// <summary>
        /// Der BEREICHS-Prüfstand über ein Bytefeld: zählt Abrufe und übertragene Bytes.
        /// </summary>
        private sealed class Bereichszaehler
        {
            private readonly byte[] _daten;

            public Bereichszaehler(byte[] daten) { _daten = daten; }

            public int Abrufe { get; private set; }
            public long Bytes { get; private set; }

            /// <summary>Beantwortet den Bereich wie ein Server mit <c>Content-Range</c>.</summary>
            public Task<(byte[] Daten, long Gesamtlaenge)> Holen(
                string adresse, long von, long laenge, CancellationToken abbruch)
            {
                if (von < 0) von = 0;
                if (von > _daten.LongLength) von = _daten.LongLength;

                long rest = _daten.LongLength - von;
                long n = laenge < 0 ? rest : Math.Min(laenge, rest);

                var stueck = new byte[n];
                Array.Copy(_daten, von, stueck, 0, n);

                Abrufe++;
                Bytes += n;
                return Task.FromResult((stueck, _daten.LongLength));
            }
        }

        // =====================================================================
        //  1 — Regionswahl
        // =====================================================================

        /// <summary>
        /// <b>Gewählt wird die NÄCHSTE Region</b> — die Regionsliste steht im
        /// Zentralverzeichnis des Pakets, nicht im Quelltext.
        /// </summary>
        [Theory]
        [InlineData(8.6, 53.5, 1)]        // Nordseekueste
        [InlineData(7.1, 50.8, 7)]        // Rheinland
        [InlineData(11.6, 48.2, 13)]      // Alpenvorland
        public void Die_naechste_Region_wird_gewaehlt(double lon, double lat, int nummer)
        {
            using (var ms = new MemoryStream(Paket()))
            {
                TryPaketErgebnis erg = TryPaketLeser.AusStrom(
                    ms, lon, lat, 2015, TrySzenario.MittleresJahr);

                Assert.Equal(nummer, erg.Region.Nummer);
                Assert.Equal(8760, erg.Stunden.Count);
                Assert.Contains("TRY2015_", erg.Eintrag);
                Assert.Contains("_Jahr.dat", erg.Eintrag);
                Assert.True(erg.Region.EntfernungKm < TryPaketLeser.MAX_ENTFERNUNG_KM);
            }
        }

        /// <summary>Szenario und Bezugsjahr steuern, WELCHE Datei entpackt wird.</summary>
        [Theory]
        [InlineData(2015, TrySzenario.MittleresJahr, "TRY2015_", "_Jahr.dat")]
        [InlineData(2015, TrySzenario.Sommerwarm, "TRY2015_", "_Somm.dat")]
        [InlineData(2045, TrySzenario.Winterkalt, "TRY2045_", "_Wint.dat")]
        public void Szenario_und_Jahr_waehlen_den_Eintrag(int jahr, TrySzenario sz,
                                                          string praefix, string endung)
        {
            using (var ms = new MemoryStream(Paket()))
            {
                TryPaketErgebnis erg = TryPaketLeser.AusStrom(ms, 8.6, 53.5, jahr, sz);

                Assert.Contains(praefix, erg.Eintrag);
                Assert.EndsWith(endung, erg.Eintrag);
                Assert.Equal(8760, erg.Stunden.Count);
            }
        }

        /// <summary>
        /// <b>Jenseits von 300 km bricht der Lauf BENANNT ab</b> — das Paket deckt nur
        /// Deutschland ab, und eine Region „irgendwo in der Nähe" wäre eine falsche
        /// Auskunft.
        /// </summary>
        [Fact]
        public void Zu_weit_entfernt_bricht_mit_Grund_ab()
        {
            using (var ms = new MemoryStream(Paket()))
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => TryPaketLeser.AusStrom(ms, -3.7, 40.4, 2015, TrySzenario.MittleresJahr));

                Assert.Contains("300", ex.Message);
                Assert.Contains("km", ex.Message);
            }
        }

        /// <summary>Die Haversine-Entfernung, gegen bekannte Abstände gehalten.</summary>
        [Fact]
        public void Die_Entfernung_wird_als_Grosskreis_gerechnet()
        {
            Assert.Equal(0.0, TryPaketLeser.EntfernungKm(50, 8, 50, 8), 9);

            // Ein Breitengrad sind rund 111 km.
            Assert.InRange(TryPaketLeser.EntfernungKm(50, 8, 51, 8), 110.0, 112.0);

            // Hamburg - Muenchen, rund 610 km Luftlinie.
            Assert.InRange(TryPaketLeser.EntfernungKm(53.55, 9.99, 48.14, 11.58), 590.0, 630.0);
        }

        // =====================================================================
        //  2 — Der Bereichsabruf
        // =====================================================================

        /// <summary>
        /// <b>Der Prüfstand des Bereichsabrufs.</b> Gelesen werden nur das
        /// Zentralverzeichnis am Dateiende und EIN Eintrag. Die übertragene Menge ist
        /// durch eine KONSTANTE Zahl Leseblöcke begrenzt — sie wächst NICHT mit dem
        /// Paket. Das Prüfpaket trägt dafür eine unkomprimierte Füllung von 8 MB; für
        /// das echte <c>data.zip</c> (892 MB) gilt dieselbe Schranke.
        /// </summary>
        [Fact]
        public async Task Der_Bereichsabruf_holt_nur_einen_Bruchteil_des_Pakets()
        {
            const int FUELLUNG = 8 * 1024 * 1024;
            const long SCHRANKE = 8L * BereichStream.BLOCK;      // 2 MiB

            byte[] paket = Paket(FUELLUNG);
            Assert.True(paket.LongLength > FUELLUNG, "Die Fuellung muss im Paket stehen.");

            var zaehler = new Bereichszaehler(paket);

            TryPaketErgebnis erg = await TryPaketLeser.LesenAusNetzAsync(
                new INetzbereich(zaehler.Holen), "https://beispiel.invalid/data.zip",
                8.6, 53.5, 2015, TrySzenario.MittleresJahr);

            Assert.Equal(1, erg.Region.Nummer);
            Assert.Equal(8760, erg.Stunden.Count);

            // Der Nachweis: eine feste, kleine Menge - und ein Bruchteil des Pakets.
            Assert.True(erg.UebertrageneBytes > 0);
            Assert.True(erg.UebertrageneBytes <= SCHRANKE,
                "Uebertragen " + erg.UebertrageneBytes.ToString(CultureInfo.InvariantCulture) +
                " Byte, erlaubt sind " + SCHRANKE.ToString(CultureInfo.InvariantCulture) + ".");
            Assert.True(erg.UebertrageneBytes < paket.LongLength / 4,
                "Uebertragen " + erg.UebertrageneBytes.ToString(CultureInfo.InvariantCulture) +
                " von " + paket.LongLength.ToString(CultureInfo.InvariantCulture) + " Byte.");
            Assert.True(erg.Abrufe <= 8,
                "Abrufe: " + erg.Abrufe.ToString(CultureInfo.InvariantCulture));

            Assert.Equal(zaehler.Abrufe, (int)erg.Abrufe);
            Assert.Equal(zaehler.Bytes, erg.UebertrageneBytes);
        }

        /// <summary>
        /// <b>Eine Adresse ohne Bereichsabruf wird BENANNT abgelehnt</b> — nie wird
        /// still das ganze Paket gezogen. Der Prüfstand antwortet wie ein Server, der
        /// <c>Range</c> übergeht: ohne <c>Content-Range</c> (Gesamtlänge 0).
        /// </summary>
        [Fact]
        public async Task Ohne_Teilabruf_gibt_es_einen_benannten_Fehler()
        {
            byte[] paket = Paket();

            Task<(byte[], long)> OhneBereich(string a, long von, long laenge, CancellationToken t)
                => Task.FromResult((paket, 0L));      // keine Gesamtlaenge = kein Content-Range

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => TryPaketLeser.LesenAusNetzAsync(
                    new INetzbereich(OhneBereich), "https://beispiel.invalid/data.zip",
                    8.6, 53.5, 2015, TrySzenario.MittleresJahr));

            Assert.Contains("data.zip", ex.Message);
            Assert.Contains("Teilabrufe", ex.Message);
        }

        /// <summary>
        /// Ein Server, der die Bereichsangabe übergeht und die GANZE Datei schickt, wird
        /// ebenfalls erkannt: Es kommen mehr Bytes zurück, als angefordert waren.
        /// </summary>
        [Fact]
        public async Task Ein_zu_grosser_Bereich_wird_erkannt()
        {
            byte[] paket = Paket();
            Assert.True(paket.LongLength > BereichStream.BLOCK,
                        "Das Pruefpaket muss groesser als ein Leseblock sein.");

            Task<(byte[], long)> Alles(string a, long von, long laenge, CancellationToken t)
                => Task.FromResult((paket, (long)paket.LongLength));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => TryPaketLeser.LesenAusNetzAsync(
                    new INetzbereich(Alles), "https://beispiel.invalid/data.zip",
                    8.6, 53.5, 2015, TrySzenario.MittleresJahr));
        }

        // =====================================================================
        //  3 — Die lokale data.zip
        // =====================================================================

        /// <summary>
        /// Die zweite Quelle: eine LOKALE <c>data.zip</c> — derselbe Weg, kein Netz,
        /// keine Bereichsabrufe.
        /// </summary>
        [Fact]
        public void Eine_lokale_Paketdatei_liefert_dasselbe()
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                "epos_try_pruef_" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                File.WriteAllBytes(pfad, Paket());

                TryPaketErgebnis erg = TryPaketLeser.LesenAusDatei(
                    pfad, 11.6, 48.2, 2015, TrySzenario.MittleresJahr);

                Assert.Equal(13, erg.Region.Nummer);
                Assert.Equal(8760, erg.Stunden.Count);
                Assert.Equal(0, erg.Abrufe);
                Assert.Equal(0, erg.UebertrageneBytes);
                Assert.Equal(48.1, erg.Region.Latitude, 4);
                Assert.Equal(11.5, erg.Region.Longitude, 4);
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>Eine fehlende Paketdatei meldet sich als solche.</summary>
        [Fact]
        public void Eine_fehlende_Paketdatei_wird_gemeldet()
        {
            Assert.Throws<FileNotFoundException>(() => TryPaketLeser.LesenAusDatei(
                Path.Combine(Path.GetTempPath(), "gibt_es_nicht_" + Guid.NewGuid().ToString("N") + ".zip"),
                9.0, 48.0, 2015, TrySzenario.MittleresJahr));
        }

        /// <summary>Ein Bezugsjahr, das das Paket nicht führt, wird benannt abgelehnt.</summary>
        [Fact]
        public void Ein_unbekanntes_Bezugsjahr_wird_gemeldet()
        {
            using (var ms = new MemoryStream(Paket()))
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => TryPaketLeser.AusStrom(ms, 8.6, 53.5, 2099, TrySzenario.MittleresJahr));

                Assert.Contains("2099", ex.Message);
            }
        }

        /// <summary>Die Kürzel der drei Szenarien, eingefroren.</summary>
        [Fact]
        public void Die_Szenariokuerzel_stehen_fest()
        {
            Assert.Equal("Jahr", TryPaketLeser.Kuerzel(TrySzenario.MittleresJahr));
            Assert.Equal("Somm", TryPaketLeser.Kuerzel(TrySzenario.Sommerwarm));
            Assert.Equal("Wint", TryPaketLeser.Kuerzel(TrySzenario.Winterkalt));
        }
    }
}
