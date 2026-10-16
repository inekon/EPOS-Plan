using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die VORSCHAU des Klimaimports</b> (Auftrag KD-2, Konzept Klimadatenimport §§ 4
    /// und 9, Entscheide KD-Q1, KD-Q3, KD-Q9): „Datensatz suchen" liest den Satz für alle
    /// drei Quellen, sagt, ob die gewählte Ausführung im Paket steht, nennt Standort,
    /// Bezugspunkt, Höhen und Kennzahlen, und der Import übernimmt den gelesenen Satz
    /// ohne zweiten Abruf und ohne zweite Geokodierung.
    ///
    /// <para><b>Kein Netz.</b> Das Regionalpaket ist eine kleine Probe-<c>data.zip</c> im
    /// Speicher mit den vier süddeutschen Repräsentanzstationen (Koordinaten wie in
    /// <c>LambertKoordinatenTests</c>); alle Zahlenwerte der Reihen sind erfunden. PVGIS
    /// und die TRY-Datei kommen aus den eingefrorenen Proben unter
    /// <c>Referenzlaeufe/Importproben/</c>. Jede Naht zählt ihre Aufrufe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaImportAblaufTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Die Probe
        // =====================================================================

        /// <summary>Die Stationen der Probe: Nummer, Breite, Länge, Höhe [m].</summary>
        private static readonly (int Nummer, double Lat, double Lon, int Hoehe)[] STATIONEN =
        {
            (12, 49.4902, 8.4637, 96),     // Mannheim
            (13, 48.2432, 12.5286, 406),   // Muehldorf/Inn
            (14, 48.6536, 9.8666, 734),    // Stoetten
            (15, 47.4945, 11.1046, 719)    // Garmisch-Partenkirchen
        };

        /// <summary>Stuttgart-Vaihingen (Konzept § 1.2).</summary>
        private const double VAIHINGEN_LAT = 48.73, VAIHINGEN_LON = 9.10;

        private static string Ziffern(double lat, double lon)
            => ((int)Math.Round(lat * 10000)).ToString("D6", CultureInfo.InvariantCulture) +
               ((int)Math.Round(lon * 10000)).ToString("D6", CultureInfo.InvariantCulture);

        /// <summary>
        /// Eine synthetische TRY-Reihe mit Kopf „Höhenlage": Temperatur 5 °C im ersten
        /// Halbjahr, 20 °C im zweiten, dazu 8 Strahlungsstunden je Tag mit B 100 und D 50.
        /// </summary>
        private static string Reihe(int hoehe, double zuschlag, string kopfzusatz = "")
        {
            var sb = new StringBuilder(700 * 1024);
            sb.Append("Probe Testreferenzjahr - erfundene Werte\r\n");
            sb.Append(kopfzusatz);
            sb.Append("Art des TRY: Jahr\r\n");
            sb.Append("Bezugszeitraum: 1995-2012\r\n");
            sb.Append("Hoehenlage: ").Append(hoehe.ToString(CultureInfo.InvariantCulture))
              .Append(" Meter ueber NN\r\n");
            sb.Append("*** \r\n");

            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++)
                    {
                        double t = (m <= 6 ? 5.0 : 20.0) + zuschlag;
                        int b = h >= 10 && h <= 17 ? 100 : 0;
                        int d = b > 0 ? 50 : 0;
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "4321000 5678000 {0} {1} {2} {3:F1} 1013 180 2.0 4 3.0 80 {4} {5} 260 300 1\r\n",
                            m, tag, h, t, b, d);
                    }
            return sb.ToString();
        }

        /// <summary>
        /// Das Probepaket: vier Stationen, 2015 mit allen drei Szenarien, 2045 NUR das
        /// mittlere Jahr — so fehlt „2045 sommerwarm" (Gegenprobe).
        /// </summary>
        private static byte[] Paket()
        {
            var speicher = new MemoryStream();
            using (var archiv = new ZipArchive(speicher, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var st in STATIONEN)
                {
                    foreach ((int jahr, string sz) in new[] { (2015, "Jahr"), (2015, "Somm"), (2015, "Wint"), (2045, "Jahr") })
                    {
                        string name = string.Format(CultureInfo.InvariantCulture,
                            "1_raw-data/{0}/TRY{1}_{2}_{3}.dat", st.Nummer, jahr, Ziffern(st.Lat, st.Lon), sz);
                        ZipArchiveEntry e = archiv.CreateEntry(name, CompressionLevel.Fastest);
                        using (var w = new StreamWriter(e.Open()))
                            w.Write(Reihe(st.Hoehe, sz == "Somm" ? 2.0 : sz == "Wint" ? -2.0 : 0.0));
                    }
                }
            }
            return speicher.ToArray();
        }

        private static readonly Lazy<byte[]> PAKET = new Lazy<byte[]>(Paket);

        /// <summary>Die Probe als lokale Datei; der Aufrufer löscht sie.</summary>
        private static string PaketDatei()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "kd2_probe_" + Guid.NewGuid().ToString("N") + ".zip");
            File.WriteAllBytes(pfad, PAKET.Value);
            return pfad;
        }

        /// <summary>Der Bereichsabruf über die Probe — zählt jeden Abruf.</summary>
        private sealed class Bereichszaehler
        {
            private readonly byte[] _daten;
            public Bereichszaehler(byte[] daten) { _daten = daten; }
            public int Abrufe { get; private set; }

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
                return Task.FromResult((stueck, _daten.LongLength));
            }
        }

        /// <summary>Die Ortsauflösung als zählende Naht.</summary>
        private sealed class Ortszaehler
        {
            private readonly double _lat, _lon;
            private readonly string _name;
            public Ortszaehler(double lat, double lon, string name) { _lat = lat; _lon = lon; _name = name; }
            public int Aufrufe { get; private set; }

            public Task<(bool Success, double Lat, double Lon, string DisplayName)> Aufloesen(string ort)
            {
                Aufrufe++;
                return Task.FromResult((true, _lat, _lon, _name));
            }
        }

        private static string Probe(string datei)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", datei);
                if (File.Exists(kandidat)) return kandidat;
            }
            throw new FileNotFoundException(datei);
        }

        private static KlimaImportAuftrag Regionalauftrag(string paket, int jahr = 2015,
                                                          TrySzenario sz = TrySzenario.MittleresJahr)
            => new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusKoordinaten,
                Bezeichnung = "KD2 Probe",
                Longitude = VAIHINGEN_LON,
                Latitude = VAIHINGEN_LAT,
                Quelle = KlimaQuelle.TryRegional,
                TryPaketPfad = paket,
                TryJahr = jahr,
                Szenario = sz,
                StandortHoeheM = 450
            };

        /// <summary>Die Rechenspalten einer Stammregion als ein Text, in Zeilenfolge.</summary>
        private static string Stundentext(int id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT group_concat(Zeile, '|') FROM (SELECT Temperatur || ';' || Globalstrahlung || ';' || " +
                "Direktstrahlung || ';' || Diffusstrahlung || ';' || Sol_Sued || ';' || Sol_Ost || ';' || " +
                "Sonnenwinkel AS Zeile FROM Tab_Solar_STAMM WHERE ID_Klimaregion = ? ORDER BY ID)",
                new DbParam("@id", id));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        // =====================================================================
        //  1 — Regionalweg: Nachrechnung Stuttgart-Vaihingen (Konzept § 1.2)
        // =====================================================================

        /// <summary>
        /// <b>Stuttgart-Vaihingen trifft Stötten in 56 bis 57 km</b> — die nächste
        /// Repräsentanzstation, 734 m gegen 450 m: Entfernungs- UND Höhenhinweis (KD-Q1).
        /// </summary>
        [Fact]
        public async Task Vaihingen_trifft_Stoetten_mit_Hoehenhinweis()
        {
            string paket = PaketDatei();
            try
            {
                KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(Regionalauftrag(paket));

                Assert.True(v.Erfolgreich, v.Meldung);
                Assert.True(v.AusfuehrungGefunden);
                Assert.True(v.Importierbar);
                Assert.Equal(14, v.Region.Nummer);
                Assert.Equal(KlimaBezugspunktArt.Repraesentanzstation, v.Bezugspunkt.Art);
                Assert.Equal("14 Stötten", v.Bezugspunkt.Name);
                Assert.InRange(v.Bezugspunkt.EntfernungKm.Value, 56.0, 57.0);
                Assert.Equal(734.0, v.Bezugspunkt.HoeheM);
                Assert.Equal(450.0, v.Standort.HoeheM);
                Assert.Equal(284.0, v.HoehenunterschiedM);
                Assert.True(v.Repraesentanzhinweis);
                Assert.True(v.Ortshinweis);
                Assert.Equal(2, v.Hinweise.Count);
                Assert.Contains("Repräsentanzstation", v.Hinweise[0]);
                Assert.Contains("TRY-Datei des DWD", v.Hinweise[1]);
                Assert.Contains("Stötten", v.Meldung);
                Assert.Contains("Repräsentanzstation", v.Meldung);
                Assert.Equal(2015, v.Bezugsjahr);
            }
            finally { File.Delete(paket); }
        }

        /// <summary>
        /// <b>Die Kennzahlen kommen aus dem gelesenen Satz</b>: 8 760 Stunden, Mittel
        /// zwischen 5 und 20 °C, Minimum 5, Maximum 20, Globalstrahlung 365 × 8 × 150 W/m²
        /// = 438 kWh/m²; die Gradtage zählen das kalte Halbjahr.
        /// </summary>
        [Fact]
        public async Task Die_Kennzahlen_kommen_aus_dem_gelesenen_Satz()
        {
            string paket = PaketDatei();
            try
            {
                KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(Regionalauftrag(paket));
                KlimaKennzahlen k = v.Kennzahlen;

                Assert.NotNull(k);
                Assert.Equal(8760, k.Stunden);
                Assert.Equal(365, k.Tage);
                Assert.Equal(5.0, k.TemperaturMin, 6);
                Assert.Equal(20.0, k.TemperaturMax, 6);
                Assert.InRange(k.TemperaturMittel, 12.0, 13.0);
                Assert.Equal(438.0, k.GlobalstrahlungKwhM2, 6);
                Assert.InRange(k.Heiztage, 180, 182);
                Assert.InRange(k.GradtagzahlKdA, 180 * 15.0 - 30, 182 * 15.0);
                Assert.Equal(5.0, k.MonatsmittelTemperatur[2], 6);
                Assert.Equal(20.0, k.MonatsmittelTemperatur[8], 6);

                // Der Parameterbestand der TRY: alles gefuehrt, Global und DNI gerechnet.
                Assert.Equal(Enum.GetValues(typeof(KlimaParameter)).Length, v.Parameterbestand.Count);
                Assert.Equal(KlimaParameterStand.Gerechnet,
                    v.Parameterbestand.Single(p => p.Parameter == KlimaParameter.DirektstrahlungNormal).Stand);
                Assert.Equal(KlimaParameterStand.Gefuehrt,
                    v.Parameterbestand.Single(p => p.Parameter == KlimaParameter.Windrichtung).Stand);
            }
            finally { File.Delete(paket); }
        }

        /// <summary>
        /// <b>Gegenprobe: Das Szenario fehlt</b> — „2045 sommerwarm" steht nicht im Paket.
        /// Die Vorschau gelingt (Region gefunden), aber <c>AusfuehrungGefunden</c> = nein
        /// mit benanntem Grund und den vorhandenen Ausführungen; nichts ist importierbar.
        /// </summary>
        [Fact]
        public async Task Fehlt_das_Szenario_sagt_die_Vorschau_nein_mit_Grund()
        {
            string paket = PaketDatei();
            try
            {
                KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(
                    Regionalauftrag(paket, 2045, TrySzenario.Sommerwarm));

                Assert.True(v.Erfolgreich, v.Meldung);
                Assert.False(v.AusfuehrungGefunden);
                Assert.False(v.Importierbar);
                Assert.Contains("Somm", v.Grund);
                Assert.Contains("14", v.Grund);
                Assert.Contains(v.Grund, v.Meldung);
                Assert.Null(v.Kennzahlen);
                Assert.Equal(4, v.Ausfuehrungen.Count);
                Assert.Contains(v.Ausfuehrungen, a => a.Jahr == 2045 && a.Szenario == TrySzenario.MittleresJahr);
                Assert.DoesNotContain(v.Ausfuehrungen, a => a.Jahr == 2045 && a.Szenario == TrySzenario.Sommerwarm);
            }
            finally { File.Delete(paket); }
        }

        // =====================================================================
        //  2 — Die Schwellen (KD-Q9)
        // =====================================================================

        [Theory]
        [InlineData(49.999, null, false)]
        [InlineData(50.0, null, true)]
        [InlineData(null, 199.9, false)]
        [InlineData(null, 200.0, true)]
        [InlineData(null, -200.0, true)]
        [InlineData(null, -199.9, false)]
        [InlineData(10.0, 150.0, false)]
        [InlineData(null, null, false)]
        public void Die_Schwellen_gelten_einschliesslich(double? km, double? dh, bool hinweis)
        {
            Assert.Equal(50.0, KlimaVorschauFestwerte.HINWEIS_ENTFERNUNG_KM);
            Assert.Equal(200.0, KlimaVorschauFestwerte.HINWEIS_HOEHE_M);
            Assert.Equal(hinweis, KlimaVorschauFestwerte.NichtOrtstypisch(km, dh));
        }

        [Theory]
        [InlineData("734 Meter ueber NN", 734.0)]
        [InlineData("Höhenlage 250,5 m", 250.5)]
        [InlineData("  12.0 m", 12.0)]
        [InlineData("unbekannt", null)]
        [InlineData("", null)]
        public void Die_Hoehe_wird_aus_dem_Kopftext_gelesen(string text, double? hoehe)
        {
            Assert.Equal(hoehe, KlimaVorschauFestwerte.HoeheAusText(text));
        }

        /// <summary>
        /// <b>Gradtagzahl G20/15</b> an einer erdachten Reihe: zwei Tage zu 10 °C und einer
        /// zu 15 °C — nur die zwei kalten zählen, je 20 − 10 = 10 Kd.
        /// </summary>
        [Fact]
        public void Die_Gradtagzahl_folgt_G20_15()
        {
            var stunden = new List<TmyHourlyData>();
            for (int tag = 1; tag <= 3; tag++)
                for (int h = 0; h < 24; h++)
                    stunden.Add(new TmyHourlyData
                    {
                        TimeString = string.Format(CultureInfo.InvariantCulture, "202001{0:D2}:{1:D2}00", tag, h),
                        Temperature = tag == 3 ? 15.0 : 10.0,
                        GlobalIrradiance = h == 12 ? 500.0 : 0.0
                    });

            KlimaKennzahlen k = KlimaKennzahlen.Rechnen(stunden);

            Assert.Equal(3, k.Tage);
            Assert.Equal(2, k.Heiztage);
            Assert.Equal(20.0, k.GradtagzahlKdA, 9);
            Assert.Equal(1.5, k.GlobalstrahlungKwhM2, 9);
            Assert.Null(KlimaKennzahlen.Rechnen(new List<TmyHourlyData>()));
        }

        // =====================================================================
        //  3 — Die zwei anderen Quellen
        // =====================================================================

        /// <summary>
        /// <b>PVGIS</b>: Bezugspunkt ist der gerechnete Punkt der Antwort samt Höhe
        /// (245 m); er ist zugleich die Höhe des Standorts — kein Hinweis.
        /// </summary>
        [Fact]
        public async Task Pvgis_nennt_Punkt_und_Hoehe_der_Antwort()
        {
            string json = File.ReadAllText(Probe("pvgis_tmy_stuttgart_72h.json"));
            int abrufe = 0;
            IPvgisAbruf pvgis = (lon, lat, az) => { abrufe++; return Task.FromResult(PVGIS_EPW_Downloader.AusJsonAbruf(json)); };

            KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(
                new KlimaImportAuftrag { Art = KlimaImportArt.AusKoordinaten, Longitude = 9.1829, Latitude = 48.7758 },
                pvgis: pvgis);

            Assert.True(v.Erfolgreich, v.Meldung);
            Assert.Equal(1, abrufe);
            Assert.True(v.AusfuehrungGefunden);
            Assert.Equal(KlimaBezugspunktArt.PvgisPunkt, v.Bezugspunkt.Art);
            Assert.Equal(245.0, v.Bezugspunkt.HoeheM);
            Assert.Equal(245.0, v.Standort.HoeheM);
            Assert.Equal(0.0, v.HoehenunterschiedM);
            Assert.InRange(v.Bezugspunkt.EntfernungKm.Value, 0.0, 0.01);
            Assert.False(v.Ortshinweis);
            Assert.False(v.Repraesentanzhinweis);
            Assert.Empty(v.Hinweise);
            Assert.Equal(72, v.Kennzahlen.Stunden);
            Assert.Equal(KlimaParameterStand.Fehlt,
                v.Parameterbestand.Single(p => p.Parameter == KlimaParameter.Bedeckungsgrad).Stand);
            Assert.Equal(KlimaParameterStand.Gefuehrt,
                v.Parameterbestand.Single(p => p.Parameter == KlimaParameter.Luftfeuchte).Stand);
        }

        /// <summary>
        /// <b>TRY-Datei</b> ohne Koordinaten: Standort und Bezugspunkt kommen aus dem
        /// Kopf (Rechts- und Hochwert der Importprobe), die Höhe aus „Hoehenlage: 250 Meter".
        /// </summary>
        [Fact]
        public async Task Die_TRY_Datei_nennt_Kopfstandort_und_Hoehe()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "kd2_probe_" + Guid.NewGuid().ToString("N") + ".dat");
            File.WriteAllText(pfad, Reihe(250, 0.0,
                "Rechtswert: 3929310 Meter\r\nHochwert: 2478193 Meter\r\n"));
            KlimaVorschauErgebnis v;
            try
            {
                v = await KlimaImportAblauf.RegionErmittelnAsync(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    });
            }
            finally { File.Delete(pfad); }

            Assert.True(v.Erfolgreich, v.Meldung);
            Assert.True(v.AusfuehrungGefunden);
            Assert.Equal(KlimaBezugspunktArt.Dateistandort, v.Bezugspunkt.Art);
            Assert.Equal(250.0, v.Bezugspunkt.HoeheM);
            Assert.Equal(250.0, v.Standort.HoeheM);
            Assert.Equal(v.Bezugspunkt.Latitude, v.Standort.Latitude);
            Assert.Equal(0.0, v.Bezugspunkt.EntfernungKm.Value, 6);
            Assert.False(v.Ortshinweis);
            Assert.Equal(2015, v.Bezugsjahr);
            Assert.Equal(8760, v.Kennzahlen.Stunden);
        }

        // =====================================================================
        //  4 — Der Import übernimmt den Vorschausatz (KD-Q3)
        // =====================================================================

        /// <summary>
        /// <b>Kein zweiter Abruf, keine zweite Geokodierung</b>: Nach der Vorschau liest
        /// der Import weder Paket noch Ort — die zählenden Nähte bleiben stehen — und
        /// schreibt dieselben Stunden wie ein Import ohne Vorschau.
        /// </summary>
        [Fact]
        public async Task Der_Import_nutzt_den_Vorschausatz_ohne_zweiten_Abruf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var netz = new Bereichszaehler(PAKET.Value);
            var orte = new Ortszaehler(VAIHINGEN_LAT, VAIHINGEN_LON, "Vaihingen, Stuttgart");
            var auftrag = new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusOrtsname,
                Ortsname = "KD2 Vaihingen Probe",
                Quelle = KlimaQuelle.TryRegional
            };

            KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(
                auftrag, orte.Aufloesen, default, netz.Holen);
            Assert.True(v.Importierbar, v.Meldung);
            Assert.Equal("Vaihingen, Stuttgart", v.Standort.Anzeigename);
            Assert.Equal(VAIHINGEN_LAT, v.Standort.Latitude);
            Assert.True(v.Standort.AusGeokodierung);

            int abrufe = netz.Abrufe, geokodiert = orte.Aufrufe;
            Assert.True(abrufe > 0);
            Assert.Equal(1, geokodiert);

            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                auftrag, null, orte.Aufloesen, null, default, netz.Holen, v);

            Assert.True(erg.Erfolgreich, erg.Meldung);
            Assert.Equal(8760, erg.Stundenwerte);
            Assert.Equal(abrufe, netz.Abrufe);
            Assert.Equal(geokodiert, orte.Aufrufe);

            // Gegenprobe: derselbe Import OHNE Vorschau liest selbst - und schreibt dasselbe.
            KlimaImportErgebnis ohne = await KlimaImportAblauf.Laufen(
                new KlimaImportAuftrag
                {
                    Art = KlimaImportArt.AusOrtsname,
                    Ortsname = "KD2 Vaihingen ohne Vorschau",
                    Quelle = KlimaQuelle.TryRegional
                },
                null, orte.Aufloesen, null, default, netz.Holen);

            Assert.True(ohne.Erfolgreich, ohne.Meldung);
            Assert.True(netz.Abrufe > abrufe);
            Assert.Equal(geokodiert + 1, orte.Aufrufe);
            Assert.Equal(Stundentext(ohne.Id), Stundentext(erg.Id));

            // Der Satz der Vorschau bleibt, wie er gelesen wurde (der Import rechnet auf einer Kopie).
            Assert.All(v.Satz.Stunden, s => Assert.Equal(0.0, s.Sol_sued));
        }

        /// <summary>
        /// <b>PVGIS</b>: Der Import übernimmt die Vorschau ohne die TMY-Naht zu rufen.
        /// </summary>
        [Fact]
        public async Task Der_PVGIS_Import_ruft_die_Naht_nicht_ein_zweites_Mal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string json = File.ReadAllText(Probe("pvgis_tmy_stuttgart_72h.json"));
            int abrufe = 0, tmyAbrufe = 0;
            IPvgisAbruf pvgis = (lon, lat, az) => { abrufe++; return Task.FromResult(PVGIS_EPW_Downloader.AusJsonAbruf(json)); };
            ITmyQuelle tmy = (lon, lat, az) => { tmyAbrufe++; return Task.FromResult(PVGIS_EPW_Downloader.AusJson(json, out _)); };

            var auftrag = new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusKoordinaten,
                Bezeichnung = "KD2 PVGIS Probe",
                Longitude = 9.1829,
                Latitude = 48.7758
            };

            KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(auftrag, pvgis: pvgis);
            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(auftrag, tmy, vorschau: v);

            Assert.True(erg.Erfolgreich, erg.Meldung);
            Assert.Equal(72, erg.Stundenwerte);
            Assert.Equal(1, abrufe);
            Assert.Equal(0, tmyAbrufe);
        }

        // =====================================================================
        //  5 — Verfall der Vorschau
        // =====================================================================

        /// <summary>
        /// <b>Ändert der Anwender Ort, Quelle, Jahr oder Szenario, verfällt die
        /// Vorschau</b>; die Bezeichnung und die Höhe ändern den gelesenen Satz nicht.
        /// Koordinaten, die genau den geokodierten Standort tragen, passen weiter.
        /// </summary>
        [Fact]
        public async Task Die_Vorschau_verfaellt_bei_geaenderter_Eingabe()
        {
            var netz = new Bereichszaehler(PAKET.Value);
            var orte = new Ortszaehler(VAIHINGEN_LAT, VAIHINGEN_LON, "Vaihingen, Stuttgart");
            Func<KlimaImportAuftrag> basis = () => new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusOrtsname,
                Ortsname = "Stuttgart-Vaihingen",
                Quelle = KlimaQuelle.TryRegional
            };

            KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(
                basis(), orte.Aufloesen, default, netz.Holen);
            Assert.True(v.Importierbar, v.Meldung);

            Assert.True(v.PasstZu(basis()));

            KlimaImportAuftrag a = basis(); a.Bezeichnung = "egal"; a.StandortHoeheM = 300;
            Assert.True(v.PasstZu(a));

            a = basis(); a.Ortsname = "Stuttgart-Mitte";
            Assert.False(v.PasstZu(a));
            a = basis(); a.Quelle = KlimaQuelle.PvgisTmy;
            Assert.False(v.PasstZu(a));
            a = basis(); a.TryJahr = 2045;
            Assert.False(v.PasstZu(a));
            a = basis(); a.Szenario = TrySzenario.Winterkalt;
            Assert.False(v.PasstZu(a));
            a = basis(); a.TryPaketPfad = "anderes.zip";
            Assert.False(v.PasstZu(a));

            // Die Felder tragen den geokodierten Standort: passt.
            a = basis(); a.Art = KlimaImportArt.AusKoordinaten;
            a.Longitude = v.Standort.Longitude; a.Latitude = v.Standort.Latitude;
            Assert.True(v.PasstZu(a));
            a.Latitude += 0.01;
            Assert.False(v.PasstZu(a));

            Assert.False(new KlimaVorschauErgebnis().PasstZu(basis()));
        }

        /// <summary>
        /// <b>Eine verfallene Vorschau wird übergangen</b>: Der Import liest selbst und
        /// nimmt die geänderte Ausführung — hier das winterkalte Jahr.
        /// </summary>
        [Fact]
        public async Task Eine_verfallene_Vorschau_liest_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var netz = new Bereichszaehler(PAKET.Value);
            KlimaImportAuftrag auftrag = Regionalauftrag("");
            auftrag.Bezeichnung = "KD2 Verfall";

            KlimaVorschauErgebnis v = await KlimaImportAblauf.RegionErmittelnAsync(auftrag, null, default, netz.Holen);
            Assert.True(v.Importierbar, v.Meldung);
            int abrufe = netz.Abrufe;

            auftrag.Szenario = TrySzenario.Winterkalt;
            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(auftrag, null, null, null, default, netz.Holen, v);

            Assert.True(erg.Erfolgreich, erg.Meldung);
            Assert.True(netz.Abrufe > abrufe);
            Assert.Contains("winterkalt", erg.Meldung, StringComparison.OrdinalIgnoreCase);
        }
    }
}
