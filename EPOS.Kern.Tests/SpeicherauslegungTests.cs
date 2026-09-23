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
    /// <b>Die Speicherauslegung nach der Vorlage V4</b> (Umsetzungskonzept Zapfprofilgenerator 4.7):
    /// ein fiktiver, von Hand nachgerechneter Referenzfall („Wohnhaus 1, 20 WE × 2 P", erfundene
    /// Parameter), D_max = 0, Zeitpunkt in Woche 2, auto/manuell, Ladefenster, Einheiten, GLF,
    /// Band, Nenninhalt und Warnliste. Keine Normzahl.
    /// </summary>
    public sealed class SpeicherauslegungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private const double CW = 1.163;
        private const double DT = 50.0;      // 62 °C − 12 °C
        private const double FNUTZ = 0.75;
        private const double ZS = 0.1;

        /// <summary>Jeden Tag 10 kWh um 7 Uhr und 20 kWh um 19 Uhr.</summary>
        private static Wochenreihe Woche(ZapfTagtyp ersterTyp = ZapfTagtyp.Werktag, double faktorTag3 = 1.0)
        {
            var s = new double[168];
            for (int k = 0; k < 7; k++)
            {
                double f = k == 2 ? faktorTag3 : 1.0;
                s[k * 24 + 7] = 10.0 * f;
                s[k * 24 + 19] = 20.0 * f;
            }
            var typen = Enumerable.Repeat(ZapfTagtyp.Werktag, 7).ToArray();
            typen[0] = ersterTyp;
            return Wochenreihe.Aus(s, 100, 0, typen);
        }

        /// <summary>Normvergleich des Wohnhauses 1: 20 WE mit 2 Personen, Einheitsausstattung.</summary>
        private static Din4708Ergebnis Din(Parametersatz ps)
        {
            ZonenStand z = Zone("Wohnhaus 1") with { Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } } };
            return Din4708Kennzahl.Rechnen(new[] { (z, Art()) }, Din4708Katalog.Leer, ps, DT, FNUTZ);
        }

        private static Speicherauslegungseingang Eingang(Parametersatz ps, double? ladeManuell = 2.0, Wochenreihe woche = null)
        {
            Din4708Ergebnis din = Din(ps);
            return new Speicherauslegungseingang
            {
                Woche = woche ?? Woche(),
                SpeicherC = 62.0, KaltwasserAuslegungC = 12.0, Nutzanteil = FNUTZ, Zuschlag = ZS,
                Ladefenster = new Tagesfenster(0.0, 24.0),
                LadeAuto = !ladeManuell.HasValue, LadeManuellKw = ladeManuell,
                Zirkulation = new Schaetzwert(true, 0.0, null), ZirkulationLaufzeit = Tagesfenster.Leer,
                Din = din, Personen = din.Personen, Wohnen = true,
                Nenninhalte = Nenninhaltsliste.Aus(new[] { 100.0, 200.0, 300.0, 500.0, 800.0 })
            };
        }

        [Fact]
        public void Der_fiktive_Referenzfall_Wohnhaus_1_stimmt_mit_der_Handrechnung()
        {
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(Eingang(ps), ps);

            // Lindley mit 2 kW rund um die Uhr: um 7 Uhr 8 kWh, bis 18 Uhr abgebaut, um 19 Uhr 18 kWh.
            Assert.Equal(18.0, r.DmaxKwh, 12);
            Assert.True(r.ProfilbasiertVorhanden);
            double vProfil = 18.0 * 1000.0 / (CW * DT) / FNUTZ * (1 + ZS);
            Assert.True(Relativ(r.VolumenProfilL.Value, vProfil) < 1e-12);

            // N = 20 · 2 · w_b / (p_b · w_b) = 10; W_z nach der Kennzahl; V_DIN ohne Zuschlag.
            double n = 10.0;
            Assert.True(Relativ(r.VolumenDinL.Value, Din4708Kennzahl.WzKwh(n, ps) * 1000.0 / (CW * DT) / FNUTZ) < 1e-12);
            // GLF(N) = W_z(1) / W_z(N); V_GLF = P · W_z(1) / p_b · 1000 / (c_w · Δθ) / f_nutz · GLF · (1 + z_S).
            double wz1 = Din4708Kennzahl.WzKwh(1.0, ps), wzN = Din4708Kennzahl.WzKwh(n, ps);
            Assert.True(Relativ(r.Gleichzeitigkeitsfaktor.Value, wz1 / wzN) < 1e-12);
            double vGlf = 40.0 * wz1 / 4.0 * 1000.0 / (CW * DT) / FNUTZ * (wz1 / wzN) * (1 + ZS);
            Assert.True(Relativ(r.VolumenGlfL.Value, vGlf) < 1e-12);
            // Klassisch: 40 P · 40 l · 45 K / 50 K · 1,1 — nur nachrichtlich.
            Assert.True(Relativ(r.VolumenKlassischL.Value, 40.0 * 40.0 * 45.0 / 50.0 * 1.1) < 1e-12);
            Assert.False(r.Verfahren.Single(v => v.Verfahren == ZapfSpeicherverfahren.Klassisch).ImBand);

            // Band über Profil, DIN und GLF; Nenninhalt der kleinste Listenwert ≥ V_max.
            double[] band = { vProfil, r.VolumenDinL.Value, vGlf };
            Assert.Equal(band.Min(), r.BandMinL.Value, 9);
            Assert.Equal(band.Max(), r.BandMaxL.Value, 9);
            Assert.Equal(500.0, r.NenninhaltL);
            Assert.False(r.Mehrspeicher);
            // Füllstand bei 500 l: C_sp = 500 · 0,75 · 1,163 · 50 / 1000; Reserve = (C_sp − 18) / C_sp.
            double csp = 500.0 * FNUTZ * CW * DT / 1000.0;
            Assert.True(Relativ(r.KapazitaetKwh.Value, csp) < 1e-12);
            Assert.True(Relativ(r.ReserveAnteil.Value, (csp - 18.0) / csp) < 1e-12);
            // Klassischer Faustwert weit über dem Band (Warnfaktor 2,5), N_L-Kriterium genannt.
            Assert.Contains(r.Hinweise, h => h.Code == "KLASSISCH_WEIT_UEBER_BAND");
            Assert.Contains(r.Hinweise, h => h.Code == "NL_KRITERIUM");
            Assert.Equal(4, r.Verfahren.Count);
        }

        [Fact]
        public void Dmax_null_zeigt_den_Strich()
        {
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(Eingang(ps, 100.0), ps);
            Assert.Equal(0.0, r.DmaxKwh);
            Assert.False(r.ProfilbasiertVorhanden);
            Assert.Null(r.VolumenProfilL);
            Assert.Null(r.ZeitpunktStunde);
            Verfahrensvolumen profil = r.Verfahren.Single(v => v.Verfahren == ZapfSpeicherverfahren.Profilbasiert);
            Assert.Null(profil.VolumenL);
            Assert.False(profil.ImBand);
            Assert.Equal("–", profil.Rechenweg);
            Assert.Contains(r.Hinweise, h => h.Code == TwwSpeicherauslegung.DMAX_NULL);
            // Das Band kommt dann aus DIN 4708 und GLF.
            Assert.Equal(Math.Min(r.VolumenDinL.Value, r.VolumenGlfL.Value), r.BandMinL.Value, 9);
        }

        [Fact]
        public void Der_Zeitpunkt_liegt_in_Woche_zwei()
        {
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(Eingang(ps), ps);
            // Dasselbe D_max steht schon in Woche 1 (Tag 1, 19 Uhr) — gezählt wird Woche 2.
            Assert.Equal(18.0, r.DefizitKwh[19], 12);
            Assert.Equal(168 + 20, r.ZeitpunktStunde);
            Assert.Equal(1, r.TagInWoche2);
            Assert.Equal(19, r.StundeDesTags);
            Assert.Equal(0, r.Wochentag);
            Assert.Equal(336, r.DefizitKwh.Count);

            // Ein größerer dritter Tag verschiebt den Zeitpunkt auf Tag 3 der Woche 2.
            r = TwwSpeicherauslegung.Rechnen(Eingang(ps, 2.0, Woche(faktorTag3: 1.5)), ps);
            Assert.Equal(3, r.TagInWoche2);
            Assert.Equal(19, r.StundeDesTags);
            Assert.Equal(168 + 2 * 24 + 20, r.ZeitpunktStunde);
            Assert.DoesNotContain(r.Hinweise, h => h.Code == "MASSGEBEND_WOCHENENDE");

            // Fällt er auf einen Samstag, nennt die Warnliste es.
            r = TwwSpeicherauslegung.Rechnen(Eingang(ps, 2.0, Woche(ZapfTagtyp.Samstag)), ps);
            Assert.Equal(ZapfTagtyp.Samstag, r.Tagtyp);
            Assert.Contains(r.Hinweise, h => h.Code == "MASSGEBEND_WOCHENENDE");
        }

        [Fact]
        public void Ein_Mengengeruest_fuer_alle_Verfahren()
        {
            // GLF und klassischer Faustwert rechnen mit den Personen desselben Mengengerüsts wie DIN 4708.
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungseingang e = Eingang(ps);
            Assert.Equal(40.0, e.Personen);
            Assert.Equal(e.Din.Personen, e.Personen);
            Speicherauslegungsergebnis a = TwwSpeicherauslegung.Rechnen(e, ps);
            Speicherauslegungsergebnis b = TwwSpeicherauslegung.Rechnen(e with { Personen = 80.0 }, ps);
            Assert.True(Relativ(b.VolumenGlfL.Value, 2 * a.VolumenGlfL.Value) < 1e-12);
            Assert.True(Relativ(b.VolumenKlassischL.Value, 2 * a.VolumenKlassischL.Value) < 1e-12);
            // Ohne Personen: kein GLF und kein Faustwert — nie geschätzt.
            Speicherauslegungsergebnis c = TwwSpeicherauslegung.Rechnen(e with { Personen = null }, ps);
            Assert.Null(c.VolumenGlfL);
            Assert.Null(c.VolumenKlassischL);
        }

        [Fact]
        public void Der_manuelle_Wert_wirkt_nur_bei_manuell()
        {
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungseingang e = Eingang(ps, null) with { LadeManuellKw = 99.0 };
            Speicherauslegungsergebnis auto = TwwSpeicherauslegung.Rechnen(e, ps);
            // Vorschlag: größter Tag 30 kWh über 24 h.
            Assert.True(Relativ(auto.Ladeleistung.Vorschlag, 30.0 / 24.0) < 1e-12);
            Assert.Equal(auto.Ladeleistung.Vorschlag, auto.Ladeleistung.Angesetzt);
            Assert.Contains("(auto)", auto.LadeRechenweg);
            Speicherauslegungsergebnis manuell = TwwSpeicherauslegung.Rechnen(e with { LadeAuto = false }, ps);
            Assert.Equal(99.0, manuell.Ladeleistung.Angesetzt);
            Assert.Contains("(manuell)", manuell.LadeRechenweg);
            Assert.Equal(new Schaetzwert(false, 3.0, null).Angesetzt, 3.0);
        }

        [Fact]
        public void Das_Ladefenster_begrenzt_die_Nachladung()
        {
            // Ladefenster 22 … 8 Uhr (10 h), Vorschlag 30 kWh / 10 h = 3 kW; tagsüber keine Ladung.
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungseingang e = Eingang(ps, null) with { Ladefenster = new Tagesfenster(22.0, 10.0) };
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(e, ps);
            Assert.True(Relativ(r.Ladeleistung.Angesetzt, 3.0) < 1e-12);
            IReadOnlyList<double> d = r.DefizitKwh;
            // Woche 2, Tag 1: um 7 Uhr (im Fenster) 10 − 3; bis 18 Uhr bleibt es stehen; um 19 Uhr +20.
            // Index 168 + h ist die Stunde h des ersten Tages der Woche 2 (D(t) mit t = 169 + h).
            Assert.Equal(7.0, d[168 + 7], 12);
            Assert.Equal(d[168 + 7], d[168 + 18], 12);
            Assert.Equal(d[168 + 18] + 20.0, d[168 + 19], 12);
            Assert.Equal(27.0, r.DmaxKwh, 12);
            Assert.DoesNotContain(r.Hinweise, h => h.Code == "LADELEISTUNG_ZU_KLEIN");
            // Mit derselben Tagesenergie rund um die Uhr geladen ist das Defizit kleiner.
            Speicherauslegungsergebnis rund = TwwSpeicherauslegung.Rechnen(Eingang(ps, null), ps);
            Assert.True(r.DmaxKwh > rund.DmaxKwh);
        }

        [Fact]
        public void Die_Einheiten_stimmen_mit_der_Anzeigeformel()
        {
            Parametersatz ps = Auslegungssatz();
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(Eingang(ps), ps);
            // V [l] = Q [kWh] · 1000 / (c_w · Δθ) — und zurück.
            double q = r.VolumenProfilL.Value * FNUTZ / (1 + ZS) * CW * DT / 1000.0;
            Assert.True(Relativ(q, r.DmaxKwh) < 1e-12);
            var f = TwwSpeicherauslegung.Fuellstand(r, 800.0, FNUTZ, DT);
            Assert.True(Relativ(f.KapazitaetKwh, 800.0 * FNUTZ * CW * DT / 1000.0) < 1e-12);
            Assert.True(Relativ(f.MinFuellstandKwh, f.KapazitaetKwh - 18.0) < 1e-12);
            Assert.True(Relativ(Mengengeruest.VolumenL(18.0, DT), 18.0 * 1000.0 / (CW * DT)) < 1e-15);
        }

        [Fact]
        public void GLF_ist_eins_bei_einer_Einheit_und_V_steigt_mit_N()
        {
            Parametersatz ps = Auslegungssatz();
            Assert.Equal(1.0, TwwSpeicherauslegung.Gleichzeitigkeitsfaktor(1.0, ps));
            double glfVor = 1.0, vVor = 0.0;
            foreach (double n in new[] { 2.0, 5.0, 10.0, 30.0, 100.0, 300.0 })
            {
                double glf = TwwSpeicherauslegung.Gleichzeitigkeitsfaktor(n, ps);
                double v = TwwSpeicherauslegung.VolumenGlfL(n * 4.0, n, DT, FNUTZ, ZS, ps);   // P = N · p_b
                Assert.True(glf < glfVor, "GLF fällt monoton in N");
                Assert.True(v > vVor, "V_GLF steigt monoton in N");
                glfVor = glf;
                vVor = v;
            }
        }

        [Fact]
        public void Band_Nenninhalt_und_Warnliste()
        {
            Parametersatz ps = Auslegungssatz();
            // Über dem Listenende: auf das Raster 500 l gerundet, Mehrspeicheranlage prüfen.
            Speicherauslegungseingang e = Eingang(ps) with { Nenninhalte = Nenninhaltsliste.Aus(new[] { 100.0, 200.0, 300.0 }) };
            Speicherauslegungsergebnis r = TwwSpeicherauslegung.Rechnen(e, ps);
            Assert.Equal(Math.Ceiling(r.BandMaxL.Value / 500.0) * 500.0, r.NenninhaltL);
            Assert.True(r.Mehrspeicher);
            Assert.Contains(r.Hinweise, h => h.Code == "MEHRSPEICHER");
            // Summenlinienpunkt außerhalb des Bands; Speichertemperatur unter der Mindesttemperatur.
            r = TwwSpeicherauslegung.Rechnen(Eingang(ps) with { SummenlinienpunktL = 100.0, SpeicherC = 55.0 }, ps);
            Assert.Contains(r.Hinweise, h => h.Code == "SUMMENLINIE_AUSSERHALB_BAND");
            Assert.Contains(r.Hinweise, h => h.Code == "SPEICHERTEMPERATUR_UNTER_MINDEST" && h.Warnung);
            // Zu kleine Ladeleistung: Mindestleistung genannt, das Defizit wächst.
            r = TwwSpeicherauslegung.Rechnen(Eingang(ps, 0.5), ps);
            Assert.Contains(r.Hinweise, h => h.Code == "LADELEISTUNG_ZU_KLEIN");
            Assert.Contains(r.Hinweise, h => h.Code == "DEFIZIT_WAECHST");
            // Nichtwohnen: DIN 4708 und GLF außerhalb des Gültigkeitsbereichs, nur das Profil im Band.
            r = TwwSpeicherauslegung.Rechnen(Eingang(ps) with { Wohnen = false }, ps);
            Assert.Null(r.VolumenDinL);
            Assert.Null(r.VolumenGlfL);
            Assert.Contains(r.Hinweise, h => h.Code == "GUELTIGKEIT_DIN_GLF");
            Assert.Equal(r.VolumenProfilL.Value, r.BandMaxL.Value);
            // Nur die Topologie Speicher.
            Assert.Equal(ZapfAuslegungsfehler.NichtGueltig, Assert.Throws<ZapfAuslegungException>(() =>
                TwwSpeicherauslegung.Rechnen(Eingang(ps) with { Topologie = ZapfTopologie.Durchfluss }, ps)).Fehler);
            // Eine ungeordnete Liste der Nenninhalte ist benannt abgelehnt.
            Assert.Throws<ZapfAuslegungException>(() => Nenninhaltsliste.Aus(new[] { 200.0, 100.0 }));
        }
    }
}
