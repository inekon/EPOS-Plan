using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schicht S4 — Bedarfstag und Wochenreihe</b> (Umsetzungskonzept Zapfprofilgenerator 4.2,
    /// 4.5): die vier Quellen des Bedarfstags, die Vorgaberegel und die Wochenreihe der
    /// maßgebenden Woche. Alle Zahlen sind erfunden und rund; keine Normzahl. Toleranz relativ
    /// 1e-12 bzw. exakt.
    /// </summary>
    public sealed class BedarfstagTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Bedarfstag
        // =================================================================================

        [Fact]
        public void Das_Stundenprofil_verteilt_gleichmaessig_und_traegt_den_Vermerk()
        {
            var stunden = new double[24];
            stunden[7] = 6.0;
            stunden[19] = 3.0;
            Bedarfstag t = Bedarfstag.AusStunden(stunden, "Stundenprofil (fiktiv)");

            Assert.Equal(ZapfBedarfstagquelle.Stundenprofil, t.Quelle);
            Assert.True(t.SpitzenUnterschaetzt);
            Assert.Equal(1440, t.MinutenKwh.Count);
            for (int m = 0; m < 60; m++)
            {
                Assert.Equal(6.0 / 60, t.MinutenKwh[7 * 60 + m]);
                Assert.Equal(3.0 / 60, t.MinutenKwh[19 * 60 + m]);
            }
            Assert.Equal(0.0, t.MinutenKwh[0]);
            Assert.True(Relativ(t.TagessummeKwh, 9.0) < 1e-12);
            // Die Minutenspitze eines gleichmäßig verteilten Stundenwerts ist der Stundenwert selbst.
            Assert.True(Relativ(t.GroessteMinutenleistungKw, 6.0) < 1e-12);
            Assert.True(Relativ(t.GroessteStundenleistungKw, 6.0) < 1e-12);
        }

        [Fact]
        public void Ereignisse_ueberlagern_sich_und_laufen_ueber_Mitternacht_weiter()
        {
            Bedarfstag t = Bedarfstag.AusEreignissen(ZapfBedarfstagquelle.Konstruktor, "Ereignisse (fiktiv)", new[]
            {
                new Zapfereignis(600, 10, 5.0),
                new Zapfereignis(605, 5, 1.0),
                new Zapfereignis(1435, 10, 2.0)
            }, Fiktiv);

            Assert.Equal(0.5, t.MinutenKwh[600]);
            Assert.Equal(0.5 + 0.2, t.MinutenKwh[605]);
            Assert.Equal(0.5 + 0.2, t.MinutenKwh[609]);
            Assert.Equal(0.0, t.MinutenKwh[610]);
            Assert.Equal(0.2, t.MinutenKwh[1435]);
            Assert.Equal(0.2, t.MinutenKwh[1439]);
            Assert.Equal(0.2, t.MinutenKwh[0]);
            Assert.Equal(0.2, t.MinutenKwh[4]);
            Assert.Equal(0.0, t.MinutenKwh[5]);
            Assert.True(Relativ(t.TagessummeKwh, 8.0) < 1e-12);
            Assert.True(Relativ(t.GroessteMinutenleistungKw, 0.7 * 60) < 1e-12);
            Assert.False(t.SpitzenUnterschaetzt);
        }

        [Fact]
        public void Ungueltige_Ereignisse_werden_benannt_abgelehnt()
        {
            void Pruefen(params Zapfereignis[] e)
            {
                var ex = Assert.Throws<ZapfAuslegungException>(() =>
                    Bedarfstag.AusEreignissen(ZapfBedarfstagquelle.Konstruktor, "x", e, Fiktiv));
                Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig, ex.Fehler);
            }
            Pruefen();
            Pruefen(new Zapfereignis(1440, 1, 1.0));
            Pruefen(new Zapfereignis(-1, 1, 1.0));
            Pruefen(new Zapfereignis(0, 0, 1.0));
            Pruefen(new Zapfereignis(0, 1441, 1.0));
            Pruefen(new Zapfereignis(0, 1, -1.0));
            Pruefen(new Zapfereignis(0, 1, double.NaN));
        }

        [Fact]
        public void Der_Konstruktor_rechnet_die_Energie_aus_Volumen_und_Temperatur()
        {
            // Erfundene Regel: 10 l/min, 4 min, 40 °C — 40 l je Vorgang.
            Parametersatz ps = Parameter(new Dictionary<string, double>
            {
                ["Konstruktor.Regel.Brause.Volumenstrom"] = 10.0,
                ["Konstruktor.Regel.Brause.Dauer"] = 4.0,
                ["Konstruktor.Regel.Brause.Temperatur"] = 40.0,
                ["Konstruktor.Regel.Becken.Volumenstrom"] = 5.0,
                ["Konstruktor.Regel.Becken.Dauer"] = 1.0,
                ["Konstruktor.Regel.Becken.Temperatur"] = 50.0,
            });
            IReadOnlyList<Zapfregel> regeln = Zapfregel.AusParametern(ps);
            Assert.Equal(new[] { "Becken", "Brause" }, regeln.Select(r => r.Name).ToArray());
            Zapfregel brause = regeln.Single(r => r.Name == "Brause");
            Assert.Equal(40.0, brause.VolumenJeVorgangL);

            var zeilen = new[]
            {
                Konstruktorzeile.AusVorgaengen(900, 960, 20, brause),          // 800 l bei 40 °C
                new Konstruktorzeile(480, 540, 60.0, 50.0, "Küche")          // 60 l bei 50 °C
            };
            Bedarfstag t = Bedarfstag.Konstruieren(zeilen, 10.0, "Konstruierter Tag (fiktiv)");

            double eBrause = 800.0 * 1.163 * 30.0 / 1000.0;
            double eKueche = 60.0 * 1.163 * 40.0 / 1000.0;
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, t.Quelle);
            Assert.True(Relativ(t.MinutenKwh[900], eBrause / 60) < 1e-12);
            Assert.True(Relativ(t.MinutenKwh[959], eBrause / 60) < 1e-12);
            Assert.Equal(0.0, t.MinutenKwh[960]);
            Assert.True(Relativ(t.MinutenKwh[480], eKueche / 60) < 1e-12);
            Assert.True(Relativ(t.TagessummeKwh, eBrause + eKueche) < 1e-12);
            Assert.Equal("Brause", zeilen[0].Verbraucher);
        }

        [Fact]
        public void Der_Konstruktor_lehnt_ungueltige_Zeilen_benannt_ab()
        {
            Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Bedarfstag.Konstruieren(new[] { new Konstruktorzeile(600, 600, 10, 50) }, 10, "x")).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Bedarfstag.Konstruieren(new[] { new Konstruktorzeile(600, 1441, 10, 50) }, 10, "x")).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.TemperaturUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Bedarfstag.Konstruieren(new[] { new Konstruktorzeile(600, 660, 10, 10) }, 10, "x")).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.GroesseUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Bedarfstag.Konstruieren(new[] { new Konstruktorzeile(600, 660, -1, 50) }, 10, "x")).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Bedarfstag.Konstruieren(new Konstruktorzeile[0], 10, "x")).Fehler);
            // Eine Regel ohne Temperatur ist nicht vollständig.
            Parametersatz ps = Parameter(new Dictionary<string, double>
            {
                ["Konstruktor.Regel.Brause.Volumenstrom"] = 10.0, ["Konstruktor.Regel.Brause.Dauer"] = 4.0
            });
            Assert.Throws<ParametersatzException>(() => Zapfregel.AusParametern(ps));
        }

        [Fact]
        public void Das_DIN_4708_Profil_verteilt_W_z_auf_die_Bloecke_des_Katalogs()
        {
            // Erfundene Blöcke: 10 min ab 7:00 mit dem ganzen W_z, 30 min ab 18:00 mit der Hälfte.
            Parametersatz ps = Parameter(new Dictionary<string, double>
            {
                ["DIN4708.Profil.Bloecke"] = 2,
                ["DIN4708.Profil.Block.1.Beginn"] = 420, ["DIN4708.Profil.Block.1.Dauer"] = 10, ["DIN4708.Profil.Block.1.Anteil"] = 1.0,
                ["DIN4708.Profil.Block.2.Beginn"] = 1080, ["DIN4708.Profil.Block.2.Dauer"] = 30, ["DIN4708.Profil.Block.2.Anteil"] = 0.5,
            });
            IReadOnlyList<Zapfblock> b = Zapfblock.AusParametern(ps);
            Assert.Equal(2, b.Count);
            Bedarfstag t = Bedarfstag.Din4708(12.0, b, 5.0);

            Assert.Equal(ZapfBedarfstagquelle.Din4708Profil, t.Quelle);
            Assert.Equal(1.2, t.MinutenKwh[420]);
            Assert.Equal(1.2, t.MinutenKwh[429]);
            Assert.Equal(0.0, t.MinutenKwh[430]);
            Assert.Equal(0.2, t.MinutenKwh[1080]);
            Assert.True(Relativ(t.TagessummeKwh, 18.0) < 1e-12);
            Assert.True(Relativ(t.GroessteMinutenleistungKw, 72.0) < 1e-12);

            // Fehlt ein Blockschlüssel, lehnt der Katalog benannt ab.
            Parametersatz ohne = Parameter(new Dictionary<string, double>
            {
                ["DIN4708.Profil.Bloecke"] = 1, ["DIN4708.Profil.Block.1.Beginn"] = 420, ["DIN4708.Profil.Block.1.Dauer"] = 10
            });
            Assert.Throws<ParametersatzException>(() => Zapfblock.AusParametern(ohne));
            Assert.Throws<ParametersatzException>(() => Zapfblock.AusParametern(Parameter()));
        }

        [Fact]
        public void Ein_Katalogtag_wird_auf_die_Bezugsmenge_skaliert()
        {
            var zeile = new BedarfstagKatalogzeile(7, "Referenztag (fiktiv)", "TEST-Z2", ZapfBedarfstagquelle.A100Referenz,
                40.0, Fiktiv, new[] { new Zapfereignis(360, 60, 12.0), new Zapfereignis(1200, 1, 1.0) });
            Assert.Equal(1.5, Bedarfstag.Skalierung(zeile, 60.0));
            Assert.Equal(1.0, Bedarfstag.Skalierung(zeile, null));
            Assert.Equal(1.0, Bedarfstag.Skalierung(zeile with { Bezugsmenge = null }, 60.0));

            Bedarfstag t = Bedarfstag.AusKatalog(zeile, Bedarfstag.Skalierung(zeile, 60.0));
            Assert.Equal(ZapfBedarfstagquelle.A100Referenz, t.Quelle);
            Assert.Same(Fiktiv, t.Herkunft);
            Assert.True(Relativ(t.TagessummeKwh, 19.5) < 1e-12);
            Assert.True(Relativ(t.MinutenKwh[1200], 1.5) < 1e-12);
            Assert.Throws<ZapfAuslegungException>(() => Bedarfstag.AusKatalog(null, 1.0));
        }

        [Fact]
        public void Die_Vorgaberegel_nimmt_nie_still_das_Stundenprofil()
        {
            var konstruiert = new BedarfstagKatalogzeile(3, "Konstruiert (fiktiv)", "TEST-Z2", ZapfBedarfstagquelle.Konstruktor,
                null, Fiktiv, new[] { new Zapfereignis(0, 1, 1.0) });

            // Wohnen mit rechenbarer Kennzahl: DIN-4708-Profil.
            Assert.Equal(ZapfBedarfstagquelle.Din4708Profil, Bedarfstagregel.Waehlen(null, true, true, null).Quelle);
            // Wohnen ohne Kennzahl und Nichtwohnen: Konstruktor öffnen, keine Quelle.
            Bedarfstagwahl w = Bedarfstagregel.Waehlen(null, true, false, null);
            Assert.Null(w.Quelle);
            Assert.True(w.KonstruktorOeffnen);
            w = Bedarfstagregel.Waehlen(null, false, true, null);
            Assert.Null(w.Quelle);
            Assert.True(w.KonstruktorOeffnen);
            // Ein gewählter Katalogtag gilt vor der Vorgabe.
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, Bedarfstagregel.Waehlen(null, true, true, konstruiert).Quelle);
            // Ausdrückliche Wahl.
            Assert.Equal(ZapfBedarfstagquelle.Stundenprofil,
                         Bedarfstagregel.Waehlen(ZapfBedarfstagquelle.Stundenprofil, false, false, null).Quelle);
            w = Bedarfstagregel.Waehlen(ZapfBedarfstagquelle.Din4708Profil, false, true, null);
            Assert.Null(w.Quelle);
            Assert.True(w.KonstruktorOeffnen);
            w = Bedarfstagregel.Waehlen(ZapfBedarfstagquelle.A100Referenz, true, true, konstruiert);
            Assert.Null(w.Quelle);
            Assert.True(w.KonstruktorOeffnen);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor,
                         Bedarfstagregel.Waehlen(ZapfBedarfstagquelle.Konstruktor, false, false, konstruiert).Quelle);
        }

        // =================================================================================
        // Wochenreihe
        // =================================================================================

        private static Wochenbaustein Baustein(Nutzungsart art, double jahresKwh, double fKwA, int jan1 = 0)
        {
            ZonenStand z = Zone(art.Name, art.Id);
            Zeitstruktur s = Formvektor.Bilden(z, art, art.Tagesgaenge, Parameter(), null, null);
            ZapfTagtyp[] k = Zapfkalender.Bilden(jan1, We(jan1), null);
            return new Wochenbaustein(z.Name, Wochenreihe.TagesmengenAuslegung(jahresKwh, fKwA, s, k, jan1, z.Name), s, k);
        }

        [Fact]
        public void Die_Tagesmengen_der_Auslegung_tragen_f_KW_A_und_keinen_Kaltwassergang()
        {
            Assert.True(Relativ(Wochenreihe.KaltwasserfaktorAuslegung(50.0, 10.0, 12.0), 40.0 / 38.0) < 1e-15);
            Assert.Equal(ZapfAuslegungsfehler.TemperaturUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Wochenreihe.KaltwasserfaktorAuslegung(10.0, 10.0, 8.0)).Fehler);

            Nutzungsart art = Art();
            Wochenbaustein b = Baustein(art, 3650.0, 1.25);
            Assert.True(Relativ(b.TagesmengenKwh.Sum(), 3650.0 * 1.25) < 1e-12);
            // Flache Monate, gleiche Wochenfaktoren an Werktagen: zwei Montage tragen dieselbe Menge.
            Assert.True(Relativ(b.TagesmengenKwh[0], b.TagesmengenKwh[7]) < 1e-12);
        }

        [Fact]
        public void Die_Wochenreihe_ist_die_Woche_mit_der_groessten_Summe()
        {
            // Juni (Tage 152 … 181) mit doppeltem Monatsfaktor: Die maßgebende Woche liegt im Juni.
            var monate = Enumerable.Repeat(1.0, 12).ToArray();
            monate[5] = 2.0;
            Nutzungsart art = Art(monate: monate);
            Wochenbaustein a = Baustein(art, 3650.0, 1.0);
            Wochenbaustein b = Baustein(Art(id: 2), 730.0, 1.1);
            var zonen = new[] { a, b };
            ZapfTagtyp[] region = Zapfkalender.Bilden(0, We(0), null);

            Wochenreihe w = Wochenreihe.Bilden(zonen, 0, region);
            Assert.Equal(168, w.StundenKwh.Count);
            Assert.Equal(152, w.ErsterTag);
            // Die Summe der Woche ist die größte aller Fenster aus sieben Tagen.
            double[] s = Enumerable.Range(0, 365).Select(d => a.TagesmengenKwh[d] + b.TagesmengenKwh[d]).ToArray();
            double groesste = Enumerable.Range(0, 359).Max(d0 => Enumerable.Range(d0, 7).Sum(d => s[d]));
            Assert.True(Relativ(w.WochensummeKwh, groesste) < 1e-12);
            // Erste Woche mit der größten Summe: kein früheres Fenster erreicht sie.
            for (int d0 = 0; d0 < w.ErsterTag - 1; d0++)
                Assert.True(Enumerable.Range(d0, 7).Sum(d => s[d]) < groesste * (1 - 1e-12));
            // Die Stunden eines Tages summieren zur Tagesmenge; die Tagtypen folgen dem Kalender.
            for (int k = 0; k < 7; k++)
            {
                Assert.True(Relativ(w.TagessummenKwh[k], s[w.ErsterTag - 1 + k]) < 1e-12);
                Assert.Equal(region[w.ErsterTag - 1 + k], w.Tagtypen[k]);
            }
            Assert.Equal((w.ErsterTag - 1) % 7, w.WochentagErsterTag);
            Assert.Equal(w.TagessummenKwh.Max(), w.GroessterTagKwh);
        }

        [Fact]
        public void Tagesstunden_und_groesster_Tag_folgen_den_Tagesmengen()
        {
            Wochenbaustein a = Baustein(Art(), 3650.0, 1.0);
            var zonen = new[] { a };
            int tag = Wochenreihe.GroessterTag(zonen);
            Assert.Equal(a.TagesmengenKwh.Max(), a.TagesmengenKwh[tag - 1]);
            Assert.Equal(Array.IndexOf(a.TagesmengenKwh, a.TagesmengenKwh.Max()) + 1, tag);
            double[] h = Wochenreihe.Tagesstunden(zonen, tag);
            Assert.True(Relativ(h.Sum(), a.TagesmengenKwh[tag - 1]) < 1e-12);
            // Werktag im Standardsatz: je ein Viertel um 6, 7, 18 und 19 Uhr.
            Assert.True(Relativ(h[6], a.TagesmengenKwh[tag - 1] * 0.25) < 1e-12);
        }

        [Fact]
        public void Eine_ungueltige_Wochenreihe_wird_benannt_abgelehnt()
        {
            var typen = Enumerable.Repeat(ZapfTagtyp.Werktag, 7).ToArray();
            Assert.Equal(ZapfAuslegungsfehler.WochenreiheUngueltig, Assert.Throws<ZapfAuslegungException>(() =>
                Wochenreihe.Aus(new double[167], 1, 0, typen)).Fehler);
            var neg = new double[168];
            neg[3] = -1;
            Assert.Throws<ZapfAuslegungException>(() => Wochenreihe.Aus(neg, 1, 0, typen));
            Assert.Throws<ZapfAuslegungException>(() => Wochenreihe.Aus(new double[168], 1, 7, typen));
            Assert.Throws<ZapfAuslegungException>(() => Wochenreihe.Bilden(new Wochenbaustein[0], 0, new ZapfTagtyp[365]));
            Wochenreihe w = Wochenreihe.Aus(new double[168], 1, 6, typen);
            Assert.Equal(0, w.Wochentag(1));
        }
    }
}
