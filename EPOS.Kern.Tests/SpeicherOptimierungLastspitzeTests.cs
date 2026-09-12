using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W11b‑E‑3 (10.09.2026): Die LASTSPITZENKAPPUNG als dritte
    /// Berechnungsart der Auslegungsoptimierung — Controller-Seite.
    ///
    /// <para><b>Warum es sie gibt.</b> Projekt 1050 führt genau einen Stromspeicher,
    /// keine PV und kein BHKW. Dauer- und Nachtnutzung bewerten den genutzten
    /// Erzeugungsüberschuss; ohne Erzeugung ist er 0, und mit Modulkosten 0 war auch
    /// der Kapitaldienst 0 — die Rasterkarte war einfarbig und trug an allen 120
    /// Punkten ΔJ = 0 (Befund W11b‑B‑25, Windows-Abnahme 09.09.2026).</para>
    ///
    /// <para><b>Ohne Datenbank.</b> Geprüft werden die Teile, die der Controller selbst
    /// entscheidet: Prüfregel, Durchreichung des Leistungspreises, die Wahl der
    /// Staffelstufe und die fünf Kennzahlen des Bestpunkts. Der Leseweg zu Variante,
    /// Tarifsatz und Energieträger ist Datenbanksache und in
    /// <c>SpeicherOptimierungCtrl.Leistungspreisquellen</c> gekapselt; seine
    /// Fachentscheidung — welche Staffelstufe an der Spitze greift — steht in
    /// <c>TarifQuelle</c> und ist hier einzeln geprüft.</para>
    ///
    /// <para><b>Der Prüfstand.</b> Zwei Tage in Stundenwerten: nachts 100 kW, tagsüber
    /// 300 kW, am zweiten Tag um 12 Uhr eine Spitze von 700 kW. Der Speicher lädt in
    /// der ersten Nacht (die Schwelle steht dann schon auf 300 kW) und entlädt in der
    /// Spitzenstunde. Verlustfrei, Preisreihe 0, Zins 0, N = 20 a — damit ist
    /// a = 1/N = 0,05 und jede Zahl von Hand nachrechenbar.</para>
    /// </summary>
    public sealed class SpeicherOptimierungLastspitzeTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherOptimierungLastspitzeTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        private const double LeistungspreisEurProKwA = 100.0;

        // =================================================================
        // Prüfstand
        // =================================================================

        private static double[] Lastgang()
        {
            double[] last = new double[48];
            for (int i = 0; i < last.Length; i++)
            {
                int stunde = i % 24;
                last[i] = stunde >= 8 && stunde < 20 ? 300.0 : 100.0;
            }
            last[24 + 12] = 700.0;
            return last;
        }

        private static SpeicherEingang Eingang()
        {
            double[] last = Lastgang();
            return SpeicherEingang.MitFixpreis(last, new double[last.Length], 0.0);
        }

        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 400.0,
            PKw = 400.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 400.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 1.0,
            CCapEurProKwh = 300.0,
            CPowEurProKw = 100.0,
            IFixEur = 0.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0
        };

        private static StromspeicherOptimierungVorbereitung Vorbereitung()
            => new StromspeicherOptimierungVorbereitung
            {
                Eingang = Eingang(),
                Basis = Basis(),
                Kontext = null
            };

        /// <summary>200 und 400 kWh bei 1 C, kein Feinraster.</summary>
        private static SpeicherOptimierungEingaben Suchraum() => new SpeicherOptimierungEingaben
        {
            CMinKwh = 200.0,
            CMaxKwh = 400.0,
            Stuetzstellen = 2,
            RMin = 1.0,
            RMax = 1.0,
            RSchritt = 1.0,
            Feinraster = false,
            Strategie = OptimiererStrategie.Lastspitzenkappung,
            LeistungspreisEurProKwA = LeistungspreisEurProKwA
        };

        private static SpeicherOptimierungErgebnis Lauf(SpeicherOptimierungEingaben e = null)
            => SpeicherOptimierungCtrl.Rechnen(Vorbereitung(), e ?? Suchraum(), null, CancellationToken.None);

        private static string Wert(SpeicherOptimierungErgebnis dto, string bezeichnung)
            => dto.Kennzahlen.Single(z => z.Bezeichnung == bezeichnung).Wert;

        // =================================================================
        // Prüfregel und Durchreichung
        // =================================================================

        [Fact]
        public void Ohne_Leistungspreis_Laeuft_Die_Kappung_Nicht()
        {
            SpeicherOptimierungEingaben ohne = Suchraum();
            ohne.LeistungspreisEurProKwA = 0.0;

            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_LP_FEHLT, SpeicherOptimierungCtrl.Pruefe(ohne));

            SpeicherOptimierungErgebnis dto = Lauf(ohne);
            Assert.False(dto.Erfolg);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_LP_FEHLT, dto.Meldung, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Anderen_Berechnungsarten_Brauchen_Keinen_Leistungspreis()
        {
            SpeicherOptimierungEingaben e = Suchraum();
            e.Strategie = OptimiererStrategie.Dauernutzung;
            e.LeistungspreisEurProKwA = 0.0;

            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_LP_FEHLT, SpeicherOptimierungCtrl.Pruefe(e));
        }

        [Fact]
        public void Der_Leistungspreis_Geht_In_Die_Optionen_Und_In_Die_Kopie()
        {
            SpeicherOptimierungEingaben e = Suchraum();

            Assert.Equal(LeistungspreisEurProKwA,
                         SpeicherOptimierungCtrl.Optionen(e).LeistungspreisEurProKwA, 9);
            Assert.Equal(OptimiererStrategie.Lastspitzenkappung,
                         SpeicherOptimierungCtrl.Optionen(e).Strategie);
            Assert.Equal(LeistungspreisEurProKwA, e.Kopie().LeistungspreisEurProKwA, 9);
        }

        // =================================================================
        // Der Lauf
        // =================================================================

        [Fact]
        public void Der_Bestpunkt_Kappt_Die_Spitze_Um_400_kW()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            Assert.True(dto.Erfolg);
            Assert.Equal(400.0, dto.KapazitaetKwh, 6);
            Assert.Equal(400.0, dto.LeistungKw, 6);

            // dJ = 100 EUR/(kW*a) * 400 kW - 0,05 * (300*400 + 100*400) = 32.000 EUR/a
            Assert.Equal(32000.0, dto.ZielfunktionEur, 6);
        }

        [Fact]
        public void Die_Kennzahlengruppe_Der_Kappung_Traegt_Die_Fuenf_Groessen()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            Assert.Equal(5, dto.Kennzahlen.Count(
                z => z.Gruppe == SpeicherOptimierungCtrl.GRUPPE_KAPPUNG));

            Assert.Equal("700", Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_SPITZE_OHNE));
            Assert.Equal("300", Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_SPITZE_MIT));
            Assert.Equal("400", Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPPUNG));
            Assert.Equal("40000,00", Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_LP_ERSPARNIS));
            Assert.Equal("300", Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_SCHWELLE));
        }

        [Fact]
        public void Ohne_Kappung_Bleibt_Die_Gruppe_Weg()
        {
            SpeicherOptimierungEingaben e = Suchraum();
            e.Strategie = OptimiererStrategie.Dauernutzung;
            e.LeistungspreisEurProKwA = 0.0;

            SpeicherOptimierungErgebnis dto = Lauf(e);

            Assert.True(dto.Erfolg);
            Assert.DoesNotContain(dto.Kennzahlen,
                z => z.Gruppe == SpeicherOptimierungCtrl.GRUPPE_KAPPUNG);
        }

        [Fact]
        public void Die_CSV_Traegt_Die_Fuenf_Neuen_Spalten()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            string[] zeilen = dto.RasterCsv
                .Split('\n')
                .Select(z => z.TrimEnd('\r'))
                .Where(z => z.Length > 0)
                .ToArray();

            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPPUNG + " [kW]", zeilen[0], StringComparison.Ordinal);
            Assert.All(zeilen, z => Assert.Equal(25, z.Count(c => c == ';')));

            // Die zweite Datenzeile ist der Bestpunkt (400 kWh) - Kappung 400 kW.
            string[] felder = zeilen[2].Split(';');
            Assert.Equal("400", felder[23]);        // Kappung [kW]
            Assert.Equal("40000", felder[24]);      // Leistungspreisersparnis [EUR/a]
        }

        // =================================================================
        // Die Staffelstufe an der Spitze
        // =================================================================

        private static TarifParameter Tarif(double grenze, double preis1, double preis2)
            => new TarifParameter
            {
                StaffelGrenzeKW = grenze,
                StaffelPreis1EurKW = preis1,
                StaffelPreis2EurKW = preis2
            };

        [Fact]
        public void Eine_Spitze_Ueber_Der_Grenze_Nimmt_Die_Zweite_Stufe()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherOptimierungCtrl.TarifQuelle(Tarif(500.0, 90.0, 120.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(120.0, q.WertEurProKwA, 9);
            Assert.Contains("751", q.Bezeichnung, StringComparison.Ordinal);
            Assert.Contains("500", q.Bezeichnung, StringComparison.Ordinal);
        }

        [Fact]
        public void Eine_Spitze_Innerhalb_Der_Grenze_Nimmt_Die_Erste_Stufe()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherOptimierungCtrl.TarifQuelle(Tarif(1000.0, 90.0, 120.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
        }

        [Fact]
        public void Ohne_Zweite_Stufe_Gilt_Die_Erste_Auch_Oberhalb()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherOptimierungCtrl.TarifQuelle(Tarif(500.0, 90.0, 0.0), 751.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
        }

        [Fact]
        public void Ohne_Bekannte_Spitze_Sagt_Der_Text_Es()
        {
            SpeicherOptimierungLeistungspreisQuelle q =
                SpeicherOptimierungCtrl.TarifQuelle(Tarif(500.0, 90.0, 120.0), 0.0);

            Assert.NotNull(q);
            Assert.Equal(90.0, q.WertEurProKwA, 9);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_QUELLE_TARIF_OHNE_SPITZE, q.Bezeichnung,
                            StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Gepflegte_Staffel_Gibt_Es_Keine_Quelle()
        {
            Assert.Null(SpeicherOptimierungCtrl.TarifQuelle(Tarif(500.0, 0.0, 0.0), 751.0));
            Assert.Null(SpeicherOptimierungCtrl.TarifQuelle(null, 751.0));
        }

        [Fact]
        public void Ohne_Projekt_Gibt_Es_Keine_Quellen()
        {
            Assert.Empty(SpeicherOptimierungCtrl.Leistungspreisquellen(0, 751.0));
        }
    }
}
