using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Referenzprojekt mit Kälteerzeuger — 1017</b> (Stufe KU2 Welle 4; Kühlkonzept 10.3–10.5,
    /// Einfrierregel „gesäte Kältedaten" in <c>Referenzlaeufe/LIESMICH.md</c>).
    ///
    /// <para>Die Testdatenbank trägt die Saat aus
    /// <c>Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py</c>: Die Wärmepumpe von 1017
    /// (Anlage 10211, Projektgerät 1017033) steht auf Kaskadenplatz 3, im Kühlbetrieb mit Kühl-Vorlauf
    /// 18 °C und Hilfsstromanteil 5 %, ohne Kühlträger (wie Heizbetrieb), und führt eine gesäte
    /// Kühlkennlinie — zwei Vorläufe, fünf Außentemperaturen, Laststufe 100.</para>
    ///
    /// <para><b>Die Rechenprobe gegen die Handrechnung je Vorlauf</b> (10.3; Abnahme KU2 in 11.1):
    /// je Stunde Kapazität = Zeitanteil × Pkühl(Außentemperatur), Kälte = min(Bedarf, Kapazität),
    /// Kältestrom = Kälte / EER × (1 + Hilfsstromanteil) — EER und Pkühl von Hand aus den gesäten
    /// Stützstellen (linear dazwischen, zur kalten Seite gekappt, zur warmen verlängert, wie die
    /// Projekteinstellung von 1017 es erlaubt).</para>
    ///
    /// <para><b>Phantasiewerte.</b> Die Kennlinie trägt runde, erfundene Zahlen — kein Produkt, keine
    /// Normzahl (dieselbe Phantasie-Kennlinie wie in <see cref="KaelteerzeugerTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ReferenzprojektKaelteerzeugerTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public ReferenzprojektKaelteerzeugerTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017, WP = 1017033, ANLAGE = 10211, KATALOGSATZ = 33, VORLAUF = 18;

        /// <summary>Das Projektgerät der Kopie im Referenzprojekt der Anlagenkopplung 1047.</summary>
        private const int WP_KOPIE = 1672046;
        private const double HILFSSTROM = 0.05;

        /// <summary>Die gesäte Kühlkennlinie (dieselben Zahlen wie das Skript).</summary>
        private static readonly int[] TEMPERATUREN = { 20, 25, 30, 35, 40 };
        private static readonly Dictionary<int, double[]> EER = new Dictionary<int, double[]>
        {
            [7] = new[] { 4.0, 3.6, 3.2, 2.8, 2.4 },
            [18] = new[] { 5.5, 5.0, 4.5, 4.0, 3.5 }
        };
        private static readonly Dictionary<int, double[]> PKUEHL = new Dictionary<int, double[]>
        {
            [7] = new[] { 12.0, 11.5, 11.0, 10.5, 10.0 },
            [18] = new[] { 15.0, 14.5, 14.0, 13.5, 13.0 }
        };

        private static object Skalar(string sql) => DataRepository.ExecuteScalar(sql);

        /// <summary>
        /// Die gesäten Kältedaten stehen, wie das Skript sie schreibt — und nur sie: Kaskadenplatz,
        /// Kühlbetrieb, Kühl-Vorlauf, Hilfsstromanteil, kein Kühlträger, keine Abrechnungsart, keine
        /// Nennkühlleistung, zehn Kennlinienzeilen in Kaltwasserlage ohne Dubletten; der Katalogsatz des
        /// Geräts trägt keine Kennlinie (deshalb Saat statt Übernahme, Kühlkonzept 10.4).
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_traegt_die_gesaeten_Kaeltedaten_des_Referenzprojekts()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(DbWerte.ERZEUGER_WAERMEPUMPE, Skalar("SELECT Tool_3 FROM Tab_Einstellungen WHERE ID_Projekt = 1017"));
            Assert.Equal(1L, Convert.ToInt64(Skalar("SELECT Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = 1017")));
            Assert.Equal(1L, Convert.ToInt64(Skalar("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = 1017033")));
            Assert.Equal(18L, Convert.ToInt64(Skalar("SELECT Kuehl_Vorlauf FROM Tab_WP WHERE ID = 1017033")));
            Assert.Equal(HILFSSTROM, Convert.ToDouble(Skalar("SELECT Kuehl_Hilfsstromanteil FROM Tab_WP WHERE ID = 1017033"),
                                                      CultureInfo.InvariantCulture));
            Assert.True(Leer(Skalar("SELECT Kuehlleistung FROM Tab_WP WHERE ID = 1017033")));
            Assert.True(Leer(Skalar("SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID = 10211")));
            Assert.True(Leer(Skalar("SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID = 10211")));
            Assert.True(Leer(Skalar("SELECT WQ_Typ FROM Tab_Energieanlagen WHERE ID = 10211")));

            // Die Kühlkennlinien der Projektseite - die Saat an 1017 und ihre Kopie im Referenzprojekt
            // der Anlagenkopplung 1047 (anlagenkopplung_1047_referenzprojekt.py), sonst keine.
            Assert.Equal(20L, Convert.ToInt64(Skalar("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung")));
            Assert.Equal(10L, Convert.ToInt64(Skalar("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = " + WP)));
            List<KuehlkennlinienZeile> zeilen = KenndatenKuehlungCtrl.ZeilenProjekt(WP);
            Assert.Equal(10, zeilen.Count);
            List<KuehlkennlinienZeile> kopie = KenndatenKuehlungCtrl.ZeilenProjekt(WP_KOPIE);
            Assert.Equal(zeilen.Select(z => (z.Vorlauf, z.Temperatur, z.Eer, z.Pkuehl, z.Last)).OrderBy(t => t),
                         kopie.Select(z => (z.Vorlauf, z.Temperatur, z.Eer, z.Pkuehl, z.Last)).OrderBy(t => t));
            foreach (KuehlkennlinienZeile z in zeilen)
            {
                int i = Array.IndexOf(TEMPERATUREN, z.Temperatur);
                Assert.True(i >= 0, "Temperatur " + z.Temperatur);
                Assert.True(EER.ContainsKey(z.Vorlauf), "Vorlauf " + z.Vorlauf);
                Assert.Equal(EER[z.Vorlauf][i], z.Eer);
                Assert.Equal(PKUEHL[z.Vorlauf][i], z.Pkuehl);
                Assert.Equal(100, z.Last);
                Assert.True(z.Temperatur > z.Vorlauf, "Kaltwasserlage: jede Stützstelle wärmer als der Vorlauf (K22).");
            }
            Assert.Equal(10, zeilen.Select(z => (z.Vorlauf, z.Temperatur, z.Last)).Distinct().Count());
            Assert.All(Kuehlkennlinie.Befunde(zeilen), b => Assert.Equal(KuehlblockBefund.Gueltig, b.Value));

            Kuehlkennlinie k = Kuehlkennlinie.Bilden(zeilen, VORLAUF);
            Assert.True(k.Rechenbar);
            Assert.Equal(VORLAUF, k.Vorlauf);
            Assert.False(k.VorlaufAusgewichen);
            Assert.Equal(new[] { 7, 18 }, k.Stuetzstellen);
            Assert.Equal(100, k.Laststufe);
            Assert.Equal(5, k.Punkte);
            Assert.Equal(0, k.Dubletten);
            Assert.Equal(7, Kuehlkennlinie.Bilden(zeilen, null).Vorlauf);          // NULL = kleinster Stützwert

            Assert.True(KenndatenKuehlungCtrl.HatKenndatenProjekt(WP));
            Assert.False(KenndatenKuehlungCtrl.HatKenndatenStamm(KATALOGSATZ));
            Assert.Null(WPCtrl.KuehlbetriebSperrgrund(WP, PROJEKT));
        }

        /// <summary>EER und Pkühl von Hand aus den gesäten Stützstellen [—, kW].</summary>
        private static (double Eer, double Pkuehl) Hand(int vorlauf, double t, bool verlaengern)
        {
            double[] eer = EER[vorlauf], pk = PKUEHL[vorlauf];
            int n = TEMPERATUREN.Length;
            if (t <= TEMPERATUREN[0]) return (eer[0], pk[0]);                          // kalt: gekappt
            if (t >= TEMPERATUREN[n - 1])
            {
                if (!verlaengern || t == TEMPERATUREN[n - 1]) return (eer[n - 1], pk[n - 1]);
                double s = (t - TEMPERATUREN[n - 2]) / (TEMPERATUREN[n - 1] - TEMPERATUREN[n - 2]);
                return (eer[n - 2] + s * (eer[n - 1] - eer[n - 2]), pk[n - 2] + s * (pk[n - 1] - pk[n - 2]));
            }
            int i = 1;
            while (t > TEMPERATUREN[i]) i++;
            double a = (t - TEMPERATUREN[i - 1]) / (TEMPERATUREN[i] - TEMPERATUREN[i - 1]);
            return (eer[i - 1] + a * (eer[i] - eer[i - 1]), pk[i - 1] + a * (pk[i] - pk[i - 1]));
        }

        private static void Nahe(double soll, double ist, string was)
        {
            Assert.True(Math.Abs(soll - ist) <= 1e-9 * Math.Max(1.0, Math.Abs(soll)),
                string.Format(CultureInfo.InvariantCulture, "{0}: Handrechnung {1:R}, Lauf {2:R}", was, soll, ist));
        }

        /// <summary>
        /// <b>Rechenprobe gegen die Handrechnung je Vorlauf</b> (Kühlkonzept 10.3, Abnahme KU2): Das
        /// Referenzprojekt rechnet mit 18 °C; dieselbe Probe mit 7 °C in der Arbeitskopie. Je Stunde
        /// stimmen Kälte und Kältestrom mit der Handrechnung überein, im Jahr die Summen und die
        /// gespeicherten Modulspalten (zwei Stellen); der kältere Vorlauf rechnet mit dem kleineren EER
        /// — der Kühl-Vorlauf wirkt.
        /// </summary>
        [Fact]
        public void Rechenprobe_gegen_die_Handrechnung_je_Vorlauf()
        {
            if (!_db.Vorhanden) return;
            var eerJahr = new Dictionary<int, double>();

            foreach (int vorlauf in new[] { VORLAUF, 7 })
            {
                if (vorlauf != VORLAUF)
                {
                    WPCtrl.SpeicherErgebnis s = WPCtrl.KuehlkonfigurationSchreiben(WP, PROJEKT, true, vorlauf, HILFSSTROM);
                    Assert.True(s.Ok, s.Meldung);
                }

                var lauf = new SimulationRunner();
                int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
                Assert.True(kopf > 0, fehler);
                Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));

                Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
                Assert.NotNull(kaskade);
                Kaelteerzeuger e = Assert.Single(kaskade.Erzeuger);
                Assert.Equal(ANLAGE, e.AnlagenID);
                Assert.Equal(WP, e.IdWp);
                Assert.Equal(vorlauf, e.Kennlinie.Vorlauf);
                Assert.Equal(HILFSSTROM, e.Hilfsstromanteil);
                Assert.Equal(0, e.Kuehltraeger);                // wie Heizbetrieb
                Assert.False(e.NebenDerStufenrechnung);

                double[] t = lauf.simulation_Waermebedarf.Stundentemperatur;
                bool verlaengern = lauf.sim.simulation_wp.Extrapolation_Erlaubt;
                double kaelte = 0, strom = 0;
                int stunden = 0;
                for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
                {
                    Assert.Equal(t[h], e.Quelltemperatur[h]);   // Wärmequelle leer: die Außenluft
                    double anteil = e.Zeitanteil[h];
                    Assert.InRange(anteil, 0.0, 1.0);
                    if (!kaskade.Kuehltage[h / 24]) Assert.Equal(0.0, anteil);

                    (double eer, double pk) = Hand(vorlauf, t[h], verlaengern);
                    double kapazitaet = anteil * pk;
                    double soll = kapazitaet > 0 && eer > 0 ? Math.Min(kaskade.Bedarf_stuendlich[h], kapazitaet) : 0.0;
                    double sollStrom = soll > 0 ? soll / eer * (1.0 + HILFSSTROM) : 0.0;
                    Nahe(soll, e.Kaelte_stuendlich[h], "Kälte Stunde " + h);
                    Nahe(sollStrom, e.Strom_stuendlich[h], "Kältestrom Stunde " + h);
                    kaelte += soll;
                    strom += sollStrom;
                    if (soll > 0) stunden++;
                }
                Assert.True(kaelte > 0 && strom > 0);
                Nahe(kaelte, e.KaelteGesamtKwh, "Kälte im Jahr");
                Nahe(strom, e.StromGesamtKwh, "Kältestrom im Jahr");
                Nahe(strom - strom / (1.0 + HILFSSTROM), e.HilfsstromGesamtKwh, "Hilfsstrom im Jahr");
                Assert.Equal(stunden, e.StundenMitKaelte);
                Assert.True(kaelte <= kaskade.BedarfGesamtKwh + 1e-6);

                // Gespeichert: die Modulspalten mit zwei Stellen.
                ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
                ErgebnisWaermepumpeModulModel mo = Assert.Single(erg.Waermepumpe.Module);
                Assert.Equal(Math.Round(kaelte / 1000.0, 2, MidpointRounding.AwayFromZero), mo.Kaelteproduktion.Value, 9);
                Assert.Equal(Math.Round(strom / 1000.0, 2, MidpointRounding.AwayFromZero), mo.Stromverbrauch_Kuehlung.Value, 9);
                Assert.True(mo.Kaeltestrom_Netzbezug.Value <= mo.Stromverbrauch_Kuehlung.Value + 1e-9);
                Assert.Null(mo.Kuehl_CarrierId);

                // Der Netzbezug des Kältestroms: anteilig am Netzbezug der Stufenrechnung (E34, auch ohne
                // Kühlträger) - höchstens der ganze Kältestrom.
                Assert.InRange(e.NetzbezugKwh, 0.0, e.StromGesamtKwh * (1.0 + 1e-12));

                eerJahr[vorlauf] = kaelte / strom;
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "1017, Kühl-Vorlauf {0} °C: Kältebedarf {1:F3} MWh/a, gedeckt {2:F3} MWh/a ({3:F1} %), " +
                    "Kältestrom {4:F3} MWh/a (Hilfsstrom {5:F3}, aus dem Netz {9:F4} von {10:F4}), EER-Jahreswert {6:F3}, " +
                    "Stunden mit Kälte {7}, Kühltage {8}",
                    vorlauf, kaskade.BedarfGesamtKwh / 1000.0, kaelte / 1000.0, 100.0 * kaelte / kaskade.BedarfGesamtKwh,
                    strom / 1000.0, e.HilfsstromGesamtKwh / 1000.0, kaelte / strom, stunden, kaskade.AnzahlKuehltage,
                    e.NetzbezugKwh / 1000.0, e.StromGesamtKwh / 1000.0));
            }

            Assert.True(eerJahr[VORLAUF] > eerJahr[7], "Der wärmere Kühl-Vorlauf rechnet mit dem größeren EER.");
        }

        /// <summary>
        /// <b>Kosten und Emissionen des Kältestroms</b> an 1017 gegen den Stand ohne Kälteerzeuger (die
        /// Wärmepumpe ohne Kaskadenplatz und ohne Kühlbetrieb, wie mit KU1): Der Kältestrom trägt den
        /// Stromträger des Projekts; die Stromkosten des Anschlusses wachsen um den Mehrbezug mal dessen
        /// Arbeitspreis, die Emissionen um den Mehrbezug mal dessen Faktor — Grund- und Leistungspreis
        /// bleiben. (Die Energiekosten des ganzen Projekts bleiben in 1017 aus: Der Brennstoff des BHKW
        /// trägt in der Testdatenbank keinen Arbeitspreis — ein benannter Grund, unverändert.) Die
        /// Zahlen stehen im Protokoll der Schlusswelle KU2.
        /// </summary>
        [Fact]
        public void Kosten_und_Emissionen_gegen_den_Stand_ohne_Kaelteerzeuger()
        {
            if (!_db.Vorhanden) return;

            VariantenDaten mit = Rechnen(out ErgebnisModel ergMit);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Tool_3 = '' WHERE ID_Projekt = 1017"));
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP, PROJEKT, false, VORLAUF, HILFSSTROM).Ok);
            VariantenDaten ohne = Rechnen(out ErgebnisModel ergOhne);

            Assert.Null(ergOhne.Waermepumpe);
            Assert.Null(ohne.KaeltestromNetzbezugMWh);
            Assert.NotNull(mit.KaeltestromNetzbezugMWh);
            Assert.NotNull(mit.StromkostenNetz);
            Assert.NotNull(ohne.StromkostenNetz);
            Assert.Equal(ohne.EnergiekostenGrund, mit.EnergiekostenGrund);

            int traeger = Kaeltestromabrechnung.Projekttraeger(PROJEKT);
            double preis = KostenEmissionRechner.ArbeitspreisJeKwh(PROJEKT, traeger).Value;
            double mehrbezug = ergMit.Energiebedarf.Stromrestbedarf - ergOhne.Energiebedarf.Stromrestbedarf;
            Assert.True(mehrbezug > 0);
            Nahe(mehrbezug * 1000.0 * preis, mit.StromkostenNetz.Value - ohne.StromkostenNetz.Value, "Mehrkosten Strom");
            Nahe(mit.KaeltestromNetzbezugMWh.Value * 1000.0 * preis, mit.KaeltestromKosten.Value, "Kosten Kältestrom");
            Assert.Equal(0.0, mit.StromkostenKuehltraeger);

            Emissionsfaktoren netz = Emissionsquelle.Netzstrom(PROJEKT, Emissionsquelle.StromTraeger(PROJEKT), mit.EmissionsModus);
            double faktor = (netz != null && netz.Co2Gepflegt && netz.Co2GKwh > 0
                ? netz.Co2GKwh : KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH) / 1000.0;   // t/MWh
            Nahe(mit.KaeltestromNetzbezugMWh.Value * faktor, mit.KaeltestromCO2t.Value, "CO₂ Kältestrom");
            if (mit.CO2Gesamt.HasValue && ohne.CO2Gesamt.HasValue)
                Nahe(mehrbezug * faktor, mit.CO2Gesamt.Value - ohne.CO2Gesamt.Value, "Mehremission");

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1017 ohne/mit Kälteerzeuger: Netzbezug {0:F2}/{1:F2} MWh/a, Stromkosten des Anschlusses {2:F2}/{3:F2} €/a, " +
                "CO₂ {4}/{5} t/a; Kältestrom {6:F2} MWh/a Netzbezug, {7:F2} €/a, {8:F3} t/a (Träger {9}, {10:F5} €/kWh, " +
                "{11:F1} g/kWh); Energiekosten: {12}",
                ergOhne.Energiebedarf.Stromrestbedarf, ergMit.Energiebedarf.Stromrestbedarf,
                ohne.StromkostenNetz.Value, mit.StromkostenNetz.Value,
                ohne.CO2Gesamt.HasValue ? ohne.CO2Gesamt.Value.ToString("F3", CultureInfo.InvariantCulture) : "—",
                mit.CO2Gesamt.HasValue ? mit.CO2Gesamt.Value.ToString("F3", CultureInfo.InvariantCulture) : "—",
                mit.KaeltestromNetzbezugMWh.Value, mit.KaeltestromKosten.Value, mit.KaeltestromCO2t.Value,
                Emissionsquelle.TraegerName(traeger), preis, faktor * 1000.0,
                mit.Energiekosten.HasValue ? mit.Energiekosten.Value.ToString("F2", CultureInfo.InvariantCulture) : mit.EnergiekostenGrund));
        }

        private static VariantenDaten Rechnen(out ErgebnisModel erg)
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static bool Leer(object wert) => wert == null || wert == DBNull.Value;
    }
}
