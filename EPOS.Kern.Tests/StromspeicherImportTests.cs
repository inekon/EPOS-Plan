using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zerleger des Stromspeicherimports</b> — Anwenderwunsch <b>W13‑E‑2</b>
    /// vom 07.09.2026, Konzept <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>.
    ///
    /// <para><b>Zwei Quellen, zwei Zerleger.</b> Geprüft wurden vier Quellen; zwei
    /// liefern Daten, die ein Katalogsatz von <c>Tab_Stromspeicher_STAMM</c>
    /// füllen können:</para>
    /// <list type="bullet">
    ///   <item><b>CEC Energy Storage System List</b> — das Geräteverzeichnis
    ///         (6 654 Zeilen, 130 Hersteller; Probe: 23 Geräte),</item>
    ///   <item><b>bslib</b> (HTW Berlin / FZ Jülich) — vier vermessene Systeme
    ///         samt Standby-Verbrauch (Probe: die vollständige Datei).</item>
    /// </list>
    ///
    /// <para><b>Die Kultur ist gepinnt.</b> Beide Listen schreiben Zahlen mit
    /// Punkt; ein Zerleger, der die Kultur des Arbeitsplatzes liest, machte aus
    /// 8,85 kWh je nach Rechner 885. Die Prüfung läuft deshalb bewusst unter
    /// <c>de-DE</c> — dort, wo ein kulturabhängiges <c>double.Parse</c>
    /// auffliegt.</para>
    /// </summary>
    public class StromspeicherImportTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public StromspeicherImportTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;

            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <summary>Die Geräte der Probe <c>stromspeicher_cec_ess_23.csv</c>.</summary>
        private const int CEC_GERAETE = 23;

        /// <summary>Davon mit einer Wirkungsgradangabe des Herstellers.</summary>
        private const int CEC_MIT_WIRKUNGSGRAD = 11;

        /// <summary>Die Speicher der Probe <c>stromspeicher_bslib_7.csv</c>.</summary>
        private const int BSLIB_SPEICHER = 4;

        /// <summary>Die übergangenen Zeilen derselben Probe (zwei PV-Wechselrichter, ein System ohne Kapazität).</summary>
        private const int BSLIB_UEBERGANGEN = 3;

        // =================================================================
        //  CEC Energy Storage System List
        // =================================================================

        /// <summary>
        /// <b>Die CEC-Probe wird vollständig gelesen.</b> Die Kopfzeile steht NICHT
        /// an Zeile 1 — davor stehen der Titel und der Stand der Liste, genau wie
        /// im Original. Der Zerleger findet sie an ihren Pflichtspalten.
        /// </summary>
        [Fact]
        public void CEC_Probe_liefert_23_Geraete()
        {
            var leser = new CecSpeicherImport();
            (bool Erfolg, SpeicherImportMeldung Meldung) r = leser.AusDatei(Probe("stromspeicher_cec_ess_23.csv"));

            Assert.True(r.Erfolg, "Die CEC-Probe ließ sich nicht lesen: " + r.Meldung);
            Assert.Equal("SPIMP_MSG_GELADEN", r.Meldung.Schluessel);
            Assert.Equal(CEC_GERAETE, leser.Saetze.Count);

            // Der Vorspann ist echt: zwei Erlaeuterungszeilen, dann Beschriftung
            // und Einheiten.
            Assert.Equal(3, leser.Kopfzeile);
            Assert.Equal("Data has not changed since August 21, 2026", leser.Stand);

            _ausgabe.WriteLine("Geräte:      " + leser.Saetze.Count);
            _ausgabe.WriteLine("Kopfzeile:   " + leser.Kopfzeile);
            _ausgabe.WriteLine("Stand:       " + leser.Stand);
            _ausgabe.WriteLine("Hersteller:  " + string.Join(", ", leser.Hersteller()));

            // Jedes Geraet traegt Bezeichner, Kapazitaet und Leistung - ohne die
            // drei waere der Katalogsatz nicht rechenbar.
            foreach (StromspeicherImportSatz s in leser.Saetze)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Bezeichner));
                Assert.True(s.EnergieKwh > 0.0, "Ohne Kapazität: " + s.Bezeichner);
                Assert.True(s.LeistungKw > 0.0, "Ohne Leistung: " + s.Bezeichner);
            }

            Assert.Equal(CEC_MIT_WIRKUNGSGRAD, leser.Saetze.Count(s => s.WirkungsgradRt > 0.0));
        }

        /// <summary>
        /// <b>Ein Satz feldgenau.</b> Zeile 585 der Liste — Chint mit einer
        /// Wirkungsgradangabe in PROZENT (85) und einer Kapazität, die als
        /// Kilowattstunde und nicht als Wattstunde ankommt.
        /// </summary>
        [Fact]
        public void CEC_ein_Satz_feldgenau()
        {
            var leser = new CecSpeicherImport();
            Assert.True(leser.AusDatei(Probe("stromspeicher_cec_ess_23.csv")).Erfolg);

            StromspeicherImportSatz s = leser.Saetze.Single(
                x => x.Modell == "CPS ES-125kW/261kWh-US");

            Assert.Equal("Chint Power Systems America", s.Hersteller);
            Assert.Equal("Chint Power Systems America: CPS ES-125kW/261kWh-US", s.Bezeichner);
            Assert.Equal("Lithium Iron Phosphate", s.Technologie);
            Assert.Equal(261.0, s.EnergieKwh, 6);
            Assert.Equal(125.0, s.LeistungKw, 6);
            Assert.Equal(0.85, s.WirkungsgradRt, 6);
            Assert.Equal(0.0, s.StandbyW);

            StromspeicherModel m = s.NachModell();
            Assert.Equal("Chint Power Systems America: CPS ES-125kW/261kWh-US", m.m_szBezeichner);
            Assert.Equal(DbWerte.SP_TYP_LITHIUM_EISEN_PHOSPHAT, m.m_szTyp);
            Assert.Equal(261.0, m.m_Energie, 6);
            Assert.Equal(125.0, m.m_Leistung, 6);
            Assert.Equal(0.85, m.m_WirkungsgradRT, 6);

            // Was die Quelle nicht liefert, bleibt 0 und faellt im Rechenweg auf
            // die Vorgaben zurueck - es wird NICHT erfunden.
            Assert.Equal(0.0, m.m_Degradation);
            Assert.Equal(0.0, m.m_Ladezustand);
            Assert.Equal(0, m.m_ZyklenZugesichert);
            Assert.Equal(0.0, m.m_Modulkosten);
            Assert.Equal(0.0, m.m_Leistungskosten);
            Assert.Equal(0.0, m.m_InvestitionFix);
            Assert.Equal(0.0, m.m_Verschleisskosten);
            Assert.Equal(0.0, m.m_StandbyVerbrauch);
        }

        /// <summary>
        /// <b>Beide Schreibweisen des Wirkungsgrads.</b> Dieselbe Spalte trägt 85
        /// (Prozent) und 0,88 (Bruch); beides muss als Bruch herauskommen, sonst
        /// stünde ein Speicher mit 88 statt 0,88 im Katalog — und die Engine
        /// wiese ihn zurück (<c>SpeicherParameter</c> verlangt (0…1]).
        /// </summary>
        [Fact]
        public void CEC_Wirkungsgrad_kommt_immer_als_Bruch()
        {
            var leser = new CecSpeicherImport();
            Assert.True(leser.AusDatei(Probe("stromspeicher_cec_ess_23.csv")).Erfolg);

            StromspeicherImportSatz prozent = leser.Saetze.Single(x => x.Modell == "CPS ES-62.5kW/261kWh-US");
            StromspeicherImportSatz bruch = leser.Saetze.First(x => x.Hersteller == "SOCOMEC");

            Assert.Equal(0.86, prozent.WirkungsgradRt, 6);
            Assert.Equal(0.88, bruch.WirkungsgradRt, 6);

            foreach (StromspeicherImportSatz s in leser.Saetze)
                Assert.InRange(s.WirkungsgradRt, 0.0, 1.0);
        }

        /// <summary>
        /// <b>Die fünf Zellchemien der Liste werden übersetzt</b>, und was die
        /// Tabelle nicht kennt, bleibt im Original stehen statt still zu
        /// „Lithium-Ionen" zu werden.
        /// </summary>
        [Fact]
        public void CEC_Zellchemie_wird_uebersetzt()
        {
            var leser = new CecSpeicherImport();
            Assert.True(leser.AusDatei(Probe("stromspeicher_cec_ess_23.csv")).Erfolg);

            Dictionary<string, int> typen = leser.Saetze
                .GroupBy(s => s.NachModell().m_szTyp)
                .ToDictionary(g => g.Key, g => g.Count());

            _ausgabe.WriteLine("Zellchemien: " + string.Join(", ",
                typen.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)));

            Assert.Equal(14, typen[DbWerte.SP_TYP_LITHIUM_EISEN_PHOSPHAT]);
            Assert.Equal(3, typen[DbWerte.SP_TYP_LITHIUM_IONEN]);
            Assert.Equal(3, typen[DbWerte.SP_TYP_LITHIUM_TITANAT]);
            Assert.Equal(1, typen[DbWerte.SP_TYP_LITHIUM_NMC]);
            Assert.Equal(2, typen[DbWerte.SP_TYP_EISEN_REDOX_FLOW]);
            Assert.Equal(5, typen.Count);

            // Ein unbekannter Text bleibt stehen.
            Assert.Equal("Natrium-Ionen", StromspeicherImportSatz.TypAusTechnologie("Natrium-Ionen"));
            Assert.Equal("", StromspeicherImportSatz.TypAusTechnologie(null));
        }

        /// <summary>
        /// <b>Die Mappe geht denselben Weg wie die CSV-Datei.</b> Die CEC liefert
        /// ihre Liste ausschließlich als <c>.xlsx</c>; die Probe im Repository ist
        /// eine CSV-Ausleitung davon. Dieser Fall baut aus derselben Probe eine
        /// echte Arbeitsmappe und liest sie zurück — beide Wege müssen dieselben
        /// 23 Geräte mit denselben Zahlen ergeben.
        /// </summary>
        [Fact]
        public void CEC_Mappe_und_CSV_ergeben_dasselbe()
        {
            var ausCsv = new CecSpeicherImport();
            Assert.True(ausCsv.AusDatei(Probe("stromspeicher_cec_ess_23.csv")).Erfolg);

            string mappe = Path.Combine(Path.GetTempPath(),
                "epos_cec_ess_" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                SchreibeMappe(Probe("stromspeicher_cec_ess_23.csv"), mappe);

                var ausMappe = new CecSpeicherImport();
                (bool Erfolg, SpeicherImportMeldung Meldung) r = ausMappe.AusDatei(mappe);

                Assert.True(r.Erfolg, "Die Mappe ließ sich nicht lesen: " + r.Meldung);
                Assert.Equal(ausCsv.Saetze.Count, ausMappe.Saetze.Count);
                Assert.Equal(ausCsv.Kopfzeile, ausMappe.Kopfzeile);

                for (int i = 0; i < ausCsv.Saetze.Count; i++)
                {
                    Assert.Equal(ausCsv.Saetze[i].Bezeichner, ausMappe.Saetze[i].Bezeichner);
                    Assert.Equal(ausCsv.Saetze[i].EnergieKwh, ausMappe.Saetze[i].EnergieKwh, 6);
                    Assert.Equal(ausCsv.Saetze[i].LeistungKw, ausMappe.Saetze[i].LeistungKw, 6);
                    Assert.Equal(ausCsv.Saetze[i].WirkungsgradRt, ausMappe.Saetze[i].WirkungsgradRt, 6);
                }
            }
            finally
            {
                if (File.Exists(mappe)) File.Delete(mappe);
            }
        }

        // =================================================================
        //  bslib
        // =================================================================

        /// <summary>
        /// <b>Die bslib-Probe liefert vier Speicher</b> — von den sieben Zeilen
        /// der Datei sind zwei reine PV-Wechselrichter und eine ein DC-System
        /// ohne Kapazitätsangabe. Sie werden benannt übergangen, nicht
        /// stillschweigend verloren.
        /// </summary>
        [Fact]
        public void Bslib_Probe_liefert_vier_Speicher()
        {
            var leser = new BslibImport();
            (bool Erfolg, SpeicherImportMeldung Meldung) r = leser.AusDatei(Probe("stromspeicher_bslib_7.csv"));

            Assert.True(r.Erfolg, "Die bslib-Probe ließ sich nicht lesen: " + r.Meldung);
            Assert.Equal(BSLIB_SPEICHER, leser.Saetze.Count);
            Assert.Equal(BSLIB_UEBERGANGEN, leser.Uebergangen.Count);

            _ausgabe.WriteLine("Speicher:    " + string.Join(" | ", leser.Saetze.Select(s => s.Bezeichner)));
            _ausgabe.WriteLine("übergangen:  " + string.Join(", ", leser.Uebergangen));

            Assert.Contains("INV1", leser.Uebergangen);
            Assert.Contains("INV2", leser.Uebergangen);
            Assert.Contains("S5", leser.Uebergangen);
        }

        /// <summary>
        /// <b>Ein Satz feldgenau — und die drei Umrechnungen.</b> Siemens
        /// Junelight: Watt nach Kilowatt (3 507 → 3,507), Prozent nach Bruch
        /// (96,87 → 0,9687) und die vier Standby-Messwerte auf EINE Zahl
        /// (14,9 + 0,1 gegen 12,1 + 0 → 15,0 W).
        ///
        /// <para>Der Modellname trägt ein KOMMA („Junelight Smart Battery 9,9")
        /// und steht deshalb in der Datei in Anführungszeichen — die Zeile ist
        /// zugleich die Probe darauf, dass der Zerleger die Anführungszeichen
        /// achtet.</para>
        /// </summary>
        [Fact]
        public void Bslib_ein_Satz_feldgenau()
        {
            var leser = new BslibImport();
            Assert.True(leser.AusDatei(Probe("stromspeicher_bslib_7.csv")).Erfolg);

            StromspeicherImportSatz s = leser.Saetze.Single(x => x.Hersteller == "Siemens");

            Assert.Equal("Junelight Smart Battery 9,9", s.Modell);
            Assert.Equal("Siemens: Junelight Smart Battery 9,9", s.Bezeichner);
            Assert.Equal(8.85, s.EnergieKwh, 6);
            Assert.Equal(3.507, s.LeistungKw, 6);
            Assert.Equal(0.9687, s.WirkungsgradRt, 6);
            Assert.Equal(15.0, s.StandbyW, 6);

            // bslib fuehrt keine Zellchemie - der Typ bleibt leer statt geraten.
            Assert.Equal("", s.NachModell().m_szTyp);
        }

        /// <summary>
        /// <b>Das DC-System ist das PAAR.</b> KOSTAL PLENTICORE plus 5.5 mit einer
        /// BYD Battery-Box H6.4 — Leistungselektronik und Batterie stehen in der
        /// Datei getrennt und ergeben erst zusammen den Speicher. Sein Standby
        /// kommt aus dem LEEREN Zustand (4,47 + 4,56 = 9,03 W), weil der bei
        /// einem DC-System der ungünstigere ist.
        /// </summary>
        [Fact]
        public void Bslib_DC_System_traegt_Elektronik_und_Batterie()
        {
            var leser = new BslibImport();
            Assert.True(leser.AusDatei(Probe("stromspeicher_bslib_7.csv")).Erfolg);

            StromspeicherImportSatz s = leser.Saetze.Single(x => x.Modell.StartsWith("PLENTICORE plus 5.5"));

            Assert.Equal("KOSTAL", s.Hersteller);
            Assert.Equal("PLENTICORE plus 5.5 / BYD Battery-Box H6.4", s.Modell);
            Assert.Equal(5.68, s.EnergieKwh, 6);
            Assert.Equal(3.157, s.LeistungKw, 6);
            Assert.Equal(0.9482, s.WirkungsgradRt, 6);
            Assert.Equal(9.03, s.StandbyW, 6);

            // Die generische Zeile bleibt generisch.
            StromspeicherImportSatz generisch = leser.Saetze.Single(x => x.Hersteller == "Generic");
            Assert.Equal("Generic: AC-System", generisch.Bezeichner);
            Assert.Equal(1.0, generisch.EnergieKwh, 6);
            Assert.Equal(0.95, generisch.WirkungsgradRt, 6);
            Assert.Equal(0.0, generisch.StandbyW, 6);
        }

        // =================================================================
        //  Gegenproben
        // =================================================================

        /// <summary>
        /// <b>Eine fremde Datei bringt eine klare Meldung, keine Ausnahme und
        /// keinen halben Katalog.</b> Geprüft wird über Kreuz: die
        /// Wechselrichterliste durch den Speicherzerleger, die Speicherliste
        /// durch den bslib-Zerleger — beide Male fehlen Pflichtspalten, und die
        /// Meldung nennt sie.
        /// </summary>
        [Fact]
        public void Fremde_Dateien_werden_abgewiesen()
        {
            var cec = new CecSpeicherImport();
            (bool Erfolg, SpeicherImportMeldung Meldung) a = cec.AusDatei(Probe("cec_wechselrichter_21.csv"));
            Assert.False(a.Erfolg);
            Assert.Equal("SPIMP_MSG_KOPFZEILE", a.Meldung.Schluessel);
            Assert.Contains("manufacturer name", a.Meldung.Werte[0]);
            Assert.Empty(cec.Saetze);
            _ausgabe.WriteLine("CEC-Zerleger an der Wechselrichterliste: " + a.Meldung);

            var bslib = new BslibImport();
            (bool Erfolg, SpeicherImportMeldung Meldung) b = bslib.AusDatei(Probe("stromspeicher_cec_ess_23.csv"));
            Assert.False(b.Erfolg);
            Assert.Equal("SPIMP_MSG_KOPFZEILE", b.Meldung.Schluessel);
            Assert.Empty(bslib.Saetze);
            _ausgabe.WriteLine("bslib-Zerleger an der CEC-Liste: " + b.Meldung);

            // Eine Datei, die es nicht gibt.
            var fehlt = new CecSpeicherImport();
            Assert.Equal("SPIMP_MSG_DATEI_FEHLT",
                fehlt.AusDatei(Path.Combine(Path.GetTempPath(), "gibt_es_nicht_4711.csv")).Meldung.Schluessel);
        }

        /// <summary>
        /// <b>Die Zahlenweiche des Wirkungsgrads</b> in Einzelfällen — sie
        /// entscheidet über einen Katalogwert, den die Engine sonst zurückweist.
        /// </summary>
        [Theory]
        [InlineData("85", 0.85)]
        [InlineData("97.5", 0.975)]
        [InlineData("97,5", 0.975)]      // Excel-Ausleitung mit Komma
        [InlineData("0.88", 0.88)]
        [InlineData("1", 1.0)]
        [InlineData("100", 1.0)]
        [InlineData("No Information Submitted", 0.0)]
        [InlineData("", 0.0)]
        [InlineData(null, 0.0)]
        [InlineData("0", 0.0)]
        [InlineData("-5", 0.0)]
        [InlineData("101", 0.0)]
        public void Wirkungsgradweiche(string text, double erwartet)
        {
            Assert.Equal(erwartet, StromspeicherImportSatz.WirkungsgradAusText(text), 9);
        }

        /// <summary>
        /// <b>Zahlen sind kulturunabhängig.</b> Unter <c>de-DE</c> ist das Komma
        /// das Dezimalzeichen — der Zerleger muss trotzdem den Punkt lesen, und
        /// ein Text mit zwei Kommas ist keine Zahl, sondern ein Verdacht auf
        /// Tausendertrennung und ergibt 0.
        /// </summary>
        [Fact]
        public void Zahlen_haengen_nicht_an_der_Kultur()
        {
            Assert.Equal("de-DE", CultureInfo.CurrentCulture.Name);

            Assert.Equal(8.85, StromspeicherImportSatz.Zahl("8.85"), 9);
            Assert.Equal(8.85, StromspeicherImportSatz.Zahl("8,85"), 9);
            Assert.Equal(261.0, StromspeicherImportSatz.Zahl(" 261 "), 9);
            Assert.Equal(0.0, StromspeicherImportSatz.Zahl("1,234,5"), 9);
            Assert.Equal(0.0, StromspeicherImportSatz.Zahl("No Information Submitted"), 9);
            Assert.Equal(0.0, StromspeicherImportSatz.Zahl(null), 9);
        }

        /// <summary>
        /// <b>Die Fußnotenziffern der CEC-Beschriftung stören die Erkennung
        /// nicht</b> — und eine Ziffer, die zum Namen gehört, bleibt stehen.
        /// </summary>
        [Theory]
        [InlineData("Maximum Continuous Discharge Rate4", "maximum continuous discharge rate")]
        [InlineData("Certified JA12 Control  Strategies1", "certified ja12 control strategies")]
        [InlineData("UL 9540", "ul 9540")]
        [InlineData("Nameplate Energy Capacity", "nameplate energy capacity")]
        [InlineData("UL 1741 SA\nFreq-Watt\nVolt-Watt", "ul 1741 sa freq-watt volt-watt")]
        [InlineData("  ", "")]
        public void Kopfnamen_werden_normalisiert(string roh, string erwartet)
        {
            Assert.Equal(erwartet, StromspeicherImportSatz.Kopfname(roh));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Importproben</c> aufwaerts vom Laufordner —
        /// dasselbe Vorgehen wie in <c>KatalogImportTests</c>.
        /// </summary>
        private static string Probe(string name)
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", name);
                if (File.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Die Probe " + name + " wurde unter Referenzlaeufe/Importproben nicht gefunden.");
            return null;
        }

        /// <summary>
        /// Schreibt eine CSV-Probe als echte Arbeitsmappe — der Weg, auf dem die
        /// CEC ihre Liste wirklich ausliefert. Die Zahlenspalten kommen dabei als
        /// ZAHLEN in die Mappe, nicht als Text; genau das muss der Zerleger auf
        /// dem Mappenweg auch verkraften.
        /// </summary>
        private static void SchreibeMappe(string csv, string ziel)
        {
            List<string[]> zeilen = StromspeicherImportSatz.TabelleAusText(
                StromspeicherImportSatz.LiesText(csv));

            using (var mappe = new XLWorkbook())
            {
                IXLWorksheet blatt = mappe.Worksheets.Add("Energy Storage System");
                for (int z = 0; z < zeilen.Count; z++)
                {
                    for (int s = 0; s < zeilen[z].Length; s++)
                    {
                        string wert = zeilen[z][s];
                        if (string.IsNullOrEmpty(wert)) continue;

                        double zahl;
                        if (double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out zahl))
                            blatt.Cell(z + 1, s + 1).Value = zahl;
                        else
                            blatt.Cell(z + 1, s + 1).Value = wert;
                    }
                }
                mappe.SaveAs(ziel);
            }
        }
    }
}
