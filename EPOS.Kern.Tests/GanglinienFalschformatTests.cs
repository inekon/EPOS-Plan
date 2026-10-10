using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kein Absturz bei falschem Dateiformat</b> (IM-1): Die Leser der CSV-Importe
    /// (<see cref="GanglinienDatei"/>, <see cref="StundenganglinieDatei"/>, die Import-Controller
    /// der PV- und Solarganglinie) liefern bei einer falschen Datei ein Ergebnis mit benanntem
    /// Befund — nie eine Ausnahme. Ohne Datenbank, je Fall eine Datei im Temp-Ordner.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GanglinienFalschformatTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-im1-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public GanglinienFalschformatTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
            _kultur.Dispose();
        }

        private string Datei(string name, byte[] inhalt)
        {
            string pfad = Path.Combine(_ordner, name);
            File.WriteAllBytes(pfad, inhalt);
            return pfad;
        }

        private string Text(string name, string inhalt, Encoding? kodierung = null)
            => Datei(name, (kodierung ?? new UTF8Encoding(false)).GetPreamble()
                           .Concat((kodierung ?? new UTF8Encoding(false)).GetBytes(inhalt)).ToArray());

        private static string Zeilen(int anzahl, Func<int, string> zeile, string? kopf = null)
        {
            var sb = new StringBuilder();
            if (kopf != null) sb.Append(kopf).Append('\n');
            for (int i = 0; i < anzahl; i++) sb.Append(zeile(i)).Append('\n');
            return sb.ToString();
        }

        /// <summary>Die falschen Dateien: Name → Inhalt.</summary>
        public static IEnumerable<object[]> FalscheDateien()
        {
            yield return new object[] { "leer" };
            yield return new object[] { "nur_leerzeilen" };
            yield return new object[] { "reiner_text" };
            yield return new object[] { "spalten_ab_zeile" };
            yield return new object[] { "zehn_zeilen" };
            yield return new object[] { "nur_kopf" };
            yield return new object[] { "binaer" };
            yield return new object[] { "csv_als_xlsx" };
            yield return new object[] { "zip_als_csv" };
            yield return new object[] { "eine_lange_zeile" };
            yield return new object[] { "anfuehrung_offen" };
        }

        private string FalscheDatei(string fall)
        {
            switch (fall)
            {
                case "leer": return Datei(fall + ".csv", Array.Empty<byte>());
                case "nur_leerzeilen": return Text(fall + ".csv", "\n\n  \n\r\n");
                case "reiner_text": return Text(fall + ".csv",
                    "Dies ist ein Brief.\nEr enthaelt keine Zahlen,\nsondern nur Saetze; und Zeichen.\n");
                case "spalten_ab_zeile": return Text(fall + ".csv",
                    Zeilen(8760, i => i < 100 ? "01.01.2026 00:00;" + (i % 7) + ".5" : "nur ein Feld", "Zeit;Leistung"));
                case "zehn_zeilen": return Text(fall + ".csv", Zeilen(10, i => (i * 1.5).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                case "nur_kopf": return Text(fall + ".csv", "Zeit;Leistung kW\n");
                case "binaer":
                {
                    var b = new byte[4096];
                    new Random(7).NextBytes(b);
                    return Datei(fall + ".csv", b);
                }
                case "csv_als_xlsx": return Text(fall + ".xlsx", Zeilen(8760, i => "1.0"));
                case "zip_als_csv": return Datei(fall + ".csv",
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0xFF, 0xFE, 0x00, 0x01 });
                case "eine_lange_zeile": return Text(fall + ".csv", string.Join(";", Enumerable.Repeat("1,5", 8760)));
                case "anfuehrung_offen": return Text(fall + ".csv", Zeilen(8760, i => i == 3 ? "\"1;2" : "1;2"));
                default: throw new ArgumentException(fall);
            }
        }

        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void Stundenganglinie_liefert_Befund_statt_Ausnahme(string fall)
        {
            string pfad = FalscheDatei(fall);
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.False(l.Erfolgreich);
            Assert.NotNull(l.ErsterFehler);
            Assert.False(string.IsNullOrWhiteSpace(GanglinienProtokollText.Text(l.ErsterFehler)));
        }

        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void GanglinienDatei_liefert_Befund_statt_Ausnahme(string fall)
        {
            string pfad = FalscheDatei(fall);
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
            GanglinienRohdaten r = GanglinienDatei.Lies(pfad, v.Vorschlag);
            GanglinienVorschau v2 = GanglinienDatei.Vorschau(pfad, v.Vorschlag);
            Assert.NotNull(v2);
            Assert.True(!v.Lesbar || !r.Erfolgreich || r.Werte.Length != 8760,
                        "eine falsche Datei darf keine Jahresreihe ergeben");
        }

        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void PvGanglinie_Vorschlag_und_Import_lehnen_benannt_ab(string fall)
        {
            string pfad = FalscheDatei(fall);
            PvGanglinieVorschlag v = PvGanglinieImportCtrl.Vorschlagen(pfad);
            Assert.Null(v.VorschlagKwp);
            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(pfad, null);
            Assert.False(b.Erfolgreich);
            Assert.True(b.IstFehler);
            Assert.False(string.IsNullOrWhiteSpace(b.Meldung));
            Assert.StartsWith("Datei \u201e" + Path.GetFileName(pfad) + "\u201c: ", b.Meldung);
        }

        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void Solarganglinie_Import_lehnt_benannt_ab(string fall)
        {
            string pfad = FalscheDatei(fall);
            SolarganglinieImportBericht b = SolarganglinieImportCtrl.Einlesen(pfad);
            Assert.False(b.Erfolgreich);
            Assert.False(string.IsNullOrWhiteSpace(b.Meldung));
        }
        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void Importablauf_endet_mit_Fehler_und_Protokoll(string fall)
        {
            string pfad = FalscheDatei(fall);
            foreach (GanglinienRaster raster in new[] { GanglinienRaster.Unbekannt, GanglinienRaster.Stunde,
                                                        GanglinienRaster.Viertelstunde, GanglinienRaster.Minute })
            {
                IList<PruefMeldung>? vorgelegt = null;
                var rueckrufe = new GanglinienImportRueckrufe
                {
                    Optionen = (p, v) =>
                    {
                        GanglinienImportOptionen o = v.Vorschlag.Kopie();
                        o.Raster = raster;
                        return System.Threading.Tasks.Task.FromResult(o);
                    },
                    Protokoll = (m, moeglich, bestaetigen) =>
                    {
                        vorgelegt = m;
                        return System.Threading.Tasks.Task.FromResult(true);
                    }
                };
                GanglinienImportErgebnis e = GanglinienImportAblauf.OhneAblage(pfad, rueckrufe).GetAwaiter().GetResult();
                Assert.NotEqual(ImportAusgang.Erfolg, e.Ausgang);
                Assert.NotNull(vorgelegt);
                Assert.Contains(vorgelegt!, m => m.Stufe == PruefStufe.Fehler);
            }
        }
        [Theory]
        [MemberData(nameof(FalscheDateien))]
        public void Katalogleser_der_Dateiimporte_werfen_nicht(string fall)
        {
            string pfad = FalscheDatei(fall);
            var cec = new CECDataService().LoadFromFile(pfad);
            Assert.False(cec.success && fall == "leer");
            var wr = new CecWechselrichterDienst().AusDatei(pfad);
            Assert.False(wr.Erfolg && fall == "leer");
            var ond = new OndWechselrichterDienst().AusDatei(pfad);
            Assert.False(ond.Erfolg && fall == "leer");
            var sp = new CecSpeicherImport().AusDatei(pfad);
            Assert.False(sp.Erfolg && fall == "leer");
            var bs = new BslibImport().AusDatei(pfad);
            Assert.False(bs.Erfolg && fall == "leer");
            // Der Leser des Quellprofils ist nachsichtig (letzte Zahl je Zeile): 8 760 lesbare Zeilen sind ihm recht.
            if (fall != "csv_als_xlsx" && fall != "anfuehrung_offen")
                Assert.Null(WaermequelleClass.WerteAusCsv(pfad, 8760));
        }
        [Fact]
        public void Binaerdatei_heisst_keine_Textdatei()
        {
            string pfad = FalscheDatei("binaer");
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.Equal(GanglinienDatei.SchluesselKeinText, l.ErsterFehler!.Schluessel);
            Assert.Contains("keine Textdatei", StundenganglinieDatei.Ablehnungstext(pfad, l));
            GanglinienRohdaten r = GanglinienDatei.Lies(pfad, new GanglinienImportOptionen());
            Assert.Contains(r.Meldungen, m => m.Schluessel == GanglinienDatei.SchluesselKeinText);
        }

        [Fact]
        public void Spaltenzahl_ab_Zeile_n_nennt_die_Zeile()
        {
            string pfad = FalscheDatei("spalten_ab_zeile");
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.Equal(GanglinienDatei.SchluesselSpalteFehlt, l.ErsterFehler!.Schluessel);
            Assert.Equal("102", l.ErsterFehler.Werte[0]);
        }

        [Fact]
        public void Zehn_Zeilen_passen_zu_keinem_Raster()
        {
            StundenganglinieLesung l = StundenganglinieDatei.Lies(FalscheDatei("zehn_zeilen"));
            Assert.Equal(StundenganglinieDatei.SchluesselAnzahl, l.ErsterFehler!.Schluessel);
            Assert.Equal("10", l.ErsterFehler.Werte[0]);
        }

        [Fact]
        public void Kopfzeile_ohne_Daten_ist_leer()
        {
            StundenganglinieLesung l = StundenganglinieDatei.Lies(FalscheDatei("nur_kopf"));
            Assert.Equal(GanglinienDatei.SchluesselDateiLeer, l.ErsterFehler!.Schluessel);
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        [InlineData("1e400")]
        public void Nicht_endliche_Werte_sind_keine_Zahl(string wert)
        {
            Assert.False(GanglinienDatei.VersucheZahl(wert, '.', out double w));
            Assert.Equal(0.0, w);
            string pfad = Text("nan.csv", Zeilen(8760, i => i == 7 ? wert : "1.5"));
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.False(l.Erfolgreich);
            Assert.Equal(GanglinienDatei.SchluesselZahlUnlesbar, l.ErsterFehler!.Schluessel);
            Assert.Equal("8", l.ErsterFehler.Werte[0]);
        }

        [Fact]
        public void Langer_Feldinhalt_wird_in_der_Meldung_gekuerzt()
        {
            string lang = new string('x', 300);
            string pfad = Text("lang.csv", Zeilen(8760, i => i == 5 ? lang + "\u0001" : "1.5"));
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.False(l.Erfolgreich);
            string text = GanglinienProtokollText.Text(l.ErsterFehler);
            Assert.True(text.Length < 120, text);
            Assert.Contains("…", text);
        }

        [Fact]
        public void Dezimalkomma_wird_gelesen()
        {
            string pfad = Text("komma.csv", Zeilen(8760, i => (i % 3) + ",25"), null);
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.True(l.Erfolgreich, StundenganglinieDatei.Ablehnungstext(pfad, l));
            Assert.Equal(1.25, l.StundenwerteKw[1], 9);
        }

        [Fact]
        public void Utf8_mit_BOM_wird_gelesen()
        {
            string pfad = Text("bom.csv", Zeilen(8760, i => "2.5", "Leistung Süd kW"), new UTF8Encoding(true));
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.True(l.Erfolgreich, StundenganglinieDatei.Ablehnungstext(pfad, l));
            Assert.Equal("Leistung Süd kW", l.Kopftext);
        }

        [Fact]
        public void Windows1252_mit_Euro_wird_ohne_Ausnahme_gelesen()
        {
            // 0x80 ist in Windows-1252 das Euro-Zeichen, in Latin-1 ein Steuerzeichen: Die Codepage
            // steht nach der einmaligen Registrierung des Anbieters ohne Ausnahme bereit.
            Encoding cp = AnsiEncoding.Get();
            Assert.Equal(1252, cp.CodePage);
            byte[] kopf = { (byte)'P', (byte)'r', (byte)'e', (byte)'i', (byte)'s', (byte)' ', 0x80, (byte)' ', 0xFC, (byte)'\n' };
            byte[] daten = Encoding.ASCII.GetBytes(Zeilen(8760, i => "3.0"));
            string pfad = Datei("euro.csv", kopf.Concat(daten).ToArray());
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.True(l.Erfolgreich, StundenganglinieDatei.Ablehnungstext(pfad, l));
            Assert.Equal("Preis € ü", l.Kopftext);
        }
    }
}
