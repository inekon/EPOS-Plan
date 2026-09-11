using System;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    public class SpeicherAuslegungRechnungTests
    {
        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 100,
            PKw = 50,
            SoCMinKwh = 10,
            SoCMaxKwh = 90,
            RoundTripWirkungsgrad = 0.9,
            CCapEurProKwh = 999,
            CPowEurProKw = 888,
            IFixEur = 777,
            Kapitalzins = 0.04,
            NutzungsdauerA = 15
        };

        private static SpeicherKostensaetze DirekteKosten() => new SpeicherKostensaetze
        {
            InvestEurProKw = 10,
            InvestEurProKwh = 20,
            BetriebEurProKwJahr = 1,
            BetriebEurProKwhJahr = 2,
            BetriebEurProKwhEntladen = 0.03,
            InvestVorhanden = true,
            BetriebVorhanden = true,
            Herkunft = "Dialog"
        };

        private static SpeicherOptimierungEingaben Eingaben(
            SpeicherAuslegungQuelle last = SpeicherAuslegungQuelle.Datei,
            SpeicherAuslegungQuelle pv = SpeicherAuslegungQuelle.Datei,
            SpeicherAuslegungQuelle preis = SpeicherAuslegungQuelle.Datei)
            => new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Lastquelle = last,
                    PvQuelle = pv,
                    Preisquelle = preis,
                    Investitionsquelle = SpeicherKostenQuelle.Dialog,
                    Betriebsquelle = SpeicherKostenQuelle.Dialog,
                    DirekteKosten = DirekteKosten()
                }
            };

        private static SpeicherZeitreihe Jahresreihe(
            SpeicherZeitreihenRolle rolle, double wert, int jahr = 2026,
            string zone = "UTC")
        {
            DateTimeOffset start = new DateTimeOffset(jahr, 1, 1, 0, 0, 0, TimeSpan.Zero);
            int n = DateTime.IsLeapYear(jahr) ? 35136 : 35040;
            DateTimeOffset[] zeit = new DateTimeOffset[n];
            double[] werte = new double[n];
            for (int i = 0; i < n; i++)
            {
                zeit[i] = start.AddMinutes(i * 15);
                werte[i] = wert;
            }
            return new SpeicherZeitreihe
            {
                QuelleName = rolle + ".csv",
                Rolle = rolle,
                ZeitstempelUtc = zeit,
                Werte = werte,
                Optionen = new SpeicherZeitreihenOptionen { Rolle = rolle, ZeitzoneId = zone }
            };
        }

        private static StromspeicherOptimierungVorbereitung Epos(double last = 4,
            double pv = 2, double preis = 30)
        {
            double[] l = Enumerable.Repeat(last, 35040).ToArray();
            double[] p = Enumerable.Repeat(pv, 35040).ToArray();
            double[] k = Enumerable.Repeat(preis, 35040).ToArray();
            SpeicherEingang eingang = new SpeicherEingang(l, p, k,
                Enumerable.Repeat(1.0, 35040).ToArray(),
                Enumerable.Repeat(8.0, 35040).ToArray(),
                Enumerable.Repeat(6.0, 35040).ToArray());
            return new StromspeicherOptimierungVorbereitung
            {
                Basis = Basis(),
                Eingang = eingang,
                Kontext = new StromspeicherLaufKontext
                {
                    Parameter = Basis(),
                    Eingang = eingang,
                    Bezeichner = "Speicher",
                    Preisversion = "EPOS"
                }
            };
        }

        private static StromspeicherOptimierungVorbereitung BereiteDateienVor(
            SpeicherOptimierungEingaben e, SpeicherKostensaetze modul = null)
        {
            e.Auslegung.LastDatei ??= Jahresreihe(SpeicherZeitreihenRolle.Last, 4);
            if (e.Auslegung.PvQuelle == SpeicherAuslegungQuelle.Datei)
                e.Auslegung.PvDatei ??= Jahresreihe(SpeicherZeitreihenRolle.Pv, 2);
            if (e.Auslegung.Preisquelle == SpeicherAuslegungQuelle.Datei)
                e.Auslegung.PreisDatei ??= Jahresreihe(SpeicherZeitreihenRolle.Bezug, 0.25);
            return SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                null, Basis(), new StromspeicherLaufKontext { Parameter = Basis() },
                e, modul, 0.0);
        }

        [Fact]
        public void Reine_Dateiquellen_behalten_die_UTC_Achse_und_rechnen_Euro_genau_einmal_in_Cent()
        {
            SpeicherOptimierungEingaben e = Eingaben();
            e.Auslegung.LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, 7);
            e.Auslegung.PvDatei = Jahresreihe(SpeicherZeitreihenRolle.Pv, 3);
            e.Auslegung.PreisDatei = Jahresreihe(SpeicherZeitreihenRolle.Bezug, -0.05);

            StromspeicherOptimierungVorbereitung v = BereiteDateienVor(e);

            Assert.Equal(35040, v.Eingang.Anzahl);
            Assert.Equal(7.0, v.Eingang.LastKw[0]);
            Assert.Equal(3.0, v.Eingang.PvKw[0]);
            Assert.Equal(-5.0, v.Eingang.PreisCtKwh[0]);
            Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                v.ZeitstempelUtc[0]);
            Assert.Equal(new DateTimeOffset(2026, 12, 31, 23, 45, 0, TimeSpan.Zero),
                v.ZeitstempelUtc[^1]);
            Assert.Equal("", v.ZeitachsenHinweis);
        }

        [Fact]
        public void Vorbereitung_friert_Eingaben_Reihen_und_getrennt_gewaehlte_Kosten_ein()
        {
            SpeicherOptimierungEingaben e = Eingaben();
            SpeicherKostensaetze modul = new SpeicherKostensaetze
            {
                InvestEurProKw = 40,
                InvestEurProKwh = 50,
                InvestVorhanden = true,
                BetriebVorhanden = false,
                Herkunft = "Modul"
            };
            e.Auslegung.Investitionsquelle = SpeicherKostenQuelle.Kostenmodul;
            double ersterLastwert = (e.Auslegung.LastDatei =
                Jahresreihe(SpeicherZeitreihenRolle.Last, 7)).Werte[0];

            StromspeicherOptimierungVorbereitung v = BereiteDateienVor(e, modul);
            e.Auslegung.LastDatei.Werte[0] = 999;
            e.Auslegung.DirekteKosten.BetriebEurProKwJahr = 999;

            Assert.Equal(ersterLastwert, v.Eingang.LastKw[0]);
            Assert.Equal(40.0, v.Basis.CPowEurProKw);
            Assert.Equal(50.0, v.Basis.CCapEurProKwh);
            Assert.Equal(0.0, v.Basis.IFixEur);
            Assert.Equal(1.0, v.Eingaben.Auslegung.VerwendeteKosten.BetriebEurProKwJahr);
            Assert.Equal(40.0, v.Eingaben.Auslegung.VerwendeteKosten.InvestEurProKw);
            Assert.NotSame(e, v.Eingaben);
            Assert.NotSame(e.Auslegung.LastDatei.Werte,
                v.Eingaben.Auslegung.LastDatei.Werte);
        }

        /// <summary>
        /// Pinnt die Kultur (Nachweis Auftrag #230, 13. Fall neben den zwölf aus Lauf 315 —
        /// dieselbe Ursache, hier ohne jede Pinnung): Die Ausnahme meldet auf Deutsch
        /// („keine verwendbaren Betriebskosten"), die Ressourcen folgen
        /// <c>CurrentUICulture</c> — auf dem Windows-Läufer (en-US) sonst englisch.
        /// </summary>
        [Fact]
        public void Fehlende_gewaehlte_Kostenmodul_Kategorie_ist_ein_Fehler()
        {
            using var kultur = new Kulturvorrichtung();

            SpeicherOptimierungEingaben e = Eingaben();
            e.Auslegung.Betriebsquelle = SpeicherKostenQuelle.Kostenmodul;
            SpeicherKostensaetze modul = new SpeicherKostensaetze
            {
                InvestVorhanden = true,
                BetriebVorhanden = false
            };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                BereiteDateienVor(e, modul));

            Assert.Contains("keine verwendbaren Betriebskosten", ex.Message);
        }

        [Fact]
        public void Preisprofil_bleibt_in_Cent_und_erhaelt_den_Tarifaufschlag_genau_einmal()
        {
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Preisprofil);
            e.Auslegung.Strompreisprofil = new KostenprofilModel
            {
                Bezeichner = "Profil",
                Monatswerte = string.Join(";", Enumerable.Repeat("10", 12)),
                Wochenwerte = ""
            };
            StromspeicherOptimierungVorbereitung alt = Epos();

            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    alt, alt.Basis, alt.Kontext, e, null, 3.5);

            Assert.All(v.Eingang.PreisCtKwh.Take(8), x => Assert.Equal(13.5, x, 12));
            Assert.Equal("Profil", v.Kontext.Preisversion);
        }

        [Fact]
        public void Reiner_EPOS_Lauf_uebernimmt_alle_bisherigen_Reihen_als_Kopien()
        {
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos);
            StromspeicherOptimierungVorbereitung alt = Epos();

            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    alt, alt.Basis, alt.Kontext, e, null, 0.0);

            Assert.Equal(alt.Eingang.LastKw, v.Eingang.LastKw);
            Assert.Equal(alt.Eingang.PvKw, v.Eingang.PvKw);
            Assert.Equal(alt.Eingang.PreisCtKwh, v.Eingang.PreisCtKwh);
            Assert.Equal(alt.Eingang.BhkwKw, v.Eingang.BhkwKw);
            Assert.NotSame(alt.Eingang.LastKw, v.Eingang.LastKw);
            Assert.Null(v.ZeitstempelUtc);
        }

        [Fact]
        public void Datei_und_Modelljahr_duerfen_ohne_ausdrueckliche_Zuordnung_nicht_gemischt_werden()
        {
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Datei,
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos);
            e.Auslegung.LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, 4);
            StromspeicherOptimierungVorbereitung alt = Epos();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    alt, alt.Basis, alt.Kontext, e, null, 0.0));

            Assert.Contains("ausdruecklicher Wahl", ex.Message);
        }

        [Fact]
        public void Modelljahrzuordnung_belaesst_CSV_auf_der_UTC_Achse_und_meldet_die_Doppelstunde()
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
            DateTime localStart = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Unspecified);
            DateTime localEnd = DateTime.SpecifyKind(new DateTime(2027, 1, 1), DateTimeKind.Unspecified);
            DateTimeOffset start = new DateTimeOffset(localStart, zone.GetUtcOffset(localStart)).ToUniversalTime();
            DateTimeOffset end = new DateTimeOffset(localEnd, zone.GetUtcOffset(localEnd)).ToUniversalTime();
            int n = (int)((end - start).TotalMinutes / 15);
            DateTimeOffset[] zeit = new DateTimeOffset[n];
            double[] werte = new double[n];
            for (int i = 0; i < n; i++)
            {
                zeit[i] = start.AddMinutes(i * 15);
                DateTime lokal = TimeZoneInfo.ConvertTime(zeit[i], zone).DateTime;
                werte[i] = zone.IsAmbiguousTime(DateTime.SpecifyKind(lokal, DateTimeKind.Unspecified))
                    ? zeit[i].Hour : 1.0;
            }
            SpeicherZeitreihe datei = new SpeicherZeitreihe
            {
                Rolle = SpeicherZeitreihenRolle.Last,
                ZeitstempelUtc = zeit,
                Werte = werte,
                Optionen = new SpeicherZeitreihenOptionen
                {
                    Rolle = SpeicherZeitreihenRolle.Last,
                    ZeitzoneId = "Europe/Berlin"
                }
            };
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Datei,
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos);
            e.Auslegung.LastDatei = datei;
            e.Auslegung.EposModelljahrZuordnen = true;
            StromspeicherOptimierungVorbereitung alt = Epos();

            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    alt, alt.Basis, alt.Kontext, e, null, 0.0);

            Assert.Equal(35040, v.Eingang.Anzahl);
            Assert.Contains("wiederholten Ortsstunde", v.ZeitachsenHinweis);
            Assert.NotNull(v.ZeitstempelUtc);
            Assert.Equal(zeit, v.ZeitstempelUtc);
            int[] doppelt = Enumerable.Range(0, n).Where(i =>
                zone.IsAmbiguousTime(DateTime.SpecifyKind(
                    TimeZoneInfo.ConvertTime(zeit[i], zone).DateTime, DateTimeKind.Unspecified))).ToArray();
            Assert.Equal(8, doppelt.Length);
            foreach (int i in doppelt) Assert.Equal(werte[i], v.Eingang.LastKw[i]);
        }

        [Fact]
        public void Schaltjahr_bleibt_vollstaendig_und_EPOS_verwendet_am_29_Februar_den_28_Februar()
        {
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Datei,
                SpeicherAuslegungQuelle.Epos,
                SpeicherAuslegungQuelle.Epos);
            SpeicherZeitreihe last = Jahresreihe(SpeicherZeitreihenRolle.Last, 1, 2028);
            int dateiFeb29 = (31 + 28) * 96;
            for (int i = 0; i < 96; i++) last.Werte[dateiFeb29 + i] = 29;
            e.Auslegung.LastDatei = last;
            e.Auslegung.EposModelljahrZuordnen = true;

            StromspeicherOptimierungVorbereitung alt = Epos();
            int modellFeb28 = (31 + 27) * 96;
            for (int i = 0; i < 96; i++) alt.Eingang.PvKw[modellFeb28 + i] = 28;

            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    alt, alt.Basis, alt.Kontext, e, null, 0.0);

            Assert.Equal(35136, v.Eingang.Anzahl);
            Assert.Equal(29.0, v.Eingang.LastKw[dateiFeb29]);
            Assert.Equal(28.0, v.Eingang.PvKw[dateiFeb29]);
            Assert.Equal(new DateTimeOffset(2028, 2, 29, 0, 0, 0, TimeSpan.Zero),
                v.ZeitstempelUtc[dateiFeb29]);
            Assert.Contains("29. Februar", v.ZeitachsenHinweis);
            Assert.Contains("CSV-Werte bleiben unveraendert", v.ZeitachsenHinweis);
        }

        [Fact]
        public void Preisprofil_verwendet_bei_Dateiachse_die_wirklichen_Wochentage()
        {
            SpeicherOptimierungEingaben e = Eingaben(
                SpeicherAuslegungQuelle.Datei,
                SpeicherAuslegungQuelle.Keine,
                SpeicherAuslegungQuelle.Preisprofil);
            e.Auslegung.LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, 1, 2026);
            e.Auslegung.EposModelljahrZuordnen = true;
            string[] woche = Enumerable.Repeat("0", 168).ToArray();
            woche[3 * 24] = "5"; // Donnerstag, 00:00
            e.Auslegung.Strompreisprofil = new KostenprofilModel
            {
                Monatswerte = string.Join(";", Enumerable.Repeat("10", 12)),
                Wochenwerte = string.Join(";", woche)
            };

            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                    null, Basis(), new StromspeicherLaufKontext { Parameter = Basis() },
                    e, null, 2.0);

            Assert.Equal(DayOfWeek.Thursday, v.ZeitstempelUtc[0].DayOfWeek);
            Assert.Equal(17.0, v.Eingang.PreisCtKwh[0], 12);
            Assert.Contains("wirklichen Monats- und Wochentagen", v.ZeitachsenHinweis);
        }

        [Fact]
        public void Dateireihen_mit_verschobener_Achse_werden_nicht_nach_Position_gemischt()
        {
            SpeicherOptimierungEingaben e = Eingaben();
            e.Auslegung.LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, 4);
            e.Auslegung.PvDatei = Jahresreihe(SpeicherZeitreihenRolle.Pv, 2);
            e.Auslegung.PreisDatei = Jahresreihe(SpeicherZeitreihenRolle.Bezug, 0.2);
            e.Auslegung.PvDatei.ZeitstempelUtc[100] =
                e.Auslegung.PvDatei.ZeitstempelUtc[100].AddMinutes(15);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                BereiteDateienVor(e));

            Assert.Contains("Luecke oder einen doppelten Zeitstempel", ex.Message);
        }

        [Fact]
        public void Preisprofil_parst_nicht_nach_der_aktuellen_Kultur()
        {
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                SpeicherOptimierungEingaben e = Eingaben(
                    SpeicherAuslegungQuelle.Epos,
                    SpeicherAuslegungQuelle.Epos,
                    SpeicherAuslegungQuelle.Preisprofil);
                e.Auslegung.Strompreisprofil = new KostenprofilModel
                {
                    Monatswerte = string.Join(";", Enumerable.Repeat("1,5", 12))
                };
                StromspeicherOptimierungVorbereitung alt = Epos();

                Assert.Throws<FormatException>(() =>
                    SpeicherAuslegungCtrl.AusQuellenVorbereiten(
                        alt, alt.Basis, alt.Kontext, e, null, 0.0));
            }
            finally
            {
                CultureInfo.CurrentCulture = vorher;
            }
        }
    }

    [Collection("Testdatenbank")]
    public class SpeicherAuslegungRechnungDatenbankTests : IClassFixture<TestDatenbank>
    {
        private const int Projekt = 1007;
        private readonly TestDatenbank _db;

        public SpeicherAuslegungRechnungDatenbankTests(TestDatenbank db) { _db = db; }

        [Fact]
        public void Reine_Dateiquellen_nutzen_aktive_Projekt_PV_Verguetung_ohne_Dateipreis_zu_aendern()
        {
            if (!_db.Vorhanden) return;

            ProjektPhotovoltaikCtrl pvCtrl = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel pvProjekt = pvCtrl.LiesOderVorbelegt(Projekt);
            pvProjekt.Aktiv = true;
            pvProjekt.Vermarktungsform = DbWerte.PV_VERMARKTUNG_SONSTIGE_DV;
            pvProjekt.Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS;
            pvProjekt.Inbetriebnahme = new DateTime(2026, 1, 1);
            pvProjekt.KwpOverride = 1.0;
            pvProjekt.PpaPreis = 12.34;
            Assert.True(pvCtrl.Speichern(pvProjekt));

            SpeicherOptimierungEingaben eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Lastquelle = SpeicherAuslegungQuelle.Datei,
                    PvQuelle = SpeicherAuslegungQuelle.Datei,
                    Preisquelle = SpeicherAuslegungQuelle.Datei,
                    Investitionsquelle = SpeicherKostenQuelle.Dialog,
                    Betriebsquelle = SpeicherKostenQuelle.Dialog,
                    DirekteKosten = new SpeicherKostensaetze
                    {
                        InvestEurProKw = 10,
                        InvestEurProKwh = 20,
                        BetriebEurProKwJahr = 1,
                        BetriebEurProKwhJahr = 2,
                        BetriebEurProKwhEntladen = 0.03,
                        InvestVorhanden = true,
                        BetriebVorhanden = true
                    },
                    LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, 4),
                    PvDatei = Jahresreihe(SpeicherZeitreihenRolle.Pv, 2),
                    PreisDatei = Jahresreihe(SpeicherZeitreihenRolle.Bezug, 0.25)
                }
            };

            StromspeicherOptimierungVorbereitung vorbereitet =
                SpeicherAuslegungCtrl.Vorbereiten(null, Projekt, eingaben);

            Assert.NotNull(vorbereitet);
            Assert.Equal(25.0, vorbereitet.Eingang.PreisCtKwh[0], 12);
            Assert.Equal(25.0, vorbereitet.Eingang.PreisCtKwh[^1], 12);
            Assert.Equal(12.34, vorbereitet.Eingang.VerguetungPvCtKwh[0], 12);
            Assert.Equal(12.34, vorbereitet.Eingang.VerguetungPvCtKwh[^1], 12);
            Assert.Null(vorbereitet.Eingang.BhkwKw);
        }

        private static SpeicherZeitreihe Jahresreihe(SpeicherZeitreihenRolle rolle, double wert)
        {
            const int n = 35040;
            DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset[] zeit = new DateTimeOffset[n];
            double[] werte = new double[n];
            for (int i = 0; i < n; i++)
            {
                zeit[i] = start.AddMinutes(i * 15);
                werte[i] = wert;
            }
            return new SpeicherZeitreihe
            {
                QuelleName = rolle + ".csv",
                Rolle = rolle,
                ZeitstempelUtc = zeit,
                Werte = werte,
                Optionen = new SpeicherZeitreihenOptionen
                {
                    Rolle = rolle,
                    ZeitzoneId = "UTC"
                }
            };
        }
    }
}
