using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt H im Löser</b> (Anlagenkopplung AK1; 3.3, 3.7, 10.2, 11.1) — die Proben am
    /// 2-K-Löser mit dem Phantasiegebäude, ohne Datenbank: die beiden Grenzfälle (Probe B
    /// bitgleich, <c>Xp = 0</c> mit sehr großer Übergabe bitgleich), die zwei Knicke des
    /// P-Reglers, die Aufheizspitze, das Reglerband, die begrenzte Übergabe, der stationäre
    /// Arbeitspunkt und der Determinismus. Alle Zahlen sind Phantasiewerte.
    /// </summary>
    public class AnlagenkopplungZonenmodellTests
    {
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungZonenmodellTests(ITestOutputHelper aus) { _aus = aus; }

        private static Stundenrand Rand(double thetaOut, double soll, double phiConv, Uebergabekennwerte k,
                                        double vorlauf, double xp, double heizMaxW = double.NaN,
                                        double strahlung = 0.3, double thetaMax = double.PositiveInfinity,
                                        double phiRadAW = 0.0, double phiRadIW = 0.0, double zusatz = 0.0,
                                        double kuehlMaxW = double.NaN)
            => new Stundenrand(thetaOut, thetaOut, soll, thetaMax, phiRadAW, phiRadIW, phiConv,
                               heizMaxW, kuehlMaxW, strahlung, 0.0, zusatz,
                               uebergabe: k, vorlaufC: vorlauf, reglerbandK: xp);

        /// <summary>Eine deterministisch gezogene Folge von Stunden mit Heizen, Kühlen, Grenzen und Sommerlüftung.</summary>
        private static List<Stundenrand> Folge(int stunden, int saat, Func<Stundenrand, Stundenrand> umbau)
        {
            var z = new Random(saat);
            var folge = new List<Stundenrand>(stunden);
            for (int h = 0; h < stunden; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 5.0 - 12.0 * Math.Cos(2.0 * Math.PI * h / 2000.0) + 4.0 * tag + 2.0 * (z.NextDouble() - 0.5);
                double soll = (h % 24) >= 6 && (h % 24) <= 21 ? 20.0 : 16.0;
                double sonne = Math.Max(0.0, tag) * 1500.0 * z.NextDouble();
                double heizMax = z.NextDouble() < 0.3 ? 1500.0 + 3000.0 * z.NextDouble() : double.NaN;
                bool kuehlen = z.NextDouble() < 0.3;
                double zusatz = z.NextDouble() < 0.1 ? 150.0 : 0.0;
                var r = new Stundenrand(aussen, aussen + 1.0, soll, kuehlen ? 25.0 : double.PositiveInfinity,
                                        0.4 * sonne, 0.6 * sonne, 200.0 + 300.0 * z.NextDouble(),
                                        heizMax, kuehlen ? 2000.0 : double.NaN, 0.3, 0.0, zusatz);
                folge.Add(umbau(r));
            }
            return folge;
        }

        private static Stundenrand MitKopplung(in Stundenrand r, Uebergabekennwerte k, double vorlauf, double xp)
            => new Stundenrand(r.ThetaOut, r.ThetaEq, r.ThetaSoll, r.ThetaMax, r.PhiRadAW, r.PhiRadIW, r.PhiConv,
                               r.HeizleistungMaxW, r.KuehlleistungMaxW, r.HeizungStrahlungsanteil,
                               r.KuehlungAnteilInnenflaeche, r.ZusatzleitwertWK,
                               uebergabe: k, vorlaufC: vorlauf, reglerbandK: xp);

        private static void Bitgleich(Stundenergebnis a, Stundenergebnis b, int h)
        {
            Assert.True(a.HeizleistungW.Equals(b.HeizleistungW), "Heizleistung, Stunde " + h + ": " + a.HeizleistungW + " / " + b.HeizleistungW);
            Assert.True(a.KuehlleistungW.Equals(b.KuehlleistungW), "Kühlleistung, Stunde " + h);
            Assert.True(a.ThetaAirMittel.Equals(b.ThetaAirMittel), "Raumluft, Stunde " + h);
            Assert.True(a.ThetaOpMittel.Equals(b.ThetaOpMittel), "operativ, Stunde " + h);
            Assert.True(a.ThetaSAwMittel.Equals(b.ThetaSAwMittel) && a.ThetaSIwMittel.Equals(b.ThetaSIwMittel), "Oberflächen, Stunde " + h);
            Assert.True(a.ThetaMAwEnde.Equals(b.ThetaMAwEnde) && a.ThetaMIwEnde.Equals(b.ThetaMIwEnde), "Zustand, Stunde " + h);
            Assert.Equal(a.Abschnitte, b.Abschnitte);
        }

        // =====================================================================
        //  Die beiden Grenzfälle (3.7, 11.1)
        // =====================================================================

        /// <summary>
        /// <b>Probe B</b>: unbegrenzte Übergabe und <c>Xp = 0</c> — der gekoppelte Zweig rechnet
        /// Stunde für Stunde BITGLEICH wie die ideale Regelung, auch mit Heizleistungsgrenze,
        /// Kühlung und Sommerlüftung; die Probe fällt, sobald jemand im gekoppelten Zweig eine
        /// Rechenoperation einfügt.
        /// </summary>
        [Fact]
        public void Probe_B_Unbegrenzte_Uebergabe_mit_Band_null_ist_bitgleich_zur_idealen_Regelung()
        {
            var unbegrenzt = new Uebergabekennwerte(double.PositiveInfinity, 1.3, 55.0, 45.0, 20.0);
            List<Stundenrand> ideal = Folge(3000, 42, r => r);
            List<Stundenrand> gekoppelt = ideal.Select(r => MitKopplung(r, unbegrenzt, 55.0, 0.0)).ToList();

            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(18.0);
            b.Zuruecksetzen(18.0);
            int heizgrenze = 0;
            for (int h = 0; h < ideal.Count; h++)
            {
                Stundenrand ri = ideal[h], rg = gekoppelt[h];
                Stundenergebnis ei = a.Schritt(in ri);
                Stundenergebnis eg = b.Schritt(in rg);
                Bitgleich(ei, eg, h);
                Assert.Equal(55.0, eg.VorlaufC);
                Assert.Equal(55.0, eg.RuecklaufC);        // W_H unendlich
                Assert.Equal(0.0, eg.UebergabeBegrenztAnteil);
                if (eg.HeizleistungMaxAnteil > 0.0) heizgrenze++;
            }
            Assert.True(heizgrenze > 0, "die Folge muss auch Stunden an der Heizleistungsgrenze haben");
        }

        /// <summary>
        /// <b>Xp = 0 fällt bitgleich auf die ideale Regelung</b> (H1): auch eine endliche, aber
        /// reichliche Übergabe rechnet wörtlich den Bestand, solange sie nie die Grenze ist.
        /// </summary>
        [Fact]
        public void Band_null_mit_reichlicher_Uebergabe_ist_bitgleich_zur_idealen_Regelung()
        {
            var reichlich = new Uebergabekennwerte(1e12, 1.3, 55.0, 45.0, 20.0);
            List<Stundenrand> ideal = Folge(1500, 7, r => r);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard());
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard());
            a.Zuruecksetzen(19.0);
            b.Zuruecksetzen(19.0);
            for (int h = 0; h < ideal.Count; h++)
            {
                Stundenrand ri = ideal[h];
                Stundenrand rg = MitKopplung(ideal[h], reichlich, 55.0, 0.0);
                Bitgleich(a.Schritt(in ri), b.Schritt(in rg), h);
            }
        }

        // =====================================================================
        //  Zwei Knicke, zwei Umschaltzeitpunkte (10.2, 11.1)
        // =====================================================================

        /// <summary>
        /// Eine Stunde, in der die Raumluft von unter θ_soll − Xp bis über θ_soll steigt (leichtes
        /// Gebäude, kalt am Stundenbeginn, innere Lasten über dem Verlust): Mit Xp = 1 K findet die
        /// Bisektion BEIDE Knicke — gesättigt → Regelbereich → ohne Heizung, zwei Fallwechsel. Mit
        /// Xp = 0 fallen die Knicke zusammen: Der gesättigte Fall endet genau einmal, am Sollwert,
        /// im geregelten Fall des Bestands; einen Regelbereich gibt es nicht.
        /// </summary>
        [Fact]
        public void Zwei_Knicke_ergeben_zwei_Fallwechsel_mit_Band_null_einen()
        {
            var k = new Uebergabekennwerte(3000.0, 1.3, 55.0, 45.0, 20.0);
            ErsatzparameterRC leicht = Phantasiegebaeude.Standard(cAW: 2.0e4, cIW: 5.0e4);

            var mitBand = new Zonenmodell2K(leicht);
            mitBand.Zuruecksetzen(5.0);
            Stundenrand r1 = Rand(10.0, 20.0, 1500.0, k, 55.0, 1.0);
            Stundenergebnis e1 = mitBand.Schritt(in r1);
            Betriebsfall[] f1 = mitBand.LetzteFallfolge;

            var ohneBand = new Zonenmodell2K(leicht);
            ohneBand.Zuruecksetzen(5.0);
            Stundenrand r0 = Rand(10.0, 20.0, 1500.0, k, 55.0, 0.0);
            Stundenergebnis e0 = ohneBand.Schritt(in r0);
            Betriebsfall[] f0 = ohneBand.LetzteFallfolge;

            _aus.WriteLine("Xp = 1: " + string.Join(" → ", f1) + "; Xp = 0: " + string.Join(" → ", f0));
            Assert.Equal(e1.Abschnitte, f1.Length);

            // Xp = 1: gesättigt, dann (auch mehrfach linearisiert) Regelbereich, dann ohne Heizung.
            Assert.Equal(Betriebsfall.UebergabeGesaettigt, f1[0]);
            Assert.Equal(Betriebsfall.Totband, f1[f1.Length - 1]);
            Assert.True(f1.Skip(1).Take(f1.Length - 2).All(f => f == Betriebsfall.UebergabeRegelbereich) && f1.Length >= 3,
                        "zwischen den Knicken nur der Regelbereich");
            Assert.Equal(2, Wechsel(f1));
            Assert.True(e1.UebergabeBegrenztAnteil > 0.0 && e1.UebergabeBegrenztAnteil < 1.0);

            // Xp = 0: ein Knick — der gesättigte Fall endet am Sollwert im geregelten Fall.
            Assert.Equal(Betriebsfall.UebergabeGesaettigt, f0[0]);
            Assert.Equal(Betriebsfall.HeizenGeregelt, f0[1]);
            Assert.DoesNotContain(Betriebsfall.UebergabeRegelbereich, f0);
            Assert.Equal(1, f0.Count(f => f == Betriebsfall.UebergabeGesaettigt));
        }

        /// <summary>Zahl der Fallwechsel einer Folge — wiederholte Linearisierung desselben Falls zählt nicht.</summary>
        private static int Wechsel(Betriebsfall[] folge)
        {
            int n = 0;
            for (int i = 1; i < folge.Length; i++) if (folge[i] != folge[i - 1]) n++;
            return n;
        }

        // =====================================================================
        //  Aufheizspitze, Reglerband, Begrenzung (3.5, 4.4, 11.1)
        // =====================================================================

        /// <summary>Ein Tag mit Nachtabsenkung bei kalter Außenluft, als eingeschwungene Folge.</summary>
        private static List<Stundenergebnis> Absenktag(ErsatzparameterRC p, Uebergabekennwerte k, double xp, int tage = 15)
        {
            var modell = new Zonenmodell2K(p);
            modell.Zuruecksetzen(18.0);
            var letzter = new List<Stundenergebnis>();
            for (int d = 0; d < tage; d++)
            {
                letzter.Clear();
                for (int s = 0; s < 24; s++)
                {
                    double soll = s >= 6 && s <= 21 ? 20.0 : 12.0;
                    Stundenrand r = k == null
                        ? Phantasiegebaeude.Rand(-10.0, soll, double.PositiveInfinity, phiConv: 200.0, strahlungsanteil: 0.3)
                        : Rand(-10.0, soll, 200.0, k, 50.0, xp);
                    letzter.Add(modell.Schritt(in r));
                }
            }
            return letzter;
        }

        /// <summary>
        /// <b>Die Aufheizspitze</b> (3.5, 11.1): Dieselbe Stunde nach der Absenkung — mit Übergabe
        /// ist die Spitze kleiner und der Sollwert später erreicht. Die Übergabe ist auf das
        /// 1,08-Fache der stationären Tageslast bemessen, die Spitze der idealen Regelung liegt rund ein Fünftel darüber. Die Tagesenergie ist mit Übergabe KLEINER
        /// oder gleich: Der Raum bleibt länger kühl und verliert weniger (Energiebilanz des
        /// eingeschwungenen Tags); das Papier nennt in 11.1 „größer" — ein Vorzeichen, das der
        /// Bilanz widerspricht und im Bericht der Welle benannt ist.
        /// </summary>
        [Fact]
        public void Die_Aufheizspitze_wird_gekappt_und_der_Sollwert_spaeter_erreicht()
        {
            ErsatzparameterRC p = Phantasiegebaeude.Standard(true, cAW: 4.0e6, cIW: 1.0e7);
            List<Stundenergebnis> ideal = Absenktag(p, null, 0.0);
            double tageslast = ideal[15].HeizleistungW;
            var k = new Uebergabekennwerte(1.08 * tageslast, 1.3, 50.0, 40.0, 20.0);
            List<Stundenergebnis> gekoppelt = Absenktag(p, k, 0.0);

            double spitzeIdeal = ideal.Max(e => e.HeizleistungW);
            double spitzeGekoppelt = gekoppelt.Max(e => e.HeizleistungW);
            double energieIdeal = ideal.Sum(e => e.HeizleistungW);
            double energieGekoppelt = gekoppelt.Sum(e => e.HeizleistungW);
            int erreichtIdeal = ideal.FindIndex(6, e => e.ThetaAirMittel >= 19.9);
            int erreichtGekoppelt = gekoppelt.FindIndex(6, e => e.ThetaAirMittel >= 19.9);
            _aus.WriteLine($"Tageslast {tageslast:0} W; Spitze {spitzeIdeal:0} → {spitzeGekoppelt:0} W; Tag {energieIdeal / 1000:0.00} → {energieGekoppelt / 1000:0.00} kWh; Sollwert ab Stunde {erreichtIdeal} → {erreichtGekoppelt}");

            Assert.True(spitzeGekoppelt < spitzeIdeal, "Spitze kleiner");
            Assert.True(energieGekoppelt <= energieIdeal, "Tagesenergie nicht größer");
            Assert.True(erreichtGekoppelt > erreichtIdeal, "Sollwert später erreicht");
            Assert.True(erreichtGekoppelt > 0, "der Sollwert wird am Tag erreicht");
            Assert.True(gekoppelt[6].UebergabeBegrenzt, "die erste Stunde nach der Absenkung ist begrenzt");
        }

        /// <summary>
        /// <b>Reglerband</b> (4.4, 11.1): Xp = 0 ist die ideale Regelung mit Grenze; ein Band
        /// größer null senkt die mittlere Raumtemperatur der Heizzeit, monoton mit Xp.
        /// </summary>
        [Fact]
        public void Das_Reglerband_senkt_die_mittlere_Raumtemperatur_monoton()
        {
            var k = new Uebergabekennwerte(6000.0, 1.3, 55.0, 45.0, 20.0);
            double vorher = double.PositiveInfinity;
            foreach (double xp in new[] { 0.0, 0.5, 1.0, 2.0 })
            {
                var modell = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
                modell.Zuruecksetzen(20.0);
                double summe = 0.0;
                int n = 0;
                for (int h = 0; h < 24 * 20; h++)
                {
                    double aussen = 2.0 + 6.0 * Math.Sin(2.0 * Math.PI * h / 24.0);
                    Stundenrand r = Rand(aussen, 20.0, 300.0, k, 50.0, xp);
                    Stundenergebnis e = modell.Schritt(in r);
                    if (h >= 24 * 5) { summe += e.ThetaAirMittel; n++; }
                }
                double mittel = summe / n;
                _aus.WriteLine("Xp " + xp + ": " + mittel.ToString("0.000") + " °C");
                Assert.True(mittel < vorher, "Xp " + xp);
                if (xp == 0.0) Assert.True(Math.Abs(mittel - 20.0) < 1e-6, "Xp = 0 hält den Sollwert, solange die Übergabe reicht");
                vorher = mittel;
            }
        }

        /// <summary>
        /// <b>Begrenzung</b>: Reicht die Übergabe nicht, fällt die Raumluft unter den Sollwert,
        /// und die Stunden tragen den Grund „Übergabe". Im eingeschwungenen Zustand liefert die
        /// Stunde genau, was die Übergabe bei der Raumluft hergibt — der Arbeitspunkt, an dem
        /// der Sekantenleitwert linearisiert ist.
        /// </summary>
        [Fact]
        public void Eine_zu_kleine_Uebergabe_laesst_die_Raumluft_fallen_und_zaehlt_die_Stunden()
        {
            var k = new Uebergabekennwerte(1500.0, 1.3, 55.0, 45.0, 20.0);
            var modell = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            modell.Zuruecksetzen(20.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 24 * 30; h++)
            {
                Stundenrand r = Rand(-10.0, 20.0, 100.0, k, 55.0, 1.0);
                e = modell.Schritt(in r);
            }
            Assert.True(e.ThetaAirMittel < 19.0, "Raumluft " + e.ThetaAirMittel);
            Assert.Equal(1.0, e.UebergabeBegrenztAnteil, 12);
            Assert.Equal(Begrenzungsgrund.Uebergabe, e.Begrenzungsgrund);
            Assert.Equal(1, e.Abschnitte);
            double soll = Waermeuebergabe.LeistungOffenW(k, 55.0, e.ThetaAirMittel);
            Assert.True(Math.Abs(e.HeizleistungW - soll) <= 1e-6 * soll, "eingeschwungen: " + e.HeizleistungW + " gegen " + soll);
            Assert.Equal(55.0 - e.HeizleistungW / k.WHWK, e.RuecklaufC, 9);
        }

        /// <summary>Jenseits der Heizgrenze (Vorlauf NaN) heizt nichts; die Stunde trägt den Grund.</summary>
        [Fact]
        public void Jenseits_der_Heizgrenze_heizt_die_Uebergabe_nicht()
        {
            var k = new Uebergabekennwerte(5000.0, 1.3, 55.0, 45.0, 20.0);
            var modell = new Zonenmodell2K(Phantasiegebaeude.Standard());
            modell.Zuruecksetzen(15.0);
            Stundenrand r = Rand(10.0, 20.0, 100.0, k, double.NaN, 1.0);
            Stundenergebnis e = modell.Schritt(in r);
            Assert.Equal(0.0, e.HeizleistungW);
            Assert.Equal(Begrenzungsgrund.Heizgrenze, e.Begrenzungsgrund);
            Assert.True(double.IsNaN(e.VorlaufC) && double.IsNaN(e.RuecklaufC));
            Assert.Equal(1.0, e.HeizgrenzeAnteil, 12);
        }

        /// <summary>Die Heizleistungsgrenze greift über der Übergabe (H4) und trägt ihren eigenen Grund.</summary>
        [Fact]
        public void Heizleistung_Max_kappt_unter_der_Uebergabe()
        {
            var k = new Uebergabekennwerte(8000.0, 1.3, 55.0, 45.0, 20.0);
            var modell = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            modell.Zuruecksetzen(20.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 24 * 20; h++)
            {
                Stundenrand r = Rand(-10.0, 20.0, 100.0, k, 55.0, 1.0, heizMaxW: 1500.0);
                e = modell.Schritt(in r);
            }
            Assert.Equal(1500.0, e.HeizleistungW, 6);
            Assert.Equal(Begrenzungsgrund.HeizleistungMax, e.Begrenzungsgrund);
            Assert.Equal(1.0, e.HeizleistungMaxAnteil, 12);
            Assert.Equal(0.0, e.UebergabeBegrenztAnteil);
        }

        /// <summary><b>Determinismus</b> (N-A2): zwei Läufe mit Übergabe sind byte-gleich.</summary>
        [Fact]
        public void Zwei_Laeufe_mit_Uebergabe_sind_byte_gleich()
        {
            var k = new Uebergabekennwerte(2500.0, 1.1, 35.0, 28.0, 20.0);
            List<Stundenrand> folge = Folge(2000, 99, r => MitKopplung(r, k, 34.0, 1.0));
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(17.0);
            b.Zuruecksetzen(17.0);
            int hoechsteAbschnitte = 0;
            for (int h = 0; h < folge.Count; h++)
            {
                Stundenrand r = folge[h];
                Stundenergebnis ea = a.Schritt(in r);
                Stundenergebnis eb = b.Schritt(in r);
                Bitgleich(ea, eb, h);
                Assert.True(ea.RuecklaufC.Equals(eb.RuecklaufC));
                hoechsteAbschnitte = Math.Max(hoechsteAbschnitte, ea.Abschnitte);
            }
            _aus.WriteLine("höchste Abschnittszahl einer Stunde: " + hoechsteAbschnitte);
        }
    }
}
