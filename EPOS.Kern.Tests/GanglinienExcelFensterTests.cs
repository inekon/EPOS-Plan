using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Lesefenster beim Excel-Weg</b> — „Erste Datenzeile“ und „Zeilen zu importieren“ des
    /// Optionendialogs (<see cref="GanglinienImportOptionen.ErsteDatenzeile"/>,
    /// <see cref="GanglinienImportOptionen.Zeilenzahl"/>).
    ///
    /// <para><b>Was hier gilt.</b> <see cref="GanglinienDatei.Lies"/> liest Text, CSV und Excel über
    /// dasselbe Zeilenfenster: gezählt werden die nicht leeren Zeilen (die Zählung der
    /// Vorschautabelle), die Zeilen vor der ersten Datenzeile und hinter der verlangten Zahl bleiben
    /// aus, Fehlbestand ist ein Fehler, Überschuss ein Hinweis. Für CSV halten das die Fälle in
    /// <see cref="GanglinienProbenTests"/>; diese Sammlung hält den Excel-Weg daneben — jede Mappe
    /// hat eine CSV-Zwillingsdatei mit denselben Zeilen, und beide Wege müssen dieselben Werte,
    /// dieselbe Zeilenzählung und dasselbe Protokoll liefern.</para>
    ///
    /// <para><b>Die Mappen entstehen im Test</b> (ClosedXML, dieselbe Bibliothek, mit der der Kern
    /// liest), in einem eigenen Ordner unter dem Temp-Pfad, der am Ende gelöscht wird. Ohne
    /// Datenbank, ohne Oberfläche, ohne Ressourcentext-Asserts — keine Kulturpinnung nötig: Die
    /// Werte stehen als Zahlzellen, der Gründetext wird mit derselben Funktion verglichen.</para>
    /// </summary>
    public sealed class GanglinienExcelFensterTests : IDisposable
    {
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-im3t-" + Guid.NewGuid().ToString("N"));

        public GanglinienExcelFensterTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        // ==================================================================
        // Proben: Mappe und CSV-Zwilling mit denselben Zeilen
        // ==================================================================

        /// <summary>
        /// Eine Stundenreihe „Zeile | Leistung“ mit <paramref name="kopf"/> Kopfzeilen und
        /// <paramref name="daten"/> Werten (Wert = laufende Nummer + 0,5); mit
        /// <paramref name="leerNach"/> &gt; 0 steht nach dieser Datenzeile eine leere Zeile.
        /// Liefert die Mappe und die CSV mit denselben Zeilen.
        /// </summary>
        private (string xlsx, string csv) Proben(string name, int kopf, int daten, int leerNach = 0, bool mitZeit = false)
        {
            string xlsx = Path.Combine(_ordner, name + ".xlsx");
            string csv = Path.Combine(_ordner, name + ".csv");
            var zeilen = new List<string>();
            DateTime beginn = new DateTime(2023, 1, 1);
            using (var wb = new XLWorkbook())
            {
                IXLWorksheet ws = wb.AddWorksheet("Lastgang");
                int z = 1;
                if (kopf >= 1)
                {
                    ws.Cell(z, 1).Value = mitZeit ? "Zeitpunkt" : "Zeile";
                    ws.Cell(z, 2).Value = "Leistung";
                    zeilen.Add((mitZeit ? "Zeitpunkt" : "Zeile") + ";Leistung");
                    z++;
                }
                for (int k = 2; k <= kopf; k++)
                {
                    ws.Cell(z, 1).Value = "Einheit";
                    ws.Cell(z, 2).Value = "kW";
                    zeilen.Add("Einheit;kW");
                    z++;
                }
                for (int i = 1; i <= daten; i++)
                {
                    DateTime t = beginn.AddHours(i - 1);
                    if (mitZeit) ws.Cell(z, 1).Value = t;
                    else ws.Cell(z, 1).Value = i;
                    ws.Cell(z, 2).Value = i + 0.5;
                    zeilen.Add((mitZeit ? t.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                                        : i.ToString(CultureInfo.InvariantCulture))
                               + ";" + i.ToString(CultureInfo.InvariantCulture) + ",5");
                    z++;
                    if (i == leerNach) { z++; zeilen.Add(""); }
                }
                wb.SaveAs(xlsx);
            }
            File.WriteAllText(csv, string.Join("\r\n", zeilen) + "\r\n");
            return (xlsx, csv);
        }

        /// <summary>Die Optionen des Lesefensters, für Mappe und CSV gleich (die Mappe übergeht Trenner und Dezimaltrenner der Zellen).</summary>
        private static GanglinienImportOptionen Optionen(int ersteDatenzeile, int zeilenzahl, bool mitZeit = false)
            => new GanglinienImportOptionen
            {
                Trennzeichen = ';', Dezimaltrenner = ',', Kopfzeile = true,
                ZeitSpalte = mitZeit ? 0 : -1, WertSpalte = 1,
                ErsteDatenzeile = ersteDatenzeile, Zeilenzahl = zeilenzahl
            };

        /// <summary>Das Protokoll als vergleichbare Zeilen: Stufe, Schlüssel, Werte.</summary>
        private static List<string> Protokoll(GanglinienRohdaten r)
            => r.Meldungen.Select(m => m.Stufe + " " + m.Schluessel + " " + string.Join("|", m.Werte)).ToList();

        /// <summary>Liest Mappe und CSV mit denselben Optionen und verlangt dasselbe Ergebnis; liefert das der Mappe.</summary>
        private static GanglinienRohdaten BeideWege(string xlsx, string csv, GanglinienImportOptionen o)
        {
            GanglinienRohdaten x = GanglinienDatei.Lies(xlsx, o.Kopie());
            GanglinienRohdaten c = GanglinienDatei.Lies(csv, o.Kopie());
            Assert.Equal(c.Erfolgreich, x.Erfolgreich);
            Assert.Equal(c.Werte, x.Werte);
            Assert.Equal(c.ZeilenAbStart, x.ZeilenAbStart);
            Assert.Equal(Protokoll(c), Protokoll(x));
            if (c.Zeitstempel != null) Assert.Equal(c.Zeitstempel, x.Zeitstempel);
            return x;
        }

        // ==================================================================
        // Erkennung und Vorschautabelle
        // ==================================================================

        [Fact]
        public void Die_Erkennung_der_Mappe_setzt_die_erste_Datenzeile_hinter_zwei_Kopfzeilen()
        {
            var (xlsx, csv) = Proben("zweikopf", 2, 8760);

            GanglinienVorschau v = GanglinienDatei.Erkenne(xlsx);
            Assert.True(v.Lesbar);
            Assert.True(v.IstExcel);
            Assert.True(v.Vorschlag.Kopfzeile);
            Assert.Equal(3, v.Vorschlag.ErsteDatenzeile);
            Assert.Equal(1, v.Vorschlag.WertSpalte);
            Assert.Equal(GanglinienDatei.Erkenne(csv).Vorschlag.ErsteDatenzeile, v.Vorschlag.ErsteDatenzeile);

            // Die Vorschautabelle: zehn Zeilen, die Kopfzeilen zuerst, dann die Daten in Dateireihenfolge.
            Assert.Equal(10, v.Zeilen.Count);
            Assert.Equal(new[] { "Zeile", "Leistung" }, v.Zeilen[0]);
            Assert.Equal(new[] { "Einheit", "kW" }, v.Zeilen[1]);
            Assert.Equal(new[] { "1", "1.5" }, v.Zeilen[2]);
            Assert.Equal(new[] { "8", "8.5" }, v.Zeilen[9]);

            GanglinienProbe probe = v.Probe;
            Assert.True(probe.Erfolgreich, probe.Grund);
            Assert.Equal(8760, probe.Datenzeilen);
            Assert.Equal(8760, probe.ZeilenAbStart);
            Assert.Equal(1.5, probe.WerteKw[0], 9);
            Assert.Equal(GanglinienRaster.Stunde, probe.Raster);
        }

        [Fact]
        public void Die_Vorschau_mit_gewaehltem_Fenster_zaehlt_die_Zeilen_wie_der_Import()
        {
            var (xlsx, _) = Proben("vorschau", 1, 8770);
            GanglinienImportOptionen o = Optionen(7, 8760);

            GanglinienVorschau v = GanglinienDatei.Vorschau(xlsx, o);
            Assert.True(v.Lesbar);
            Assert.Equal("Lastgang", v.Vorschlag.Blattname);
            Assert.Equal(10, v.Zeilen.Count);
            // Die Zeile 7 der Vorschautabelle ist die erste Datenzeile des Fensters: Wert 6,5.
            Assert.Equal(new[] { "6", "6.5" }, v.Zeilen[o.ErsteDatenzeile - 1]);

            GanglinienProbe probe = v.Probe;
            Assert.True(probe.Erfolgreich, probe.Grund);
            Assert.Equal(8760, probe.Datenzeilen);
            Assert.Equal(8765, probe.ZeilenAbStart);
            Assert.Equal(6.5, probe.WerteKw[0], 9);
            Assert.Equal(8765.5, probe.WerteKw[8759], 9);
        }

        // ==================================================================
        // Das Fenster
        // ==================================================================

        [Fact]
        public void Ohne_Angabe_liest_die_Mappe_nach_der_Kopfzeile_bis_zum_Ende()
        {
            var (xlsx, csv) = Proben("einkopf", 1, 30);
            GanglinienImportOptionen o = Optionen(0, 0);
            Assert.Equal(2, o.ErsteDatenzeileWirksam);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.True(r.Erfolgreich);
            Assert.Equal(30, r.Werte.Length);
            Assert.Equal(1.5, r.Werte[0], 9);
            Assert.Equal(30, r.ZeilenAbStart);
            Assert.DoesNotContain(r.Meldungen, m => m.Schluessel == GanglinienDatei.SchluesselZeilenUebrig
                                                 || m.Schluessel == GanglinienDatei.SchluesselZeilenFehlen);
        }

        [Fact]
        public void Erste_Datenzeile_verschiebt_den_Beginn_und_die_Zeilenzahl_begrenzt_das_Ende()
        {
            // 8 770 Datenzeilen unter einer Kopfzeile; ab Zeile 7 (Wert 6,5) 8 760 Zeilen - fünf bleiben übrig.
            var (xlsx, csv) = Proben("fenster", 1, 8770);
            GanglinienImportOptionen o = Optionen(7, 8760);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.True(r.Erfolgreich);
            Assert.Equal(8760, r.Werte.Length);
            Assert.Equal(6.5, r.Werte[0], 9);
            Assert.Equal(8765.5, r.Werte[8759], 9);
            Assert.Equal(8765, r.ZeilenAbStart);
            PruefMeldung uebrig = Assert.Single(r.Meldungen, m => m.Schluessel == GanglinienDatei.SchluesselZeilenUebrig);
            Assert.Equal(PruefStufe.Info, uebrig.Stufe);
            Assert.Equal(new[] { "5", "8760", "7" }, uebrig.Werte);

            GanglinienProbe probe = GanglinienProbe.Lies(xlsx, o);
            Assert.True(probe.Erfolgreich, probe.Grund);
            Assert.Equal(GanglinienRaster.Stunde, probe.Raster);
            Assert.Equal(8760, probe.Datenzeilen);
            Assert.Equal(8765, probe.ZeilenAbStart);
        }

        [Fact]
        public void Das_Fenster_traegt_die_Zeitstempel_mit()
        {
            // Mit Zeitspalte: der erste Zeitstempel ist der der ersten Datenzeile, nicht der des Blattanfangs.
            var (xlsx, csv) = Proben("zeit", 1, 40, mitZeit: true);
            GanglinienImportOptionen o = Optionen(12, 20, mitZeit: true);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.True(r.Erfolgreich);
            Assert.Equal(20, r.Werte.Length);
            Assert.Equal(20, r.Zeitstempel.Length);
            Assert.Equal(new DateTime(2023, 1, 1, 10, 0, 0), r.Zeitstempel[0]);
            Assert.Equal(new DateTime(2023, 1, 2, 5, 0, 0), r.Zeitstempel[19]);
            Assert.Equal(11.5, r.Werte[0], 9);
            Assert.Equal(30, r.ZeilenAbStart);
        }

        [Fact]
        public void Eine_leere_Zeile_im_Blatt_zaehlt_nicht()
        {
            // Nach der fünften Datenzeile steht eine leere Zeile; gezählt werden nur die nicht leeren
            // Zeilen - die Zeile 8 des Fensters ist der siebte Wert, wie in der Vorschautabelle.
            var (xlsx, csv) = Proben("leer", 1, 20, leerNach: 5);
            GanglinienImportOptionen o = Optionen(8, 5);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.True(r.Erfolgreich);
            Assert.Equal(new[] { 7.5, 8.5, 9.5, 10.5, 11.5 }, r.Werte);
            Assert.Equal(14, r.ZeilenAbStart);

            GanglinienVorschau v = GanglinienDatei.Vorschau(xlsx, o);
            Assert.Equal(new[] { "7", "7.5" }, v.Zeilen[o.ErsteDatenzeile - 1]);
        }

        // ==================================================================
        // Grenzfälle
        // ==================================================================

        [Fact]
        public void Ein_Fenster_ueber_das_Blattende_hinaus_benennt_den_Fehlbestand_und_lehnt_ab()
        {
            var (xlsx, csv) = Proben("kurz", 1, 8770);
            GanglinienImportOptionen o = Optionen(20, 8760);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.False(r.Erfolgreich);
            Assert.Equal(8752, r.Werte.Length);
            Assert.Equal(8752, r.ZeilenAbStart);
            PruefMeldung fehlt = Assert.Single(r.Meldungen, m => m.Schluessel == GanglinienDatei.SchluesselZeilenFehlen);
            Assert.Equal(PruefStufe.Fehler, fehlt.Stufe);
            Assert.Equal(new[] { "20", "8752", "8760" }, fehlt.Werte);

            GanglinienProbe probe = GanglinienProbe.Lies(xlsx, o);
            Assert.False(probe.Erfolgreich);
            Assert.Equal(8752, probe.Datenzeilen);
            Assert.Equal(GanglinienProtokollText.Text(fehlt), probe.Grund);
        }

        [Fact]
        public void Eine_erste_Datenzeile_hinter_der_letzten_Zeile_liest_nichts_und_lehnt_als_leer_ab()
        {
            // 31 nicht leere Zeilen (Kopf und 30 Werte); die erste Datenzeile 40 liegt dahinter.
            var (xlsx, csv) = Proben("dahinter", 1, 30);
            GanglinienImportOptionen o = Optionen(40, 0);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.False(r.Erfolgreich);
            Assert.Empty(r.Werte);
            Assert.Equal(0, r.ZeilenAbStart);
            PruefMeldung leer = Assert.Single(r.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GanglinienDatei.SchluesselDateiLeer, leer.Schluessel);

            GanglinienProbe probe = GanglinienProbe.Lies(xlsx, o);
            Assert.False(probe.Erfolgreich);
            Assert.Equal(0, probe.Datenzeilen);
            Assert.Equal(0, probe.ZeilenAbStart);
            Assert.Equal(GanglinienProtokollText.Text(leer), probe.Grund);
        }

        [Fact]
        public void Eine_erste_Datenzeile_hinter_der_letzten_Zeile_mit_Zeilenzahl_benennt_den_Fehlbestand()
        {
            var (xlsx, csv) = Proben("dahinter2", 1, 30);
            GanglinienImportOptionen o = Optionen(40, 10);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.False(r.Erfolgreich);
            Assert.Empty(r.Werte);
            PruefMeldung fehlt = Assert.Single(r.Meldungen, m => m.Schluessel == GanglinienDatei.SchluesselZeilenFehlen);
            Assert.Equal(new[] { "40", "0", "10" }, fehlt.Werte);

            GanglinienProbe probe = GanglinienProbe.Lies(xlsx, o);
            Assert.False(probe.Erfolgreich);
            Assert.Equal(GanglinienProtokollText.Text(fehlt), probe.Grund);
        }

        [Fact]
        public void Die_letzte_Zeile_als_erste_Datenzeile_liest_genau_einen_Wert()
        {
            var (xlsx, csv) = Proben("letzte", 1, 30);
            GanglinienImportOptionen o = Optionen(31, 0);

            GanglinienRohdaten r = BeideWege(xlsx, csv, o);
            Assert.True(r.Erfolgreich);
            Assert.Equal(new[] { 30.5 }, r.Werte);
            Assert.Equal(1, r.ZeilenAbStart);
        }
    }
}
