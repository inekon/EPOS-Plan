using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Proben der Welle AK3-K-K2</b> (Entwurf AK3-K 4.2, 4.3, 4.5; Festlegungen 11 bis 14) — ohne Datenbank, mit dem
    /// Phantasiegebäude und Phantasiekennlinien (runde, erfundene Werte, kein Produkt):
    /// <list type="bullet">
    /// <item><b>Ohne Grenze bitgleich:</b> eine Kälteschranke ohne Grenze (+∞, 1 GW) rechnet Bit für Bit wie ohne.</item>
    /// <item><b>Kühltag:</b> die reversible Wärmepumpe fehlt der Heizseite, der Kessel deckt das Brauchwasser.</item>
    /// <item><b>Heiztag:</b> die Wärmepumpe fehlt der Kälteseite (Grund Umschaltung).</item>
    /// <item><b>Kältespeicher überbrückt:</b> am Heiztag trägt der Kältespeicher die Schranke (min(SOC, Entladeleistung)).</item>
    /// <item><b>Kältemaschine zu klein:</b> die Kälteschranke greift, der Raum wird wärmer, die Überschreitungsstunden steigen.</item>
    /// </list>
    /// </summary>
    public sealed class Ak3KaelteschrankeTests
    {
        private const int H = 8760;
        private readonly ITestOutputHelper _aus;

        public Ak3KaelteschrankeTests(ITestOutputHelper aus) { _aus = aus; }

        // ---------------------------------------------------------------------------------------
        //  Bausteine
        // ---------------------------------------------------------------------------------------

        private static double[] Konstant(double wert) => Enumerable.Repeat(wert, H).ToArray();

        /// <summary>Stunden mit Kühlbedarf: sommerliche Folge (Muster der Kälteseite), Kühlsollwert 25 °C.</summary>
        private static List<Stundenrand> Sommer(int stunden, int saat, bool kuehldecke)
        {
            var z = new Random(saat);
            var folge = new List<Stundenrand>(stunden);
            Uebergabekennwerte decke = kuehldecke ? Kuehluebergabe.Gespiegelt(3000.0, 1.1, 16.0, 19.0, 26.0) : null;
            for (int h = 0; h < stunden; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 24.0 + 6.0 * tag + 2.0 * (z.NextDouble() - 0.5);
                double sonne = Math.Max(0.0, tag) * 3000.0 * z.NextDouble();
                double kuehlMax = z.NextDouble() < 0.3 ? 800.0 + 2000.0 * z.NextDouble() : double.NaN;
                folge.Add(new Stundenrand(aussen, aussen + 1.0, 20.0, 25.0, 0.4 * sonne, 0.6 * sonne, 300.0 + 900.0 * z.NextDouble(),
                                          double.NaN, kuehlMax, 0.3, 0.0, 0.0,
                                          kuehlUebergabeGespiegelt: decke, kuehlVorlaufC: kuehldecke ? 16.0 : double.NaN,
                                          kuehlStrahlungsanteil: kuehldecke ? 0.5 : 0.0, reglerbandK: kuehldecke ? 1.0 : 0.0));
            }
            return folge;
        }

        private static void Gleich(double a, double b, string was, int h)
            => Assert.True(a.Equals(b), was + " in Stunde " + h + ": " + a.ToString("R") + " ≠ " + b.ToString("R"));

        /// <summary>Eine flache Kühlkennlinie: Vorlauf 16 °C, Pkuehl 8 kW, EER 4 über 0 bis 40 °C.</summary>
        private static Kuehlkennlinie Kuehlkurve()
        {
            var zeilen = new List<KuehlkennlinienZeile>();
            int id = 1;
            foreach (int t in new[] { 0, 10, 20, 30, 40 }) zeilen.Add(new KuehlkennlinienZeile(id++, 16, t, 4.0, 8.0, 100));
            return Kuehlkennlinie.Bilden(zeilen, 16);
        }

        private static Kaelteerzeuger WpImKuehlbetrieb() => new Kaelteerzeuger
        {
            Bezeichner = "WP", Modulindex = 0, Kennlinie = Kuehlkurve(), Quelltemperatur = Konstant(20.0), KuehlVorlaufC = 16.0,
        };

        /// <summary>Kühltage: die erste Hälfte des Jahres Heiztage, die zweite Kühltage.</summary>
        private static bool[] Kuehltage()
        {
            var t = new bool[365];
            for (int d = 182; d < 365; d++) t[d] = true;
            return t;
        }

        private const int HEIZSTUNDE = 100 * 24 + 12;
        private const int KUEHLSTUNDE = 200 * 24 + 12;

        private static SimulationWaermepumpe._Kenndaten Kurve(int vorlauf, params (double t, double cop, double p)[] punkte)
            => new SimulationWaermepumpe._Kenndaten
            {
                Vorlauf = vorlauf,
                anz = punkte.Length,
                dat = punkte.Select(x => new SimulationWaermepumpe._DAT { Temperatur = x.t, COP = x.cop, Leistung = x.p }).ToArray(),
            };

        private static SimulationPufferspeicher Kaeltespeicher(double socKwh, double entladeMaxKw)
        {
            var sp = new SimulationPufferspeicher();
            sp.InitKaelte(2000, 6, 12, 0.0);
            sp.EntladeleistungMax = entladeMaxKw;
            sp.SOC = socKwh;
            return sp;
        }

        // ---------------------------------------------------------------------------------------
        //  Ohne Grenze bitgleich
        // ---------------------------------------------------------------------------------------

        [Theory]
        [InlineData(false, double.PositiveInfinity)]
        [InlineData(false, 1e9)]
        [InlineData(true, double.PositiveInfinity)]
        [InlineData(true, 1e9)]
        public void Ohne_Grenze_bitgleich(bool kuehldecke, double schrankeW)
        {
            List<Stundenrand> folge = Sommer(2000, 7, kuehldecke);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(23.0);
            b.Zuruecksetzen(23.0);
            int gekuehlt = 0;
            for (int h = 0; h < folge.Count; h++)
            {
                Stundenrand r = folge[h];
                Stundenrand rk = r.MitKaelteverfuegbarkeit(schrankeW, Verfuegbarkeitsgrund.KeineBegrenzung, 16.0);
                Stundenergebnis e0 = a.Schritt(in r);
                Stundenergebnis e1 = b.Schritt(in rk);
                Gleich(e0.HeizleistungW, e1.HeizleistungW, "Heizleistung", h);
                Gleich(e0.KuehlleistungW, e1.KuehlleistungW, "Kühlleistung", h);
                Gleich(e0.ThetaAirMittel, e1.ThetaAirMittel, "Raumluft", h);
                Gleich(e0.ThetaMAwEnde, e1.ThetaMAwEnde, "Masse AW", h);
                Gleich(e0.ThetaMIwEnde, e1.ThetaMIwEnde, "Masse IW", h);
                Assert.Equal(e0.Abschnitte, e1.Abschnitte);
                Assert.Equal(e0.KuehlBegrenzungsgrund, e1.KuehlBegrenzungsgrund);
                Assert.Equal(0.0, e1.KaelteverfuegbarkeitAnteil);
                if (e0.KuehlleistungW > 0.0) gekuehlt++;
            }
            Assert.True(gekuehlt > 200, "die Folge muss kühlen");
        }

        // ---------------------------------------------------------------------------------------
        //  Kühltag und Heiztag — die Umschaltung als Verfügbarkeitsgrenze (4.5)
        // ---------------------------------------------------------------------------------------

        [Fact]
        public void Kuehltag_Waermepumpe_fehlt_der_Heizseite_Kessel_deckt_Brauchwasser()
        {
            var kurven = new[] { Kurve(35, (10, 5.0, 12.0), (0, 4.0, 10.0), (-10, 3.0, 8.0)) };
            bool[] kuehltage = Kuehltage();
            var wp = new WaermepumpeKapazitaet(new Fahrplanerzeuger { Bezeichner = "WP" }, kurven, null,
                                               new Quellprofil(Konstant(0.0)), true)
            {
                Kuehltag = h => kuehltage[h / 24],
            };
            var kessel = new FesteKapazitaet(new Fahrplanerzeuger { Bezeichner = "Kessel", NennleistungKw = 6.0 });
            var erzeuger = new IErzeugerkapazitaet[] { wp, kessel };
            var brauchwasser = new Stundenvorrang(4.0, 0.0, 0.0, 0.0);

            Stundenangebot heiztag = Angebotsfunktion.Angebot(HEIZSTUNDE, 35.0, erzeuger, null, brauchwasser);
            Stundenangebot kuehltag = Angebotsfunktion.Angebot(KUEHLSTUNDE, 35.0, erzeuger, null, brauchwasser);
            Assert.Equal(10.0 + 6.0 - 4.0, heiztag.LeistungKw, 12);
            // Am Kühltag ist die Wärmepumpe Kältemaschine: allein der Kessel bleibt; er deckt das Brauchwasser zuerst.
            Assert.Equal(6.0, kuehltag.ErzeugerKw, 12);
            Assert.Equal(6.0 - 4.0, kuehltag.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.Umschaltung, kuehltag.Grund);
        }

        [Fact]
        public void Heiztag_Waermepumpe_fehlt_der_Kaelteseite()
        {
            var wp = new WaermepumpeKaeltekapazitaet(WpImKuehlbetrieb(), Kuehltage(), null, true);
            Erzeugerangebot heiz = wp.Abfragen(HEIZSTUNDE, 16.0);
            Erzeugerangebot kuehl = wp.Abfragen(KUEHLSTUNDE, 16.0);
            Assert.Equal(8.0, heiz.KapazitaetKw, 12);
            Assert.Equal(0.0, heiz.VerfuegbarKw);
            Assert.Equal(Verfuegbarkeitsgrund.Umschaltung, heiz.Grund);
            Assert.Equal(8.0, kuehl.VerfuegbarKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, kuehl.Grund);

            // Vorrangschätzung (Festlegung 14): Brauchwasser 3 kW an 12 kW Heizkapazität nimmt ein Viertel der Stunde.
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { wp }, null, 16.0);
            wp.Heizzeitanteil = h => Kaelteschranke.Heizzeitanteil(schranke.VorrangDerStunde, 12.0);
            Kaeltestundenangebot k = schranke.Angebot(KUEHLSTUNDE, new Stundenvorrang(3.0, 0.0, 0.0, 0.0));
            Assert.Equal(0.75 * 8.0, k.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.Umschaltung, k.Grund);
            Kaeltestundenangebot h0 = schranke.Angebot(HEIZSTUNDE, new Stundenvorrang(3.0, 0.0, 0.0, 0.0));
            Assert.Equal(0.0, h0.LeistungKw);
        }

        [Fact]
        public void Kaeltespeicher_ueberbrueckt()
        {
            var wp = new WaermepumpeKaeltekapazitaet(WpImKuehlbetrieb(), Kuehltage(), null, true);
            SimulationPufferspeicher sp = Kaeltespeicher(5.0, 3.0);
            double socVorher = sp.SOC;
            var leser = new Kaeltespeicherleser(new[] { sp });
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { wp }, leser, 16.0);

            // Heiztag: die Wärmepumpe fehlt, der Speicher trägt min(SOC, Entladeleistung).
            Kaeltestundenangebot heiz = schranke.Angebot(HEIZSTUNDE, new Stundenvorrang(0, 0, 0, 0));
            Assert.Equal(3.0, heiz.SpeicherKw, 12);
            Assert.Equal(3.0, heiz.LeistungKw, 12);
            sp.EntladeleistungMax = 0.0;
            Assert.Equal(5.0, schranke.Angebot(HEIZSTUNDE, new Stundenvorrang(0, 0, 0, 0)).LeistungKw, 12);
            // Am Kühltag Speicher und Wärmepumpe zusammen; die Abfrage ändert keinen Speicher.
            Assert.Equal(8.0 + 5.0, schranke.Angebot(KUEHLSTUNDE, new Stundenvorrang(0, 0, 0, 0)).LeistungKw, 12);
            Assert.Equal(socVorher, sp.SOC);

            // Im Gebäude: dieselbe Kühlstunde an der Schranke des Speichers statt ganz ohne Kälte.
            List<Stundenrand> folge = Sommer(400, 11, false);
            var ohne = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var mit = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            ohne.Zuruecksetzen(23.0);
            mit.Zuruecksetzen(23.0);
            double kuehlOhne = 0.0, kuehlMit = 0.0;
            foreach (Stundenrand r in folge)
            {
                kuehlOhne += ohne.Schritt(r.MitKaelteverfuegbarkeit(0.0, Verfuegbarkeitsgrund.Umschaltung, 16.0)).KuehlleistungW;
                kuehlMit += mit.Schritt(r.MitKaelteverfuegbarkeit(heiz.LeistungKw * 1000.0, heiz.Grund, 16.0)).KuehlleistungW;
            }
            Assert.Equal(0.0, kuehlOhne);
            Assert.True(kuehlMit > 0.0);
        }

        // ---------------------------------------------------------------------------------------
        //  Kältemaschine zu klein — die Kälteschranke greift
        // ---------------------------------------------------------------------------------------

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Kaeltemaschine_zu_klein_Kaelteschranke_greift_Raum_waermer(bool kuehldecke)
        {
            var maschine = new Kaeltemaschine
            {
                Bezeichner = "KM", NennleistungKw = 0.6, Kaltwassertemperatur = 6.0,
                Kennlinie = new KaeltemaschinenKennlinie(new[]
                {
                    (25.0, 6.0, (double?)3.0, (double?)0.6), (35.0, 6.0, (double?)2.5, (double?)0.5),
                }),
                Rueckkuehltemperatur_stuendlich = Konstant(30.0),
            };
            var km = new KaeltemaschineKapazitaet(new Kaelteerzeuger { Bezeichner = "KM", Maschine = maschine });
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { km }, null, 6.0);

            List<Stundenrand> folge = Sommer(2000, 5, kuehldecke);
            var frei = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var begrenzt = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            frei.Zuruecksetzen(23.0);
            begrenzt.Zuruecksetzen(23.0);
            int ueberFrei = 0, ueberBegrenzt = 0, gegriffen = 0, ueberwiegend = 0;
            double luftFrei = 0.0, luftBegrenzt = 0.0;
            for (int h = 0; h < folge.Count; h++)
            {
                Stundenrand r = folge[h];
                Kaeltestundenangebot k = schranke.Angebot(h, new Stundenvorrang(0, 0, 0, 0));
                Stundenrand rk = r.MitKaelteverfuegbarkeit(k.LeistungKw * 1000.0, k.Grund, k.KuehlVorlaufC);
                Stundenergebnis e0 = frei.Schritt(in r);
                Stundenergebnis e1 = begrenzt.Schritt(in rk);
                Assert.True(e1.KuehlleistungW <= k.LeistungKw * 1000.0 + 1e-6, "Kühlleistung über der Schranke in Stunde " + h);
                luftFrei += e0.ThetaAirMittel;
                luftBegrenzt += e1.ThetaAirMittel;
                if (e0.ThetaAirMittel > r.ThetaMax + 0.5) ueberFrei++;
                if (e1.ThetaAirMittel > r.ThetaMax + 0.5) ueberBegrenzt++;
                if (e1.KaelteverfuegbarkeitBegrenzt)
                {
                    gegriffen++;
                    // Paarungsregel: gebäudeseitig Verfügbarkeit, der Anlagengrund reist daneben (Leistungsgrenze).
                    Assert.Equal(Verfuegbarkeitsgrund.Leistungsgrenze, e1.Kaelteverfuegbarkeitsgrund);
                    if (e1.KuehlBegrenzungsgrund == Begrenzungsgrund.Verfuegbarkeit) ueberwiegend++;
                }
            }
            _aus.WriteLine("Kühldecke {0}: Schranke gegriffen {1} h, Überschreitungsstunden {2} → {3}, mittlere Raumluft {4:0.000} → {5:0.000} °C",
                           kuehldecke, gegriffen, ueberFrei, ueberBegrenzt, luftFrei / folge.Count, luftBegrenzt / folge.Count);
            Assert.True(gegriffen > 100);
            // Mit Kühldecke trägt die Stunde den Kühlgrund Verfügbarkeit, wo die Schranke den größten Zeitanteil hält.
            if (kuehldecke) Assert.True(ueberwiegend > 0);
            else Assert.Equal(0, ueberwiegend);
            Assert.True(luftBegrenzt > luftFrei);
            Assert.True(ueberBegrenzt > ueberFrei);
        }

        [Fact]
        public void Ohne_Kaelteerzeuger_und_Speicher_kein_Erzeuger()
        {
            Kaeltestundenangebot k = Kaelteangebotsfunktion.Angebot(0, 16.0, Array.Empty<IKaelteerzeugerkapazitaet>(), null, 0.0);
            Assert.Equal(0.0, k.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.KeinErzeuger, k.Grund);
            // Prozesskälte geht vor (4.3).
            var wp = new WaermepumpeKaeltekapazitaet(WpImKuehlbetrieb(), null, null, true);
            Assert.Equal(8.0 - 2.5, Kaelteangebotsfunktion.Angebot(0, 16.0, new IKaelteerzeugerkapazitaet[] { wp }, null, 2.5).LeistungKw, 12);
        }
    }
}
