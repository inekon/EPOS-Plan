using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Strategie <see cref="Nachtnutzung"/> (AP6, Fachkonzept 6.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum diese Tests die einzige Absicherung sind.</b> Die Nachtnutzung ist
    /// eine <b>Neudefinition, kein Port</b>: Die V7-Mappe hinterlegte fuer den Button
    /// "Start Nachtnutzung" nur eine Altversion, die als Dauernutzungssimulation
    /// unbrauchbar war (kein Laden aus BHKW oder Netz, volle Last statt Residuallast
    /// im Entladezweig). Es gibt deshalb <b>keinen</b> Excel-Verifikationsanker wie
    /// bei der Dauernutzung - die Korrektheit haengt an Handrechnungen,
    /// Bilanzidentitaeten und Aequivalenzankern.
    /// </para>
    /// <para>Die Abdeckung ist entsprechend geschnitten:</para>
    /// <list type="number">
    ///   <item><description><b>Kerneigenschaft</b> - keine Entladung, solange PV
    ///     erzeugt; jahresweit geprueft, auf synthetischen Reihen und am
    ///     Referenzjahrgang.</description></item>
    ///   <item><description><b>Handrechnungen</b> - Tag/Nacht-Wechsel, voller
    ///     Speicher, leerer Speicher, naechtliche BHKW-Ladung.</description></item>
    ///   <item><description><b>Bilanzidentitaeten</b> je Intervall und im Jahr
    ///     (Muster <c>QuellenmatrixTests.Bilanz_Schliesst_In_Jedem_Intervall</c>).</description></item>
    ///   <item><description><b>Aequivalenzanker</b> - bei P_pv identisch 0 rechnet
    ///     die Nachtnutzung bitgleich zur Dauernutzung, ueber den vollen
    ///     Jahreslauf.</description></item>
    ///   <item><description><b>Abgrenzung</b> - der Excel-Kompatibilitaetsmodus wird
    ///     abgelehnt.</description></item>
    /// </list>
    /// <para>
    /// Mini-Parametersatz wie in <c>QuellenmatrixTests</c>: dt = 0,25 h,
    /// P*dt = 2,5 kWh, Band 0 .. 10 kWh, eta_RT = 0,81 (eta_ch = eta_dis = 0,9 ist
    /// als IEEE-754-Double exakt).
    /// </para>
    /// </remarks>
    public sealed class NachtnutzungTests
    {
        private const double Dt = 0.25;
        private const double PreisCtKwh = 20.0;
        private const double VPvCtKwh = 5.0;
        private const double VBhkwCtKwh = 12.0;

        private static SpeicherParameter MiniParameter() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 0.81,
            DtH = Dt,
            VerguetungCtKwh = VPvCtKwh,
            CCapEurProKwh = 500.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.025
        };

        private static SpeicherErgebnis Nacht(SpeicherEingang e, SpeicherParameter p)
            => new Nachtnutzung().Berechne(e, p);

        private static SpeicherErgebnis Dauer(SpeicherEingang e, SpeicherParameter p)
            => new Dauernutzung(SpeicherModus.Energetisch).Berechne(e, p);

        private static SpeicherEingang Eingang(double[] last, double[] pv, double[]? bhkw = null)
            => new SpeicherEingang(last, pv, SpeicherEingang.KonstanteReihe(PreisCtKwh, last.Length), bhkw)
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

        // ==================================================================
        // 1. Handrechnungen
        // ==================================================================

        /// <summary>
        /// Tag/Nacht-Wechsel, von Hand nachgerechnet (dt = 0,25 h, P*dt = 2,5 kWh,
        /// eta_ch = eta_dis = 0,9, Start-SoC = SoC_min = 0):
        /// <code>
        /// k=0  Last 0,  PV 8   -> TAG: E_pv = 2,0 = E_quelle
        ///      E_ac_ch = min(2,0 ; 2,5 ; (10-0)/0,9) = 2,0
        ///      F = -2,0*5/100 = -0,10 ; SoC = 0 + 2,0*0,9 = 1,80
        /// k=1  Last 20, PV 4   -> TAG mit Defizit: E_last = 5,0 ; E_pv = 1,0
        ///      E_restlast = 5,0 -> E_pv_frei = 0 -> E_quelle = 0
        ///      KEIN Entladepfad am Tag: E_ac_ch = E_ac_dis = 0
        ///      F = 0 ; SoC bleibt 1,80        (Dauernutzung entlaede hier 1,62 kWh)
        /// k=2  Last 20, PV 0   -> NACHT: E_defizit = 5,0
        ///      E_ac_dis = min(5,0 ; 2,5 ; 1,80*0,9 = 1,62) = 1,62
        ///      F = +1,62*20/100 = +0,324 ; SoC = 1,80 - 1,62/0,9 = 0
        /// k=3  Last 20, PV 0   -> NACHT, aber leer: E_ac_dis = min(...; 0*0,9) = 0
        ///      F = 0 ; SoC = 0
        /// </code>
        /// </summary>
        [Fact]
        public void TagNachtWechsel_Rechnet_Von_Hand_Nach()
        {
            SpeicherEingang e = Eingang(
                new double[] { 0.0, 20.0, 20.0, 20.0 },
                new double[] { 8.0, 4.0, 0.0, 0.0 });

            SpeicherErgebnis r = Nacht(e, MiniParameter());

            Assert.Equal(1.80, r.SoCKwh[0], 12);
            Assert.Equal(1.80, r.SoCKwh[1], 12);
            Assert.Equal(0.00, r.SoCKwh[2], 12);
            Assert.Equal(0.00, r.SoCKwh[3], 12);

            Assert.Equal(-0.100, r.GeldwertEur[0], 12);
            Assert.Equal(0.000, r.GeldwertEur[1], 12);
            Assert.Equal(0.324, r.GeldwertEur[2], 12);
            Assert.Equal(0.000, r.GeldwertEur[3], 12);

            Assert.Equal(2.00, r.LadeenergieKwh, 12);
            Assert.Equal(1.62, r.EntladeenergieKwh, 12);
            Assert.Equal(0.224, r.SummeGeldwertEur, 12);

            // Intervallreihen: Ladung nur am Tag, Entladung nur nachts.
            ReihenGleich(new double[] { 2.0, 0.0, 0.0, 0.0 }, r.LadungAcKwh);
            ReihenGleich(new double[] { 0.0, 0.0, 1.62, 0.0 }, r.EntladungAcKwh);
        }

        /// <summary>
        /// Derselbe Eingang unter der Dauernutzung - der Beleg, dass k=1 der
        /// eigentliche Unterschied ist: Sie entlaedt tagsueber gegen die Residuallast
        /// (min(4,0 ; 2,5 ; 1,80*0,9) = 1,62 kWh), die Nachtnutzung nicht.
        /// </summary>
        [Fact]
        public void Dauernutzung_Entlaedt_Am_Tag_Wo_Die_Nachtnutzung_Haelt()
        {
            SpeicherEingang e = Eingang(
                new double[] { 0.0, 20.0, 20.0, 20.0 },
                new double[] { 8.0, 4.0, 0.0, 0.0 });
            SpeicherParameter p = MiniParameter();

            SpeicherErgebnis d = Dauer(e, p);
            SpeicherErgebnis nn = Nacht(e, p);

            Assert.Equal(1.62, d.EntladungAcKwh[1], 12);      // Tagesintervall mit PV = 4 kW
            Assert.Equal(0.00, nn.EntladungAcKwh[1], 12);

            // Der zurueckgehaltene Ladezustand steht der Nacht zur Verfuegung: Die
            // Nachtnutzung entlaedt im Intervall 2 genau das, was die Dauernutzung
            // schon im Intervall 1 verbraucht hat.
            Assert.Equal(0.00, d.EntladungAcKwh[2], 12);
            Assert.Equal(1.62, nn.EntladungAcKwh[2], 12);
            Assert.Equal(d.EntladeenergieKwh, nn.EntladeenergieKwh, 12);
        }

        /// <summary>
        /// Voller Speicher am Tag: Der SoC-Kopf begrenzt die Ladung auf
        /// <c>(SoC_max - SoC)/eta_ch = (10 - 9,5)/0,9 = 0,5555... kWh</c>; der
        /// Ladezustand landet exakt auf SoC_max und bleibt dort. Bewertet wird nur die
        /// tatsaechlich geladene Energie.
        /// </summary>
        [Fact]
        public void Voller_Speicher_Begrenzt_Die_Tagesladung()
        {
            SpeicherEingang e = Eingang(
                new double[] { 0.0, 0.0 },
                new double[] { 40.0, 40.0 });        // E_pv = 10,0 kWh je Intervall

            SpeicherParameter p = MiniParameter() with { StartSoCKwh = 9.5 };
            SpeicherErgebnis r = Nacht(e, p);

            Assert.Equal(0.5 / 0.9, r.LadungAcKwh[0], 12);
            Assert.Equal(10.0, r.SoCKwh[0], 12);
            Assert.Equal(0.0, r.LadungAcKwh[1], 12);
            Assert.Equal(10.0, r.SoCKwh[1], 12);
            Assert.Equal(0.0, r.EntladeenergieKwh, 12);

            Assert.Equal(-(0.5 / 0.9) * VPvCtKwh / 100.0, r.GeldwertEur[0], 12);
            Assert.Equal(0.0, r.GeldwertEur[1], 12);
        }

        /// <summary>
        /// Leerer Speicher in der Nacht: Das Defizit bleibt vollstaendig am Netz,
        /// entladen wird nichts, und der Netzbezug "mit Speicher" ist gleich dem
        /// "ohne Speicher".
        /// </summary>
        [Fact]
        public void Leerer_Speicher_Entlaedt_Nichts()
        {
            SpeicherEingang e = Eingang(
                new double[] { 20.0, 20.0 },
                new double[] { 0.0, 0.0 });

            SpeicherErgebnis r = Nacht(e, MiniParameter());   // Start-SoC = SoC_min = 0

            Assert.Equal(0.0, r.EntladeenergieKwh, 12);
            Assert.Equal(0.0, r.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.SummeGeldwertEur, 12);
            Assert.Equal(10.0, r.Kennzahlen.NetzbezugOhneSpeicherKwh, 12);
            Assert.Equal(10.0, r.Kennzahlen.NetzbezugMitSpeicherKwh, 12);
        }

        /// <summary>
        /// Naechtliche BHKW-Ladung (Fachkonzept 6.1, zweiter Zweig): Last 4 kW,
        /// PV 0, BHKW 20 kW.
        /// <code>
        /// E_last = 1,0 ; E_bhkw = 5,0 ; E_restlast = 0 ; E_bhkw_frei = 4,0
        /// E_defizit = 0  -> E_ac_dis = 0
        /// E_quelle = 4,0 -> E_ac_ch = min(4,0 ; 2,5 ; (10-0)/0,9) = 2,5
        /// Merit-Order: PV-Anteil 0, BHKW-Anteil 2,5
        /// F = -2,5*12/100 = -0,30 ; SoC = 0 + 2,5*0,9 = 2,25
        /// </code>
        /// </summary>
        [Fact]
        public void Nachts_Laedt_Der_BHKW_Ueberschuss()
        {
            SpeicherEingang e = Eingang(
                new double[] { 4.0 },
                new double[] { 0.0 },
                new double[] { 20.0 });

            SpeicherErgebnis r = Nacht(e, MiniParameter());

            Assert.Equal(2.50, r.LadeenergieKwh, 12);
            Assert.Equal(0.00, r.EntladeenergieKwh, 12);
            Assert.Equal(0.00, r.Kennzahlen.LadeenergiePvKwh, 12);
            Assert.Equal(2.50, r.Kennzahlen.LadeenergieBhkwKwh, 12);
            Assert.Equal(-0.30, r.GeldwertEur[0], 12);
            Assert.Equal(2.25, r.SoCKwh[0], 12);
        }

        /// <summary>
        /// Der Quellenschalter wirkt auch nachts: Ist der BHKW-Ueberschuss als
        /// Ladequelle gesperrt, bleibt der Speicher leer und der Ueberschuss geht
        /// vollstaendig ins Netz.
        /// </summary>
        [Fact]
        public void Gesperrter_BHKW_Ueberschuss_Laedt_Nachts_Nicht()
        {
            SpeicherEingang e = Eingang(
                new double[] { 4.0 },
                new double[] { 0.0 },
                new double[] { 20.0 });

            SpeicherErgebnis r = Nacht(e, MiniParameter() with { BhkwUeberschussZulaessig = false });

            Assert.Equal(0.0, r.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.SoCKwh[0], 12);
            Assert.Equal(4.0, r.Kennzahlen.EinspeisungMitSpeicherKwh, 12);
        }

        /// <summary>
        /// Die Schwelle eps entscheidet Tag/Nacht: Ein Rundungsrest unterhalb
        /// <see cref="Nachtnutzung.PvSchwelleKw"/> - und erst recht ein negativer
        /// Restwert - zaehlt als Nacht, der Speicher darf also entladen.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(1e-12)]
        [InlineData(-1e-6)]
        public void Rundungsreste_In_Der_PV_Reihe_Zaehlen_Als_Nacht(double pvKw)
        {
            SpeicherEingang e = Eingang(new double[] { 20.0 }, new double[] { pvKw });
            SpeicherParameter p = MiniParameter() with { StartSoCKwh = 5.0 };

            SpeicherErgebnis r = Nacht(e, p);

            Assert.True(r.EntladungAcKwh[0] > 0.0, "kein Entladepfad trotz PV unterhalb eps");
            Assert.Equal(2.5, r.EntladungAcKwh[0], 12);   // Leistungsgrenze P*dt
        }

        /// <summary>
        /// Gegenprobe: Knapp <b>oberhalb</b> der Schwelle gilt Tag - und am Tag wird
        /// nicht entladen, auch wenn ein Defizit offen bleibt.
        /// </summary>
        [Fact]
        public void PV_Oberhalb_Der_Schwelle_Sperrt_Die_Entladung()
        {
            SpeicherEingang e = Eingang(new double[] { 20.0 }, new double[] { 1e-6 });
            SpeicherParameter p = MiniParameter() with { StartSoCKwh = 5.0 };

            SpeicherErgebnis r = Nacht(e, p);

            Assert.Equal(0.0, r.EntladungAcKwh[0], 12);
            Assert.Equal(0.0, r.LadungAcKwh[0], 12);
            Assert.Equal(5.0, r.SoCKwh[0], 12);
        }

        // ==================================================================
        // 2. Kerneigenschaft: keine Entladung, solange PV erzeugt
        // ==================================================================

        /// <summary>
        /// <b>Die definierende Eigenschaft der Strategie</b>, jahresweit geprueft
        /// (35.040 Viertelstunden, synthetische Reihen mit Tagesgang): In
        /// <b>keinem</b> Intervall mit <c>P_pv &gt; eps</c> wird entladen. Zur
        /// Kontrolle wird derselbe Fall zusaetzlich mit der Dauernutzung gerechnet -
        /// sie entlaedt dort sehr wohl, der Test ist also nicht trivial erfuellt.
        /// </summary>
        [Theory]
        [InlineData(true, true, 0.90)]     // Gruen: PV + BHKW
        [InlineData(true, false, 0.90)]    // Gruen: nur PV
        [InlineData(false, true, 0.81)]    // Gruen: nur BHKW
        [InlineData(true, true, 1.00)]     // verlustfrei
        public void Keine_Entladung_Solange_PV_Erzeugt(bool pvZulaessig, bool bhkwZulaessig, double etaRt)
        {
            const int n = 35040;
            (double[] last, double[] pv, double[] bhkw) = JahresReihen(n);

            SpeicherEingang e = Eingang(last, pv, bhkw);
            SpeicherParameter p = JahresParameter() with
            {
                RoundTripWirkungsgrad = etaRt,
                PvZulaessig = pvZulaessig,
                BhkwUeberschussZulaessig = bhkwZulaessig
            };

            SpeicherErgebnis r = Nacht(e, p);

            int tagesintervalle = 0;
            for (int k = 0; k < n; k++)
            {
                if (!(pv[k] > Nachtnutzung.PvSchwelleKw)) continue;
                tagesintervalle++;

                Assert.True(r.EntladungAcKwh[k] == 0.0,
                    "Entladung bei PV > eps in Intervall " + k +
                    " (P_pv = " + pv[k] + " kW, E_dis = " + r.EntladungAcKwh[k] + " kWh)");
                Assert.True(r.SoCKwh[k] >= (k == 0 ? p.StartSoCEffektivKwh : r.SoCKwh[k - 1]) - 1e-12,
                    "Ladezustand faellt in einem Tagesintervall: " + k);
            }

            // Der Testfall muss ueberhaupt Tagesintervalle enthalten.
            Assert.True(tagesintervalle > n / 4, "zu wenige Tagesintervalle: " + tagesintervalle);

            // Gegenprobe: Die Dauernutzung entlaedt in genau diesen Intervallen.
            SpeicherErgebnis d = Dauer(e, p);
            int tagesentladungen = 0;
            for (int k = 0; k < n; k++)
                if (pv[k] > Nachtnutzung.PvSchwelleKw && d.EntladungAcKwh[k] > 0.0) tagesentladungen++;
            Assert.True(tagesentladungen > 0, "Gegenprobe leer - der Testfall traegt nicht");
        }

        /// <summary>
        /// Dieselbe Eigenschaft am <b>Referenzjahrgang</b> der V7-Mappe (35.137
        /// Intervalle echter Last- und PV-Werte) - realistische Reihen statt
        /// synthetischer.
        /// </summary>
        [Fact]
        public void Keine_Entladung_Solange_PV_Erzeugt_Am_Referenzjahrgang()
        {
            SpeicherEingang e = Referenzdaten.V7Eingang();
            SpeicherParameter p = Referenzdaten.V7Parameter() with { RoundTripWirkungsgrad = 0.90 };

            SpeicherErgebnis r = Nacht(e, p);

            int tagesintervalle = 0;
            for (int k = 0; k < e.Anzahl; k++)
            {
                if (!(e.PvKw[k] > Nachtnutzung.PvSchwelleKw)) continue;
                tagesintervalle++;
                Assert.True(r.EntladungAcKwh[k] == 0.0, "Entladung bei PV > eps in Intervall " + k);
            }

            Assert.True(tagesintervalle > 1000, "zu wenige Tagesintervalle: " + tagesintervalle);
            Assert.True(r.EntladeenergieKwh > 0.0, "gar keine Entladung - der Fall traegt nicht");
        }

        // ==================================================================
        // 3. Bilanzidentitaeten (Muster QuellenmatrixTests)
        // ==================================================================

        /// <summary>
        /// In <b>jedem</b> Intervall gilt <c>Last = Direkt + Entladung + Netzbezug</c>
        /// und <c>Erzeugung = Direkt + Ladung + Einspeisung</c>; Leistungs- und
        /// SoC-Grenzen werden eingehalten, Laden und Entladen schliessen einander aus,
        /// und der Speicher erzeugt keine Energie aus dem Nichts.
        /// </summary>
        /// <remarks>
        /// Lade- und Entladeenergie werden <b>aus dem veroeffentlichten SoC-Verlauf
        /// zurueckgerechnet</b> (Delta SoC = E_ch*eta_ch - E_dis/eta_dis) - geprueft
        /// wird damit das Ergebnis, nicht die Innereien der Schleife.
        /// </remarks>
        [Theory]
        [InlineData(true, true, 0.90)]
        [InlineData(true, false, 0.90)]
        [InlineData(false, true, 0.81)]
        [InlineData(true, true, 1.00)]
        public void Bilanz_Schliesst_In_Jedem_Intervall(bool pvZulaessig, bool bhkwZulaessig, double etaRt)
        {
            const int n = 2688;   // vier Wochen im Viertelstundenraster
            (double[] last, double[] pv, double[] bhkw) = JahresReihen(n);

            SpeicherEingang e = Eingang(last, pv, bhkw);
            SpeicherParameter p = JahresParameter() with
            {
                RoundTripWirkungsgrad = etaRt,
                PvZulaessig = pvZulaessig,
                BhkwUeberschussZulaessig = bhkwZulaessig
            };

            SpeicherErgebnis r = Nacht(e, p);

            Assert.True(r.LadeenergieKwh > 100.0, "zu wenig Ladevorgaenge im Testfall");
            Assert.True(r.EntladeenergieKwh > 100.0, "zu wenig Entladevorgaenge im Testfall");
            Assert.Equal(pvZulaessig, r.Kennzahlen.LadeenergiePvKwh > 0.0);
            Assert.Equal(bhkwZulaessig, r.Kennzahlen.LadeenergieBhkwKwh > 0.0);

            const double tol = 1e-9;
            double etaCh = p.EtaCh;
            double etaDis = p.EtaDis;
            double leistungsgrenze = p.PKw * p.DtH;

            double summeLade = 0.0, summeEntlade = 0.0;
            double summeDirekt = 0.0, summeNetzbezug = 0.0, summeEinspeisung = 0.0;
            double summeLast = 0.0, summeErzeugung = 0.0;
            double prev = p.StartSoCEffektivKwh;

            for (int k = 0; k < n; k++)
            {
                // Unabhaengige Vorverarbeitung nach Fachkonzept 6.
                double eLast = last[k] * p.DtH;
                double ePv = pv[k] * p.DtH;
                double eBhkw = bhkw[k] * p.DtH;
                double eRestlast = Math.Max(0.0, eLast - eBhkw);
                double ePvFrei = Math.Max(0.0, ePv - eRestlast);
                double eBhkwFrei = Math.Max(0.0, eBhkw - eLast);
                double eDefizit = Math.Max(0.0, eLast - ePv - eBhkw);
                double eDirekt = Math.Min(eLast, ePv + eBhkw);
                double eUeberschuss = ePvFrei + eBhkwFrei;

                // Lade-/Entladeenergie aus dem veroeffentlichten SoC-Verlauf.
                double soc = r.SoCKwh[k];
                double delta = soc - prev;
                double lade = delta > 0.0 ? delta / etaCh : 0.0;
                double entlade = delta < 0.0 ? -delta * etaDis : 0.0;

                string wo = " (Intervall " + k + ")";

                Assert.True(soc >= p.SoCMinKwh - tol && soc <= p.SoCMaxKwh + tol, "SoC ausserhalb des Bandes" + wo);
                Assert.True(lade <= leistungsgrenze + tol, "Ladeleistung ueberschritten" + wo);
                Assert.True(entlade <= leistungsgrenze + tol, "Entladeleistung ueberschritten" + wo);
                Assert.True(lade <= tol || entlade <= tol, "Laden und Entladen im selben Intervall" + wo);

                // Kerneigenschaft, hier aus dem SoC-Verlauf statt aus der Entladereihe.
                if (pv[k] > Nachtnutzung.PvSchwelleKw)
                    Assert.True(entlade <= tol, "Entladung am Tag" + wo);

                double zulaessig = (pvZulaessig ? ePvFrei : 0.0) + (bhkwZulaessig ? eBhkwFrei : 0.0);
                Assert.True(lade <= zulaessig + tol, "mehr geladen als zugelassen" + wo);
                Assert.True(entlade <= eDefizit + tol, "mehr entladen als Defizit" + wo);

                double netzbezug = eDefizit - entlade;
                double einspeisung = eUeberschuss - lade;
                Assert.True(netzbezug >= -tol, "negativer Netzbezug" + wo);
                Assert.True(einspeisung >= -tol, "negative Einspeisung" + wo);

                // Bilanz 1: Last = Direkt + Entladung + Netzbezug
                Assert.True(Math.Abs(eLast - (eDirekt + entlade + netzbezug)) <= tol, "Lastbilanz" + wo);
                // Bilanz 2: Erzeugung = Direkt + Ladung + Einspeisung
                Assert.True(Math.Abs((ePv + eBhkw) - (eDirekt + lade + einspeisung)) <= tol, "Erzeugungsbilanz" + wo);

                // Die veroeffentlichten Intervallreihen tragen dieselben Werte.
                Assert.True(Math.Abs(r.LadungAcKwh[k] - lade) <= tol, "Ladereihe" + wo);
                Assert.True(Math.Abs(r.EntladungAcKwh[k] - entlade) <= tol, "Entladereihe" + wo);

                summeLade += lade;
                summeEntlade += entlade;
                summeDirekt += eDirekt;
                summeNetzbezug += netzbezug;
                summeEinspeisung += einspeisung;
                summeLast += eLast;
                summeErzeugung += ePv + eBhkw;
                prev = soc;
            }

            // Der Kennzahlenblock trifft die unabhaengig gebildeten Jahressummen.
            const double jahrTol = 1e-6;
            Assert.Equal(summeLade, r.LadeenergieKwh, 6);
            Assert.Equal(summeEntlade, r.EntladeenergieKwh, 6);
            Assert.Equal(summeLade, r.Kennzahlen.LadeenergieKwh, 6);
            Assert.Equal(summeLast, r.Kennzahlen.LastKwh, 6);
            Assert.Equal(summeErzeugung, r.Kennzahlen.ErzeugungKwh, 6);
            Assert.Equal(summeDirekt, r.Kennzahlen.DirektverbrauchKwh, 6);
            Assert.Equal(summeNetzbezug, r.Kennzahlen.NetzbezugMitSpeicherKwh, 6);
            Assert.Equal(summeEinspeisung, r.Kennzahlen.EinspeisungMitSpeicherKwh, 6);

            Assert.True(Math.Abs(r.Kennzahlen.LastKwh -
                (r.Kennzahlen.DirektverbrauchKwh + r.EntladeenergieKwh + r.Kennzahlen.NetzbezugMitSpeicherKwh)) <= jahrTol,
                "Jahres-Lastbilanz");
            Assert.True(Math.Abs(r.Kennzahlen.ErzeugungKwh -
                (r.Kennzahlen.DirektverbrauchKwh + r.LadeenergieKwh + r.Kennzahlen.EinspeisungMitSpeicherKwh)) <= jahrTol,
                "Jahres-Erzeugungsbilanz");

            // Der Speicher verbessert beide Seiten - oder laesst sie unveraendert.
            Assert.True(r.Kennzahlen.NetzbezugMitSpeicherKwh <= r.Kennzahlen.NetzbezugOhneSpeicherKwh + jahrTol);
            Assert.True(r.Kennzahlen.EinspeisungMitSpeicherKwh <= r.Kennzahlen.EinspeisungOhneSpeicherKwh + jahrTol);
            Assert.True(r.Kennzahlen.AutarkiegradMitSpeicher >= r.Kennzahlen.AutarkiegradOhneSpeicher - 1e-12);
            Assert.True(r.Kennzahlen.EigenverbrauchsquoteMitSpeicher >=
                        r.Kennzahlen.EigenverbrauchsquoteOhneSpeicher - 1e-12);

            // Verlustausweis: Ladeenergie - Entladeenergie - Delta SoC
            Assert.Equal(r.LadeenergieKwh - r.EntladeenergieKwh - (r.SoCKwh[n - 1] - p.StartSoCEffektivKwh),
                         r.Kennzahlen.SpeicherverlusteKwh, 6);
            Assert.True(r.Kennzahlen.SpeicherverlusteKwh >= -1e-6);

            // Zyklen- und Verschleissausweis (Fachkonzept 5.4).
            Assert.Equal(r.EntladeenergieKwh / etaDis, r.Kennzahlen.EntladeenergieDcKwh, 6);
            Assert.Equal(r.Kennzahlen.EntladeenergieDcKwh / p.CNutzKwh, r.Kennzahlen.AequivalenteVollzyklen, 9);
            Assert.Equal(r.Kennzahlen.AequivalenteVollzyklen * p.CNomKwh * p.CVerEurProKwhZyklus,
                         r.Kennzahlen.VerschleisskostenEurProA, 9);
        }

        // ==================================================================
        // 4. Aequivalenzanker: P_pv identisch 0
        // ==================================================================

        /// <summary>
        /// <b>Ohne jede PV-Erzeugung sind Nachtnutzung und Dauernutzung dieselbe
        /// Rechnung</b> - und zwar bitgleich, ueber den vollen Jahrgang des
        /// Referenzfalls (35.137 Intervalle).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Der Beweis steckt in der Fallunterscheidung: Bei <c>P_pv = 0</c> greift
        /// ausschliesslich der Nachtzweig. Dort wird zuerst <c>E_defizit</c> mit
        /// derselben Begrenzungsfolge entladen wie im else-Zweig der Dauernutzung und
        /// anschliessend bei <c>E_quelle &gt; 0</c> mit derselben Folge geladen wie in
        /// deren if-Zweig. Weil <c>E_quelle</c> und <c>E_defizit</c> disjunkt sind,
        /// ist immer genau einer der beiden Zweige wirksam, der andere liefert exakt
        /// 0,0 - und <c>x - 0,0/eta</c> ist derselbe Ausdruck wie in der Vorlage.
        /// </para>
        /// <para>
        /// Der Test faehrt zusaetzlich eine BHKW-Reihe, damit auch der naechtliche
        /// Ladepfad in den Vergleich faellt, und variiert eta_RT.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData(0.81, false)]
        [InlineData(0.90, false)]
        [InlineData(1.00, false)]
        [InlineData(0.81, true)]
        [InlineData(0.90, true)]
        [InlineData(1.00, true)]
        public void Ohne_PV_Bitgleich_Zur_Dauernutzung(double etaRt, bool mitBhkw)
        {
            Referenzdaten.Zeitreihen reihen = Referenzdaten.Reihen;
            int n = reihen.Anzahl;

            double[] ohnePv = new double[n];                       // P_pv identisch 0
            double[]? bhkw = mitBhkw ? BhkwReihe(reihen.LastKw) : null;

            SpeicherEingang e = new SpeicherEingang(reihen.LastKw, ohnePv, reihen.PreisCtKwh, bhkw)
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            // Start in der Bandmitte: Ohne PV und ohne BHKW gaebe es sonst nie eine
            // Ladung, der Speicher bliebe auf SoC_min stehen und der Vergleich waere
            // trivial (beide Reihen durchgehend 0).
            SpeicherParameter v7 = Referenzdaten.V7Parameter();
            SpeicherParameter p = v7 with
            {
                RoundTripWirkungsgrad = etaRt,
                StartSoCKwh = 0.5 * (v7.SoCMinKwh + v7.SoCMaxKwh)
            };

            SpeicherErgebnis nn = Nacht(e, p);
            SpeicherErgebnis d = Dauer(e, p);

            Assert.Equal(d.Anzahl, nn.Anzahl);
            for (int k = 0; k < n; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(nn.SoCKwh[k], d.SoCKwh[k]),
                    "SoC weicht ab in Intervall " + k + ": Nacht = " + V7ReferenzTests.Bits(nn.SoCKwh[k]) +
                    ", Dauer = " + V7ReferenzTests.Bits(d.SoCKwh[k]));
                Assert.True(V7ReferenzTests.Bitgleich(nn.GeldwertEur[k], d.GeldwertEur[k]),
                    "Geldwert weicht ab in Intervall " + k);
                Assert.True(V7ReferenzTests.Bitgleich(nn.LadungAcKwh[k], d.LadungAcKwh[k]),
                    "Ladung weicht ab in Intervall " + k);
                Assert.True(V7ReferenzTests.Bitgleich(nn.EntladungAcKwh[k], d.EntladungAcKwh[k]),
                    "Entladung weicht ab in Intervall " + k);
            }

            Assert.True(V7ReferenzTests.Bitgleich(nn.SummeGeldwertEur, d.SummeGeldwertEur));
            Assert.True(V7ReferenzTests.Bitgleich(nn.LadeenergieKwh, d.LadeenergieKwh));
            Assert.True(V7ReferenzTests.Bitgleich(nn.EntladeenergieKwh, d.EntladeenergieKwh));
            Assert.True(V7ReferenzTests.Bitgleich(nn.Kennzahlen.LadeenergieBhkwKwh, d.Kennzahlen.LadeenergieBhkwKwh));
            Assert.True(V7ReferenzTests.Bitgleich(nn.Kennzahlen.EntladeenergieDcKwh, d.Kennzahlen.EntladeenergieDcKwh));
            Assert.True(V7ReferenzTests.Bitgleich(nn.Wirtschaftlichkeit.JahresueberschussEur,
                                                  d.Wirtschaftlichkeit.JahresueberschussEur));

            // Der Fall muss den Speicher wirklich bewegen.
            Assert.True(nn.EntladeenergieKwh > 0.0, "keine Entladung - der Aequivalenzanker traegt nicht");
            Assert.Equal(mitBhkw, nn.LadeenergieKwh > 0.0);
        }

        /// <summary>
        /// Gegenprobe zum Aequivalenzanker: <b>Mit</b> PV weichen die beiden
        /// Strategien am Referenzjahrgang nachweislich voneinander ab - die
        /// Bitgleichheit oben ist also eine Eigenschaft des PV-freien Falls und kein
        /// Artefakt.
        /// </summary>
        [Fact]
        public void Mit_PV_Weichen_Die_Strategien_Voneinander_Ab()
        {
            SpeicherEingang e = Referenzdaten.V7Eingang();
            SpeicherParameter p = Referenzdaten.V7Parameter() with { RoundTripWirkungsgrad = 0.90 };

            SpeicherErgebnis nn = Nacht(e, p);
            SpeicherErgebnis d = Dauer(e, p);

            Assert.False(V7ReferenzTests.Bitgleich(nn.SummeGeldwertEur, d.SummeGeldwertEur));
            Assert.NotEqual(d.EntladeenergieKwh, nn.EntladeenergieKwh, 3);
        }

        // ==================================================================
        // 5. Abgrenzung, Vertrag, Zustandsfreiheit
        // ==================================================================

        /// <summary>
        /// Der Excel-Kompatibilitaetsmodus wird abgelehnt: Fuer die Nachtnutzung
        /// existiert keine Excel-Referenz (Fachkonzept 6.1). Die Meldung traegt den
        /// sprachneutralen Schluessel, die Oberflaeche bietet die Kombination gar
        /// nicht erst an.
        /// </summary>
        [Fact]
        public void Excel_Kompatibilitaetsmodus_Wird_Abgelehnt()
        {
            SpeicherEingang e = Eingang(new double[] { 20.0 }, new double[] { 0.0 });
            var strategie = new Nachtnutzung(SpeicherModus.ExcelKompatibilitaet);

            // Konstruierbar und benennbar bleibt sie - die Ausnahme kommt erst beim Rechnen.
            Assert.Equal(SpeicherModus.ExcelKompatibilitaet, strategie.Modus);
            Assert.Equal("Nachtnutzung", strategie.Name);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => strategie.Berechne(e, MiniParameter()));
            Assert.Equal(Nachtnutzung.SchluesselOhneExcelReferenz, ex.Message);
            Assert.Equal("NACHT_OHNE_EXCEL_REFERENZ", Nachtnutzung.SchluesselOhneExcelReferenz);
        }

        /// <summary>Vorbelegung des Konstruktors ist der energetische Produktivmodus.</summary>
        [Fact]
        public void Vorbelegung_Ist_Der_Energetische_Modus()
        {
            var strategie = new Nachtnutzung();
            Assert.Equal(SpeicherModus.Energetisch, strategie.Modus);

            SpeicherErgebnis r = strategie.Berechne(
                Eingang(new double[] { 20.0 }, new double[] { 0.0 }), MiniParameter());
            Assert.Equal(SpeicherModus.Energetisch, r.Modus);
        }

        /// <summary>Fehlende Pflichtangaben werden abgewiesen, bevor gerechnet wird.</summary>
        [Fact]
        public void Fehlende_Pflichtangaben_Werden_Abgewiesen()
        {
            var strategie = new Nachtnutzung();
            Assert.Throws<ArgumentNullException>(() => strategie.Berechne(null!, MiniParameter()));
            Assert.Throws<ArgumentNullException>(
                () => strategie.Berechne(Eingang(new double[] { 1.0 }, new double[] { 0.0 }), null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => strategie.Berechne(Eingang(new double[] { 1.0 }, new double[] { 0.0 }),
                                         MiniParameter() with { DtH = 0.0 }));
        }

        /// <summary>
        /// Die Strategie ist zustandsfrei: Zwei Laeufe derselben Instanz auf demselben
        /// Eingang liefern bitgleiche Ergebnisse (Voraussetzung fuer
        /// <c>Parallel.For</c>, Fachkonzept 8.1).
        /// </summary>
        [Fact]
        public void Wiederholter_Lauf_Liefert_Bitgleiches_Ergebnis()
        {
            (double[] last, double[] pv, double[] bhkw) = JahresReihen(2688);
            SpeicherEingang e = Eingang(last, pv, bhkw);
            SpeicherParameter p = JahresParameter();

            var strategie = new Nachtnutzung();
            SpeicherErgebnis a = strategie.Berechne(e, p);
            SpeicherErgebnis b = strategie.Berechne(e, p);

            Assert.True(V7ReferenzTests.Bitgleich(a.SummeGeldwertEur, b.SummeGeldwertEur));
            for (int k = 0; k < a.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(a.SoCKwh[k], b.SoCKwh[k]));
                Assert.True(V7ReferenzTests.Bitgleich(a.GeldwertEur[k], b.GeldwertEur[k]));
            }
        }

        // ==================================================================
        // Testdaten
        // ==================================================================

        /// <summary>
        /// Vergleicht zwei Intervallreihen elementweise auf 12 Nachkommastellen -
        /// <c>Assert.Equal</c> kennt fuer Sammlungen keine Stellenangabe.
        /// </summary>
        private static void ReihenGleich(double[] soll, double[] ist)
        {
            Assert.Equal(soll.Length, ist.Length);
            for (int k = 0; k < soll.Length; k++) Assert.Equal(soll[k], ist[k], 12);
        }

        /// <summary>
        /// Parametersatz der Jahreslaeufe: 200 kWh Nennkapazitaet, Band 20 .. 180 kWh,
        /// 60 kW - dieselben Groessen wie im Bilanztest der Dauernutzung, damit die
        /// beiden Testreihen vergleichbar bleiben.
        /// </summary>
        private static SpeicherParameter JahresParameter() => MiniParameter() with
        {
            CNomKwh = 200.0,
            PKw = 60.0,
            SoCMinKwh = 20.0,
            SoCMaxKwh = 180.0
        };

        /// <summary>
        /// Synthetische Tagesgaenge fuer Last, PV und BHKW (Muster
        /// <c>QuellenmatrixTests.SynthetischeReihen</c>, eigener Startwert). Die
        /// PV-Reihe traegt zwischen 19 und 7 Uhr <b>exakte Nullen</b> - genau der
        /// Nachtfall der Strategie.
        /// </summary>
        private static (double[] last, double[] pv, double[] bhkw) JahresReihen(int n)
        {
            var zufall = new Random(20260817);
            double[] last = new double[n];
            double[] pv = new double[n];
            double[] bhkw = new double[n];

            for (int k = 0; k < n; k++)
            {
                double stunde = (k % 96) / 4.0;

                last[k] = 20.0 + 25.0 * Math.Sin(Math.PI * stunde / 24.0) + zufall.NextDouble() * 10.0;

                pv[k] = (stunde >= 7.0 && stunde < 19.0)
                    ? 90.0 * Math.Sin(Math.PI * (stunde - 7.0) / 12.0) * (0.5 + zufall.NextDouble())
                    : 0.0;

                bhkw[k] = ((stunde >= 5.0 && stunde < 9.0) || (stunde >= 17.0 && stunde < 22.0))
                    ? 30.0 + zufall.NextDouble() * 20.0
                    : 0.0;
            }
            return (last, pv, bhkw);
        }

        /// <summary>
        /// BHKW-Reihe zum Aequivalenzanker: deterministischer Zweistufenbetrieb, der
        /// die Last in einem Teil der Intervalle uebersteigt und damit den
        /// naechtlichen Ladepfad ausloest.
        /// </summary>
        private static double[] BhkwReihe(double[] lastKw)
        {
            double[] reihe = new double[lastKw.Length];
            for (int k = 0; k < reihe.Length; k++)
            {
                // 6 h Volllast, 6 h aus - Vielfache von 24 Viertelstunden.
                reihe[k] = (k / 24) % 2 == 0 ? 0.0 : 1.6 * lastKw[k] + 5.0;
            }
            return reihe;
        }
    }
}
