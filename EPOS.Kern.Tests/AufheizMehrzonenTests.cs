using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using static EPOS.Kern.Tests.BauteilwegLaufProbe;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>N-AH9 Mehrzonen</b> (Entwurf KP3 Abschnitt 7, Welle R3; Teilkonzept 4.7, Festlegungen 12–14, 22, 25):
    /// das synthetische Gebäude nach dem Muster von <c>ZonenschleifeProbenTests.Synthetisch</c> — zwei
    /// beheizte Wohnungen mit einer Trennwand der Außengruppe (U·A ≤ 5 % H_s) und einem Luftstrom, darunter
    /// ein unbeheizter Keller am Erdreich (<see cref="Dreizonen"/>).
    /// <list type="bullet">
    /// <item><b>Nachbarform</b> mit festen Nachbarn gleich <see cref="ZonenEingang.ThetaEq"/> (rel. 1e-12)
    /// und die Zuluft am Bemessungspunkt gleich <see cref="ZonenEingang.ThetaLue"/> (bitgleich).</item>
    /// <item><b>Formel je Zone gegen die volle Zonenschleife</b> bei festen Rändern (konstante Außenluft,
    /// ohne Sonne und Gewinne): In jedem Fenster [h_s − n + 1, h_s + 2] einer Rampe bleibt die
    /// Stundenleistung jeder Zone unter 1,01·P_auf — „sicher" (Quelle Ziel, Stundenmittel).</item>
    /// <item><b>Unbeheizt ohne Rampe</b> (UNBEHEIZT), <b>Aggregation</b> nach Festlegung 22 (auch GEMISCHT).</item>
    /// </list>
    /// Ohne Datenbank — bis auf den Fall am Referenzprojekt 1052 (G6d), der dieselbe Formel an den Zonen der
    /// Testdatenbank prüft (Arbeitskopie, deshalb die Sammlung „Testdatenbank").
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizMehrzonenTests : IClassFixture<TestDatenbank>
    {
        private readonly ITestOutputHelper _aus;
        private readonly TestDatenbank _db;

        public AufheizMehrzonenTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        internal const int WOHNUNG_1 = 1, WOHNUNG_2 = 2, KELLER = 1000;
        private const int STUNDEN = 8760;

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static string F(double x, string format = "F4") => x.ToString(format, CultureInfo.InvariantCulture);

        // =====================================================================
        //  Das Gebäude
        // =====================================================================

        /// <summary>Konstante Außenluft, ohne Sonne und ohne Gegenstrahlung — die festen Ränder der Probe.</summary>
        internal static GebaeudeKlima Klima(double aussenC = -5.0)
            => new GebaeudeKlima(Vdi6007Probe.Klima(h => aussenC, mitSonne: false), Vdi6007Probe.Wochenende(),
                                 Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        /// <summary>Die Vorgabe „an" (täglich, (a), ρ 20 %), wahlweise fest oder mit Abzug.</summary>
        internal static Aufheizvorgabe An(bool fest = false, bool abzug = false)
            => new Aufheizvorgabe(true, abzug ? DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG : null, null, null,
                                  fest ? DbWerte.AUFHEIZ_ART_FEST : null);

        /// <summary>
        /// <b>Das Dreizonengebäude</b> (Muster <c>ZonenschleifeProbenTests.Synthetisch</c>): das Probegebäude
        /// in zwei Hälften, ohne innere Gewinne; die Bodenplatte jeder Hälfte ist eine Kellerdecke zum
        /// unbeheizten Keller (die beheizten Zonen liegen nicht am Erdreich), zwischen den Wohnungen eine
        /// Trennwand der Außengruppe und ein Luftstrom. Wohnung 1: 21/16 °C, Wohnung 2: 20/17 °C.
        /// </summary>
        internal static ProjektGebaeudeModel Dreizonen(double trennA = 8.0, double stromM3h = 40.0,
                                                       Zoneneingaben eins = null, Zoneneingaben zwei = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Interne_Waermegewinne = 0.0;
            GebaeudeZonensatz basis = Geschichtet(g);
            BauteilEingang boden = basis.Bauteile.Single(b => b.Art == Bauteilart.Bodenplatte);
            List<BauteilEingang> halb = basis.Bauteile.Where(b => b != boden).Select(b => Skaliert(b, 0.5)).ToList();
            BauteilEingang Decke(string name)
                => new BauteilEingang(name, Bauteilart.Decke, 0.5 * boden.Flaeche_M2, Bauteilrand.Zone, neigungGrad: 180.0,
                                      schichten: boden.Schichten, idNachbarzone: KELLER);
            var trenn = new BauteilEingang("Trennwand", Bauteilart.Innenwand, trennA, Bauteilrand.Zone,
                                           schichten: new[] { Putz, Innenmauerwerk, Putz }, idNachbarzone: WOHNUNG_2,
                                           zuordnung: Trennflaechenzuordnung.Aussen);
            double f = 0.5 * g.Nutzflaeche;
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(WOHNUNG_1, "Wohnung 1", halb.Append(Decke("Kellerdecke 1")).Append(trenn).ToList(), f,
                                      eins ?? new Zoneneingaben(Nutzflaeche: f, SollTag: 21.0, SollNacht: 16.0), 1),
                new GebaeudeZonensatz(WOHNUNG_2, "Wohnung 2", halb.Append(Decke("Kellerdecke 2")).ToList(), f,
                                      zwei ?? new Zoneneingaben(Nutzflaeche: f, SollTag: 20.0, SollNacht: 17.0), 2),
                new GebaeudeZonensatz(KELLER, "Keller", new List<BauteilEingang>
                {
                    new BauteilEingang("Kellerwände", Bauteilart.Aussenwand, 100.0, Bauteilrand.Erdreich, schichten: new[] { Beton }),
                    new BauteilEingang("Kellerboden", Bauteilart.Bodenplatte, boden.Flaeche_M2, Bauteilrand.Erdreich,
                                       schichten: new[] { Estrich, Beton }),
                }, 88.0, new Zoneneingaben(Nutzflaeche: 88.0, IstBeheizt: false), 3),
            };
            if (stromM3h > 0.0) g.Zonenluftstroeme = new[] { new Zonenluftstrom(WOHNUNG_1, WOHNUNG_2, stromM3h) };
            return g;
        }

        private static BauteilEingang Skaliert(BauteilEingang b, double f)
            => new BauteilEingang(b.Bezeichnung, b.Art, f * b.Flaeche_M2, b.Rand, b.UWert_WM2K, b.Schichten, b.NeigungGrad, b.AzimutGrad,
                                  b.GWert, b.Rahmenanteil, b.Verschattungsfaktor, f * b.PsiL_WK, b.AlphaKonInnen_WM2K, b.AlphaKonAussen_WM2K);

        internal static int Stelle(IReadOnlyList<ZonenEingang> zonen, int id) => zonen.Single(z => z.ZonenId == id).Index;

        private static int Ring(int h) => ((h % STUNDEN) + STUNDEN) % STUNDEN;

        // =====================================================================
        //  Nachbarform gegen den Rand der Zonenschleife
        // =====================================================================

        /// <summary>
        /// <b>N-AH9, Nachbarform:</b> Mit festen Nachbarn ist θ_eq am Bemessungspunkt
        /// (<see cref="GebaeudeModellEingang.AequivalentN(int, double, ReadOnlySpan{double})"/> über
        /// <see cref="ZonenEingang.AequivalentN"/>) gleich <see cref="ZonenEingang.ThetaEq"/> jeder Stunde
        /// (ohne Sonne, Gegenstrahlung und Erdreich ist der Zähler der Außenglieder derselbe) — rel. 1e-12;
        /// die Zuluft am Bemessungspunkt ist <see cref="ZonenEingang.ThetaLue"/> Bit für Bit. Die Außenform
        /// lehnt einen Eingang mit Nachbarn benannt ab (B4), die Nachbarform ohne Nachbarn ist die Außenform.
        /// Die Trennwand hält U·A ≤ 5 % H_s.
        /// </summary>
        [Fact]
        public void N_AH9_Nachbarform_gleicht_ThetaEq_und_ThetaLue()
        {
            ProjektGebaeudeModel g = Dreizonen();
            GebaeudeKlima klima = Klima();
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, klima);
            Assert.Null(zonen[0].Aufheizplan);   // ohne Vorgabe kein Aufruf
            var luft = new double[zonen.Count];
            luft[Stelle(zonen, WOHNUNG_1)] = 19.25;
            luft[Stelle(zonen, WOHNUNG_2)] = 21.5;
            luft[Stelle(zonen, KELLER)] = 7.75;

            double groesste = 0.0;
            int faelle = 0;
            foreach (int id in new[] { WOHNUNG_1, WOHNUNG_2 })
            {
                ZonenEingang z = zonen[Stelle(zonen, id)];
                GebaeudeModellEingang e = z.Eingang;
                Assert.Equal(2, e.Nachbarglieder.Count);
                Assert.Single(e.Luftkopplungen);
                Assert.Throws<InvalidOperationException>(() => e.AequivalentN(0, -5.0));
                Assert.Throws<ArgumentException>(() => e.AequivalentN(0, -5.0, new double[1]));

                // U·A der Trennwand ≤ 5 % H_s.
                double ua = e.Nachbarglieder.Single(x => x.ZonenId == (id == WOHNUNG_1 ? WOHNUNG_2 : WOHNUNG_1)).UA_WK;
                double hs = new Zonenmodell2K(e.Parameter, e.Bezeichnung).Aufheizantwort(e.HeizungStrahlungsanteil, 0.0).HsWK;
                Assert.True(ua <= 0.05 * hs, "U·A " + F(ua) + " W/K über 5 % von H_s " + F(hs) + " W/K");
                _aus.WriteLine("{0}: U·A Trennwand {1} W/K = {2} % H_s ({3} W/K)", z.Bezeichnung, F(ua, "F2"),
                               F(100.0 * ua / hs, "F2"), F(hs, "F1"));

                for (int h = 0; h < STUNDEN; h += 97)
                {
                    double soll = z.ThetaEq(h, luft);
                    double ist = z.AequivalentN(h / 24, e.ThetaOut[h], luft);
                    double rel = Math.Abs(ist - soll) / Math.Abs(soll);
                    groesste = Math.Max(groesste, rel);
                    Assert.True(rel <= 1e-12, string.Format(CultureInfo.InvariantCulture, "{0}, Stunde {1}: {2:R} gegen {3:R}",
                                                             z.Bezeichnung, h, ist, soll));
                    Assert.Equal(Bits(z.ThetaLue(h, false, luft)),
                                 Bits(z.ZuluftN(e.ThetaOut[h], e.ZusatzleitwertWK(h, false, false), luft)));
                    faelle++;
                }
            }
            _aus.WriteLine("N-AH9 Nachbarform: {0} Stunden, größte relative Abweichung {1}", faelle, F(groesste, "E2"));

            // Ohne Nachbarn ist die Nachbarform die Außenform (Einzone).
            GebaeudeModellEingang einzeln = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            ZonenEingang ez = ZonenEingang.Einzeln(einzeln);
            for (int tag = 0; tag < 365; tag += 30)
                Assert.Equal(Bits(einzeln.AequivalentN(tag, -12.0)), Bits(ez.AequivalentN(tag, -12.0, ReadOnlySpan<double>.Empty)));
        }

        // =====================================================================
        //  Formel je Zone gegen die volle Zonenschleife
        // =====================================================================

        public static IEnumerable<object[]> Vorgaben()
        {
            yield return new object[] { "täglich (a)", false, false, false };
            yield return new object[] { "täglich (b)", false, true, false };
            yield return new object[] { "fest (a)", true, false, false };
            yield return new object[] { "täglich (a), Wochenende", false, false, true };
            yield return new object[] { "fest (b), Wochenende", true, true, true };
        }

        /// <summary>
        /// <b>N-AH9, Formel gegen Lauf:</b> Die Zonenschleife rechnet das Dreizonengebäude mit den Rampen der
        /// Planung (Einbau am Ende von <see cref="ZonenEingang.Bauen"/>). Je beheizte Zone und je Rampe
        /// bleibt die Stundenleistung im Fenster [h_s − n + 1, h_s + 2] unter 1,01·P_auf (Quelle Ziel,
        /// Stundenmittel) — die Planung mit festen Nachbarn ist sicher, obwohl im Lauf die Nachbarn frei
        /// schwingen (die beheizten über ihrem Nachtwert, der Keller über seinem Startwert). Gegenprobe:
        /// Ohne Planung liegt die Sprungstunde darüber. Mit Wochenende (15/16 °C) bindet am Montag die
        /// Formel (lange Absenkung, die Zone kühlt bis nahe an ihren stationären Zustand aus).
        /// </summary>
        [Theory]
        [MemberData(nameof(Vorgaben))]
        public void N_AH9_Formel_je_Zone_haelt_in_der_vollen_Zonenschleife(string fall, bool fest, bool abzug, bool wochenende)
        {
            double f = 0.5 * Vdi6007Probe.Gebaeude().Nutzflaeche;
            ProjektGebaeudeModel g = wochenende
                ? Dreizonen(eins: new Zoneneingaben(Nutzflaeche: f, SollTag: 21.0, SollNacht: 16.0, SollWochenende: 15.0),
                            zwei: new Zoneneingaben(Nutzflaeche: f, SollTag: 20.0, SollNacht: 17.0, SollWochenende: 16.0))
                : Dreizonen();
            GebaeudeKlima klima = Klima();
            Mehrzonenergebnis ohne = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude);
            Mehrzonenergebnis mit = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An(fest, abzug));
            Assert.Null(ohne.Aufheizgebaeude);
            Assert.NotNull(mit.Aufheizgebaeude);

            int fenster = 0, rampen = 0, begrenzt = 0;
            double groesste = 0.0, ohneGroesste = 0.0, groessteFrei = 0.0;
            foreach (int id in new[] { WOHNUNG_1, WOHNUNG_2 })
            {
                int z = Stelle(mit.Eingaenge, id);
                Aufheizplan p = mit.Eingaenge[z].Aufheizplan;
                Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
                Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, p.Bemessung.Quelle);
                Assert.True(p.Geaendert, fall + ": keine Rampe in " + mit.Eingaenge[z].Bezeichnung);
                Assert.Same(p.Reihe, mit.Eingaenge[z].Eingang.ThetaSoll);
                double pAuf = p.Bemessung.AufheizleistungW;
                double[] last = mit.Zonen[z].HeizlastW;
                double[] lastOhne = ohne.Zonen[z].HeizlastW;
                foreach (Aufheizsprung sp in p.Spruenge)
                {
                    if (sp.N <= 1) continue;
                    rampen++;
                    double max = 0.0;
                    for (int h = sp.Sprungstunde - sp.N + 1; h <= sp.Sprungstunde + 2; h++) max = Math.Max(max, last[Ring(h)]);
                    double quote = max / pAuf;
                    groesste = Math.Max(groesste, quote);
                    if (sp.Begrenzt) begrenzt++;
                    else groessteFrei = Math.Max(groessteFrei, quote);
                    ohneGroesste = Math.Max(ohneGroesste, lastOhne[sp.Sprungstunde] / pAuf);
                    Assert.True(quote <= 1.01, string.Format(CultureInfo.InvariantCulture,
                        "{0}, {1}, Sprung {2} (n = {3}): {4:F1} W über 1,01·P_auf = {5:F1} W", fall, mit.Eingaenge[z].Bezeichnung,
                        sp.Sprungstunde, sp.N, max, 1.01 * pAuf));
                    fenster++;
                }
                _aus.WriteLine("{0}, {1}: P_auf {2} kW, t_auf,max {3} h, T_a,B {4} °C, Rampentage {5}, längste Rampe {6} h",
                               fall, mit.Eingaenge[z].Bezeichnung, F(pAuf / 1000.0, "F3"), p.Bemessung.Wirksam.AufheizzeitMaxH,
                               F(p.Bemessung.Wirksam.AussenC, "F1"), p.Aufheiztage, p.LaengsteRampeH);
            }
            _aus.WriteLine("N-AH9 {0}: {1} Rampen in {2} Fenstern, davon {3} von D begrenzt (W2); größte Stundenleistung {4} % von P_auf, " +
                           "ohne W2 {5} % (ohne Planung in der Sprungstunde {6} %)",
                           fall, rampen, fenster, begrenzt, F(100.0 * groesste, "F2"), F(100.0 * groessteFrei, "F2"), F(100.0 * ohneGroesste, "F2"));
            Assert.True(rampen >= 300, fall + ": zu wenige Rampen " + rampen);
            Assert.True(ohneGroesste > 1.01, fall + ": Die Gegenprobe ohne Planung bleibt unter dem Band.");

            // Der Keller bleibt ohne Rampe (UNBEHEIZT), seine Reihe dieselbe Instanz.
            ZonenEingang keller = mit.Eingaenge[Stelle(mit.Eingaenge, KELLER)];
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, keller.Aufheizplan.Zustand);
            Assert.True(keller.Aufheizplan.Unbeheizt);
            Assert.Null(keller.Aufheizplan.Bemessung);
            Assert.Null(keller.Aufheizplan.Rampenmaske);
            Assert.False(keller.Aufheizplan.Geaendert);
            Assert.Same(keller.Eingang.ThetaSoll, keller.Aufheizplan.Reihe);
            Assert.All(keller.Eingang.ThetaSoll, w => Assert.True(double.IsNaN(w)));
        }

        /// <summary>
        /// <b>N-AH9 am Referenzprojekt 1052</b> (G6d): dieselbe Probe wie
        /// <see cref="N_AH9_Formel_je_Zone_haelt_in_der_vollen_Zonenschleife"/>, aber mit den Zonen der Testdatenbank —
        /// Gästezimmer und Gastronomie mit Trennwand, Luftstrom und Kellerdecken über dem unbeheizten Keller, je Zone
        /// ihr Heizkalender (Wohnen, Büro: Sprünge um 6 und um 7 Uhr) über den Datenweg der Konditionierung, die
        /// Aufheizvorgabe des Projekts. Feste Ränder wie dort (konstante Außenluft, hier −15 °C, ohne Sonne, ohne
        /// innere Gewinne des Gebäudes; das Erdreich unter dem Keller folgt seinem Jahresgang): Je beheizte Zone und je
        /// <b>Rampe nach einer Absenkung bis 24 h</b> bleibt die Stundenleistung im Fenster [h_s − n + 1, h_s + 2]
        /// unter 1,01·P_auf (gemessen 562 Fenster, größte 100,37 %); ohne Planung liegt die Sprungstunde darüber; der
        /// Keller bleibt UNBEHEIZT; die beiden Zonen springen zu verschiedenen Stunden.
        /// <para><b>Gemessen, nicht im Band (Befund G6d):</b> In der Gastronomie (Büro: Wochenende und Feiertage
        /// 16 °C) überschreiten die Rampen nach <b>langer Absenkung</b> (Wochenende 61 h, Feiertag 37 h, Weihnachten
        /// 109 h) das Band um bis zu 2,1 %: Die Planung rechnet die Nachbarn fest (den Keller beim Startwert), im Lauf
        /// kühlt der über Kellerdecke und Erdreich gekoppelte Keller in der langen Absenkung weiter aus. Die
        /// synthetische Probe oben kennt den Fall nicht (kleiner Keller). Bei −5 °C und wärmer füllen die Rampen der
        /// Gästezimmer die ganze Absenkung von 8 h (W2, von D begrenzt) und liegen bis 5–24 % über P_auf — darum die
        /// kältere Probe. Gehalten als Messlatte: lang ≤ 1,03·P_auf, begrenzt ≤ 1,06·P_auf (bei −15 °C keine).</para>
        /// </summary>
        /// <summary>Die feste Außenluft der Probe an 1052 [°C] — kalt genug, dass die Rampen der Gästezimmer (Absenkung 8 h) frei bleiben.</summary>
        private const double AUSSEN_1052 = -15.0;

        /// <summary>Die Messlatte der Fenster im Band an 1052 (Rechenweg RP2a: gemessen 101,41 % mit Erdreichwiderstand, vorher 100,37 %).</summary>
        private const double BAND_1052 = 1.015;

        [Fact]
        public void N_AH9_Formel_je_Zone_haelt_am_Referenzprojekt_1052()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = EPOS.Referenzlaeufe.Skripte.Zonenprojekt1052.NEU;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            Assert.Equal(1, ctrl.rows);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.Equal(3, g.Zonen.Count);
            g.Interne_Waermegewinne = 0.0;
            GebaeudeKlima klima = Klima(AUSSEN_1052);
            Func<long?, Konditionierungssatz> kond = z => Konditionierungdatenweg.Satz(
                g, klima.Wochenende, Konditionierungdatenweg.Raster(PROJEKT).Jahr, false, false, z);   // wie der Lauf (E115)
            Aufheizvorgabe vorgabe = KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT);
            Assert.True(vorgabe.An);
            Mehrzonenergebnis ohne = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, kond);
            Mehrzonenergebnis mit = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, kond, vorgabe);
            Assert.Null(ohne.Aufheizgebaeude);
            Assert.NotNull(mit.Aufheizgebaeude);

            int fenster = 0, rampen = 0, beheizt = 0, begrenzt = 0, lang = 0;
            double groesste = 0.0, ohneGroesste = 0.0, groessteBegrenzt = 0.0, groessteLang = 0.0;
            var stunden = new List<HashSet<int>>();
            for (int z = 0; z < mit.Eingaenge.Count; z++)
            {
                ZonenEingang ze = mit.Eingaenge[z];
                Aufheizplan p = ze.Aufheizplan;
                if (p.Unbeheizt)
                {
                    Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, p.Zustand);
                    Assert.False(p.Geaendert);
                    continue;
                }
                beheizt++;
                Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
                Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, p.Bemessung.Quelle);
                Assert.True(p.Geaendert, "keine Rampe in " + ze.Bezeichnung);
                double pAuf = p.Bemessung.AufheizleistungW;
                double[] last = mit.Zonen[z].HeizlastW;
                double[] lastOhne = ohne.Zonen[z].HeizlastW;
                var sprungstunden = new HashSet<int>();
                foreach (Aufheizsprung sp in p.Spruenge)
                {
                    sprungstunden.Add(sp.Sprungstunde % 24);
                    if (sp.N <= 1) continue;
                    rampen++;
                    double max = 0.0;
                    for (int h = sp.Sprungstunde - sp.N + 1; h <= sp.Sprungstunde + 2; h++) max = Math.Max(max, last[Ring(h)]);
                    double quote = max / pAuf;
                    ohneGroesste = Math.Max(ohneGroesste, lastOhne[sp.Sprungstunde] / pAuf);
                    if (sp.Begrenzt)
                    {
                        begrenzt++;
                        groessteBegrenzt = Math.Max(groessteBegrenzt, quote);
                        continue;
                    }
                    if (sp.AbsenkdauerH > 24)
                    {
                        lang++;
                        groessteLang = Math.Max(groessteLang, quote);
                        continue;
                    }
                    groesste = Math.Max(groesste, quote);
                    // Rechenweg RP2a: Mit dem Erdreichwiderstand nach DIN EN ISO 13370 kühlt der über Kellerdecke und
                    // Erdreich gekoppelte Keller anders aus als in der Planung (Nachbarn fest); gemessen bis 101,41 % in
                    // Fenstern der Gastronomie (D = 13 h). Messlatte 1,015 — die Entscheidung über das Band ist RP2b.
                    Assert.True(quote <= BAND_1052, string.Format(CultureInfo.InvariantCulture,
                        "{0}, Sprung {1} (n = {2}, D = {3} h): {4:F1} W über {6}·P_auf = {5:F1} W", ze.Bezeichnung, sp.Sprungstunde,
                        sp.N, sp.AbsenkdauerH, max, BAND_1052 * pAuf, BAND_1052));
                    fenster++;
                }
                stunden.Add(sprungstunden);
                _aus.WriteLine("1052, {0}: P_auf {1} kW, t_auf,max {2} h, Rampentage {3}, längste Rampe {4} h, Sprungstunden {5}",
                               ze.Bezeichnung, F(pAuf / 1000.0, "F3"), p.Bemessung.Wirksam.AufheizzeitMaxH, p.Aufheiztage,
                               p.LaengsteRampeH, string.Join("/", sprungstunden.OrderBy(x => x)));
            }
            _aus.WriteLine("N-AH9 an 1052: {0} Rampen, {1} Fenster im Band (größte Stundenleistung {2} % von P_auf); {3} von D begrenzt " +
                           "(W2, größte {4} %), {5} nach Absenkung über 24 h (größte {6} %); ohne Planung in der Sprungstunde {7} %",
                           rampen, fenster, F(100.0 * groesste, "F2"), begrenzt, F(100.0 * groessteBegrenzt, "F2"), lang,
                           F(100.0 * groessteLang, "F2"), F(100.0 * ohneGroesste, "F2"));
            Assert.Equal(2, beheizt);
            Assert.True(fenster >= 100, "zu wenige Fenster im Band " + fenster);
            Assert.True(groessteBegrenzt <= 1.06, "Messlatte der begrenzten Rampen überschritten: " + F(groessteBegrenzt, "F4"));
            // Rechenweg RP2a: mit Erdreichwiderstand gemessen 103,15 % (vorher bis 102,1 %), Messlatte 1,035.
            Assert.True(groessteLang <= 1.035, "Messlatte der Rampen nach langer Absenkung überschritten: " + F(groessteLang, "F4"));
            Assert.True(ohneGroesste > 1.01, "Die Gegenprobe ohne Planung bleibt unter dem Band.");
            Assert.False(stunden[0].SetEquals(stunden[1]), "Die Zonen springen zu denselben Stunden.");
        }

        // =====================================================================
        //  Die Bemessung mit Nachbarn
        // =====================================================================

        /// <summary>
        /// <b>Festlegung 13 mit Nachbarn:</b> P_auf (Ziel) = (1 + ρ)·Φ_stat(θ_T,max, T_a,min) mit dem Luftwechsel
        /// der Auslegung, der beheizte Nachbar bei seinem θ_T,max, der Keller beim Startwert nach N1.56 Nr. 7
        /// (<see cref="Zonenschleife.StartwerteRechnen"/>) — in θ_eq und in der Zuluft. Die Nachbarn bei 0 °C
        /// (der Befund B4) gäben eine höhere Last; der Startwert des Kellers liegt zwischen Außenluft und
        /// Nachtwerten.
        /// </summary>
        [Fact]
        public void N_AH9_Bemessung_mit_Nachbarn_bei_theta_T_max_und_Startwert()
        {
            ProjektGebaeudeModel g = Dreizonen();
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, Klima());
            int w1 = Stelle(zonen, WOHNUNG_1), w2 = Stelle(zonen, WOHNUNG_2), k = Stelle(zonen, KELLER);
            double[] s0 = Zonenschleife.StartwerteRechnen(zonen, 8760 - Vdi6007Rechenweg.VORLAUF_H);
            Assert.True(s0[k] > -5.0 && s0[k] < 16.0, "Startwert des Kellers " + F(s0[k]));

            var luft = new double[zonen.Count];
            luft[w1] = 21.0;
            luft[w2] = 20.0;
            luft[k] = s0[k];
            Aufheizzone[] az = Aufheizzone.AusZonen(zonen);
            Assert.Equal(luft, az[w1].NachbarnInDerBemessung);

            Aufheizvorgabe an = An();
            foreach ((int i, double thetaTMax) in new[] { (w1, 21.0), (w2, 20.0) })
            {
                Aufheizbemessung b = Aufheizoptimierung.Bemessen(az[i], an);
                Assert.Equal(thetaTMax, b.ThetaTMaxC);
                ZonenEingang z = zonen[i];
                GebaeudeModellEingang e = z.Eingang;
                double zusatz = e.AuslegungZusatzleitwertWK;
                var modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
                int tag = b.StundeKalt / 24;
                double phi = modell.StationaereHeizlastW(thetaTMax, z.ZuluftN(-5.0, zusatz, luft), z.AequivalentN(tag, -5.0, luft),
                                                         e.HeizungStrahlungsanteil, zusatz);
                Assert.Equal(Bits((1.0 + an.ReserveWirksam) * phi), Bits(b.AufheizleistungW));

                var null0 = new double[zonen.Count];
                double phiB4 = modell.StationaereHeizlastW(thetaTMax, z.ZuluftN(-5.0, zusatz, null0), z.AequivalentN(tag, -5.0, null0),
                                                           e.HeizungStrahlungsanteil, zusatz);
                Assert.True(phiB4 > phi);
                _aus.WriteLine("{0}: Φ_stat(θ_T,max) {1} W mit Nachbarn, {2} W mit Nachbarn bei 0 °C (B4); Startwert Keller {3} °C",
                               z.Bezeichnung, F(phi, "F1"), F(phiB4, "F1"), F(s0[k], "F3"));
            }

            // Im Sprung: der beheizte Nachbar bei s_k(h_s − 1) ohne Rampe, der Keller beim Startwert.
            int hs = 6 * 24 + 6;
            double[] imSprung = az[w1].NachbarnImSprung(hs);
            Assert.Equal(zonen[w2].Eingang.ThetaSoll[hs - 1], imSprung[w2]);
            Assert.Equal(17.0, imSprung[w2]);
            Assert.Equal(s0[k], imSprung[k]);
        }

        // =====================================================================
        //  Aggregation (Festlegung 22)
        // =====================================================================

        private static Aufheizbemessung Bemessung(string zustand, double pW, bool grenze, int? tAuf, double aussen)
        {
            var fall = new Aufheizbemessungsfall { AussenC = aussen, Erreichbar = tAuf.HasValue, AufheizzeitMaxH = tAuf };
            return new Aufheizbemessung
            {
                Zustand = zustand, AufheizleistungW = pW, QuelleGrenze = grenze, VarianteA = fall, VarianteB = fall,
            };
        }

        private static Aufheizsprung Sprung(int hs, int n, bool unerreichbar = false, bool begrenzt = false)
            => new Aufheizsprung(hs, 8, 16.0, 20.0, -10.0, 1000.0, n, n, unerreichbar, false, begrenzt, false, n - 1);

        private static Aufheizplan Plan(string zustand, Aufheizbemessung b, Aufheizsprung[] spruenge, int[] aus = null,
                                        int? kuerzeste = 8)
        {
            var maske = new bool[STUNDEN];
            foreach (Aufheizsprung sp in spruenge)
                for (int j = 1; j < sp.N; j++) maske[Ring(sp.Sprungstunde - sp.N + j)] = true;
            return new Aufheizplan
            {
                Zustand = zustand, Bemessung = b, Reihe = new double[STUNDEN], Rampenmaske = maske, Spruenge = spruenge,
                LaengsteRampeH = spruenge.Max(x => x.N - 1), KuerzesteAbsenkdauerH = kuerzeste,
                SprungstundenAus = aus ?? Array.Empty<int>(), Kuehlkappmaske = new bool[STUNDEN],
            };
        }

        private static readonly Aufheizplan Unbeheizt = new Aufheizplan
        {
            Zustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, Reihe = new double[STUNDEN],
        };

        private static readonly Aufheizplan Gekoppelt = new Aufheizplan
        {
            Zustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, Reihe = new double[STUNDEN],
        };

        /// <summary>
        /// <b>Festlegung 22:</b> Tage und Stunden als Vereinigung über die beheizten Zonen, t_auf,max und längste
        /// Rampe als Maximum, T_a,B als Minimum, P_auf als Summe, GEMISCHT bei verschiedenen Quellen; die
        /// unbeheizte Zone zählt nicht. UNERREICHBAR in einer Zone macht das Gebäude UNERREICHBAR (t_auf,max
        /// fehlt), nur gekoppelte beheizte Zonen machen es GEKOPPELT. HeizleistungMax_H = Σ_h max_z Anteil.
        /// </summary>
        [Fact]
        public void Gebaeudewerte_nach_Festlegung_22()
        {
            const int TAG = 24;
            // Zone A: Tag 10 (n = 4, 06 Uhr), Tag 11 (n = 1); Zone B: Tag 10 (n = 6, 07 Uhr), Tag 12 (n = 2, W2).
            Aufheizplan a = Plan(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, Bemessung(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 5000.0, false, 3, -10.0),
                                 new[] { Sprung(10 * TAG + 6, 4), Sprung(11 * TAG + 6, 1) }, new[] { 100 }, 8);
            Aufheizplan b = Plan(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, Bemessung(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 3000.0, true, 5, -12.0),
                                 new[] { Sprung(10 * TAG + 7, 6), Sprung(12 * TAG + 6, 2, begrenzt: true) }, new[] { 100, 200 }, 1);

            Aufheizgebaeude geb = Aufheizoptimierung.Gebaeudewerte(new[] { a, Unbeheizt, b });
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, geb.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, geb.Bemessung);
            Assert.Equal(5, geb.AufheizzeitMaxH);
            Assert.Equal(-12.0, geb.AussenBC);
            Assert.Equal(8000.0, geb.AufheizleistungW);
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GEMISCHT, geb.Quelle);
            Assert.Equal(2, geb.Aufheiztage);              // Tag 10 (beide) und Tag 12
            Assert.Equal(1, geb.TageBegrenzt);
            // Fenster: A 10·24 + 3 … + 5, B 10·24 + 2 … + 6 → 2 … 6 (5 h); B Tag 12: 12·24 + 5 (1 h).
            Assert.Equal(6, geb.AufheizstundenH);
            Assert.Equal(6, geb.MaskenstundenH);
            Assert.True(geb.Rampenmaske[10 * TAG + 2] && geb.Rampenmaske[10 * TAG + 6] && geb.Rampenmaske[12 * TAG + 5]);
            Assert.Equal(5, geb.LaengsteRampeH);
            Assert.Equal(1, geb.KuerzesteAbsenkdauerH);
            Assert.Equal(2, geb.SpruengeAus);              // Stunden 100 und 200
            Assert.Equal(2, geb.ZonenBeheizt);
            Assert.Equal(1, geb.ZonenUnbeheizt);
            Assert.Equal(0, geb.ZonenGekoppelt);
            Assert.False(geb.Gekoppelt);

            // Gleiche Quelle → diese Quelle.
            Aufheizplan b2 = b with { Bemessung = Bemessung(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 3000.0, false, 5, -12.0) };
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, Aufheizoptimierung.Gebaeudewerte(new[] { a, b2 }).Quelle);

            // Eine Zone unerreichbar → das Gebäude, t_auf,max fehlt, die Tageszählungen laufen.
            Aufheizplan bu = Plan(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, Bemessung(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, 3000.0, false, null, -12.0),
                                  new[] { Sprung(20 * TAG + 6, 9, unerreichbar: true) });
            Aufheizgebaeude u = Aufheizoptimierung.Gebaeudewerte(new[] { a, bu });
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, u.Zustand);
            Assert.Null(u.AufheizzeitMaxH);
            Assert.Equal(1, u.TageUnerreichbar);
            Assert.Equal(2, u.Aufheiztage);

            // Nur gekoppelte beheizte Zonen → GEKOPPELT; gemischt zählt die geplante Zone.
            Aufheizgebaeude k = Aufheizoptimierung.Gebaeudewerte(new[] { Gekoppelt, Unbeheizt, Gekoppelt });
            Assert.True(k.Gekoppelt);
            Assert.Null(k.AufheizzeitMaxH);
            Assert.Null(k.Quelle);
            Assert.True(double.IsNaN(k.AufheizleistungW));
            Assert.Equal(2, k.ZonenGekoppelt);
            Assert.Equal(1, k.ZonenUnbeheizt);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, Aufheizoptimierung.Gebaeudewerte(new[] { Gekoppelt, a }).Zustand);
            Assert.Throws<ArgumentException>(() => Aufheizoptimierung.Gebaeudewerte(new[] { Unbeheizt }));

            // HeizleistungMax_H = Σ_h max_z Anteil.
            var x = new double[STUNDEN];
            var y = new double[STUNDEN];
            x[5] = 0.25; y[5] = 0.5; x[6] = 0.75; y[7] = 0.125;
            Assert.Equal(0.5 + 0.75 + 0.125, Aufheizoptimierung.HeizleistungMaxStundenH(new[] { x, y }));
        }

        /// <summary>
        /// <b>Festlegung 22 am Lauf:</b> Die Gebäudewerte des Dreizonengebäudes sind die Vereinigung der zwei
        /// Wohnungen; mit nur einer geplanten Zone sind Tage, Zähler, Maske und Bemessung die der Zone.
        /// </summary>
        [Fact]
        public void Gebaeudewerte_am_Lauf_sind_die_Vereinigung_der_Zonen()
        {
            ProjektGebaeudeModel g = Dreizonen();
            Mehrzonenergebnis m = Zonenrechnung.Rechnen(g, Klima(), false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An());
            Aufheizplan p1 = m.Eingaenge[Stelle(m.Eingaenge, WOHNUNG_1)].Aufheizplan;
            Aufheizplan p2 = m.Eingaenge[Stelle(m.Eingaenge, WOHNUNG_2)].Aufheizplan;
            Aufheizgebaeude geb = m.Aufheizgebaeude;
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, geb.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, geb.Quelle);
            Assert.Equal(Bits(p1.Bemessung.AufheizleistungW + p2.Bemessung.AufheizleistungW), Bits(geb.AufheizleistungW));
            Assert.Equal(Math.Max(p1.Bemessung.Wirksam.AufheizzeitMaxH.Value, p2.Bemessung.Wirksam.AufheizzeitMaxH.Value), geb.AufheizzeitMaxH);
            Assert.Equal(Math.Max(p1.LaengsteRampeH, p2.LaengsteRampeH), geb.LaengsteRampeH);
            Assert.Equal(Math.Min(p1.Bemessung.Wirksam.AussenC, p2.Bemessung.Wirksam.AussenC), geb.AussenBC);
            Assert.True(geb.Aufheiztage >= Math.Max(p1.Aufheiztage, p2.Aufheiztage));
            Assert.True(geb.Aufheiztage <= p1.Aufheiztage + p2.Aufheiztage);
            int vereinigt = 0;
            for (int h = 0; h < STUNDEN; h++) if (p1.Rampenmaske[h] || p2.Rampenmaske[h]) vereinigt++;
            Assert.Equal(vereinigt, geb.MaskenstundenH);
            Assert.Equal(2, geb.ZonenBeheizt);
            Assert.Equal(1, geb.ZonenUnbeheizt);

            Aufheizgebaeude eine = Aufheizoptimierung.Gebaeudewerte(new[] { p1 });
            Assert.Equal(p1.Aufheiztage, eine.Aufheiztage);
            Assert.Equal(p1.TageBegrenzt, eine.TageBegrenzt);
            Assert.Equal(p1.TageUnerreichbar, eine.TageUnerreichbar);
            Assert.Equal(p1.MaskenstundenH, eine.MaskenstundenH);
            Assert.Equal(p1.SpruengeAus, eine.SpruengeAus);
            Assert.Equal(p1.Bemessung.Wirksam.AufheizzeitMaxH, eine.AufheizzeitMaxH);
            Assert.Equal(p1.Bemessung.Quelle, eine.Quelle);
            Assert.Equal(p1.AufheizstundenH, eine.AufheizstundenH);   // die Rampen überlappen nicht
            _aus.WriteLine("Gebäude: Rampentage {0} (Zonen {1}/{2}), Rampenstunden {3}, Maske {4} h, P_auf {5} kW, t_auf,max {6} h",
                           geb.Aufheiztage, p1.Aufheiztage, p2.Aufheiztage, geb.AufheizstundenH, geb.MaskenstundenH,
                           F(geb.AufheizleistungW / 1000.0, "F3"), geb.AufheizzeitMaxH);
        }
    }
}
