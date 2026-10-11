using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Formatbeschreibung des Ganglinienimports</b> (IM-4): die Formate selbst, die
    /// Probelesung nach einem Sonderformat (feste Werteanzahl, Temperaturachse, keine Kennzahlen)
    /// und der Lastgang, der seinen Weg unverändert geht und Summe und Höchstleistung nennt.
    /// Ohne Datenbank; die Texte unter de-DE (Kulturvorrichtung).
    /// </summary>
    public class GanglinienImportFormatTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly List<string> _dateien = new();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Eine Datei mit Kopfzeile und <paramref name="anzahl"/> Werten (Semikolon, Komma).</summary>
        private string Datei(int anzahl, Func<int, double> wert)
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-format-" + Guid.NewGuid().ToString("N") + ".csv");
            var zeilen = new List<string> { "Nr;Wert" };
            for (int i = 0; i < anzahl; i++)
                zeilen.Add((i + 1) + ";" + wert(i).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));
            File.WriteAllLines(pfad, zeilen);
            _dateien.Add(pfad);
            return pfad;
        }

        private static GanglinienProbe Probe(string pfad, GanglinienImportFormat format)
        {
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad, format);
            Assert.True(v.Lesbar);
            return v.Probe;
        }

        // ================================================================== die Formate

        [Fact]
        public void Die_Ganglinie_waehlt_Einheit_und_Raster_und_nennt_Summe_und_Nennleistung()
        {
            GanglinienImportFormat g = GanglinienImportFormat.Ganglinie;
            Assert.True(g.IstGanglinie);
            Assert.Equal("kW", g.Einheit);
            Assert.Equal(0, g.Werteanzahl);
            Assert.True(g.Einheitswahl);
            Assert.True(g.Rasterwahl);
            Assert.True(g.Summe);
            Assert.True(g.Nennleistung);
            Assert.False(g.Jahresfeld);
            Assert.Equal(GanglinienBildart.Jahr, g.Bildart);
            Assert.Equal(Resource.IMPORT_TITEL_OPTIONEN, g.Titel);
        }

        [Theory]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_TAG, 365)]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE, 8760)]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT, 12)]
        public void Das_Quellprofil_traegt_Grad_Celsius_die_feste_Werteanzahl_und_keine_Kennzahlen(string betriebsart, int anzahl)
        {
            GanglinienImportFormat q = GanglinienImportFormat.Quellprofil(betriebsart);
            Assert.False(q.IstGanglinie);
            Assert.Equal(GanglinienImportFormat.KENNUNG_QUELLPROFIL, q.Kennung);
            Assert.Equal("°C", q.Einheit);
            Assert.Equal(anzahl, q.Werteanzahl);
            Assert.False(q.Einheitswahl);
            Assert.False(q.Rasterwahl);
            Assert.False(q.Summe);
            Assert.False(q.Nennleistung);
            Assert.False(q.Jahresfeld);
            Assert.Equal(GanglinienBildart.Jahr, q.Bildart);
            Assert.Equal(Farbrolle.QUELLTEMPERATUR, q.Farbrolle);
            Assert.Equal(Resource.CHART_ACHSE_QUELLTEMPERATUR, q.Achsentitel);
            Assert.Equal(Resource.IMPORT_TITEL_QUELLPROFIL, q.Titel);
        }

        // ================================================================== Probe: Quellprofil

        [Fact]
        public void Ein_Quellprofil_mit_365_Tageswerten_wird_gelesen_und_als_Jahresprofil_mit_Temperaturachse_gezeichnet()
        {
            string pfad = Datei(365, i => 5 + i % 7 + 0.25);
            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG));

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(365, p.Werte.Length);
            Assert.Equal(5.25, p.Werte[0], 9);
            Assert.Equal(11.25, p.Werte[6], 9);
            Assert.Equal("", p.Kennzahlen);
            Assert.Equal("365 Datenzeilen gelesen → 365 Werte in °C", p.Zeile);

            // Das Bild zeigt das Jahresprofil der Simulation (8 760 Stunden), die Achse ist die Quelltemperatur.
            string svg = SvgSchreiber.Text(p.Modell);
            Assert.Contains(Resource.CHART_ACHSE_QUELLTEMPERATUR, svg);
            Assert.DoesNotContain(Resource.CHART_ACHSE_LEISTUNG, svg);
        }

        [Theory]
        [InlineData(364)]
        [InlineData(366)]
        [InlineData(8760)]
        public void Eine_abweichende_Werteanzahl_wird_benannt_abgelehnt(int anzahl)
        {
            string pfad = Datei(anzahl, i => 10);
            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG));

            Assert.False(p.Erfolgreich);
            Assert.Null(p.Modell);
            Assert.Empty(p.Werte);
            string n = anzahl.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
            Assert.Equal($"Die Datei liefert {n} Werte, erwartet sind genau 365.", p.Grund);
            Assert.StartsWith(n + " Datenzeilen gelesen → abgelehnt: ", p.Zeile);
        }

        [Fact]
        public void Die_Zeilenzahl_des_Dialogs_schneidet_ein_passendes_Fenster_aus()
        {
            string pfad = Datei(400, i => i);
            var o = GanglinienDatei.Erkenne(pfad).Vorschlag.Kopie();
            o.ErsteDatenzeile = 2 + 10;
            o.Zeilenzahl = 365;
            GanglinienProbe p = GanglinienDatei.Vorschau(pfad, o,
                GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG)).Probe;

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(10, p.Werte[0], 9);
            Assert.Equal(374, p.Werte[364], 9);
        }

        [Fact]
        public void Eine_eigene_Pruefung_lehnt_benannt_ab_und_eine_Ausnahme_steht_im_Ausnahmeprotokoll()
        {
            string pfad = Datei(12, i => i);
            var abgelehnt = new GanglinienImportFormat
            {
                Kennung = "PRUEFFORMAT", Einheit = "x", Werteanzahl = 12,
                Pruefung = w => w[0] < 1 ? "erster Wert zu klein" : ""
            };
            GanglinienProbe p = Probe(pfad, abgelehnt);
            Assert.False(p.Erfolgreich);
            Assert.Equal("erster Wert zu klein", p.Grund);

            var vermerkt = new List<string>();
            Action<string> mitschreiben = z => { lock (vermerkt) vermerkt.Add(z); };
            Ausnahmeprotokoll.Vermerkt += mitschreiben;
            try
            {
                var wirft = new GanglinienImportFormat
                {
                    Kennung = "WIRFT", Einheit = "x",
                    Pruefung = _ => throw new InvalidOperationException("Probefehler")
                };
                GanglinienProbe q = Probe(pfad, wirft);
                Assert.False(q.Erfolgreich);
                Assert.Equal("Probefehler", q.Grund);
            }
            finally { Ausnahmeprotokoll.Vermerkt -= mitschreiben; }
            lock (vermerkt)
                Assert.Contains(vermerkt, z => z.Contains("WIRFT") && z.Contains("Probefehler"));
        }

        [Fact]
        public void Ein_eigener_Bildzeichner_ersetzt_das_Jahresbild()
        {
            string pfad = Datei(24, i => i);
            Zeichenmodell eigen = ChartRenderer.JahresverlaufModell("Tag", new double[24], "y", Farbrolle.BEDARF);
            var tag = new GanglinienImportFormat
            {
                Kennung = "TAGESBILD", Einheit = "l/h", Werteanzahl = 24, Bildart = GanglinienBildart.Tag,
                Bildzeichner = _ => eigen
            };
            GanglinienProbe p = Probe(pfad, tag);
            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Same(eigen, p.Modell);
        }

        // ================================================================== Probe: Ganglinie

        [Fact]
        public void Die_Ganglinie_geht_ihren_Weg_und_nennt_Jahressumme_und_Hoechstleistung()
        {
            string pfad = Datei(8760, i => i == 100 ? 7.5 : 1.0);
            GanglinienProbe ohne = Probe(pfad, null);
            GanglinienProbe mit = Probe(pfad, GanglinienImportFormat.Ganglinie);

            Assert.True(mit.Erfolgreich, mit.Grund);
            Assert.Equal(ohne.Zeile, mit.Zeile);
            Assert.Equal(ohne.WerteKw, mit.WerteKw);
            Assert.Equal("Jahressumme 8.766 kWh · Höchstleistung 7,5 kW", mit.Kennzahlen);
        }

        // ================================================================== Optionen

        [Fact]
        public void Das_Jahr_reist_mit_der_Kopie_der_Optionen()
        {
            var o = new GanglinienImportOptionen { Jahr = 2025, Zeilenzahl = 8760 };
            GanglinienImportOptionen k = o.Kopie();
            Assert.Equal(2025, k.Jahr);
            Assert.Equal(0, new GanglinienImportOptionen().Jahr);
        }
    }
}
