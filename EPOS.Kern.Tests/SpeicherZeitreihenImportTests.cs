using System;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    public class SpeicherZeitreihenImportTests
    {
        private static byte[] Csv(string text) => Encoding.UTF8.GetBytes(text.Replace("\n", "\r\n"));

        private static SpeicherZeitreihenOptionen Optionen(
            SpeicherZeitreihenRolle rolle = SpeicherZeitreihenRolle.Last,
            SpeicherZeitreihenEinheit einheit = SpeicherZeitreihenEinheit.Kilowatt)
            => new SpeicherZeitreihenOptionen
            {
                Rolle = rolle,
                Einheit = einheit,
                Trennzeichen = ';',
                Dezimaltrenner = '.',
                Kopfzeile = true,
                ZeitstempelSpalte = 0,
                WertSpalte = 1,
                ZeitzoneId = "Europe/Berlin",
                Konvention = IntervallKonvention.Anfang
            };

        [Fact]
        public void Semikolon_Dezimalkomma_und_freie_Spaltenwahl_werden_gelesen()
        {
            SpeicherZeitreihenOptionen o = Optionen();
            o.Dezimaltrenner = ',';
            o.ZeitstempelSpalte = 2;
            o.WertSpalte = 0;
            byte[] csv = Csv("Ganz freie Leistung;Ignoriert;Beliebige Zeit\n" +
                "1,25;x;2026-01-01 00:00\n2,5;y;2026-01-01 00:15\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "last.csv", o);

            Assert.Equal(new[] { 1.25, 2.5 }, r.Werte);
            Assert.Equal("last.csv", r.QuelleName);
            Assert.Equal(64, r.SHA256.Length);
            Assert.Equal(TimeSpan.Zero, r.ZeitstempelUtc[0].Offset);
            Assert.Equal(new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[0]);
        }

        [Fact]
        public void Getrennte_Datums_und_Uhrzeitspalten_sowie_uebersprungene_Zeile_funktionieren()
        {
            SpeicherZeitreihenOptionen o = Optionen();
            o.ZuUeberspringendeZeilen = 1;
            o.Kopfzeile = true;
            o.ZeitstempelSpalte = -1;
            o.DatumSpalte = 2;
            o.UhrzeitSpalte = 0;
            o.WertSpalte = 1;
            byte[] csv = Csv("Export von Zaehler 7\nUhr;Wert;Tag\n" +
                "00:00;4;01.01.2026\n00:15;5;01.01.2026\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "getrennt.csv", o);

            Assert.Equal(new[] { 4.0, 5.0 }, r.Werte);
            Assert.Equal(new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[0]);
        }

        [Fact]
        public void Stundenleistung_wird_auf_vier_Viertelstunden_wiederholt()
        {
            byte[] csv = Csv("Zeit;Last\n2026-01-01T00:00Z;8\n2026-01-01T01:00Z;12\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "h.csv", Optionen());

            Assert.Equal(8, r.Werte.Length);
            Assert.Equal(new[] { 8.0, 8.0, 8.0, 8.0, 12.0, 12.0, 12.0, 12.0 }, r.Werte);
            Assert.Equal(TimeSpan.FromMinutes(15), r.ZeitstempelUtc[1] - r.ZeitstempelUtc[0]);
        }

        [Fact]
        public void Kwh_je_Stunde_behaelt_beim_Aufteilen_die_Energie()
        {
            SpeicherZeitreihenOptionen o = Optionen(einheit:
                SpeicherZeitreihenEinheit.KilowattstundeJeIntervall);
            byte[] csv = Csv("Zeit;Energie\n2026-01-01T00:00Z;2\n2026-01-01T01:00Z;3\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "energie.csv", o);
            double energieNachher = r.Werte.Sum() * 0.25;

            Assert.Equal(5.0, energieNachher, 12);
            Assert.Equal(new[] { 2.0, 2.0, 2.0, 2.0, 3.0, 3.0, 3.0, 3.0 }, r.Werte);
        }

        [Fact]
        public void Negative_Bezugspreise_sind_erlaubt_und_Cent_wird_in_Euro_umgerechnet()
        {
            SpeicherZeitreihenOptionen o = Optionen(SpeicherZeitreihenRolle.Bezug,
                SpeicherZeitreihenEinheit.CentJeKilowattstunde);
            byte[] csv = Csv("Zeit;Preis\n2026-01-01T00:00+01:00;-5\n" +
                "2026-01-01T01:00+01:00;10\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "preis.csv", o);

            Assert.Equal(-0.05, r.Werte[0], 12);
            Assert.Equal(0.10, r.Werte[4], 12);
            Assert.Equal(SpeicherZeitreihenRolle.Bezug, r.Rolle);
        }

        [Fact]
        public void Negative_Pv_Werte_werden_abgewiesen()
        {
            SpeicherZeitreihenOptionen o = Optionen(SpeicherZeitreihenRolle.Pv);
            byte[] csv = Csv("Zeit;PV\n2026-01-01T00:00Z;-1\n2026-01-01T00:15Z;0\n");

            FormatException ex = Assert.Throws<FormatException>(() =>
                SpeicherZeitreihenImport.Lesen(csv, "pv.csv", o));

            Assert.Contains("duerfen nicht negativ", ex.Message);
        }

        [Fact]
        public void Iso_Offsets_machen_die_doppelte_Herbststunde_eindeutig()
        {
            SpeicherZeitreihenOptionen o = Optionen(SpeicherZeitreihenRolle.Bezug,
                SpeicherZeitreihenEinheit.EuroJeKilowattstunde);
            o.ZeitzoneId = "wird-bei-expliziten-offsets-nicht-verwendet";
            byte[] csv = Csv("Zeit;Preis\n2026-10-25T02:00+02:00;0.1\n" +
                "2026-10-25T02:00+01:00;0.2\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "dst.csv", o);

            Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[0]);
            Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 45, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[7]);
        }

        [Fact]
        public void Mehrdeutige_Ortszeit_ohne_Offset_wird_strikt_abgewiesen()
        {
            byte[] csv = Csv("Zeit;Last\n2026-10-25 01:45;1\n2026-10-25 02:00;1\n");

            FormatException ex = Assert.Throws<FormatException>(() =>
                SpeicherZeitreihenImport.Lesen(csv, "mehrdeutig.csv", Optionen()));

            Assert.Contains("mehrdeutig", ex.Message);
        }

        [Fact]
        public void Intervallende_wird_auf_Intervallanfang_verschoben()
        {
            SpeicherZeitreihenOptionen o = Optionen();
            o.Konvention = IntervallKonvention.Ende;
            byte[] csv = Csv("Zeit;Last\n2026-01-01T00:15Z;1\n2026-01-01T00:30Z;2\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "ende.csv", o);

            Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[0]);
            Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 15, 0, TimeSpan.Zero),
                r.ZeitstempelUtc[1]);
        }

        [Fact]
        public void Luecken_und_Dubletten_werden_abgewiesen()
        {
            byte[] luecke = Csv("Zeit;Last\n2026-01-01T00:00Z;1\n" +
                "2026-01-01T00:15Z;1\n2026-01-01T00:45Z;1\n");
            byte[] dublette = Csv("Zeit;Last\n2026-01-01T00:00Z;1\n" +
                "2026-01-01T00:15Z;1\n2026-01-01T00:15Z;2\n");

            Assert.Contains("Fehlendes oder doppeltes Intervall",
                Assert.Throws<FormatException>(() =>
                    SpeicherZeitreihenImport.Lesen(luecke, "luecke.csv", Optionen())).Message);
            Assert.Contains("Doppelter oder rueckwaerts",
                Assert.Throws<FormatException>(() =>
                    SpeicherZeitreihenImport.Lesen(dublette, "doppelt.csv", Optionen())).Message);
        }

        [Fact]
        public void Schaltjahr_bleibt_auf_der_echten_Zeitachse_erhalten()
        {
            byte[] csv = Csv("Zeit;Last\n2028-02-29T23:45Z;1\n2028-03-01T00:00Z;2\n");

            SpeicherZeitreihe r = SpeicherZeitreihenImport.Lesen(csv, "schaltjahr.csv", Optionen());

            Assert.Equal(29, r.ZeitstempelUtc[0].Day);
            Assert.Equal(2, r.Werte.Length);
        }

        [Fact]
        public void Zahlenformat_wird_nicht_aus_CurrentCulture_geraten()
        {
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                byte[] csv = Csv("Zeit;Last\n2026-01-01T00:00Z;1,234\n2026-01-01T00:15Z;2,5\n");
                SpeicherZeitreihenOptionen o = Optionen();
                o.Dezimaltrenner = '.';

                Assert.Throws<FormatException>(() =>
                    SpeicherZeitreihenImport.Lesen(csv, "kultur.csv", o));
            }
            finally
            {
                CultureInfo.CurrentCulture = vorher;
            }
        }

        [Fact]
        public void Vorschau_beachtet_Anfuehrungszeichen_und_Trennzeichen_im_Feld()
        {
            byte[] csv = Csv("Zeit;Beschreibung;Last\n" +
                "2026-01-01T00:00Z;\"Werk; Nord\";1\n");

            SpeicherZeitreihenVorschau v = SpeicherZeitreihenImport.Vorschau(csv, Optionen());

            Assert.Equal(3, v.Spaltenzahl);
            Assert.Equal("Werk; Nord", v.Zeilen[1][1]);
        }

        [Fact]
        public void Windows1252_wird_explizit_dekodiert()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SpeicherZeitreihenOptionen o = Optionen();
            o.Encoding = SpeicherZeitreihenEncoding.Windows1252;
            byte[] csv = Encoding.GetEncoding(1252).GetBytes(
                "Zeit;Beschreibung;Last\r\n2026-01-01T00:00Z;Preis in €;1\r\n");

            SpeicherZeitreihenVorschau v = SpeicherZeitreihenImport.Vorschau(csv, o);

            Assert.Equal("Preis in €", v.Zeilen[1][1]);
        }
    }
}
