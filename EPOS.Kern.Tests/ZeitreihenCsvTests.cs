using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// CSV am Diagramm: der Zeitreihenschreiber des Kerns (<see cref="ZeitreihenCsv"/>) und die Spalten
    /// des Kälte-Exports (<see cref="SimulationErgebnisCtrl.KaelteCsvSpalten"/>).
    /// </summary>
    public sealed class ZeitreihenCsvTests
    {
        private static readonly Farbton TON = new Farbton(default(Farbrolle));

        private static double[] Reihe(int n, double faktor)
            => Enumerable.Range(0, n).Select(i => i * faktor).ToArray();

        [Theory]
        [InlineData(8760, Zeitraster.Stunde, "Stunde")]
        [InlineData(35040, Zeitraster.Viertelstunde, "Viertelstunde")]
        [InlineData(365, Zeitraster.Tag, "Tag")]
        [InlineData(12, Zeitraster.Monat, "Monat")]
        [InlineData(168, Zeitraster.Wochenstunde, "Wochenstunde")]
        [InlineData(24, Zeitraster.Tagesstunde, "Stunde")]
        [InlineData(52, Zeitraster.Woche, "Woche")]
        [InlineData(7, Zeitraster.Index, "Nr.")]
        public void Das_Raster_folgt_der_Laenge_und_benennt_die_erste_Spalte(int laenge, Zeitraster raster, string kopf)
        {
            Assert.Equal(raster, ZeitreihenCsv.RasterAus(laenge));
            Assert.Equal(kopf, ZeitreihenCsv.Rasterkopf(raster));
        }

        /// <summary>
        /// Kopf „Stunde;Name [Einheit];…“, danach 8 760 Zeilen mit laufender Stunde; Semikolon,
        /// Dezimalkomma, Format 0.0## wie der Bestandsexport; gleichnamige Spalten werden eindeutig.
        /// </summary>
        [Fact]
        public void Stundenreihen_ergeben_Kopf_und_8760_Zeilen()
        {
            var spalten = new List<ZeitreihenSpalte>
            {
                new("Wärmelast", "kW", Reihe(8760, 0.5)),
                new("Wärmelast", "kW", Reihe(8760, 1.0)),
                new("Anteil;roh", "", Reihe(12, 1.0))
            };

            string[] zeilen = ZeitreihenCsv.Text(Zeitraster.Stunde, spalten)
                                           .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(8760 + 1, zeilen.Length);
            Assert.Equal("Stunde;Wärmelast [kW];Wärmelast [kW]_2;Anteil,roh", zeilen[0]);
            Assert.Equal("1;0,0;0,0;0,0", zeilen[1]);
            Assert.Equal("4;1,5;3,0;3,0", zeilen[4]);
            Assert.Equal("8760;4379,5;8759,0;", zeilen[8760]);
        }

        /// <summary>
        /// Das Modell liefert die GEZEIGTEN Reihen: eine Stapelschicht ihren Beitrag (Werte − Unten),
        /// eine Punktwolke nichts.
        /// </summary>
        [Fact]
        public void Aus_dem_Modell_kommen_Linien_und_Schichtbeitraege_ohne_Punktwolken()
        {
            var modell = new Zeichenmodell(100, 50, TON);
            modell.FuegeReihe(new Datenreihe("Summe", TON, 1f, null, new[] { 5.0, 7.0 }, Einheit: "kW"));
            modell.FuegeReihe(new Datenreihe("Schicht", TON, 1f, null, new[] { 5.0, 7.0 },
                                             Art: Reihenart.Flaeche, Unten: new[] { 2.0, 3.0 }, Einheit: "kW"));
            modell.FuegeReihe(new Datenreihe("Wolke", TON, 1f, null, new[] { 1.0 },
                                             Art: Reihenart.Punkte, XWerte: new[] { 3.0 }));

            IReadOnlyList<ZeitreihenSpalte> spalten = ZeitreihenCsv.AusModell(modell);

            Assert.Equal(new[] { "Summe", "Schicht" }, spalten.Select(s => s.Name));
            Assert.Equal(new[] { 3.0, 4.0 }, spalten[1].Werte);
            Assert.Equal("kW", spalten[0].Einheit);
            Assert.Empty(ZeitreihenCsv.AusModell(null));
        }

        [Theory]
        [InlineData("Wärmelast Jahresganglinie", "Wärmelast_Jahresganglinie")]
        [InlineData("BHKW: Strom / Netz", "BHKW_Strom_Netz")]
        [InlineData("", "Zeitreihe")]
        public void Der_Dateistamm_kommt_aus_dem_Titel(string titel, string stamm)
            => Assert.Equal(stamm, ZeitreihenCsv.Dateistamm(titel));

        /// <summary>
        /// Der Kälte-Export: Zeitstempel, Außentemperatur, die Kältelast und je Bedarfsart eine Spalte
        /// („Kältelast [kW] Kühlung“, wie die Kanalspalten der Wärme), 8 760 Zeilen unter dem Kopf.
        /// </summary>
        [Fact]
        public void Der_Kaelteexport_fuehrt_die_Kaeltelast_und_die_Spalte_Kuehlung()
        {
            var k = new SimulationErgebnisCtrl.KaelteErgebnis { KaeltebedarfKwh = Reihe(8760, 0.25) };
            List<CsvSpalte> spalten = SimulationErgebnisCtrl.KaelteCsvSpalten(k);

            Assert.Equal(2, spalten.Count);
            Assert.Empty(SimulationErgebnisCtrl.KaelteCsvSpalten(null));

            string datei = Path.Combine(Path.GetTempPath(), "epos-kaeltespalten-" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                CsvExportClass.Schreiben(datei, null, spalten, false);
                string[] zeilen = File.ReadAllLines(datei);
                Assert.Equal(8760 + 1, zeilen.Length);
                string kuehlung = WindowsFormsApplication1.MyResource.Resource.CHART_CSV_KAELTELAST + " "
                                  + Warnkriterien.KanalAnzeige(Kanal.KUEHLUNG);
                Assert.Equal("Zeitstempel;Außentemperatur [°C];" + WindowsFormsApplication1.MyResource.Resource.CHART_CSV_KAELTELAST
                             + ";" + kuehlung, zeilen[0]);
                Assert.EndsWith(";1,0;1,0", zeilen[5]);
            }
            finally
            {
                File.Delete(datei);
            }
        }
    }
}
