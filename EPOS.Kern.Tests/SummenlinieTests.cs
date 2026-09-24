using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.AuslegungTestbau;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Summenlinie nach DIN EN 12831-3 mit A100/A1</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.5 a): Handrechnung eines konstruierten Tags, Φ_N = min, Φ_eff negativ
    /// in Ladepausen, Einschaltpunkt, Zirkulation als Minutenlast, Bisektion, Monotonieprüfung,
    /// Zeitkonstante, Schnellpfad. Alle Parameter sind erfunden (<see cref="AuslegungTestbau"/>).
    /// Toleranz relativ 1e-9.
    /// </summary>
    public sealed class SummenlinieTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Die_Handrechnung_eines_konstruierten_Tags_stimmt()
        {
            // 100 l, Δθ 50 K, f_l 0,8: Q_max = 4,652 kWh; Sensor 0,5: Q_on = 2,326 kWh.
            // Eine Zapfung von 3 kWh in Minute 10; Erzeuger 30 kW = 0,5 kWh je Minute, 2 min Verzögerung.
            Bedarfstag tag = Tag(new Zapfereignis(10, 1, 3.0));
            Summenliniennachweis n = Summenlinie.Nachweis(tag, 100.0, 30.0, Linie(), true);

            double qMax = 100.0 * KAPPA;
            Assert.True(Relativ(n.SpeicherMaxKwh, 4.652) < 1e-12);
            Assert.True(Relativ(n.EinschaltpunktKwh, 2.326) < 1e-12);
            Assert.Equal(0.0, n.MindestinhaltKwh);
            Assert.True(n.Erfuellt);
            Assert.Equal(-1, n.ErsteVerletzungMinute);
            Assert.True(Relativ(n.KleinsterAbstandKwh, qMax - 3.0) < 1e-12);

            IReadOnlyList<double> q = n.InhaltKwh;
            Assert.Equal(1441, q.Count);
            Assert.True(Relativ(q[10], qMax) < 1e-12);
            Assert.True(Relativ(q[11], qMax - 3.0) < 1e-12);    // Minute 11: unter Q_on, Erzeuger ein
            Assert.True(Relativ(q[13], qMax - 3.0) < 1e-12);    // Minuten 11, 12: Verzögerung
            Assert.True(Relativ(q[14], qMax - 3.0 + 0.5) < 1e-12);
            Assert.True(Relativ(q[18], qMax - 3.0 + 2.5) < 1e-12);
            Assert.True(Relativ(q[19], qMax) < 1e-12);           // gekappt auf Q_max, Erzeuger aus
            Assert.Equal(q[19], q[1440]);
            // Erzeuger ein in den Minuten 11 … 18: acht Minuten.
            Assert.True(Relativ(n.LadezeitH, 8.0 / 60.0) < 1e-12);
        }

        [Fact]
        public void Die_Leistung_ist_das_Minimum_aus_Erzeuger_und_Uebertrager()
        {
            Summenlinienparameter p = Linie(30.0) with { Uebertrager = new Uebertrager(null, 1000.0, null, null, 20.0, null, null) };
            Assert.Equal(20.0, Summenlinie.LeistungKw(100.0, p, out _));
            Assert.Equal(30.0, Summenlinie.LeistungKw(100.0, Linie(30.0), out _));
            p = Linie(10.0) with { Uebertrager = new Uebertrager(null, 1000.0, null, null, 20.0, null, null) };
            Assert.Equal(10.0, Summenlinie.LeistungKw(100.0, p, out _));

            // Schätzformel A = 0,01 · V − 0,5, U 500, Δθ_Ü 20: bei 100 l 0,5 m² → 5 kW; bei 40 l unplausibel.
            var schaetz = new Uebertrager(null, null, null, 500.0, 20.0, 0.01, -0.5);
            p = Linie(30.0) with { Uebertrager = schaetz };
            Assert.True(Relativ(Summenlinie.LeistungKw(100.0, p, out bool u1), 5.0) < 1e-12);
            Assert.False(u1);
            Assert.Equal(0.0, Summenlinie.LeistungKw(40.0, p, out bool u2));
            Assert.True(u2);
            // Ohne Erzeuger: Übertrager allein; ohne beides: benannt abgelehnt.
            Assert.True(Relativ(Summenlinie.LeistungKw(100.0, p with { ErzeugerKw = null }, out _), 5.0) < 1e-12);
            Assert.Equal(ZapfAuslegungsfehler.LeistungFehlt, Assert.Throws<ZapfAuslegungException>(() =>
                Summenlinie.Pruefen(Linie() with { ErzeugerKw = null })).Fehler);
        }

        [Fact]
        public void Phi_eff_ist_in_Ladepausen_negativ()
        {
            // Speicherverlust 0,6 kW: ohne Zapfung sinkt der Inhalt je Minute um 0,01 kWh,
            // bis der Erzeuger einschaltet; die Leistung des Erzeugers deckt dann auch den Verlust.
            Bedarfstag tag = Tag(new Zapfereignis(1439, 1, 0.0));
            Summenlinienparameter p = Linie(30.0) with { SpeicherverlustKw = 0.6 };
            Summenliniennachweis n = Summenlinie.Nachweis(tag, 100.0, 30.0, p, true);
            Assert.True(Relativ(n.InhaltKwh[1], 100.0 * KAPPA - 0.01) < 1e-12);
            Assert.True(Relativ(n.InhaltKwh[100], 100.0 * KAPPA - 1.0) < 1e-9);
            Assert.True(n.Erfuellt);
            // Ein Erzeuger ohne Leistung: Φ_eff bleibt −Φ_V auch „ein"; der Inhalt fällt den ganzen Tag.
            Summenliniennachweis ohne = Summenlinie.Nachweis(tag, 100.0, 0.0, p, true);
            Assert.True(Relativ(ohne.InhaltKwh[1440], 100.0 * KAPPA - 14.4) < 1e-9);
            Assert.False(ohne.Erfuellt);
        }

        [Fact]
        public void Der_Einschaltpunkt_folgt_der_Sensorhoehe()
        {
            Bedarfstag tag = Tag(new Zapfereignis(10, 1, 1.0));
            // Sensor 0,5: Q_on = 2,326 kWh; nach 1 kWh bleibt der Erzeuger aus (Inhalt 3,652 kWh).
            Summenliniennachweis aus = Summenlinie.Nachweis(tag, 100.0, 30.0, Linie(sensor: 0.5), true);
            Assert.Equal(0.0, aus.LadezeitH);
            Assert.True(Relativ(aus.InhaltKwh[1440], 100.0 * KAPPA - 1.0) < 1e-12);
            // Sensor 0,2: Q_on = 3,7216 kWh — nach 1 kWh schaltet er ein und lädt nach.
            Summenliniennachweis ein = Summenlinie.Nachweis(tag, 100.0, 30.0, Linie(sensor: 0.2), true);
            Assert.True(ein.LadezeitH > 0);
            Assert.True(Relativ(ein.InhaltKwh[1440], 100.0 * KAPPA) < 1e-12);
        }

        [Fact]
        public void Die_Zirkulation_ist_eine_Minutenlast_in_der_Laufzeit()
        {
            // 1,2 kW von 6 bis 8 Uhr: je Minute 0,02 kWh; Speicher groß, Erzeuger erst leer ein (Sensor 1).
            Bedarfstag tag = Tag(new Zapfereignis(1439, 1, 0.0));
            Summenlinienparameter p = Linie(30.0, sensor: 1.0) with
            {
                Zirkulation = new Zirkulationslast(1.2, new Tagesfenster(6.0, 2.0))
            };
            Summenliniennachweis n = Summenlinie.Nachweis(tag, 1000.0, 30.0, p, true);
            double qMax = 1000.0 * KAPPA;
            Assert.True(Relativ(n.InhaltKwh[360], qMax) < 1e-12);
            Assert.True(Relativ(n.InhaltKwh[361], qMax - 0.02) < 1e-12);
            Assert.True(Relativ(n.InhaltKwh[480], qMax - 2.4) < 1e-9);
            Assert.True(Relativ(n.InhaltKwh[1440], qMax - 2.4) < 1e-9);
            // Die Belegung einer gebrochenen Laufzeit: 6:00 bis 6:00:30 → halbe Minute.
            var f = new Tagesfenster(6.0, 0.5 / 60.0);
            Assert.True(Relativ(f.AnteilMinute(360), 0.5) < 1e-9);
            Assert.Equal(0.0, f.AnteilMinute(361));
            // Ein Fenster über Mitternacht (22 Uhr, 10 h) belegt 22 … 23 und 0 … 7.
            var nacht = new Tagesfenster(22.0, 10.0);
            Assert.Equal(1.0, nacht.AnteilStunde(23));
            Assert.Equal(1.0, nacht.AnteilStunde(0));
            Assert.Equal(1.0, nacht.AnteilStunde(7));
            Assert.Equal(0.0, nacht.AnteilStunde(8));
            Assert.Equal(0.0, nacht.AnteilStunde(21));
            Assert.Equal(10.0, Enumerable.Range(0, 24).Sum(h => nacht.AnteilStunde(h)));
        }

        [Fact]
        public void Die_Bisektion_trifft_den_Nachweis()
        {
            // Eine Zapfung von 3 kWh bei großer Leistung: das kleinste Volumen fasst genau 3 kWh.
            Bedarfstag tag = Tag(new Zapfereignis(10, 1, 3.0));
            var hinweise = new List<Auslegungshinweis>();
            Summenlinienpunkt p = Summenlinie.KleinstesVolumen(tag, Linie(300.0), hinweise);
            Assert.Equal(Summenliniensuche.Bisektion, p.Suche);
            Assert.True(Relativ(p.VolumenL, 3.0 / KAPPA) < 1e-9, p.VolumenL + " l");
            Assert.True(Summenlinie.Nachweis(tag, p.VolumenL, 300.0, Linie(300.0)).Erfuellt);
            Assert.False(Summenlinie.Nachweis(tag, p.VolumenL * (1 - 1e-9), 300.0, Linie(300.0)).Erfuellt);
            Assert.Empty(hinweise);

            // Zwei Zapfungen: Mit kleiner Leistung braucht es mehr Volumen als mit großer.
            Bedarfstag zwei = Tag(new Zapfereignis(60, 10, 3.0), new Zapfereignis(90, 10, 3.0));
            Summenlinienpunkt gross = Summenlinie.KleinstesVolumen(zwei, Linie(300.0), null);
            Summenlinienpunkt klein = Summenlinie.KleinstesVolumen(zwei, Linie(6.0), null);
            Assert.True(klein.VolumenL > gross.VolumenL);
            Assert.True(Summenlinie.Nachweis(zwei, klein.VolumenL, 6.0, Linie(6.0)).Erfuellt);
            Assert.False(Summenlinie.Nachweis(zwei, klein.VolumenL * (1 - 1e-6), 6.0, Linie(6.0)).Erfuellt);
        }

        [Fact]
        public void Die_Monotoniepruefung_faellt_auf_den_Rasterlauf_zurueck()
        {
            // Erfundener Fall ohne monotonen Nachweis: 1 kWh in Minute 0, 1,5 kWh in Minute 100,
            // Sensor 0,5, große Leistung ohne Verzögerung. κV in [1,5; 2] besteht (der Erzeuger
            // schaltet nach der ersten Zapfung ein), κV in (2; 2,5) nicht (er bleibt aus), ab 2,5 wieder.
            Bedarfstag tag = Tag(new Zapfereignis(0, 1, 1.0), new Zapfereignis(100, 1, 1.5));
            Summenlinienparameter p = Linie(600.0, 0.5, 0.0);
            Assert.True(Summenlinie.Nachweis(tag, 1.8 / KAPPA, 600.0, p).Erfuellt);
            Assert.False(Summenlinie.Nachweis(tag, 2.2 / KAPPA, 600.0, p).Erfuellt);
            Assert.True(Summenlinie.Nachweis(tag, 2.6 / KAPPA, 600.0, p).Erfuellt);

            var hinweise = new List<Auslegungshinweis>();
            Summenlinienpunkt punkt = Summenlinie.KleinstesVolumen(tag, p, hinweise);
            Assert.Equal(Summenliniensuche.Rasterlauf, punkt.Suche);
            Assert.Contains(hinweise, h => h.Code == "SUMMENLINIE_NICHT_MONOTON");
            Assert.InRange(punkt.VolumenL * KAPPA, 1.5 - 1e-9, 1.5 + 5.0 / Summenlinie.RASTER_FEIN + 1e-9);
            Assert.True(Summenlinie.Nachweis(tag, punkt.VolumenL, 600.0, p).Erfuellt);
        }

        [Fact]
        public void Die_Zeitkonstante_hat_die_Dimension_Minuten()
        {
            // τ = m · c_w / (U·A) · k_τ nach A1, c_w in kJ/(kg·K): 1000 l · 1,163 Wh/(l·K) · 3,6 kJ/Wh
            // = 4186,8 kJ/K; / 1163 W/K = 3,6 kJ/W; mit dem erfundenen k_τ 25 min·W/kJ → 90 min.
            // Ein k_τ von 60 hätte eine Umrechnung Wh → min verdeckt; 25 ist keine.
            Assert.True(Relativ(Summenlinie.ZeitkonstanteMin(1000.0, 1163.0, 25.0), 90.0) < 1e-12);
            Assert.True(Relativ(Summenlinie.ZeitkonstanteMin(2000.0, 1163.0, 25.0), 180.0) < 1e-12);
            Assert.True(Relativ(Summenlinie.ZeitkonstanteMin(1000.0, 2326.0, 25.0), 45.0) < 1e-12);
            // Doppeltes k_τ, doppeltes τ: der Koeffizient ist ein Faktor der A1, keine Einheit.
            Assert.True(Relativ(Summenlinie.ZeitkonstanteMin(1000.0, 1163.0, 50.0), 180.0) < 1e-12);
            Assert.Throws<ZapfAuslegungException>(() => Summenlinie.ZeitkonstanteMin(1000.0, 0.0, 25.0));

            // Im Ergebnis nur mit U·A und Koeffizient.
            Bedarfstag tag = Tag(new Zapfereignis(10, 1, 3.0));
            Summenlinienparameter p = Linie(30.0) with
            {
                Uebertrager = new Uebertrager(null, 1163.0, null, null, 20.0, null, null), ZeitkonstanteKoeffizient = 25.0
            };
            Summenlinienergebnis e = Summenlinie.Rechnen(tag, p, 0);
            Assert.True(Relativ(e.ZeitkonstanteMin.Value, e.Punkt.VolumenL / 1000.0 * 3.6 * 25.0) < 1e-12);
            Assert.Null(Summenlinie.Rechnen(tag, Linie(30.0), 0).ZeitkonstanteMin);
        }

        [Fact]
        public void Schnellpfad_und_Vollverfahren_stimmen_im_Gueltigkeitsbereich()
        {
            Parametersatz ps = Auslegungssatz();
            Assert.True(Summenlinie.SchnellpfadGilt(true, 5, ps));
            Assert.False(Summenlinie.SchnellpfadGilt(true, 6, ps));
            Assert.False(Summenlinie.SchnellpfadGilt(false, 2, ps));

            Bedarfstag tag = Tag(new Zapfereignis(420, 10, 2.0), new Zapfereignis(1140, 30, 3.0));
            Summenlinienparameter voll = Linie(8.0) with { SensorhoeheAnteil = 0.7, SpeicherC = 58.0 };
            Summenlinienparameter schnell = Summenlinie.Schnellpfad(Linie(8.0), ps);
            Assert.True(schnell.Schnellpfad);
            Summenlinienergebnis a = Summenlinie.Rechnen(tag, voll, 0);
            Summenlinienergebnis b = Summenlinie.Rechnen(tag, schnell, 0);
            Assert.Equal(a.Punkt.VolumenL, b.Punkt.VolumenL);
            Assert.Equal(a.Punkt.LadezeitH, b.Punkt.LadezeitH);
            Assert.True(b.Schnellpfad);
            Assert.False(a.Schnellpfad);
            Assert.Equal(Summenlinie.VERMERK_ENTWURF, b.Vermerk.Kennung);
        }

        [Fact]
        public void Die_Wertepaarkurve_faellt_mit_der_Leistung()
        {
            Bedarfstag tag = Tag(new Zapfereignis(420, 60, 6.0), new Zapfereignis(1080, 60, 6.0));
            Summenlinienergebnis e = Summenlinie.Rechnen(tag, Linie(12.0), 4);
            Assert.Equal(4, e.Wertepaare.Count);
            Assert.Equal(new[] { 3.0, 6.0, 9.0, 12.0 }, e.Wertepaare.Select(w => w.LeistungKw).ToArray());
            for (int k = 1; k < e.Wertepaare.Count; k++)
                Assert.True(e.Wertepaare[k].VolumenL <= e.Wertepaare[k - 1].VolumenL);
            // Der letzte Punkt ist der Auslegungspunkt (Φ_N fest wie beim Erzeuger ohne Übertrager).
            Assert.Equal(e.Punkt.VolumenL, e.Wertepaare[3].VolumenL);
            Assert.Contains(e.Hinweise, h => h.Code == "WERTEPAARKURVE");
        }

        [Fact]
        public void Die_Wertepaarkurve_achtet_den_Uebertrager()
        {
            // Erfundener Übertrager nach der Schätzformel: Φ_Ü(V) = 500 · (0,01 · V − 0,5) · 20 / 1000
            // = 0,1 · V − 5 kW. Die Kurve rastert die Erzeugerleistung 40 kW in vier Schritten;
            // Φ_N(V) = min(Φ_E, Φ_Ü(V)).
            Bedarfstag tag = Tag(new Zapfereignis(420, 20, 8.0), new Zapfereignis(1080, 30, 10.0));
            Summenlinienparameter p = Linie(40.0) with
            {
                Uebertrager = new Uebertrager(null, null, null, 500.0, 20.0, 0.01, -0.5)
            };
            Summenlinienergebnis e = Summenlinie.Rechnen(tag, p, 4);
            Assert.Equal(4, e.Wertepaare.Count);
            bool begrenzt = false;
            for (int k = 1; k <= 4; k++)
            {
                Summenlinienpunkt w = e.Wertepaare[k - 1];
                double erzeuger = 40.0 * k / 4;
                double ue = Math.Max(0.0, 0.1 * w.VolumenL - 5.0);
                // Jedes Paar ist baubar: Φ_N ist das Minimum aus Erzeugerraster und Übertrager beim Volumen.
                Assert.True(Relativ(w.LeistungKw, Math.Min(erzeuger, ue)) < 1e-9,
                    "Paar " + k + ": Φ_N " + w.LeistungKw + " kW, Erzeuger " + erzeuger + " kW, Übertrager " + ue + " kW");
                Assert.True(w.LeistungKw <= ue + 1e-9);
                Assert.True(Summenlinie.Nachweis(tag, w.VolumenL, w.LeistungKw, p).Erfuellt);
                begrenzt |= w.LeistungKw < erzeuger - 1e-9;
            }
            // Mindestens ein Paar begrenzt der Übertrager — ein festes Φ_N gleich dem Raster wäre dort nicht baubar.
            Assert.True(begrenzt, "Kein Paar vom Übertrager begrenzt — der Fall prüft die Begrenzung nicht.");
            // Der letzte Punkt ist der Auslegungspunkt.
            Assert.Equal(e.Punkt.VolumenL, e.Wertepaare[3].VolumenL);
            Assert.Equal(e.Punkt.LeistungKw, e.Wertepaare[3].LeistungKw);
        }

        [Fact]
        public void Der_gemischte_Speicher_haelt_einen_Mindestinhalt()
        {
            Bedarfstag tag = Tag(new Zapfereignis(10, 1, 3.0));
            Summenlinienparameter lade = Linie(300.0);
            Summenlinienparameter gemischt = lade with { Speicherart = ZapfSpeicherart.GemischterSpeicher, MischwasserC = 44.0 };
            Summenliniennachweis n = Summenlinie.Nachweis(tag, 100.0, 300.0, gemischt);
            // Q_min = V · c_w · (1 − 0,5/2) · (44 − 12) · 0,8 / 1000
            Assert.True(Relativ(n.MindestinhaltKwh, 100.0 * 1.163 * 0.75 * 32.0 * 0.8 / 1000.0) < 1e-12);
            double vLade = Summenlinie.KleinstesVolumen(tag, lade, null).VolumenL;
            double vGemischt = Summenlinie.KleinstesVolumen(tag, gemischt, null).VolumenL;
            // Q_max − Q_min = 3 kWh: V · κ · (1 − 0,75 · 32/50) = 3.
            Assert.True(Relativ(vGemischt, 3.0 / (KAPPA * (1 - 0.75 * 32.0 / 50.0))) < 1e-9);
            Assert.True(vGemischt > vLade);
            Assert.Throws<ZapfAuslegungException>(() => Summenlinie.Pruefen(lade with { Speicherart = ZapfSpeicherart.GemischterSpeicher }));
        }

        [Fact]
        public void Der_Uebertrager_waehlt_Schaetzformel_und_U_nach_Erzeugerart_und_Werkstoff()
        {
            Parametersatz ps = Auslegungssatz();
            // Erfundene Schlüsselpaare: Kessel (NA.1) 0,02 m²/l · V − 1,0 m², Wärmepumpe (NA.2) 0,01 · V − 0,5;
            // U Stahl 400, Edelstahl 500 W/(m²·K).
            Uebertrager kessel = Summenlinie.Parameter(Projekt(), ps, 57.0, 7.0, null, null, null,
                ZapfErzeugerart.Kessel, ZapfUebertragerwerkstoff.Stahl).Uebertrager;
            Assert.Equal((0.02, -1.0, 400.0), (kessel.FlaecheSteigungM2JeL.Value, kessel.FlaecheAchsabschnittM2.Value, kessel.UWJeM2K.Value));
            Uebertrager wp = Summenlinie.Parameter(Projekt(), ps, 57.0, 7.0, null, null, null,
                ZapfErzeugerart.Waermepumpe, ZapfUebertragerwerkstoff.Edelstahl).Uebertrager;
            Assert.Equal((0.01, -0.5, 500.0), (wp.FlaecheSteigungM2JeL.Value, wp.FlaecheAchsabschnittM2.Value, wp.UWJeM2K.Value));

            // Ohne Erzeugerart keine Schätzformel, ohne Werkstoff kein U — benannt, nie geraten.
            Assert.Equal(ZapfAuslegungsfehler.UebertragerUnbestimmt, Assert.Throws<ZapfAuslegungException>(() =>
                Summenlinie.Parameter(Projekt(), ps, 57.0, 7.0, null, null, null, null, ZapfUebertragerwerkstoff.Stahl)).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.UebertragerUnbestimmt, Assert.Throws<ZapfAuslegungException>(() =>
                Summenlinie.Parameter(Projekt(), ps, 57.0, 7.0, null, null, null, ZapfErzeugerart.Kessel, null)).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.UebertragerUnbestimmt, Assert.Throws<ZapfAuslegungException>(() =>
                Summenlinie.Parameter(Projekt() with { UebertragerFlaecheM2 = 2.0 }, ps, 57.0, 7.0, null, null, null)).Fehler);
            // Eine Fläche im Projekt braucht nur den Werkstoff, U·A im Projekt keines von beiden.
            Assert.Equal(400.0, Summenlinie.Parameter(Projekt() with { UebertragerFlaecheM2 = 2.0 }, ps, 57.0, 7.0, null, null, null,
                null, ZapfUebertragerwerkstoff.Stahl).Uebertrager.UWJeM2K);
            Assert.Equal(900.0, Summenlinie.Parameter(Projekt() with { UebertragerUaWJeK = 900.0 }, ps, 57.0, 7.0, null, null, null)
                .Uebertrager.UaWJeK);
        }

        [Fact]
        public void Die_Groessen_kommen_aus_Projekt_und_Parametersatz()
        {
            Parametersatz ps = Auslegungssatz();
            var hinweise = new List<Auslegungshinweis>();
            var prot = new Herkunftsprotokoll();
            // Die Speichertemperatur wählt die Fassade EINMAL je Gruppe (Speichertemperaturwahl); die Summenlinie übernimmt sie.
            Summenlinienparameter p = Summenlinie.Parameter(Projekt(), ps, 57.0, 7.0, null, prot, hinweise,
                                                            ZapfErzeugerart.Waermepumpe, ZapfUebertragerwerkstoff.Edelstahl);
            Assert.Equal(12.0, p.KaltwasserAuslegungC);
            Assert.Equal(57.0, p.SpeicherC);
            Assert.Equal(0.8, p.Ladungsfaktor);
            Assert.Equal(0.5, p.SensorhoeheAnteil);
            Assert.Equal(2.0, p.VerzoegerungMin);
            Assert.Equal(7.0, p.ErzeugerKw);
            Assert.Equal(0.0, p.SpeicherverlustKw);
            Assert.Contains(hinweise, h => h.Code == "SPEICHERVERLUST_NULL");
            Assert.Equal(0.01, p.Uebertrager.FlaecheSteigungM2JeL);
            Assert.Equal(Wertstatus.Vorgabe, prot.Letzter("", "Auslegung.KaltwasserC").Status);

            ProjektStand eigen = Projekt() with
            {
                SpeicherC = 65.0, KaltwasserAuslegungC = 8.0, ErzeugerKw = 20.0, SpeicherverlustW = 150.0,
                UebertragerUaWJeK = 800.0, SensorhoeheAnteil = 0.3
            };
            p = Summenlinie.Parameter(eigen, ps, 65.0, 7.0, null, prot, null);
            Assert.Equal(65.0, p.SpeicherC);
            Assert.Equal(8.0, p.KaltwasserAuslegungC);
            Assert.Equal(20.0, p.ErzeugerKw);
            Assert.Equal(0.15, p.SpeicherverlustKw);
            Assert.Equal(800.0, p.Uebertrager.UaWJeK);
            Assert.Equal(0.3, p.SensorhoeheAnteil);
            Assert.Equal(Wertstatus.Ueberschrieben, prot.Letzter("", "Auslegung.KaltwasserC").Status);

            // Ein fehlender Pflichtparameter ist eine benannte Ablehnung, kein Rückfall.
            Assert.Throws<ParametersatzException>(() => Summenlinie.Parameter(Projekt(),
                Auslegungssatz(null, ZapfAuslegungParameter.LADUNGSFAKTOR), 57.0, 7.0, null, null, null,
                ZapfErzeugerart.Waermepumpe, ZapfUebertragerwerkstoff.Edelstahl));
            // Die Zeitkonstante entscheidet nichts: fehlt k_τ, nur ein Hinweis.
            var h2 = new List<Auslegungshinweis>();
            p = Summenlinie.Parameter(Projekt(), Auslegungssatz(null, ZapfAuslegungParameter.ZEITKONSTANTE_KOEFFIZIENT),
                                      57.0, 7.0, null, null, h2, ZapfErzeugerart.Kessel, ZapfUebertragerwerkstoff.Stahl);
            Assert.Null(p.ZeitkonstanteKoeffizient);
            Assert.Contains(h2, h => h.Code == ZapfHinweis.PARAMETER_FEHLT);
        }
    }
}
