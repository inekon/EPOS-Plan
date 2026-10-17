using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Importleser der Solarthermie-Ganglinie</b> (<see cref="StundenganglinieDatei"/>,
    /// Auftrag D1): drei Trennzeichen, Dezimalkomma, 8 760 und 35 040 Werte mit
    /// Stundenmittel, Kopfzeile, Zeitstempelspalte, die einspaltige Textdatei und die
    /// Fehlerfälle — ohne Datenbank, je Fall eine Datei im Temp-Ordner.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class StundenganglinieDateiTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-sgl-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public StundenganglinieDateiTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Die Leistung der Stunde <paramref name="h"/>: ein Tagesbogen, nachts 0.</summary>
        private static double Wert(int h)
        {
            int stunde = h % 24;
            return stunde >= 6 && stunde < 18 ? (stunde - 5) * 1.25 : 0.0;
        }

        private string Datei(string name, Func<int, string> zeile, int anzahl, string? kopf = null)
        {
            var sb = new StringBuilder();
            if (kopf != null) sb.Append(kopf).Append('\n');
            for (int i = 0; i < anzahl; i++) sb.Append(zeile(i)).Append('\n');
            string pfad = Path.Combine(_ordner, name);
            File.WriteAllText(pfad, sb.ToString(), new UTF8Encoding(false));
            return pfad;
        }

        private static string Zahl(double w, char dezimal)
        {
            string t = w.ToString("0.00", CultureInfo.InvariantCulture);
            return dezimal == ',' ? t.Replace('.', ',') : t;
        }

        private static double Summe() => Enumerable.Range(0, 8760).Sum(Wert);

        [Theory]
        [InlineData(';', ',')]
        [InlineData('\t', ',')]
        [InlineData('|', '.')]
        [InlineData(',', '.')]
        public void Trennzeichen_und_Zeitstempel_werden_erkannt(char trenn, char dezimal)
        {
            var start = new DateTime(2025, 1, 1);
            string pfad = Datei("tz.csv",
                i => start.AddHours(i).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                     + trenn + Zahl(Wert(i), dezimal),
                8760, "Zeit" + trenn + "Leistung [kW]");

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal(trenn, l.Format.Trennzeichen);
            Assert.Equal(dezimal, l.Format.Dezimaltrenner);
            Assert.True(l.Format.Kopfzeile);
            Assert.Equal(0, l.Format.ZeitSpalte);
            Assert.Equal(1, l.Format.WertSpalte);
            Assert.Equal("Leistung [kW]", l.Kopftext);
            Assert.Equal(8760, l.AnzahlWerte);
            Assert.Equal(GanglinienRaster.Stunde, l.Raster);
            Assert.Equal(Wert(10), l.StundenwerteKw[10], 9);
            Assert.Equal(Summe() / 1000.0, l.JahresarbeitMwh, 6);
            Assert.Equal(Enumerable.Range(0, 24).Max(Wert), l.SpitzeKw, 9);
        }

        [Fact]
        public void Viertelstunden_werden_je_Stunde_gemittelt_nicht_summiert()
        {
            // Je Stunde die vier Viertelstunden w-1, w, w, w+1: Mittel w, Summe 4w.
            string pfad = Datei("vs.csv", i =>
            {
                double w = Wert(i / 4);
                int q = i % 4;
                return Zahl(q == 0 ? w - 1 : q == 3 ? w + 1 : w, ',');
            }, 35040, "Leistung");

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal(35040, l.AnzahlWerte);
            Assert.Equal(GanglinienRaster.Viertelstunde, l.Raster);
            Assert.Equal(8760, l.StundenwerteKw.Length);
            Assert.Equal(Wert(12), l.StundenwerteKw[12], 9);
            Assert.Equal(Summe() / 1000.0, l.JahresarbeitMwh, 6);
        }

        [Fact]
        public void Die_einspaltige_Textdatei_mit_Beschreibung_bleibt_lesbar()
        {
            string pfad = Datei("alt.txt", i => Zahl(Wert(i), '.'), 8760,
                                "Solarganglinie Sued 45 Grad, Leistung Solarsystem [W]");

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal('\0', l.Format.Trennzeichen);
            Assert.True(l.Format.Kopfzeile);
            Assert.Equal("Solarganglinie Sued 45 Grad, Leistung Solarsystem [W]", l.Kopftext);
            Assert.Equal(8760, l.AnzahlWerte);
        }

        [Fact]
        public void Eine_Spalte_mit_Dezimalkomma_ohne_Kopfzeile_ist_kein_Kommatrenner()
        {
            // Jeder Wert mit Nachkommastelle: Die Erkennung saehe "12,5" als zwei Felder.
            string pfad = Datei("komma.txt", i => Zahl(Wert(i) + 0.5, ','), 8760);

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal('\0', l.Format.Trennzeichen);
            Assert.Equal(',', l.Format.Dezimaltrenner);
            Assert.False(l.Format.Kopfzeile);
            Assert.Equal(Wert(10) + 0.5, l.StundenwerteKw[10], 9);
        }

        [Fact]
        public void Eine_falsche_Wertzahl_ist_ein_benannter_Fehler()
        {
            string pfad = Datei("kurz.csv", i => Zahl(Wert(i), '.'), 8000);

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.False(l.Erfolgreich);
            Assert.Equal(StundenganglinieDatei.SchluesselAnzahl, l.ErsterFehler!.Schluessel);
            Assert.Equal("8000", l.ErsterFehler.Werte[0]);
            Assert.Contains("8000", GanglinienProtokollText.Text(l.ErsterFehler));
        }

        [Fact]
        public void Eine_fehlende_oder_leere_Datei_ist_ein_Fehler()
        {
            Assert.False(StundenganglinieDatei.Lies(Path.Combine(_ordner, "gibtsnicht.csv")).Erfolgreich);
            Assert.False(StundenganglinieDatei.Lies("").Erfolgreich);

            string leer = Path.Combine(_ordner, "leer.csv");
            File.WriteAllText(leer, "\n\n");
            StundenganglinieLesung l = StundenganglinieDatei.Lies(leer);
            Assert.False(l.Erfolgreich);
            Assert.NotNull(l.ErsterFehler);
        }

        [Fact]
        public void Eine_unlesbare_Zahl_ist_ein_Fehler()
        {
            string pfad = Datei("text.csv", i => i == 100 ? "abc" : Zahl(Wert(i), '.'), 8760, "Leistung");

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.False(l.Erfolgreich);
            Assert.NotNull(l.ErsterFehler);
        }

        [Fact]
        public void Die_Protokollzeile_nennt_Format_Anzahl_Raster_Jahresarbeit_und_Spitze()
        {
            string pfad = Datei("prot.csv", i => i.ToString(CultureInfo.InvariantCulture) + ";" + Zahl(Wert(i / 4), ','),
                                35040, "Nr;Leistung");

            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);
            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));

            string zeile = SolarganglinieImportCtrl.Protokolltext(l);

            Assert.Contains("Trennzeichen ;", zeile);
            Assert.Contains("Dezimalzeichen ,", zeile);
            Assert.Contains("35.040 Werte", zeile);
            Assert.Contains("Viertelstundenraster", zeile);
            Assert.Contains((Summe() / 1000.0).ToString("N1", CultureInfo.GetCultureInfo("de-DE")) + " MWh", zeile);
            Assert.Contains("Spitze 15,0 kW", zeile);
        }

        /// <summary>
        /// Die ganze Kette gegen eine Kopie der Testdatenbank: lesen, schreiben, die
        /// Dublette als Hinweis ohne zweiten Satz; Bezeichner aus dem Dateinamen,
        /// Beschreibung aus der Kopfzeile.
        /// </summary>
        [Fact]
        public void Der_Import_schreibt_einmal_und_meldet_die_Dublette()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = "D1-SG-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string pfad = Datei(name + ".csv", i => Zahl(Wert(i), ','), 8760, "Kollektorfeld Sued");

            SolarganglinieImportBericht b = SolarganglinieImportCtrl.Einlesen(pfad);
            Assert.True(b.Erfolgreich, b.Meldung);
            Assert.Equal(name, b.Bezeichner);
            Assert.Contains("8.760 Werte", b.Protokoll);

            var ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();
            SolarganglinieModel satz = ctrl.items.Single(m => m.m_szBezeichner == name);
            Assert.Equal("Kollektorfeld Sued", satz.m_szBeschreibung);

            SolarganglinieImportBericht zweiter = SolarganglinieImportCtrl.Einlesen(pfad);
            Assert.False(zweiter.Erfolgreich);
            Assert.False(zweiter.IstFehler);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SGAD_MSG_VORHANDEN, zweiter.Meldung);
            ctrl.ReadAll();
            Assert.Single(ctrl.items, m => m.m_szBezeichner == name);

            Assert.True(ctrl.Delete(name));
        }

        [Fact]
        public void Ohne_Pfad_meldet_der_Import_den_Grund()
        {
            SolarganglinieImportBericht b = SolarganglinieImportCtrl.Einlesen("");
            Assert.False(b.Erfolgreich);
            Assert.True(b.IstFehler);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.IMP_TXT_KEIN_PFAD, b.Meldung);
        }
    }
}
