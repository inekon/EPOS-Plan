using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Dezimalerkennung und Rückweg der Ganglinienimporte</b>: Die Formaterkennung leitet den
    /// Dezimaltrenner aus den Zahlenfeldern ab („0.0“, „11.013“ → Punkt; „11,013“, „1.234,5“ → Komma;
    /// „1,234.5“ → Punkt), ein Komma im Text der Kopfzeile entscheidet nichts, und eine laufende Nummer
    /// ist keine Wertspalte. Rückweg: Eine mit <see cref="ZeitreihenCsv"/> exportierte Stunden- oder
    /// Viertelstundenreihe wird über Erkennung und Importkette wieder eingelesen und ergibt dieselben
    /// Werte — unter de-DE und en-US, mit Dezimalkomma wie exportiert und mit Dezimalpunkt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GanglinienDezimalerkennungTests : IDisposable
    {
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-dezimal-" + Guid.NewGuid().ToString("N"));

        public GanglinienDezimalerkennungTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch (IOException) { }
        }

        private string Datei(string name, string inhalt)
        {
            string pfad = Path.Combine(_ordner, name);
            File.WriteAllText(pfad, inhalt);
            return pfad;
        }

        [Theory]
        [InlineData("0.0", '.')]
        [InlineData("11.013", '.')]
        [InlineData("11,013", ',')]
        [InlineData("1.234,5", ',')]
        [InlineData("1,234.5", '.')]
        [InlineData("0.013", '.')]
        [InlineData("11,5", ',')]
        [InlineData("1.234.567,25", ',')]
        public void Der_Dezimaltrenner_folgt_aus_den_Werten(string wert, char erwartet)
        {
            string pfad = Datei("d.csv", "Stunde;Wert\r\n1;" + wert + "\r\n2;" + wert + "\r\n3;" + wert + "\r\n");
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
            Assert.True(v.Lesbar);
            Assert.Equal(erwartet, v.Vorschlag.Dezimaltrenner);
        }

        [Fact]
        public void Die_Datei_des_Anwenders_liest_mit_Punkt_und_ueberspringt_die_laufende_Nummer()
        {
            // Viertelstunde;Strombedarf;Photovoltaik mit Werten wie 11.013 und 0.0, dazu ein Komma im Kopftext.
            string pfad = Datei("Strombedarf_Photovoltaik.csv",
                "Viertelstunde;Strombedarf, Last;Photovoltaik\r\n1;11.013;0.0\r\n2;10.5;0.0\r\n3;9.875;0.0\r\n4;11.013;0.25\r\n");
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
            Assert.Equal('.', v.Vorschlag.Dezimaltrenner);
            Assert.True(v.Vorschlag.Kopfzeile);
            Assert.Equal(1, v.Vorschlag.WertSpalte);
        }

        public static TheoryData<string, Zeitraster, bool> Rueckwege() => new()
        {
            { "de-DE", Zeitraster.Stunde, false },
            { "de-DE", Zeitraster.Viertelstunde, false },
            { "en-US", Zeitraster.Stunde, false },
            { "en-US", Zeitraster.Viertelstunde, false },
            { "de-DE", Zeitraster.Stunde, true },
            { "en-US", Zeitraster.Viertelstunde, true },
        };

        [Theory]
        [MemberData(nameof(Rueckwege))]
        public async Task Eine_exportierte_Reihe_liest_ueber_Erkennung_und_Ablauf_dieselben_Werte(
            string kultur, Zeitraster raster, bool mitPunkt)
        {
            using var k = new Kulturvorrichtung(kultur);
            int n = raster == Zeitraster.Viertelstunde ? 35040 : 8760;
            double[] last = Enumerable.Range(0, n).Select(i => 9.0 + (i % 7) * 0.25 + (i % 5) * 0.013).ToArray();
            double[] pv = Enumerable.Range(0, n).Select(i => (i % 24) < 6 ? 0.0 : (i % 11) * 1.125).ToArray();
            string text = ZeitreihenCsv.Text(raster, new List<ZeitreihenSpalte>
            {
                new("Strombedarf", "kW", last),
                new("Photovoltaik", "kW", pv)
            });
            if (mitPunkt) text = text.Replace(',', '.');
            string pfad = Datei("Rueckweg.csv", text);

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(pfad, new GanglinienImportRueckrufe
            {
                Optionen = (_, v) => Task.FromResult(v.Vorschlag),
                Protokoll = (_, moeglich, _) => Task.FromResult(moeglich)
            });

            Assert.True(erg.Erfolgreich, erg.Meldung + " " + string.Join(" | ", erg.Protokoll.Select(m => m.Schluessel)));
            Assert.Equal(mitPunkt ? '.' : ',', erg.Optionen.Dezimaltrenner);
            Assert.Equal(1, erg.Optionen.WertSpalte);
            Assert.Equal(raster == Zeitraster.Viertelstunde ? 4 : 1, erg.Zeitinterval);
            Assert.Equal(n, erg.Werte.Length);
            for (int i = 0; i < n; i++) Assert.Equal(last[i], erg.Werte[i], 9);

            // Der Katalogimport (PV-, Solarganglinie) liest dieselbe Datei auf seinem Weg ebenso.
            StundenganglinieLesung lesung = StundenganglinieDatei.Lies(pfad);
            Assert.True(lesung.Erfolgreich);
            Assert.Equal(n, lesung.WerteImDateirasterKw.Length);
            for (int i = 0; i < n; i++) Assert.Equal(last[i], lesung.WerteImDateirasterKw[i], 9);
        }
    }
}
