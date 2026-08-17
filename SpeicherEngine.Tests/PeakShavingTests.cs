using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Strategie (d) Peak-Shaving (Fachkonzept 6.4, Arbeitspaket AP7).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Drei Saeulen: <b>Handrechnungen</b> auf Minifaellen (fest und adaptiv),
    /// <b>Eigenschaftstests</b> (Schwelle, SoC-Band, Energieerhaltung, Minimalitaet
    /// der adaptiven Schwelle) und der <b>Regressionstest</b> gegen die verifizierte
    /// Vorlage.
    /// </para>
    /// <para>
    /// <b>Herkunft der Referenzdatei <c>TestData\peakshaving_kauffmann.csv</c>.</b>
    /// Ausgelesen aus <c>Lastgangauswertung_2024_Kauffmann-V4.xlsm</c> (Kopie,
    /// schreibgeschuetzt geoeffnet), Blatt <c>Daten</c>, Zeilen 3 bis 20446:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Spalte C "Lastgang" -&gt; <c>p_last_kw</c> [kW]</description></item>
    ///   <item><description>Spalte J "Lastgang PaekShave" -&gt; <c>p_neu_soll_kw</c> [kW]</description></item>
    ///   <item><description>Spalte M (unbeschriftet) -&gt; <c>soc_soll_kwh</c> [kWh]</description></item>
    /// </list>
    /// <para>
    /// Die Parameter des Laufs stehen im selben Blatt in K13:L18:
    /// <c>L14 = 200</c> (Leistung Speicher [kW]), <c>L15 = 300</c> (Energie Speicher
    /// [kWh]), <c>L17 = 1</c> (Adaptiv), <c>L16 = 687,2</c> (Lastspitze [kW], das
    /// zurueckgeschriebene Ergebnis) und <c>L18 = MAX(J:J) = 687,2</c>. Die
    /// Ausgangsspitze steht in <c>L5 = 738,4</c> kW.
    /// </para>
    /// <para>
    /// Die Reihe ist bewusst <b>kein volles Jahr</b>: die Mappe traegt 20.444
    /// Viertelstundenwerte (01.01.2023 00:00 bis 01.08.2023 23:45; die vier
    /// Intervalle der Sommerzeitumstellung fehlen). Der Algorithmus ist
    /// laengenunabhaengig, der Regressionswert dadurch nicht beeintraechtigt.
    /// </para>
    /// </remarks>
    public sealed class PeakShavingTests
    {
        // ==================================================================
        // Gemeinsame Bausteine der Handrechnungen
        // ==================================================================

        /// <summary>
        /// Verlustfreier Minispeicher: P = 25 kW, Band 0 .. 5 kWh, eta_RT = 1,
        /// dt = 0,25 h. Start-SoC wird je Fall gesetzt.
        /// </summary>
        private static SpeicherParameter Mini(double startSoCKwh)
        {
            return new SpeicherParameter
            {
                CNomKwh = 5.0,
                PKw = 25.0,
                SoCMinKwh = 0.0,
                SoCMaxKwh = 5.0,
                RoundTripWirkungsgrad = 1.0,
                StartSoCKwh = startSoCKwh,
                DtH = 0.25,
                CCapEurProKwh = 0.0,
                Kapitalzins = 0.0,
                NutzungsdauerA = 10.0
            };
        }

        private static PeakShavingParameter Fest(double zielKw, double lp = 0.0, double preisCt = 0.0)
            => new PeakShavingParameter
            {
                PZielKw = zielKw,
                Adaptiv = false,
                LeistungspreisEurProKwA = lp,
                BezugspreisMittelCtKwh = preisCt
            };

        private static PeakShavingParameter Adaptiv(double lp = 0.0, double preisCt = 0.0)
            => new PeakShavingParameter
            {
                Adaptiv = true,
                LeistungspreisEurProKwA = lp,
                BezugspreisMittelCtKwh = preisCt
            };

        // ==================================================================
        // 1 - Handrechnung fester Modus
        // ==================================================================

        /// <summary>
        /// Handrechnung fester Modus, Speicher reicht.
        /// P = 25 kW, Band 0..5 kWh, Start-SoC 5 kWh, eta = 1, P_ziel = 100 kW,
        /// Last = [60, 120, 60, 120] kW:
        /// <code>
        /// i=0  dMax=min(25, 5/0,25)=20   60&lt;=100 -> pc=min(25, 40, (5-5)/0,25=0)=0    P_neu= 60  SoC=5
        /// i=1  dMax=20                  120&gt; 100 -> pd=min(20, 20)=20                P_neu=100  SoC=0
        /// i=2  dMax=0                    60&lt;=100 -> pc=min(25, 40, (5-0)/0,25=20)=20  P_neu= 80  SoC=5
        /// i=3  dMax=20                  120&gt; 100 -> pd=min(20, 20)=20                P_neu=100  SoC=0
        /// </code>
        /// </summary>
        [Fact]
        public void Handrechnung_Fest_Speicher_Reicht()
        {
            double[] last = { 60.0, 120.0, 60.0, 120.0 };
            PeakShavingErgebnis e = new PeakShaving(Fest(100.0))
                .BerechnePeakShaving(last, Mini(5.0));

            Assert.Equal(new[] { 60.0, 100.0, 80.0, 100.0 }, e.PNeuKw);
            Assert.Equal(new[] { 5.0, 0.0, 5.0, 0.0 }, e.SoCKwh);
            Assert.Equal(new[] { 0.0, 0.0, 5.0, 0.0 }, e.LadungAcKwh);
            Assert.Equal(new[] { 0.0, 5.0, 0.0, 5.0 }, e.EntladungAcKwh);

            Assert.Equal(120.0, e.PAltMaxKw);
            Assert.Equal(100.0, e.PNeuMaxKw);
            Assert.Equal(20.0, e.KappungKw);
            Assert.Equal(100.0, e.ErreichteSchwelleKw);
            Assert.False(e.SchwelleGerissen);

            Assert.Equal(5.0, e.LadeenergieKwh);
            Assert.Equal(10.0, e.EntladeenergieKwh);
            // eta = 1: kein Umwandlungsverlust, die Differenz ist reine SoC-Aenderung.
            Assert.Equal(0.0, e.SpeicherverlusteKwh, 12);
        }

        /// <summary>
        /// Derselbe Fall mit P_ziel = 90 kW: der Speicher haelt die Schwelle nicht,
        /// P_neu_max bleibt bei 100 kW und das Flag "Schwelle gerissen" steht.
        /// </summary>
        [Fact]
        public void Handrechnung_Fest_Speicher_Zu_Klein_Setzt_Flag()
        {
            double[] last = { 60.0, 120.0, 60.0, 120.0 };
            PeakShavingErgebnis e = new PeakShaving(Fest(90.0))
                .BerechnePeakShaving(last, Mini(5.0));

            Assert.Equal(new[] { 60.0, 100.0, 80.0, 100.0 }, e.PNeuKw);
            Assert.Equal(100.0, e.PNeuMaxKw);
            Assert.Equal(90.0, e.ErreichteSchwelleKw);
            Assert.True(e.SchwelleGerissen);
        }

        // ==================================================================
        // 2 - Handrechnung adaptiver Modus
        // ==================================================================

        /// <summary>
        /// Handrechnung adaptiver Modus. P = 25 kW, Band 0..5 kWh, Start-SoC 0,
        /// eta = 1, Last = [50, 10, 10, 100] kW:
        /// <code>
        /// i=0  dMax=0   50-0 = 50 &gt; 0  -> P_ziel=50   50&gt;50? nein  pc=min(25, 0, 20)=0    P_neu=50  SoC=0
        /// i=1  dMax=0   10-0 = 10 &gt; 50? nein           10&gt;50? nein  pc=min(25, 40, 20)=20  P_neu=30  SoC=5
        /// i=2  dMax=20  10-20 &lt; 50                     10&gt;50? nein  pc=min(25, 40,  0)=0   P_neu=10  SoC=5
        /// i=3  dMax=20 100-20 = 80 &gt; 50 -> P_ziel=80  100&gt;80        pd=min(20, 20)=20      P_neu=80  SoC=0
        /// </code>
        /// Die Schwelle wird nur nachgezogen, wenn der Speicher sie nicht halten kann;
        /// am Ende steht die minimal erreichbare Spitze.
        /// </summary>
        [Fact]
        public void Handrechnung_Adaptiv()
        {
            double[] last = { 50.0, 10.0, 10.0, 100.0 };
            PeakShavingErgebnis e = new PeakShaving(Adaptiv())
                .BerechnePeakShaving(last, Mini(0.0));

            Assert.Equal(new[] { 50.0, 30.0, 10.0, 80.0 }, e.PNeuKw);
            Assert.Equal(new[] { 0.0, 5.0, 5.0, 0.0 }, e.SoCKwh);

            Assert.Equal(100.0, e.PAltMaxKw);
            Assert.Equal(80.0, e.PNeuMaxKw);
            Assert.Equal(80.0, e.ErreichteSchwelleKw);
            Assert.False(e.SchwelleGerissen);   // adaptiv reisst nie
        }

        /// <summary>
        /// Im adaptiven Modus faellt die erreichte Schwelle konstruktionsbedingt mit
        /// der neuen Spitze zusammen - das ist die Ruecklieferung nach Fachkonzept 6.4.
        /// </summary>
        [Fact]
        public void Adaptiv_Erreichte_Schwelle_Ist_Die_Neue_Spitze()
        {
            double[] last = Testreihe(2000, 17);
            PeakShavingErgebnis e = new PeakShaving(Adaptiv())
                .BerechnePeakShaving(last, Standard(150.0));

            Assert.Equal(e.PNeuMaxKw, e.ErreichteSchwelleKw, 12);
            Assert.False(e.SchwelleGerissen);
            Assert.True(e.PNeuMaxKw < e.PAltMaxKw, "Der Lauf muss ueberhaupt kappen.");
        }

        // ==================================================================
        // 3 - Eigenschaft: die feste Schwelle wird nie ueberschritten,
        //     solange der Speicher reicht
        // ==================================================================

        /// <summary>
        /// Im festen Modus liegt P_neu[i] nie ueber der Schwelle, solange der
        /// Speicher reicht. Reicht er nicht, steht das Flag - und jedes
        /// Ueberschreiten hat genau eine von zwei Ursachen: der Speicher steht an
        /// der Untergrenze (Energie erschoepft) oder die Entladung laeuft an der
        /// Leistungsgrenze P.
        /// </summary>
        [Fact]
        public void Fest_Schwelle_Wird_Nur_Bei_Erschoepftem_Speicher_Ueberschritten()
        {
            double[] last = Testreihe(3000, 4711);
            SpeicherParameter p = Standard(500.0);

            // Eine Schwelle oberhalb der minimal erreichbaren ist haltbar.
            double minimal = new PeakShaving(Adaptiv()).BerechnePeakShaving(last, p).ErreichteSchwelleKw;
            double schwelle = minimal + 5.0;

            PeakShavingErgebnis reicht = new PeakShaving(Fest(schwelle)).BerechnePeakShaving(last, p);
            Assert.False(reicht.SchwelleGerissen);
            for (int i = 0; i < reicht.Anzahl; i++)
                Assert.True(reicht.PNeuKw[i] <= schwelle + 1e-9,
                    "P_neu[" + i + "] = " + reicht.PNeuKw[i] + " > " + schwelle);
            Assert.True(reicht.PNeuMaxKw < reicht.PAltMaxKw, "Der Lauf muss ueberhaupt kappen.");

            // Zu klein -> das Flag steht, und jedes Ueberschreiten ist erklaerbar.
            PeakShavingErgebnis knapp = new PeakShaving(Fest(90.0)).BerechnePeakShaving(last, p);
            Assert.True(knapp.SchwelleGerissen);
            bool ueberschritten = false;
            for (int i = 0; i < knapp.Anzahl; i++)
            {
                if (knapp.PNeuKw[i] <= 90.0 + 1e-9) continue;
                ueberschritten = true;
                bool leer = knapp.SoCKwh[i] <= p.SoCMinKwh + 1e-9;
                bool anDerLeistungsgrenze = knapp.EntladungAcKwh[i] >= p.PKw * p.DtH - 1e-9;
                Assert.True(leer || anDerLeistungsgrenze,
                    "Intervall " + i + ": Schwelle ueberschritten, obwohl SoC = " + knapp.SoCKwh[i] +
                    " und Entladung = " + knapp.EntladungAcKwh[i]);
            }
            Assert.True(ueberschritten, "Der knappe Fall muss die Schwelle wenigstens einmal reissen.");
        }

        // ==================================================================
        // 4 - Eigenschaft: die adaptive Schwelle ist die minimal erreichbare
        // ==================================================================

        /// <summary>
        /// Die adaptiv gefundene Schwelle ist immer haltbar und nie kleiner als die
        /// per Bisektion bestimmte Untergrenze. Auf dieser Reihe fallen beide
        /// zusammen - das Greedy trifft hier das Optimum; am Referenzlastgang tut es
        /// das <b>nicht</b> (siehe
        /// <see cref="Kauffmann_Adaptive_Schwelle_Ist_Haltbar_Aber_Nicht_Minimal"/>).
        /// </summary>
        [Fact]
        public void Adaptive_Schwelle_Ist_Haltbar_Und_Nie_Unter_Der_Untergrenze()
        {
            double[] last = Testreihe(2500, 909);
            SpeicherParameter p = Standard(200.0);

            double s = new PeakShaving(Adaptiv()).BerechnePeakShaving(last, p).ErreichteSchwelleKw;
            double minimal = PeakShaving.MinimaleSchwelleKw(last, p);

            // Genau auf der adaptiven Schwelle: haltbar.
            PeakShavingErgebnis auf = new PeakShaving(Fest(s)).BerechnePeakShaving(last, p);
            Assert.False(auf.SchwelleGerissen);
            Assert.Equal(s, auf.PNeuMaxKw, 9);

            // Die Bisektion kann nie hoeher liegen als das Greedy.
            Assert.True(minimal <= s + 1e-9, "minimal = " + minimal + ", adaptiv = " + s);

            // Auf dieser Reihe ist das Greedy optimal - unterhalb reisst jede Vorgabe.
            Assert.Equal(s, minimal, 6);
            foreach (double abstand in new[] { 0.001, 0.1, 1.0, 5.0, 25.0 })
            {
                PeakShavingErgebnis unter = new PeakShaving(Fest(s - abstand))
                    .BerechnePeakShaving(last, p);
                Assert.True(unter.SchwelleGerissen,
                    "Schwelle " + (s - abstand).ToString("R", CultureInfo.InvariantCulture) +
                    " duerfte mit diesem Speicher nicht haltbar sein.");
                Assert.True(unter.PNeuMaxKw > s - abstand + 1e-9);
            }
        }

        // ==================================================================
        // 5 - Eigenschaft: SoC bleibt im Band
        // ==================================================================

        /// <summary>
        /// Der Ladezustand verlaesst das Band SoC_min .. SoC_max in keinem Intervall -
        /// weder im festen noch im adaptiven Modus, mit und ohne Verluste.
        /// </summary>
        [Theory]
        [InlineData(true, 1.00)]
        [InlineData(true, 0.81)]
        [InlineData(false, 1.00)]
        [InlineData(false, 0.81)]
        public void SoC_Bleibt_Im_Band(bool adaptiv, double etaRt)
        {
            double[] last = Testreihe(4000, 31337);
            SpeicherParameter p = Standard(400.0) with
            {
                SoCMinKwh = 40.0,
                SoCMaxKwh = 360.0,
                StartSoCKwh = 120.0,
                RoundTripWirkungsgrad = etaRt
            };

            PeakShavingErgebnis e = adaptiv
                ? new PeakShaving(Adaptiv()).BerechnePeakShaving(last, p)
                : new PeakShaving(Fest(140.0)).BerechnePeakShaving(last, p);

            for (int i = 0; i < e.Anzahl; i++)
            {
                Assert.True(e.SoCKwh[i] >= p.SoCMinKwh - 1e-9,
                    "SoC[" + i + "] = " + e.SoCKwh[i] + " < SoC_min");
                Assert.True(e.SoCKwh[i] <= p.SoCMaxKwh + 1e-9,
                    "SoC[" + i + "] = " + e.SoCKwh[i] + " > SoC_max");
            }
        }

        // ==================================================================
        // 6 - Eigenschaft: Energieerhaltung im energetischen Modus
        // ==================================================================

        /// <summary>
        /// Energiebilanz des energetischen Modus. Je Intervall gilt
        /// <c>SoC += E_ac_ch * eta_ch</c> beziehungsweise
        /// <c>SoC -= E_ac_dis / eta_dis</c>, ueber das Jahr also
        /// <code>
        /// E_lade * eta_ch - E_entlade / eta_dis = SoC_Ende - SoC_Start
        /// </code>
        /// Bei ausgeglichenem Speicherstand folgt daraus die bekannte Form
        /// <c>E_entlade = E_lade * eta_ch * eta_dis = E_lade * eta_RT</c>. Zusaetzlich
        /// wird der Verlustausweis der Kennzahlen geprueft.
        /// </summary>
        [Fact]
        public void Energieerhaltung_Im_Energetischen_Modus()
        {
            double[] last = Testreihe(3500, 2024);
            SpeicherParameter p = Standard(400.0) with
            {
                SoCMinKwh = 0.0,
                SoCMaxKwh = 400.0,
                StartSoCKwh = 0.0,
                RoundTripWirkungsgrad = 0.81   // eta_ch = eta_dis = 0,9
            };

            PeakShavingErgebnis e = new PeakShaving(Adaptiv()).BerechnePeakShaving(last, p);

            double socEnde = e.SoCKwh[e.Anzahl - 1];
            double socStart = p.StartSoCEffektivKwh;

            Assert.True(e.LadeenergieKwh > 0.0);
            Assert.True(e.EntladeenergieKwh > 0.0);

            Assert.Equal(socEnde - socStart,
                         e.LadeenergieKwh * p.EtaCh - e.EntladeenergieKwh / p.EtaDis, 6);

            // Verlustausweis nach der Konvention aus SpeicherKennzahlen.
            Assert.Equal(e.LadeenergieKwh - e.EntladeenergieKwh - (socEnde - socStart),
                         e.SpeicherverlusteKwh, 9);

            // Verluste treten bei eta_RT < 1 tatsaechlich auf.
            Assert.True(e.SpeicherverlusteKwh > 0.0);

            // Ohne Verluste bleibt nur die SoC-Aenderung uebrig.
            PeakShavingErgebnis ideal = new PeakShaving(Adaptiv())
                .BerechnePeakShaving(last, p with { RoundTripWirkungsgrad = 1.0 });
            Assert.Equal(0.0, ideal.SpeicherverlusteKwh, 8);
        }

        /// <summary>
        /// Einzelschritt-Handrechnung der Wirkungsgrade (energetischer Modus).
        /// Entladen: P = 20 kW, Band 2..10 kWh, SoC 6 kWh, eta_dis = 0,9,
        /// P_ziel = 100 kW, Last 120 kW ->
        /// <c>dMax = min(20; (6-2)*0,9/0,25 = 14,4) = 14,4</c>,
        /// <c>P_neu = 105,6</c>, <c>SoC = 6 - 14,4*0,25/0,9 = 2</c>.
        /// Laden: SoC 2 kWh, Last 50 kW -> <c>pc = min(20; 50; (10-2)/(0,9*0,25)) = 20</c>,
        /// <c>P_neu = 70</c>, <c>SoC = 2 + 20*0,25*0,9 = 6,5</c>.
        /// </summary>
        [Fact]
        public void Handrechnung_Wirkungsgrade_Einzelschritt()
        {
            SpeicherParameter basis = new SpeicherParameter
            {
                CNomKwh = 10.0,
                PKw = 20.0,
                SoCMinKwh = 2.0,
                SoCMaxKwh = 10.0,
                RoundTripWirkungsgrad = 0.81,
                DtH = 0.25,
                Kapitalzins = 0.0,
                NutzungsdauerA = 10.0
            };

            PeakShavingErgebnis entladen = new PeakShaving(Fest(100.0))
                .BerechnePeakShaving(new[] { 120.0 }, basis with { StartSoCKwh = 6.0 });
            Assert.Equal(105.6, entladen.PNeuKw[0], 10);
            Assert.Equal(2.0, entladen.SoCKwh[0], 10);
            Assert.Equal(3.6, entladen.EntladeenergieKwh, 10);

            PeakShavingErgebnis laden = new PeakShaving(Fest(100.0))
                .BerechnePeakShaving(new[] { 50.0 }, basis with { StartSoCKwh = 2.0 });
            Assert.Equal(70.0, laden.PNeuKw[0], 10);
            Assert.Equal(6.5, laden.SoCKwh[0], 10);
            Assert.Equal(5.0, laden.LadeenergieKwh, 10);
        }

        // ==================================================================
        // 7 - Kompatibilitaetsmodus
        // ==================================================================

        /// <summary>
        /// Der Kompatibilitaetsmodus erzwingt die Originalfassung
        /// (eta = 1, SoC_min = 0, Start-SoC = 0) und ignoriert dazu abweichende
        /// Parameter. Nachweis: ein Kompatibilitaetslauf mit "stoerenden" Parametern
        /// ist bitgleich zu einem energetischen Lauf, der die Originalwerte
        /// ausdruecklich gesetzt bekommt.
        /// </summary>
        [Fact]
        public void Kompatibilitaetsmodus_Erzwingt_Originalfassung()
        {
            double[] last = Testreihe(1500, 4);

            SpeicherParameter stoerend = Standard(300.0) with
            {
                SoCMinKwh = 60.0,
                SoCMaxKwh = 300.0,
                StartSoCKwh = 180.0,
                RoundTripWirkungsgrad = 0.7
            };
            SpeicherParameter original = stoerend with
            {
                SoCMinKwh = 0.0,
                StartSoCKwh = 0.0,
                RoundTripWirkungsgrad = 1.0
            };

            PeakShavingErgebnis kompat = new PeakShaving(Adaptiv(), SpeicherModus.ExcelKompatibilitaet)
                .BerechnePeakShaving(last, stoerend);
            PeakShavingErgebnis energ = new PeakShaving(Adaptiv())
                .BerechnePeakShaving(last, original);

            Assert.Equal(SpeicherModus.ExcelKompatibilitaet, kompat.Modus);
            for (int i = 0; i < kompat.Anzahl; i++)
            {
                Assert.True(Bitgleich(kompat.PNeuKw[i], energ.PNeuKw[i]), "P_neu[" + i + "]");
                Assert.True(Bitgleich(kompat.SoCKwh[i], energ.SoCKwh[i]), "SoC[" + i + "]");
            }
            Assert.Equal(energ.ErreichteSchwelleKw, kompat.ErreichteSchwelleKw);
        }

        /// <summary>
        /// Anders als bei <see cref="Dauernutzung"/> laesst der
        /// Kompatibilitaetsmodus des Peak-Shavings <b>Intervall 0 nicht aus</b>: die
        /// Vorlage rechnet den ersten Viertelstundenwert mit.
        /// </summary>
        [Fact]
        public void Kompatibilitaetsmodus_Rechnet_Intervall_0_Mit()
        {
            // Der uebergebene Start-SoC von 5 kWh wird auf 0 gezwungen, also kann
            // Intervall 0 nur laden. Mit fester Schwelle 100 und Last 60 begrenzt
            // nicht P (25 kW), sondern das Band: (5 - 0)/(1*0,25) = 20 kW.
            PeakShavingErgebnis e = new PeakShaving(Fest(100.0), SpeicherModus.ExcelKompatibilitaet)
                .BerechnePeakShaving(new[] { 60.0 }, Mini(5.0));

            Assert.Equal(80.0, e.PNeuKw[0]);          // 60 + 20
            Assert.Equal(5.0, e.SoCKwh[0]);           // 0 + 20*0,25 -- Start-SoC war 0, nicht 5
            Assert.Equal(5.0, e.LadeenergieKwh);

            // Gegenprobe: energetisch mit Start-SoC 5 kWh laedt Intervall 0 gar nicht.
            PeakShavingErgebnis energ = new PeakShaving(Fest(100.0))
                .BerechnePeakShaving(new[] { 60.0 }, Mini(5.0));
            Assert.Equal(60.0, energ.PNeuKw[0]);
            Assert.Equal(5.0, energ.SoCKwh[0]);
            Assert.Equal(0.0, energ.LadeenergieKwh);
        }

        // ==================================================================
        // 8 - Monetarisierung (Fachkonzept 6.4)
        // ==================================================================

        /// <summary>
        /// Monetarisierung nach Fachkonzept 6.4:
        /// <code>
        /// Ertrag_PS = (P_alt_max - P_neu_max) * L_P - (E_lade - E_entlade) * p_bezug,mittel/100
        /// </code>
        /// Handrechnung zum Fall <see cref="Handrechnung_Fest_Speicher_Reicht"/> mit
        /// L_P = 100 EUR/(kW*a) und p = 30 ct/kWh:
        /// Ersparnis = 20 * 100 = 2000 EUR; Verlustterm = (5 - 10) * 0,30 = -1,50 EUR;
        /// Ertrag_PS = 2000 + 1,50 = 2001,50 EUR.
        /// </summary>
        [Fact]
        public void Monetarisierung_Nach_Fachkonzept_6_4()
        {
            double[] last = { 60.0, 120.0, 60.0, 120.0 };
            PeakShavingErgebnis e = new PeakShaving(Fest(100.0, lp: 100.0, preisCt: 30.0))
                .BerechnePeakShaving(last, Mini(5.0));

            Assert.Equal(2000.0, e.LeistungspreisersparnisEur, 10);
            Assert.Equal(-1.5, e.VerlustkostenEur, 10);
            Assert.Equal(2001.5, e.ErtragPsEur, 10);

            // Die Intervallreihe traegt genau den Verlustterm.
            Assert.Equal(-e.VerlustkostenEur, e.Basis.SummeGeldwertEur, 10);

            // E_a,1 der Wirtschaftlichkeitsrechnung ist Ertrag_PS.
            Assert.Equal(e.ErtragPsEur, e.Wirtschaftlichkeit.ErtragReferenzjahrEur);
        }

        /// <summary>
        /// Der Wirtschaftlichkeitsblock ist der vorhandene aus Fachkonzept 6.2, nur
        /// mit E_a,1 = Ertrag_PS gespeist - Gegenprobe gegen den Direktaufruf.
        /// </summary>
        [Fact]
        public void Wirtschaftlichkeitsblock_Kommt_Aus_Der_Vorhandenen_Rechnung()
        {
            double[] last = Testreihe(2000, 12);
            SpeicherParameter p = Standard(300.0) with
            {
                CCapEurProKwh = 400.0,
                CPowEurProKw = 150.0,
                IFixEur = 5000.0,
                Kapitalzins = 0.04,
                NutzungsdauerA = 15.0,
                DegradationProA = 0.01
            };

            PeakShavingErgebnis e = new PeakShaving(Adaptiv(lp: 120.0, preisCt: 28.0))
                .BerechnePeakShaving(last, p);

            WirtschaftlichkeitErgebnis soll = Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = e.ErtragPsEur,
                InvestitionEur = p.InvestitionEur,
                Kapitalzins = p.Kapitalzins,
                NutzungsdauerA = p.NutzungsdauerA,
                DegradationProA = p.DegradationProA
            });

            Assert.Equal(soll, e.Wirtschaftlichkeit);
            Assert.True(e.ErtragPsEur > 0.0);
        }

        // ==================================================================
        // 9 - Bezugsgroesse der Lastspitze (offener Punkt 4)
        // ==================================================================

        /// <summary>
        /// <b>Gesetzter Default (offener Punkt 4):</b> Bezugsgroesse ist das
        /// <b>Jahresmaximum</b> der Viertelstundenleistung. Die Monatsauswertung ist
        /// Option und wird zusaetzlich geliefert.
        /// </summary>
        [Fact]
        public void Default_Ist_Das_Jahresmaximum_Der_Viertelstundenleistung()
        {
            double[] last = Testreihe(35040, 8);
            PeakShavingErgebnis e = new PeakShaving(Adaptiv()).BerechnePeakShaving(last, Standard(500.0));

            double maxAlt = double.NegativeInfinity, maxNeu = double.NegativeInfinity;
            for (int i = 0; i < last.Length; i++)
            {
                if (last[i] > maxAlt) maxAlt = last[i];
                if (e.PNeuKw[i] > maxNeu) maxNeu = e.PNeuKw[i];
            }
            Assert.Equal(maxAlt, e.PAltMaxKw);
            Assert.Equal(maxNeu, e.PNeuMaxKw);

            // Monatsspitzen als Option: zwoelf Monate, Summe der Intervalle = 35.040,
            // Jahresmaximum = groesste Monatsspitze.
            IReadOnlyList<Monatsspitze> monate = e.Monatsspitzen;
            Assert.Equal(12, monate.Count);
            int summe = 0;
            double groessteAlt = double.NegativeInfinity;
            for (int m = 0; m < monate.Count; m++)
            {
                Assert.Equal(m + 1, monate[m].Monat);
                summe += monate[m].Intervalle;
                if (monate[m].PAltMaxKw > groessteAlt) groessteAlt = monate[m].PAltMaxKw;
            }
            Assert.Equal(35040, summe);
            Assert.Equal(e.PAltMaxKw, groessteAlt);

            // Januar 31 Tage, Februar 28 Tage (Gemeinjahr) je 96 Intervalle.
            Assert.Equal(31 * 96, monate[0].Intervalle);
            Assert.Equal(28 * 96, monate[1].Intervalle);
            Assert.Equal(31 * 96, monate[11].Intervalle);
        }

        /// <summary>
        /// Die Monatszuordnung erkennt Schaltjahr (35.136) und Stundenraster (8.760).
        /// </summary>
        [Theory]
        [InlineData(35136, 0.25, 29)]   // Schaltjahr, Viertelstunden -> Februar 29 Tage
        [InlineData(35040, 0.25, 28)]
        [InlineData(8784, 1.0, 29)]     // Schaltjahr, Stunden
        [InlineData(8760, 1.0, 28)]
        public void Monatsspitzen_Erkennen_Raster_Und_Schaltjahr(int n, double dtH, int februarTage)
        {
            double[] alt = Testreihe(n, 5);
            IReadOnlyList<Monatsspitze> monate = PeakShaving.Monatsspitzen(alt, alt, dtH);

            int proTag = (int)Math.Round(24.0 / dtH);
            Assert.Equal(12, monate.Count);
            Assert.Equal(31 * proTag, monate[0].Intervalle);
            Assert.Equal(februarTage * proTag, monate[1].Intervalle);

            int summe = 0;
            for (int m = 0; m < monate.Count; m++) summe += monate[m].Intervalle;
            Assert.Equal(n, summe);
        }

        // ==================================================================
        // 10 - Vertraege: Zustandsfreiheit, Eingang unveraendert, Strategiepfad
        // ==================================================================

        /// <summary>
        /// Die Strategie ist zustandsfrei: zwei Laeufe derselben Instanz liefern
        /// bitgleiche Ergebnisse, und die Eingangsreihe bleibt unveraendert.
        /// </summary>
        [Fact]
        public void Zustandsfrei_Und_Eingang_Unveraendert()
        {
            double[] last = Testreihe(2000, 77);
            double[] kopie = (double[])last.Clone();
            PeakShaving strategie = new PeakShaving(Adaptiv(lp: 90.0, preisCt: 25.0));
            SpeicherParameter p = Standard(250.0);

            PeakShavingErgebnis a = strategie.BerechnePeakShaving(last, p);
            PeakShavingErgebnis b = strategie.BerechnePeakShaving(last, p);

            for (int i = 0; i < a.Anzahl; i++)
            {
                Assert.True(Bitgleich(a.PNeuKw[i], b.PNeuKw[i]));
                Assert.True(Bitgleich(a.SoCKwh[i], b.SoCKwh[i]));
                Assert.True(Bitgleich(last[i], kopie[i]), "Die Eingangsreihe wurde veraendert.");
            }
            Assert.True(Bitgleich(a.ErtragPsEur, b.ErtragPsEur));

            // Das Ergebnis haelt eine eigene Kopie des Lastgangs.
            Assert.NotSame(last, a.PAltKw);
        }

        /// <summary>
        /// Der Weg ueber <see cref="ISpeicherStrategie"/> liefert dasselbe
        /// Basisergebnis wie der Direktaufruf; Erzeugungs- und Preisreihe des
        /// Eingangs bleiben dabei wirkungslos (Fachkonzept 6.4).
        /// </summary>
        [Fact]
        public void Strategiepfad_Ignoriert_Erzeugung_Und_Preisreihe()
        {
            double[] last = Testreihe(1200, 3);
            SpeicherParameter p = Standard(200.0);
            ISpeicherStrategie strategie = new PeakShaving(Adaptiv(lp: 80.0, preisCt: 30.0));

            double[] pv = new double[last.Length];
            for (int i = 0; i < pv.Length; i++) pv[i] = 500.0;   // waere fuer Dauernutzung entscheidend

            SpeicherErgebnis mitPv = strategie.Berechne(
                new SpeicherEingang(last, pv, SpeicherEingang.KonstanteReihe(42.0, last.Length)), p);
            SpeicherErgebnis ohnePv = strategie.Berechne(PeakShaving.NurLast(last), p);

            for (int i = 0; i < mitPv.Anzahl; i++)
            {
                Assert.True(Bitgleich(mitPv.SoCKwh[i], ohnePv.SoCKwh[i]));
                Assert.True(Bitgleich(mitPv.GeldwertEur[i], ohnePv.GeldwertEur[i]));
            }
            Assert.Equal("Peak-Shaving", strategie.Name);
        }

        /// <summary>Unbrauchbare Parameter werden abgewiesen.</summary>
        [Fact]
        public void Parameter_Werden_Geprueft()
        {
            double[] last = { 10.0 };
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PeakShaving(Fest(-1.0)).BerechnePeakShaving(last, Mini(0.0)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PeakShaving(Adaptiv(lp: -5.0)).BerechnePeakShaving(last, Mini(0.0)));
            Assert.Throws<ArgumentException>(() =>
                new PeakShaving(Adaptiv()).BerechnePeakShaving(Array.Empty<double>(), Mini(0.0)));
            Assert.Throws<ArgumentNullException>(() =>
                new PeakShaving(Adaptiv()).BerechnePeakShaving((double[])null!, Mini(0.0)));

            // Eine negative feste Schwelle ist unzulaessig, im adaptiven Modus aber
            // ohne Bedeutung.
            PeakShavingErgebnis e = new PeakShaving(new PeakShavingParameter { Adaptiv = true, PZielKw = -1.0 })
                .BerechnePeakShaving(last, Mini(0.0));
            Assert.Equal(10.0, e.ErreichteSchwelleKw);
        }

        // ==================================================================
        // 11 - Regressionstest gegen die Kauffmann-Mappe
        // ==================================================================

        /// <summary>Struktur der Referenzdatei (siehe Klassenbemerkung).</summary>
        [Fact]
        public void Kauffmann_Referenzdatei_Hat_20444_Intervalle()
        {
            Kauffmann k = Kauffmann.Daten;
            Assert.Equal(20444, k.LastKw.Length);
            Assert.Equal(20444, k.SollPNeuKw.Length);
            Assert.Equal(20444, k.SollSoCKwh.Length);

            // L5 der Mappe: Ausgangsspitze 738,4 kW.
            double max = double.NegativeInfinity;
            for (int i = 0; i < k.LastKw.Length; i++) if (k.LastKw[i] > max) max = k.LastKw[i];
            Assert.Equal(738.4, max, 10);
        }

        /// <summary>
        /// <b>Regressionstest.</b> Der Kompatibilitaetsmodus reproduziert den
        /// gekappten Lastgang (Blattspalte J) und den SoC-Verlauf (Blattspalte M) der
        /// Kauffmann-Mappe <b>bitgenau</b> - Abweichung 0, keine Toleranz.
        /// Parameter aus K13:L18: P = 200 kW, Band 0 .. 300 kWh, adaptiv,
        /// Start-SoC 0, eta = 1, dt = 0,25 h.
        /// </summary>
        [Fact]
        public void Kauffmann_Regression_Kompatibilitaetsmodus_Ist_Bitgenau()
        {
            Kauffmann k = Kauffmann.Daten;
            PeakShavingErgebnis e = new PeakShaving(Adaptiv(), SpeicherModus.ExcelKompatibilitaet)
                .BerechnePeakShaving(k.LastKw, Kauffmann.Parameter());

            for (int i = 0; i < k.LastKw.Length; i++)
            {
                if (!Bitgleich(e.PNeuKw[i], k.SollPNeuKw[i]))
                    Assert.Fail(
                        "Gekappter Lastgang weicht ab in Intervall " + i.ToString(CultureInfo.InvariantCulture) +
                        " (Blattzeile " + (i + 3).ToString(CultureInfo.InvariantCulture) + ", Spalte J)." +
                        Environment.NewLine + "  ist  = " + Bits(e.PNeuKw[i]) +
                        Environment.NewLine + "  soll = " + Bits(k.SollPNeuKw[i]));

                if (!Bitgleich(e.SoCKwh[i], k.SollSoCKwh[i]))
                    Assert.Fail(
                        "SoC weicht ab in Intervall " + i.ToString(CultureInfo.InvariantCulture) +
                        " (Blattzeile " + (i + 3).ToString(CultureInfo.InvariantCulture) + ", Spalte M)." +
                        Environment.NewLine + "  ist  = " + Bits(e.SoCKwh[i]) +
                        Environment.NewLine + "  soll = " + Bits(k.SollSoCKwh[i]));
            }
        }

        /// <summary>
        /// Die Kennzahlen des Referenzlaufs stimmen mit den Zellen der Mappe ueberein:
        /// <c>L5 = 738,4</c> (Spitze vorher), <c>L18 = MAX(J:J) = 687,2</c> (Spitze
        /// nachher) und <c>L16 = 687,2</c> (zurueckgeschriebene Lastspitze).
        /// </summary>
        [Fact]
        public void Kauffmann_Regression_Kennzahlen()
        {
            PeakShavingErgebnis e = new PeakShaving(Adaptiv(), SpeicherModus.ExcelKompatibilitaet)
                .BerechnePeakShaving(Kauffmann.Daten.LastKw, Kauffmann.Parameter());

            Assert.Equal(738.4, e.PAltMaxKw, 10);          // Mappe L5
            Assert.Equal(687.2, e.PNeuMaxKw, 10);          // Mappe L18 = MAX(J:J)
            Assert.Equal(687.2, e.ErreichteSchwelleKw, 10); // Mappe L16
            Assert.Equal(51.2, e.KappungKw, 10);
            Assert.False(e.SchwelleGerissen);

            // Achtprobe: Januar bis August, der August unvollstaendig.
            Assert.Equal(8, e.Monatsspitzen.Count);
            Assert.Equal(1, e.Monatsspitzen[0].Monat);
            Assert.Equal(8, e.Monatsspitzen[7].Monat);
            Assert.True(e.Monatsspitzen[7].Intervalle < 31 * 96);
        }

        /// <summary>
        /// Die adaptiv gefundene Schwelle ist am echten Lastgang <b>haltbar</b>, aber
        /// <b>nicht minimal</b> - der Befund, der <see cref="PeakShaving.MinimaleSchwelleKw"/>
        /// begruendet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Fachkonzept 6.4 sagt, der adaptive Modus liefere "die minimal erreichbare
        /// Spitze". Am Referenzlastgang stimmt das nicht: adaptiv kommt 687,2 kW
        /// heraus, eine feste Vorgabe von 565,76 kW ist aber noch haltbar. Ursache
        /// ist die Anlaufphase - solange die Schwelle noch niedrig ist, begrenzt
        /// <c>P_ziel - P_last</c> das Laden auf nahezu 0, der Speicher geht fast leer
        /// in die erste grosse Spitze, und die einmal nachgezogene Schwelle faellt
        /// nie wieder.
        /// </para>
        /// <para>
        /// Der adaptive Modus bleibt trotzdem unveraendert: er ist die verifizierte
        /// Vorlage und traegt den Regressionstest. Die Minimalsuche steht daneben.
        /// </para>
        /// </remarks>
        [Fact]
        public void Kauffmann_Adaptive_Schwelle_Ist_Haltbar_Aber_Nicht_Minimal()
        {
            double[] last = Kauffmann.Daten.LastKw;
            SpeicherParameter p = Kauffmann.Parameter();
            const SpeicherModus kompat = SpeicherModus.ExcelKompatibilitaet;

            double adaptivSchwelle = new PeakShaving(Adaptiv(), kompat)
                .BerechnePeakShaving(last, p).ErreichteSchwelleKw;
            Assert.Equal(687.2, adaptivSchwelle, 10);

            // Haltbar ist sie - im festen Modus reisst sie nicht.
            PeakShavingErgebnis auf = new PeakShaving(Fest(adaptivSchwelle), kompat)
                .BerechnePeakShaving(last, p);
            Assert.False(auf.SchwelleGerissen);
            Assert.True(Bitgleich(adaptivSchwelle, auf.PNeuMaxKw));

            // Minimal ist sie nicht - schon 687,1 kW haelt derselbe Speicher.
            Assert.False(new PeakShaving(Fest(687.1), kompat)
                .BerechnePeakShaving(last, p).SchwelleGerissen);

            // Die Bisektion findet die tatsaechliche Untergrenze.
            double minimal = PeakShaving.MinimaleSchwelleKw(last, p, kompat);
            Assert.Equal(565.76, minimal, 2);
            Assert.True(minimal < adaptivSchwelle);
            Assert.Equal(121.44, adaptivSchwelle - minimal, 2);
        }

        /// <summary>
        /// Vertrag der Minimalsuche: das Ergebnis ist haltbar, knapp darunter reisst
        /// die Schwelle, und die Kappung uebertrifft die des adaptiven Laufs deutlich
        /// (738,4 -&gt; 565,76 statt 738,4 -&gt; 687,2 kW).
        /// </summary>
        [Fact]
        public void Minimale_Schwelle_Ist_Haltbar_Und_Scharf()
        {
            double[] last = Kauffmann.Daten.LastKw;
            SpeicherParameter p = Kauffmann.Parameter();
            const SpeicherModus kompat = SpeicherModus.ExcelKompatibilitaet;

            double minimal = PeakShaving.MinimaleSchwelleKw(last, p, kompat);

            PeakShavingErgebnis auf = new PeakShaving(Fest(minimal), kompat).BerechnePeakShaving(last, p);
            Assert.False(auf.SchwelleGerissen);
            Assert.Equal(738.4, auf.PAltMaxKw, 10);
            Assert.True(auf.KappungKw > 170.0, "Kappung = " + auf.KappungKw);

            // Knapp darunter haelt der Speicher nicht mehr.
            foreach (double abstand in new[] { 0.01, 0.5, 5.0, 50.0 })
                Assert.True(new PeakShaving(Fest(minimal - abstand), kompat)
                    .BerechnePeakShaving(last, p).SchwelleGerissen,
                    "Schwelle " + (minimal - abstand).ToString("R", CultureInfo.InvariantCulture) +
                    " duerfte nicht mehr haltbar sein.");
        }

        /// <summary>
        /// Randfaelle der Minimalsuche: die Jahresspitze ist immer haltbar (dort wird
        /// nie entladen), und ein Speicher ohne Leistung kann nichts kappen.
        /// </summary>
        [Fact]
        public void Minimale_Schwelle_Randfaelle()
        {
            double[] last = Testreihe(1000, 21);
            SpeicherParameter p = Standard(200.0);

            double minimal = PeakShaving.MinimaleSchwelleKw(last, p);
            double maxLast = double.NegativeInfinity;
            for (int i = 0; i < last.Length; i++) if (last[i] > maxLast) maxLast = last[i];
            Assert.True(minimal <= maxLast);
            Assert.False(new PeakShaving(Fest(minimal)).BerechnePeakShaving(last, p).SchwelleGerissen);

            // Ohne Leistung bleibt nur die Jahresspitze. Der Rest von rund 2,6e-7 kW
            // ist die Haltbarkeitstoleranz der Suche (1e-9 relativ zur Schwelle) -
            // nicht mehr, als die Bisektion per Konstruktion offenlassen darf.
            double ohneLeistung = PeakShaving.MinimaleSchwelleKw(last, p with { PKw = 0.0 });
            Assert.True(Math.Abs(maxLast - ohneLeistung) <= 1e-8 * Math.Max(1.0, maxLast),
                "ohne Leistung = " + ohneLeistung.ToString("R", CultureInfo.InvariantCulture) +
                ", Jahresspitze = " + maxLast.ToString("R", CultureInfo.InvariantCulture));

            Assert.Throws<ArgumentException>(() =>
                PeakShaving.MinimaleSchwelleKw(Array.Empty<double>(), p));
            Assert.Throws<ArgumentNullException>(() =>
                PeakShaving.MinimaleSchwelleKw(null!, p));
        }

        // ==================================================================
        // Hilfen
        // ==================================================================

        /// <summary>
        /// Speicher fuer die Eigenschaftstests: C_nutz = <paramref name="kapazitaetKwh"/>,
        /// P = C/2 (C-Rate 0,5), Band 0 .. C, Start-SoC 0.
        /// </summary>
        private static SpeicherParameter Standard(double kapazitaetKwh)
        {
            return new SpeicherParameter
            {
                CNomKwh = kapazitaetKwh,
                PKw = kapazitaetKwh / 2.0,
                SoCMinKwh = 0.0,
                SoCMaxKwh = kapazitaetKwh,
                RoundTripWirkungsgrad = 1.0,
                StartSoCKwh = 0.0,
                DtH = 0.25,
                CCapEurProKwh = 0.0,
                Kapitalzins = 0.0,
                NutzungsdauerA = 10.0
            };
        }

        /// <summary>
        /// Reproduzierbare Testlastreihe [kW]: Tagesgang mit Wochenrhythmus und
        /// deterministischem Rauschen. Kein Zufall ohne Saat - die Tests muessen bei
        /// jedem Lauf dieselben Zahlen sehen.
        /// </summary>
        private static double[] Testreihe(int n, int saat)
        {
            double[] r = new double[n];
            uint z = (uint)(saat * 2654435761u + 1u);
            for (int i = 0; i < n; i++)
            {
                z = z * 1664525u + 1013904223u;
                double rausch = (z >> 8) / 16777216.0;                 // [0 .. 1)
                double tag = Math.Sin(2.0 * Math.PI * (i % 96) / 96.0);
                double woche = Math.Sin(2.0 * Math.PI * (i % 672) / 672.0);
                r[i] = 100.0 + 60.0 * tag + 20.0 * woche + 40.0 * rausch;
                if (i % 337 == 0) r[i] += 120.0;                        // Lastspitzen
            }
            return r;
        }

        private static bool Bitgleich(double a, double b)
            => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

        private static string Bits(double d)
            => d.ToString("R", CultureInfo.InvariantCulture) +
               " [0x" + BitConverter.DoubleToInt64Bits(d).ToString("X16", CultureInfo.InvariantCulture) + "]";

        // ==================================================================
        // Referenzdaten der Kauffmann-Mappe
        // ==================================================================

        /// <summary>
        /// Zugriff auf <c>TestData\peakshaving_kauffmann.csv</c>. Bewusst eigenstaendig
        /// gehalten (nicht in <see cref="Referenzdaten"/> eingebaut), weil die Datei
        /// zu einer anderen Mappe gehoert als die V7-Referenz.
        /// </summary>
        private sealed class Kauffmann
        {
            private const string Datei = "peakshaving_kauffmann.csv";

            private static readonly Lazy<Kauffmann> _daten =
                new Lazy<Kauffmann>(Lies, isThreadSafe: true);

            /// <summary>Einmal je Testlauf gelesene Reihen.</summary>
            public static Kauffmann Daten => _daten.Value;

            /// <summary>Lastgang der Mappe, Blattspalte C [kW].</summary>
            public double[] LastKw { get; }

            /// <summary>Sollwerte des gekappten Lastgangs, Blattspalte J [kW].</summary>
            public double[] SollPNeuKw { get; }

            /// <summary>Sollwerte des Ladezustands, Blattspalte M [kWh].</summary>
            public double[] SollSoCKwh { get; }

            private Kauffmann(double[] last, double[] pNeu, double[] soc)
            {
                LastKw = last;
                SollPNeuKw = pNeu;
                SollSoCKwh = soc;
            }

            /// <summary>
            /// Parametersatz des Referenzlaufs, Blattzellen L14 (P), L15 (Kapazitaet).
            /// SoC-Band, Start-SoC und Wirkungsgrade erzwingt der
            /// Kompatibilitaetsmodus; sie stehen hier nur zur Klarheit.
            /// </summary>
            public static SpeicherParameter Parameter()
            {
                return new SpeicherParameter
                {
                    CNomKwh = 300.0,                 // L15  Energie Speicher [kWh]
                    PKw = 200.0,                     // L14  Leistung Speicher [kW]
                    SoCMinKwh = 0.0,
                    SoCMaxKwh = 300.0,
                    RoundTripWirkungsgrad = 1.0,
                    StartSoCKwh = 0.0,
                    DtH = 0.25,
                    CCapEurProKwh = 0.0,
                    Kapitalzins = 0.0,
                    NutzungsdauerA = 10.0
                };
            }

            private static Kauffmann Lies()
            {
                string pfad = Path.Combine(AppContext.BaseDirectory, "TestData", Datei);
                if (!File.Exists(pfad))
                    throw new FileNotFoundException("Referenzdaten fehlen: " + pfad, pfad);

                string[] zeilen = File.ReadAllLines(pfad, new UTF8Encoding(false));
                int n = zeilen.Length - 1;
                if (n > 0 && zeilen[zeilen.Length - 1].Length == 0) n--;
                if (n <= 0) throw new InvalidOperationException(Datei + " enthaelt keine Datenzeilen.");

                double[] last = new double[n];
                double[] pNeu = new double[n];
                double[] soc = new double[n];
                for (int i = 0; i < n; i++)
                {
                    string[] f = zeilen[i + 1].Split(',');
                    if (f.Length < 4)
                        throw new InvalidOperationException(
                            Datei + ": Zeile " + (i + 2) + " hat " + f.Length + " Felder statt 4.");
                    last[i] = Zahl(f[1], i, "p_last_kw");
                    pNeu[i] = Zahl(f[2], i, "p_neu_soll_kw");
                    soc[i] = Zahl(f[3], i, "soc_soll_kwh");
                }
                return new Kauffmann(last, pNeu, soc);
            }

            private static double Zahl(string s, int zeile, string spalte)
            {
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw new InvalidOperationException(
                        Datei + ": '" + s + "' in Zeile " + zeile + ", Spalte " + spalte + " ist keine Zahl.");
                return d;
            }
        }
    }
}
