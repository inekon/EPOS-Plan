using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Leser der DWD-TESTREFERENZJAHRE</b> (Auftrag KL1-B) — Format, Zeitbasis
    /// und die Umrechnung auf Direkt-Normal.
    ///
    /// <para><b>Ohne Netz und ohne Datenbank.</b> Die 8 760-Zeilen-Reihen entstehen im
    /// Speicher; die eingefrorene Probe
    /// <c>Referenzlaeufe/Importproben/dwd_try_synthetisch_72h.dat</c> prüft Kopf und
    /// Spalten. <b>Sie enthält keine DWD-Originaldaten</b> — Ortsangaben und Werte sind
    /// erfunden.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8): Die Fehlbilder werden
    /// gegen deutsche Ressourcentexte gehalten.</para>
    /// </summary>
    public class DwdTryLeserTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Die eingefrorene Probe (72 Stunden, 34 Kopfzeilen).</summary>
        private static string ProbeDatei()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben",
                                               "dwd_try_synthetisch_72h.dat");
                if (File.Exists(kandidat)) return kandidat;
            }
            throw new FileNotFoundException("Die synthetische TRY-Probe wurde nicht gefunden.");
        }

        /// <summary>Kopfzeilen samt Trennzeile — <paramref name="anzahl"/> Zeilen davor.</summary>
        private static List<string> Kopf(int anzahl)
        {
            var z = new List<string>(anzahl + 1);
            for (int i = 1; i <= anzahl; i++)
                z.Add("Kopfzeile " + i.ToString(CultureInfo.InvariantCulture));
            z.Add("*** ");
            return z;
        }

        /// <summary>
        /// Eine Datenzeile mit 17 Feldern. <paramref name="b"/>/<paramref name="d"/> sind
        /// Direkt- und Diffusstrahlung horizontal.
        /// </summary>
        private static string Zeile(int monat, int tag, int stunde, double t, double b, double d)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "4321000 5678000 {0} {1} {2} {3} 1013 180 2.0 4 3.0 80 {4} {5} 260 300 1",
                monat, tag, stunde, t, b, d);
        }

        /// <summary>Ein volles Jahr: 8 760 Zeilen, Temperatur = Ortszeitindex.</summary>
        private static List<string> Jahr(int kopfzeilen = 34, double b = 0, double d = 0)
        {
            List<string> z = Kopf(kopfzeilen);
            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            int index = 0;
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++, index++)
                        z.Add(Zeile(m, tag, h, index, b, d));

            Assert.Equal(8760, index);
            return z;
        }

        // =====================================================================
        //  1 — Kopf und Spalten
        // =====================================================================

        /// <summary>
        /// <b>34 Kopfzeilen (2015) und 36 (2045)</b> — die Zahl wird GEZÄHLT, nicht
        /// vorausgesetzt: Der Leser sucht die Trennzeile <c>***</c>.
        /// </summary>
        [Theory]
        [InlineData(34)]
        [InlineData(36)]
        public void Der_Kopf_endet_an_der_Trennzeile(int kopfzeilen)
        {
            List<TmyHourlyData> stunden = DwdTryLeser.Lesen(Jahr(kopfzeilen), out TryKopf kopf);

            Assert.Equal(kopfzeilen, kopf.Kopfzeilen);
            Assert.Equal(8760, stunden.Count);
        }

        /// <summary>
        /// <b>Die eingefrorene Probe</b>: 34 Kopfzeilen, 17 Spalten, 72 Stunden — gelesen
        /// mit der Prüfoption (kein volles Jahr, keine Drehung). Nachts ist
        /// <c>B = D = 0</c>.
        /// </summary>
        [Fact]
        public void Die_synthetische_Probe_liefert_Kopf_und_72_Stunden()
        {
            List<TmyHourlyData> stunden = DwdTryLeser.LesenDatei(
                ProbeDatei(), out TryKopf kopf, vollesJahr: false);

            Assert.Equal(34, kopf.Kopfzeilen);
            Assert.Equal(72, stunden.Count);

            // Erste Zeile ist 01.01. Stunde 1 -> Ortszeitindex 0.
            Assert.Equal("20250101:0000", stunden[0].TimeString);
            Assert.Equal("20250103:2300", stunden[71].TimeString);

            // Nachts steht nichts an Strahlung.
            Assert.Equal(0.0, stunden[0].GlobalIrradiance);
            Assert.Equal(0.0, stunden[0].DirectIrradiance);
            Assert.Equal(0.0, stunden[0].DiffuseIrradiance);

            // Mittags schon; GHI ist die SUMME aus B und D.
            TmyHourlyData mittag = stunden[11];          // 01.01., Stunde 12
            Assert.True(mittag.GlobalIrradiance > 0);
            Assert.Equal(mittag.DirectIrradiance + mittag.DiffuseIrradiance,
                         mittag.GlobalIrradiance, 9);

            // Die Kopfangaben, soweit lesbar.
            Assert.Contains("3929310", kopf.Rechtswert);
            Assert.Contains("2478193", kopf.Hochwert);

            // Und der STANDORT daraus (Auftrag KL-2): der runde Punkt der Probe.
            Assert.True(kopf.StandortBekannt);
            Assert.Equal(9.0, kopf.Laenge!.Value, 4);
            Assert.Equal(49.0, kopf.Breite!.Value, 4);
        }

        // =====================================================================
        //  Der Standort aus dem Dateikopf (Auftrag KL-2)
        // =====================================================================

        /// <summary>
        /// <b><c>KopfLesen</c> liest NUR den Kopf</b> — und kommt ohne die 8 760
        /// Datenzeilen zum Standort. Genau das ruft der Klimadaten-Dialog, wenn der
        /// Anwender eine Datei wählt.
        /// </summary>
        [Fact]
        public void KopfLesen_holt_den_Standort_ohne_die_Datenzeilen()
        {
            Assert.True(DwdTryLeser.KopfLesen(ProbeDatei(), out TryKopf kopf));

            Assert.Equal(34, kopf.Kopfzeilen);
            Assert.True(kopf.StandortBekannt);
            Assert.Equal(9.0, kopf.Laenge!.Value, 4);
            Assert.Equal(49.0, kopf.Breite!.Value, 4);
            Assert.Contains("Meter", kopf.Rechtswert);
        }

        /// <summary>
        /// Ein Kopf OHNE Rechts- und Hochwert lässt den Standort <c>null</c> — keine
        /// stille Null im Golf von Guinea.
        /// </summary>
        [Fact]
        public void Ein_Kopf_ohne_Lambertwerte_laesst_den_Standort_leer()
        {
            TryKopf kopf = DwdTryLeser.KopfLesen(Kopf(34));

            Assert.Equal(34, kopf.Kopfzeilen);
            Assert.False(kopf.StandortBekannt);
            Assert.Null(kopf.Laenge);
            Assert.Null(kopf.Breite);
        }

        /// <summary>
        /// Ein Rechts-/Hochwert AUSSERHALB der Plausibilitätsgrenzen zählt nicht als
        /// Standort — die alten, erfundenen Werte der Probe lagen genau dort.
        /// </summary>
        [Fact]
        public void Ein_unplausibler_Lambertpunkt_zaehlt_nicht_als_Standort()
        {
            var z = new System.Collections.Generic.List<string>
            {
                "Rechtswert: 4321000 Meter",
                "Hochwert: 5678000 Meter",
                "*** "
            };

            TryKopf kopf = DwdTryLeser.KopfLesen(z);

            Assert.Equal("4321000 Meter", kopf.Rechtswert);
            Assert.False(kopf.StandortBekannt);
        }

        /// <summary>
        /// <b>Was der volle Leser findet, findet auch <c>KopfLesen</c></b> — derselbe
        /// Kopf, dieselben Zahlen. Sonst zeigte der Dialog etwas anderes an als der
        /// Import schreibt.
        /// </summary>
        [Fact]
        public void KopfLesen_und_Lesen_liefern_denselben_Standort()
        {
            DwdTryLeser.LesenDatei(ProbeDatei(), out TryKopf voll, vollesJahr: false);
            Assert.True(DwdTryLeser.KopfLesen(ProbeDatei(), out TryKopf nurKopf));

            Assert.Equal(voll.Kopfzeilen, nurKopf.Kopfzeilen);
            Assert.Equal(voll.Rechtswert, nurKopf.Rechtswert);
            Assert.Equal(voll.Laenge, nurKopf.Laenge);
            Assert.Equal(voll.Breite, nurKopf.Breite);
        }

        /// <summary>
        /// Eine Datei, die es nicht gibt, ist kein Absturz: <c>false</c> und ein leerer
        /// Kopf — der Dialog meldet das und lässt die Felder stehen.
        /// </summary>
        [Fact]
        public void KopfLesen_einer_fehlenden_Datei_wirft_nicht()
        {
            Assert.False(DwdTryLeser.KopfLesen(
                Path.Combine(Path.GetTempPath(), "epos_gibt_es_nicht_" +
                             Guid.NewGuid().ToString("N") + ".dat"), out TryKopf kopf));

            Assert.False(kopf.StandortBekannt);
            Assert.Equal("", kopf.Rechtswert);

            Assert.False(DwdTryLeser.KopfLesen("   ", out TryKopf leer));
            Assert.False(leer.StandortBekannt);
        }

        /// <summary>Ohne Trennzeile gibt es keinen Kopf — benannt, nicht still.</summary>
        [Fact]
        public void KopfLesen_ohne_Trennzeile_meldet_das_fehlende_Kopfende()
        {
            var z = new System.Collections.Generic.List<string> { "Rechtswert: 3929310 Meter" };

            FormatException ex = Assert.Throws<FormatException>(() => DwdTryLeser.KopfLesen(z));
            Assert.Contains("***", ex.Message);
        }

        /// <summary>
        /// <c>Meterwert</c> liest die Zahl aus „3929310 Meter“ — und lehnt ab, was keine
        /// ist. Die Kultur spielt keine Rolle: Gelesen wird invariant.
        /// </summary>
        [Theory]
        [InlineData("3929310 Meter", 3929310.0)]
        [InlineData("  2478193  ", 2478193.0)]
        [InlineData("3929310,5 Meter", 3929310.5)]
        public void Meterwert_liest_die_Zahl_vor_der_Einheit(string angabe, double erwartet)
        {
            Assert.True(DwdTryLeser.Meterwert(angabe, out double wert));
            Assert.Equal(erwartet, wert, 6);
        }

        /// <summary>Was keine Zahl ist, wird abgelehnt statt zu 0 gemacht.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unbekannt")]
        public void Meterwert_lehnt_ab_was_keine_Zahl_ist(string angabe)
        {
            Assert.False(DwdTryLeser.Meterwert(angabe, out double wert));
            Assert.Equal(0.0, wert);
        }

        /// <summary>
        /// Die Zuordnung der drei Strahlungsspalten:
        /// <c>Global = B + D</c>, <c>Diffus = D</c>, <c>Direkt = B</c> (noch horizontal).
        /// <c>WindSpeed</c> bleibt 0 — WG ist verworfen.
        /// </summary>
        [Fact]
        public void Die_Strahlungsspalten_werden_benannt_zugeordnet()
        {
            List<string> z = Kopf(34);
            z.Add(Zeile(1, 1, 1, 3.5, 200, 80));

            List<TmyHourlyData> s = DwdTryLeser.Lesen(z, out _, vollesJahr: false);

            Assert.Single(s);
            Assert.Equal(3.5, s[0].Temperature);
            Assert.Equal(280.0, s[0].GlobalIrradiance);
            Assert.Equal(200.0, s[0].DirectIrradiance);
            Assert.Equal(80.0, s[0].DiffuseIrradiance);
            Assert.Equal(0.0, s[0].WindSpeed);
        }

        /// <summary>
        /// <b>Die drei Größen der Gebäudesimulation</b> (Schemaschritt 95, Auftrag KL-3):
        /// <c>A</c> → Gegenstrahlung, <c>RF</c> → Luftfeuchte, <c>N</c> → Bedeckungsgrad.
        /// Die Prüfzeile führt <c>N = 4</c>, <c>RF = 80</c>, <c>A = 260</c> — jede Zahl
        /// steht an einer anderen Stelle, eine Vertauschung fällt deshalb auf.
        /// </summary>
        [Fact]
        public void Gegenstrahlung_Feuchte_und_Bedeckung_werden_uebernommen()
        {
            List<string> z = Kopf(34);
            z.Add(Zeile(1, 1, 1, 3.5, 200, 80));

            List<TmyHourlyData> s = DwdTryLeser.Lesen(z, out _, vollesJahr: false);

            Assert.Single(s);
            Assert.Equal(260.0, s[0].Gegenstrahlung);
            Assert.Equal(80.0, s[0].Humidity);
            Assert.Equal(4.0, s[0].Bedeckungsgrad);
        }

        /// <summary>
        /// <b>Die drei Größen reisen mit der Drehung MEZ → UTC</b> — sie hängen an
        /// derselben Zeile wie Temperatur und Strahlung, also am selben Zeitpunkt. Ein
        /// getrenntes Einsortieren wäre eine zweite Zeitbasis.
        /// </summary>
        [Fact]
        public void Die_drei_Groessen_folgen_der_Drehung_der_Zeile()
        {
            List<TmyHourlyData> utc = DwdTryLeser.Lesen(Jahr(), out _);

            Assert.Equal(8760, utc.Count);
            foreach (TmyHourlyData s in utc)
            {
                Assert.Equal(260.0, s.Gegenstrahlung);
                Assert.Equal(80.0, s.Humidity);
                Assert.Equal(4.0, s.Bedeckungsgrad);
            }
        }

        /// <summary>
        /// <b>Die sechs verworfenen Größen stehen BENANNT im Kopfergebnis</b> — Entscheid
        /// des Anwenders: nur vorhandene Spalten füllen, aber nie still übergehen.
        ///
        /// <para><b>Drei sind mit Schemaschritt 95 herausgefallen</b> (Auftrag KL-3):
        /// <c>N</c>, <c>RF</c> und <c>A</c> werden gespeichert und stehen deshalb NICHT
        /// mehr auf der Liste. <c>WG</c> steht weiter darauf — keine Spalte ohne
        /// Leser.</para>
        /// </summary>
        [Fact]
        public void Die_verworfenen_Groessen_sind_benannt()
        {
            DwdTryLeser.Lesen(Jahr(), out TryKopf kopf);

            Assert.Equal(new[] { "p", "WR", "WG", "x", "E", "IL" },
                         kopf.Verworfen.ToArray());
            Assert.Contains("WR", kopf.VerworfenText);
            Assert.Contains("IL", kopf.VerworfenText);
            Assert.DoesNotContain("RF", kopf.VerworfenText);
        }

        // =====================================================================
        //  2 — Fehlbilder mit Zeilennummer
        // =====================================================================

        /// <summary>Eine Zeile mit 16 statt 17 Feldern nennt ihre ZEILENNUMMER.</summary>
        [Fact]
        public void Eine_falsche_Spaltenzahl_nennt_die_Zeilennummer()
        {
            List<string> z = Kopf(34);
            z.Add(Zeile(1, 1, 1, 0, 0, 0));
            z.Add("4321000 5678000 1 1 2 0 1013 180 2.0 4 3.0 80 0 0 260 300");   // 16 Felder

            var ex = Assert.Throws<FormatException>(
                () => DwdTryLeser.Lesen(z, out _, vollesJahr: false));

            Assert.Contains("37", ex.Message);      // 34 Kopf + Trennzeile + 1 Datenzeile
            Assert.Contains("17", ex.Message);
            Assert.Contains("16", ex.Message);
        }

        /// <summary>
        /// Eine Reihe ohne die vollen 8 760 Zeilen wird abgelehnt — mit Soll, Ist und der
        /// Zeile, in der die Datei endet.
        /// </summary>
        [Fact]
        public void Eine_unvollstaendige_Reihe_wird_abgelehnt()
        {
            List<string> z = Kopf(34);
            z.Add(Zeile(1, 1, 1, 0, 0, 0));

            var ex = Assert.Throws<FormatException>(() => DwdTryLeser.Lesen(z, out _));

            Assert.Contains("8760", ex.Message);
            Assert.Contains("1", ex.Message);
        }

        /// <summary>Stunde 0 oder 25 gibt es nicht — Stunde 24 dagegen schon.</summary>
        [Fact]
        public void Die_Stunde_laeuft_von_1_bis_24()
        {
            List<string> gut = Kopf(34);
            gut.Add(Zeile(12, 31, 24, -1, 0, 0));
            List<TmyHourlyData> s = DwdTryLeser.Lesen(gut, out _, vollesJahr: false);
            Assert.Equal("20251231:2300", s[0].TimeString);     // Ortszeitindex 8759

            List<string> schlecht = Kopf(34);
            schlecht.Add(Zeile(1, 1, 25, 0, 0, 0));
            var ex = Assert.Throws<FormatException>(
                () => DwdTryLeser.Lesen(schlecht, out _, vollesJahr: false));
            Assert.Contains("25", ex.Message);
        }

        /// <summary>Ohne Trennzeile ist die Datei nicht zu deuten.</summary>
        [Fact]
        public void Ohne_Trennzeile_gibt_es_einen_benannten_Fehler()
        {
            var ex = Assert.Throws<FormatException>(
                () => DwdTryLeser.Lesen(new[] { "Kopf 1", "Kopf 2" }, out _, vollesJahr: false));

            Assert.Contains("***", ex.Message);
        }

        // =====================================================================
        //  3 — Die Zeitbasis MEZ -> UTC
        // =====================================================================

        /// <summary>
        /// <b>Die Drehung an der Jahresgrenze.</b> <c>Tab_Solar_STAMM</c> führt
        /// UTC-Reihenfolge, TRY liefert MEZ: <c>utc[u] = mez[(u + 1) % 8760]</c>. Die
        /// erste MEZ-Stunde des 1.1. (Ortszeitindex 0) landet damit auf UTC-Index
        /// <b>8759</b> — der letzten Stunde des Jahres.
        /// </summary>
        [Fact]
        public void Die_erste_MEZ_Stunde_landet_auf_dem_letzten_UTC_Index()
        {
            // Temperature traegt den Ortszeitindex (siehe Jahr()).
            List<TmyHourlyData> utc = DwdTryLeser.Lesen(Jahr(), out _);

            Assert.Equal(8760, utc.Count);
            Assert.Equal(0.0, utc[8759].Temperature);        // mez[0] steht auf utc[8759]
            Assert.Equal(1.0, utc[0].Temperature);           // mez[1] steht auf utc[0]
            Assert.Equal(8759.0, utc[8758].Temperature);     // mez[8759] steht auf utc[8758]

            // Der Zeitstempel folgt dem UTC-Index, nicht dem Inhalt.
            Assert.Equal("20250101:0000", utc[0].TimeString);
            Assert.Equal("20251231:2300", utc[8759].TimeString);
        }

        /// <summary>
        /// <b>Die Gegenprobe gegen <see cref="SolarZeitbasis"/>.</b> Das Haus liest die
        /// Reihe später als UTC und legt die EU-Regel darüber. Im WINTER (Versatz 1 h)
        /// bekommt der Ortszeitindex <c>L</c> genau seine eigene MEZ-Stunde zurück; im
        /// SOMMER (Versatz 2 h) die Stunde <c>L − 1</c> — TRY kennt keine MESZ. Genau das
        /// steht im Kopfkommentar des Lesers.
        /// </summary>
        [Fact]
        public void Die_Rueckrechnung_ueber_SolarZeitbasis_trifft_im_Winter_und_liegt_im_Sommer_um_eine_Stunde()
        {
            List<TmyHourlyData> utc = DwdTryLeser.Lesen(Jahr(), out _);
            const int jahr = DbWerte.SOLAR_REFERENZJAHR_STANDARD;

            // Winter: 15. Januar, 12 Uhr Ortszeit.
            int lWinter = (DwdTryLeser.TagImJahr(1, 15) - 1) * 24 + 12;
            Assert.False(SolarZeitbasis.IstSommerzeit(lWinter, jahr));
            Assert.Equal(lWinter, (int)utc[SolarZeitbasis.UtcIndex(lWinter, jahr)].Temperature);

            // Sommer: 15. Juli, 12 Uhr Ortszeit - eine Stunde zu frueh.
            int lSommer = (DwdTryLeser.TagImJahr(7, 15) - 1) * 24 + 12;
            Assert.True(SolarZeitbasis.IstSommerzeit(lSommer, jahr));
            Assert.Equal(lSommer - 1, (int)utc[SolarZeitbasis.UtcIndex(lSommer, jahr)].Temperature);
        }

        /// <summary>Der Zeitstempel eines Rasterindex — Monatsgrenzen inbegriffen.</summary>
        [Theory]
        [InlineData(0, "20250101:0000")]
        [InlineData(23, "20250101:2300")]
        [InlineData(24, "20250102:0000")]
        [InlineData(744, "20250201:0000")]     // 31 Tage Januar
        [InlineData(8759, "20251231:2300")]
        public void Der_Zeitstempel_folgt_dem_Jahresraster(int index, string erwartet)
        {
            Assert.Equal(erwartet, DwdTryLeser.Zeitstempel(index));
        }

        // =====================================================================
        //  4 — DirektNormal
        // =====================================================================

        /// <summary>
        /// <b>Unter 5° Sonnenhöhe wird nicht geteilt</b>: <c>Direct</c> wird 0, der
        /// Betrag wandert in <c>Diffuse</c> — die GLOBALSTRAHLUNG bleibt deshalb in
        /// jeder Stunde unverändert.
        /// </summary>
        [Fact]
        public void Unter_der_Mindesthoehe_faellt_die_Direktstrahlung_in_den_Diffusanteil()
        {
            // 01.01., Mitternacht: die Sonne steht unter dem Horizont.
            var s = new List<TmyHourlyData>
            {
                new TmyHourlyData
                {
                    TimeString = "20250101:0000",
                    DirectIrradiance = 40, DiffuseIrradiance = 10, GlobalIrradiance = 50
                }
            };

            DwdTryLeser.DirektNormal(s, 9.1829, 48.7758);

            Assert.Equal(0.0, s[0].DirectIrradiance);
            Assert.Equal(50.0, s[0].DiffuseIrradiance);
            Assert.Equal(50.0, s[0].GlobalIrradiance);      // unveraendert
        }

        /// <summary>
        /// Über der Mindesthöhe gilt <c>DNI = B / sin alpha</c> — mit der Klemme auf die
        /// Solarkonstante 1367 W/m².
        /// </summary>
        [Fact]
        public void Ueber_der_Mindesthoehe_wird_auf_Direkt_Normal_gerechnet_und_geklemmt()
        {
            const double lon = 9.1829, lat = 48.7758;

            // 15. Juni, 12 Uhr: die Sonne steht hoch.
            string zeit = DwdTryLeser.Zeitstempel((DwdTryLeser.TagImJahr(6, 15) - 1) * 24 + 12);
            double alpha = SolarCalculator.Sonnenhoehe(lon, lat,
                new DateTime(2025, 6, 15).DayOfYear, 12);
            Assert.True(alpha > DwdTryLeser.MINDESTHOEHE_GRAD);

            var s = new List<TmyHourlyData>
            {
                new TmyHourlyData
                {
                    TimeString = zeit,
                    DirectIrradiance = 600, DiffuseIrradiance = 100, GlobalIrradiance = 700
                },
                new TmyHourlyData
                {
                    // Ein unmoeglich hoher Wert - die Klemme muss greifen.
                    TimeString = zeit,
                    DirectIrradiance = 1360, DiffuseIrradiance = 0, GlobalIrradiance = 1360
                }
            };

            DwdTryLeser.DirektNormal(s, lon, lat);

            Assert.Equal(600.0 / Math.Sin(alpha * Math.PI / 180.0), s[0].DirectIrradiance, 6);
            Assert.Equal(100.0, s[0].DiffuseIrradiance);       // unberuehrt
            Assert.Equal(700.0, s[0].GlobalIrradiance);

            Assert.Equal(DwdTryLeser.SOLARKONSTANTE, s[1].DirectIrradiance);
        }

        /// <summary>
        /// <b>Die Globalstrahlung bleibt über die ganze Reihe erhalten</b> — die
        /// Umrechnung verschiebt nur zwischen Direkt und Diffus, sie erzeugt und
        /// vernichtet nichts.
        /// </summary>
        [Fact]
        public void Die_Globalstrahlung_ueberlebt_die_Umrechnung()
        {
            List<TmyHourlyData> s = DwdTryLeser.Lesen(Jahr(b: 120, d: 60), out _);
            double vorher = s.Sum(x => x.GlobalIrradiance);

            DwdTryLeser.DirektNormal(s, 9.1829, 48.7758);

            Assert.Equal(vorher, s.Sum(x => x.GlobalIrradiance), 6);
            Assert.All(s, x => Assert.True(x.DirectIrradiance <= DwdTryLeser.SOLARKONSTANTE));
        }

        /// <summary>
        /// <see cref="SolarCalculator.Sonnenhoehe"/> ist REIN LESEND: Sie rührt die drei
        /// statischen Felder nicht an, die zum Vertrag von <c>CalculateHourly</c> gehören.
        /// </summary>
        [Fact]
        public void Sonnenhoehe_laesst_die_statischen_Felder_in_Ruhe()
        {
            SolarCalculator.CalculateHourly(9.1829, 48.7758, 90, 0, 500, 300, 200, 5, 1, 12);
            double winkel = SolarCalculator.sonnenwinkel;
            double azimut = SolarCalculator.sonnen_azimut;
            double cosTheta = SolarCalculator.lastCosTheta;

            SolarCalculator.Sonnenhoehe(9.1829, 48.7758, 180, 6);

            Assert.Equal(winkel, SolarCalculator.sonnenwinkel);
            Assert.Equal(azimut, SolarCalculator.sonnen_azimut);
            Assert.Equal(cosTheta, SolarCalculator.lastCosTheta);
        }
    }
}
