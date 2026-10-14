using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ausweis der Vorlaufwahl im Ergebnis</b> (VW1a, E88): (a) die Zählung am gerechneten Vorlauf über drei
    /// Stützstellen samt einer Stunde darüber und darunter und der Ausweis daraus; (b) das gekoppelte Referenzprojekt
    /// 1047 schreibt die drei Spalten, und ihre Summe trifft die Stunden der Protokollmeldung; (c) Projekt 1017 ohne
    /// Kopplung lässt alle drei NULL.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorlaufwahlErgebnisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  (a) Rechenprobe ohne Datenbank
        // =============================================================================

        [Fact]
        public void Drei_Stuetzstellen_darueber_und_darunter_werden_getrennt_gezaehlt()
        {
            var wahl = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);
            // 30 darunter (unterste), 36 -> 35, 40 Gleichstand -> 45 (die höhere), 41 und 47 -> 45, 52 und 55 -> 55,
            // 60 darüber (oberste).
            double[] reihe = { 30.0, 36.0, 40.0, 41.0, 47.0, 52.0, 55.0, 60.0 };
            foreach (double v in reihe) wahl.Zaehlen(v, true, out _);

            // Die Zähler der Protokollmeldung: Stunden je Stützstelle samt der Stunden außerhalb.
            Assert.Equal(new[] { 2, 3, 3 }, wahl.Stunden);
            Assert.Equal(1, wahl.Darueber);
            Assert.Equal(1, wahl.Darunter);

            SimulationWaermepumpe.VorlaufwahlAusweis a = wahl.Ausweis();
            Assert.Equal("35:1;45:3;55:2", a.StundenText);
            Assert.Equal(1, a.Darueber);
            Assert.Equal(1, a.Darunter);
            int summe = VorlaufwahlSchema.StundenLesen(a.StundenText).Sum(p => p.Value) + a.Darueber + a.Darunter;
            Assert.Equal(reihe.Length, summe);
            Assert.Equal(wahl.Stunden.Sum(), summe);
        }

        [Fact]
        public void Verbotene_Extrapolation_zaehlt_nicht_und_eine_Stuetzstelle_traegt_beide_Raender()
        {
            var wahl = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);
            Assert.Equal(SimulationWaermepumpe.Vorlauflage.Verboten, wahl.Zaehlen(60.0, false, out _));
            Assert.Equal(new[] { 0, 0, 0 }, wahl.Stunden);
            Assert.Equal(0, wahl.Darueber);
            Assert.Equal("35:0;45:0;55:0", wahl.Ausweis().StundenText);

            var eine = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(45);
            foreach (double v in new[] { 40.0, 45.0, 50.0 }) eine.Zaehlen(v, true, out _);
            SimulationWaermepumpe.VorlaufwahlAusweis a = eine.Ausweis();
            Assert.Equal("45:1", a.StundenText);
            Assert.Equal(1, a.Darueber);
            Assert.Equal(1, a.Darunter);
        }

        // =============================================================================
        //  (b), (c) Datenbankproben
        // =============================================================================

        private static ErgebnisWaermepumpeModel LaufUndLesen(int projekt)
        {
            SimulationProtokoll.NeuStarten();
            int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            ErgebnisWaermepumpeModel w = new ErgebnisCtrl().Load(projekt).Waermepumpe;
            Assert.NotNull(w);
            return w;
        }

        /// <summary>
        /// Die Stunden einer Randmeldung (Vorlage „{0}: … {1} Stunden über/unter … {2} …“): gesucht wird der Hinweis
        /// mit dem festen Text zwischen {1} und {2}, gelesen die Zahl unmittelbar davor. Ohne Meldung 0.
        /// </summary>
        private static int StundenDerRandmeldung(IEnumerable<string> hinweise, string vorlage)
        {
            string seg = vorlage.Substring(vorlage.IndexOf("{1}", StringComparison.Ordinal) + 3);
            seg = seg.Substring(0, seg.IndexOf("{2}", StringComparison.Ordinal));
            string hinweis = hinweise.SingleOrDefault(h => h.Contains(seg, StringComparison.Ordinal));
            if (hinweis == null) return 0;
            Match m = Regex.Match(hinweis.Substring(0, hinweis.IndexOf(seg, StringComparison.Ordinal)), @"(\d+)$");
            Assert.True(m.Success, hinweis);
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Das_gekoppelte_Referenzprojekt_1047_weist_die_Stunden_der_Protokollmeldung_aus()
        {
            if (!_db.Vorhanden) return;
            ErgebnisWaermepumpeModel w = LaufUndLesen(1047);
            List<string> hinweise = SimulationProtokoll.Aktuell.Hinweise.ToList();

            ErgebnisWaermepumpeModulModel mo = Assert.Single(w.Module, m => m.Vorlaufwahl_Stunden != null);
            Assert.NotNull(mo.Vorlauf_Darueber_Stunden);
            Assert.NotNull(mo.Vorlauf_Darunter_Stunden);
            IReadOnlyList<KeyValuePair<int, int>> paare = VorlaufwahlSchema.StundenLesen(mo.Vorlaufwahl_Stunden);
            Assert.True(paare.Count >= 2, mo.Vorlaufwahl_Stunden);
            Assert.Equal(paare.Select(p => p.Key).OrderBy(k => k), paare.Select(p => p.Key));
            Assert.All(paare, p => Assert.True(p.Value >= 0));

            // Die Protokollmeldung: „{0}: Kennlinie je Stunde … Stützstelle: 35 °C: 1200 h, 45 °C: 800 h.“
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_WP_VORLAUF_KENNLINIENWAHL;
            string vorDenTeilen = kopf.Substring(kopf.IndexOf("{0}", StringComparison.Ordinal) + 3);
            vorDenTeilen = vorDenTeilen.Substring(0, vorDenTeilen.IndexOf("{1}", StringComparison.Ordinal));
            string meldung = Assert.Single(hinweise, h => h.Contains(vorDenTeilen, StringComparison.Ordinal));
            string teile = meldung.Substring(meldung.IndexOf(vorDenTeilen, StringComparison.Ordinal) + vorDenTeilen.Length);
            int stundenMeldung = Regex.Matches(teile, @"(-?\d+) °C: (\d+) h")
                                      .Sum(m => int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
            Assert.True(stundenMeldung > 0, meldung);

            int summe = paare.Sum(p => p.Value) + mo.Vorlauf_Darueber_Stunden.Value + mo.Vorlauf_Darunter_Stunden.Value;
            Assert.Equal(stundenMeldung, summe);
            Assert.InRange(summe, 1, 8760);

            // Darüber und darunter sind dieselben Zähler wie die Randmeldungen (ohne Meldung: 0).
            Assert.Equal(StundenDerRandmeldung(hinweise, WindowsFormsApplication1.MyResource.Resource.SIMENG_WP_VORLAUF_AUSSERHALB_HINWEIS),
                         mo.Vorlauf_Darueber_Stunden.Value);
            Assert.Equal(StundenDerRandmeldung(hinweise, WindowsFormsApplication1.MyResource.Resource.SIMENG_WP_VORLAUF_UNTER_STUETZSTELLEN),
                         mo.Vorlauf_Darunter_Stunden.Value);

            // Die Zeile in der Datenbank trägt dieselben Werte.
            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ErgebnisWaermepumpeModul WHERE Vorlaufwahl_Stunden = ? AND Vorlauf_Darueber_Stunden = ? " +
                "AND Vorlauf_Darunter_Stunden = ?",
                new DbParam("@t", mo.Vorlaufwahl_Stunden), new DbParam("@o", mo.Vorlauf_Darueber_Stunden.Value),
                new DbParam("@u", mo.Vorlauf_Darunter_Stunden.Value))));
        }

        [Fact]
        public void Projekt_1017_ohne_Kopplung_laesst_alle_drei_Spalten_leer()
        {
            if (!_db.Vorhanden) return;
            ErgebnisWaermepumpeModel w = LaufUndLesen(1017);
            Assert.NotEmpty(w.Module);
            Assert.All(w.Module, m =>
            {
                Assert.Null(m.Vorlaufwahl_Stunden);
                Assert.Null(m.Vorlauf_Darueber_Stunden);
                Assert.Null(m.Vorlauf_Darunter_Stunden);
            });
        }
    }
}
