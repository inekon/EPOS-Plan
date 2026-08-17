using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests des energetischen Modus mit Quellen-Matrix und Merit-Order (AP2,
    /// Fachkonzept 2.1, 2.2, 5.4, 6, 6.2): Ladeaufteilung PV vor BHKW,
    /// BHKW-Ueberschuss, Bilanzschluss je Intervall, Grenzfaelle und die
    /// Rueckwaertskompatibilitaet zur Fassung aus AP1.
    /// </summary>
    public sealed class QuellenmatrixTests
    {
        private const double Dt = 0.25;
        private const double PreisCtKwh = 20.0;
        private const double VPvCtKwh = 5.0;
        private const double VBhkwCtKwh = 12.0;

        /// <summary>
        /// Mini-Parametersatz: P*dt = 2,5 kWh, Band 0..10 kWh, eta_ch = eta_dis = 0,9
        /// (eta_RT = 0,81 ist als IEEE-754-Double exakt).
        /// </summary>
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

        private static SpeicherErgebnis Rechne(SpeicherEingang e, SpeicherParameter p)
            => new Dauernutzung(SpeicherModus.Energetisch).Berechne(e, p);

        // ==================================================================
        // 1. Merit-Order beim Laden: PV vor BHKW
        // ==================================================================

        /// <summary>
        /// Von Hand nachgerechnet (dt = 0,25 h, P*dt = 2,5 kWh, eta = 0,9):
        /// <code>
        /// k=0  Last 0, PV 4 kW, BHKW 8 kW
        ///      E_pv = 1,0 ; E_bhkw = 2,0 ; E_restlast = 0
        ///      E_pv_frei = 1,0 ; E_bhkw_frei = 2,0 ; E_quelle = 3,0
        ///      E_ac_ch = min(3,0 ; 2,5 ; 10/0,9) = 2,5
        ///      Merit-Order: PV 1,0 (voll), BHKW 1,5
        ///      F = -1,0*5/100 - 1,5*12/100 = -0,05 - 0,18 = -0,23
        ///      SoC = 0 + 2,5*0,9 = 2,25 ; Einspeisung = 3,0 - 2,5 = 0,5
        /// k=1  Last 20 kW, keine Erzeugung
        ///      E_defizit = 5,0 ; E_ac_dis = min(5,0 ; 2,5 ; 2,25*0,9 = 2,025) = 2,025
        ///      F = +2,025*20/100 = +0,405 ; SoC = 2,25 - 2,025/0,9 = 0
        /// k=2  wie k=1, aber leerer Speicher: E_ac_dis = 0, F = 0
        /// </code>
        /// </summary>
        [Fact]
        public void MeritOrder_Laedt_PV_Vor_BHKW_Und_Bewertet_Getrennt()
        {
            SpeicherEingang e = MiniEingang();
            SpeicherErgebnis r = Rechne(e, MiniParameter());

            Assert.Equal(2.25, r.SoCKwh[0], 12);
            Assert.Equal(0.0, r.SoCKwh[1], 12);
            Assert.Equal(0.0, r.SoCKwh[2], 12);

            Assert.Equal(-0.23, r.GeldwertEur[0], 12);
            Assert.Equal(0.405, r.GeldwertEur[1], 12);
            Assert.Equal(0.0, r.GeldwertEur[2], 12);

            Assert.Equal(2.5, r.LadeenergieKwh, 12);
            Assert.Equal(2.025, r.EntladeenergieKwh, 12);

            // Quellenaufteilung: PV zuerst, der Rest aus dem BHKW.
            Assert.Equal(1.0, r.Kennzahlen.LadeenergiePvKwh, 12);
            Assert.Equal(1.5, r.Kennzahlen.LadeenergieBhkwKwh, 12);
            Assert.Equal(r.LadeenergieKwh, r.Kennzahlen.LadeenergieKwh, 12);
        }

        /// <summary>
        /// Reicht der PV-Ueberschuss allein bis zur Ladegrenze, bleibt fuer das BHKW
        /// nichts uebrig - die Bewertung erfolgt vollstaendig mit v_pv.
        /// </summary>
        [Fact]
        public void MeritOrder_PV_Deckt_Die_Ladegrenze_Allein()
        {
            // PV 16 kW -> E_pv_frei = 4,0 kWh > P*dt = 2,5 kWh; BHKW 8 kW zusaetzlich.
            var eingang = new SpeicherEingang(
                new double[] { 0.0 },
                new double[] { 16.0 },
                new double[] { PreisCtKwh },
                new double[] { 8.0 })
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherErgebnis r = Rechne(eingang, MiniParameter());

            Assert.Equal(2.5, r.LadeenergieKwh, 12);
            Assert.Equal(2.5, r.Kennzahlen.LadeenergiePvKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.LadeenergieBhkwKwh, 12);
            Assert.Equal(-2.5 * VPvCtKwh / 100.0, r.GeldwertEur[0], 12);
        }

        /// <summary>
        /// BHKW-Ueberschuss ohne PV: geladen wird ausschliesslich aus dem BHKW und
        /// mit v_bhkw bewertet (Fachkonzept 3.3 - die Reihe existiert im Bestand
        /// nicht und wird hier erstmals gebildet).
        /// </summary>
        [Fact]
        public void BHKW_Ueberschuss_Laedt_Und_Wird_Mit_V_Bhkw_Bewertet()
        {
            var eingang = new SpeicherEingang(
                new double[] { 4.0 },          // Last 4 kW -> E_last = 1,0
                new double[] { 0.0 },          // keine PV
                new double[] { PreisCtKwh },
                new double[] { 12.0 })         // BHKW 12 kW -> E_bhkw = 3,0, frei 2,0
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherErgebnis r = Rechne(eingang, MiniParameter());

            Assert.Equal(2.0, r.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.LadeenergiePvKwh, 12);
            Assert.Equal(2.0, r.Kennzahlen.LadeenergieBhkwKwh, 12);
            Assert.Equal(-2.0 * VBhkwCtKwh / 100.0, r.GeldwertEur[0], 12);
            Assert.Equal(2.0 * 0.9, r.SoCKwh[0], 12);
        }

        /// <summary>
        /// Gruen-Untervariante "nur PV": der BHKW-Ueberschuss deckt weiter vorrangig
        /// die Last, darf aber nicht laden - er geht vollstaendig ins Netz.
        /// </summary>
        [Fact]
        public void Gesperrte_BHKW_Quelle_Laedt_Nicht_Speist_Aber_Ein()
        {
            var eingang = new SpeicherEingang(
                new double[] { 4.0 }, new double[] { 0.0 }, new double[] { PreisCtKwh },
                new double[] { 12.0 });

            SpeicherParameter p = MiniParameter() with { BhkwUeberschussZulaessig = false };
            SpeicherErgebnis r = Rechne(eingang, p);

            Assert.Equal(0.0, r.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.EntladeenergieKwh, 12);
            Assert.Equal(0.0, r.SoCKwh[0], 12);
            Assert.Equal(0.0, r.GeldwertEur[0], 12);

            // Die Bilanz bleibt vollstaendig: 2 kWh Ueberschuss gehen ins Netz.
            Assert.Equal(2.0, r.Kennzahlen.EinspeisungOhneSpeicherKwh, 12);
            Assert.Equal(2.0, r.Kennzahlen.EinspeisungMitSpeicherKwh, 12);
            Assert.Equal(1.0, r.Kennzahlen.DirektverbrauchKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.NetzbezugMitSpeicherKwh, 12);
        }

        /// <summary>Gesperrte PV-Quelle: geladen wird nur noch aus dem BHKW.</summary>
        [Fact]
        public void Gesperrte_PV_Quelle_Laedt_Nur_Aus_BHKW()
        {
            SpeicherErgebnis r = Rechne(MiniEingang(), MiniParameter() with { PvZulaessig = false });

            // E_quelle = E_bhkw_frei = 2,0 kWh (statt 3,0), Ladegrenze 2,5 greift nicht.
            Assert.Equal(2.0, r.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.LadeenergiePvKwh, 12);
            Assert.Equal(2.0, r.Kennzahlen.LadeenergieBhkwKwh, 12);
        }

        // ==================================================================
        // 2. Verguetungsreihen
        // ==================================================================

        /// <summary>
        /// Ohne Verguetungsreihen gilt der Standardwert aus den Parametern fuer beide
        /// Quellen - das ist die Rueckfallebene bis zum Preismodell (AP4).
        /// </summary>
        [Fact]
        public void Ohne_Verguetungsreihen_Gilt_Der_Standardwert()
        {
            var ohneReihen = new SpeicherEingang(
                new double[] { 0.0 }, new double[] { 4.0 }, new double[] { PreisCtKwh },
                new double[] { 8.0 });

            SpeicherParameter p = MiniParameter() with { VerguetungCtKwh = 7.0 };
            SpeicherErgebnis r = Rechne(ohneReihen, p);

            // 1,0 kWh PV + 1,5 kWh BHKW, beide zu 7 ct/kWh.
            Assert.Equal(-2.5 * 7.0 / 100.0, r.GeldwertEur[0], 12);
        }

        /// <summary>Zeitvariable Verguetungsreihen werden intervallweise ausgewertet.</summary>
        [Fact]
        public void Zeitvariable_Verguetungen_Wirken_Intervallweise()
        {
            var eingang = new SpeicherEingang(
                new double[] { 0.0, 0.0 },
                new double[] { 16.0, 16.0 },
                new double[] { PreisCtKwh, PreisCtKwh },
                null,
                new double[] { 5.0, 9.0 },
                new double[] { 0.0, 0.0 });

            SpeicherErgebnis r = Rechne(eingang, MiniParameter());

            Assert.Equal(-2.5 * 5.0 / 100.0, r.GeldwertEur[0], 12);
            Assert.Equal(-2.5 * 9.0 / 100.0, r.GeldwertEur[1], 12);
        }

        // ==================================================================
        // 3. Grenzfaelle
        // ==================================================================

        /// <summary>Leerer Speicher: das Defizit bleibt vollstaendig am Netz.</summary>
        [Fact]
        public void Grenzfall_Leerer_Speicher_Entlaedt_Nicht()
        {
            var eingang = SpeicherEingang.MitFixpreis(
                new double[] { 20.0 }, new double[] { 0.0 }, PreisCtKwh);

            SpeicherErgebnis r = Rechne(eingang, MiniParameter());   // Start-SoC = SoC_min = 0

            Assert.Equal(0.0, r.EntladeenergieKwh, 12);
            Assert.Equal(0.0, r.GeldwertEur[0], 12);
            Assert.Equal(0.0, r.SoCKwh[0], 12);
            Assert.Equal(5.0, r.Kennzahlen.NetzbezugOhneSpeicherKwh, 12);
            Assert.Equal(5.0, r.Kennzahlen.NetzbezugMitSpeicherKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.AequivalenteVollzyklen, 12);
            Assert.Equal(0.0, r.Kennzahlen.VerschleisskostenEurProA, 12);
        }

        /// <summary>Voller Speicher: der Ueberschuss geht vollstaendig ins Netz.</summary>
        [Fact]
        public void Grenzfall_Voller_Speicher_Laedt_Nicht()
        {
            var eingang = new SpeicherEingang(
                new double[] { 0.0 }, new double[] { 16.0 }, new double[] { PreisCtKwh },
                new double[] { 8.0 });

            SpeicherParameter p = MiniParameter() with
            {
                SoCMinKwh = 0.0,
                SoCMaxKwh = 2.0,
                StartSoCKwh = 2.0     // voll
            };
            SpeicherErgebnis r = Rechne(eingang, p);

            Assert.Equal(0.0, r.LadeenergieKwh, 12);
            Assert.Equal(2.0, r.SoCKwh[0], 12);
            Assert.Equal(0.0, r.GeldwertEur[0], 12);

            // E_pv_frei 4,0 + E_bhkw_frei 2,0 = 6,0 kWh Einspeisung, mit wie ohne Speicher.
            Assert.Equal(6.0, r.Kennzahlen.EinspeisungOhneSpeicherKwh, 12);
            Assert.Equal(6.0, r.Kennzahlen.EinspeisungMitSpeicherKwh, 12);
        }

        /// <summary>Bei C_nutz = 0 bleiben Zyklen und Verschleiss definiert (0 statt NaN).</summary>
        [Fact]
        public void Grenzfall_Kein_Nutzbares_Band()
        {
            SpeicherParameter p = MiniParameter() with { SoCMinKwh = 5.0, SoCMaxKwh = 5.0 };
            SpeicherErgebnis r = Rechne(MiniEingang(), p);

            Assert.Equal(0.0, p.CNutzKwh);
            Assert.Equal(0.0, r.Kennzahlen.AequivalenteVollzyklen);
            Assert.Equal(0.0, r.Kennzahlen.VerschleisskostenEurProA);
        }

        // ==================================================================
        // 4. Kennzahlen: Zyklen, Verschleiss, Verluste
        // ==================================================================

        /// <summary>
        /// n_zyk bezieht die <b>DC-seitig</b> entnommene Energie auf C_nutz, K_ver
        /// folgt daraus (Fachkonzept 5.4). Mini-Beispiel: E_ac_dis = 2,025 kWh,
        /// DC = 2,025/0,9 = 2,25 kWh, C_nutz = 10 -&gt; n_zyk = 0,225;
        /// K_ver = 0,225 * 10 kWh * 0,025 EUR = 0,05625 EUR/a.
        /// </summary>
        [Fact]
        public void Kennzahlen_Vollzyklen_Und_Verschleisskosten()
        {
            SpeicherErgebnis r = Rechne(MiniEingang(), MiniParameter());

            Assert.Equal(2.25, r.Kennzahlen.EntladeenergieDcKwh, 12);
            Assert.Equal(0.225, r.Kennzahlen.AequivalenteVollzyklen, 12);
            Assert.Equal(0.05625, r.Kennzahlen.VerschleisskostenEurProA, 12);

            // Speicherverluste = Ladeenergie - Entladeenergie - Delta SoC (AC-seitig)
            Assert.Equal(2.5 - 2.025 - 0.0, r.Kennzahlen.SpeicherverlusteKwh, 12);
        }

        /// <summary>
        /// <b>K_ver ist reiner Ausweis</b>: c_ver veraendert weder die Geldwertreihe
        /// noch die Jahressumme noch den Wirtschaftlichkeitsblock (Fachkonzept 5.4,
        /// Zielfunktions-Option Default AUS).
        /// </summary>
        [Fact]
        public void Verschleisskosten_Fliessen_Nicht_In_Die_Geldwertsumme()
        {
            SpeicherEingang e = MiniEingang();
            SpeicherErgebnis ohne = Rechne(e, MiniParameter() with { CVerEurProKwhZyklus = 0.0 });
            SpeicherErgebnis mit = Rechne(e, MiniParameter() with { CVerEurProKwhZyklus = 0.25 });

            Assert.True(V7ReferenzTests.Bitgleich(ohne.SummeGeldwertEur, mit.SummeGeldwertEur));
            Assert.True(V7ReferenzTests.Bitgleich(
                ohne.Wirtschaftlichkeit.JahresueberschussEur,
                mit.Wirtschaftlichkeit.JahresueberschussEur));

            Assert.Equal(0.0, ohne.Kennzahlen.VerschleisskostenEurProA, 12);
            Assert.Equal(0.225 * 10.0 * 0.25, mit.Kennzahlen.VerschleisskostenEurProA, 12);
        }

        /// <summary>Ein negatives c_ver ist ein Parameterfehler.</summary>
        [Fact]
        public void Negatives_C_Ver_Wird_Abgewiesen()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Rechne(MiniEingang(), MiniParameter() with { CVerEurProKwhZyklus = -0.1 }));
        }

        // ==================================================================
        // 5. Bilanztest je Intervall (Abnahmekriterium AP2)
        // ==================================================================

        /// <summary>
        /// Ueber synthetische Gruen-/Grau-Konstellationen: In <b>jedem</b> Intervall
        /// gilt <c>Last = Direkt + Entladung + Netzbezug</c> und
        /// <c>Erzeugung = Direkt + Ladung + Einspeisung</c>; der Speicher erzeugt
        /// keine Energie aus dem Nichts und haelt Leistungs- wie SoC-Grenzen ein.
        /// </summary>
        /// <remarks>
        /// Lade- und Entladeenergie werden <b>aus dem veroeffentlichten SoC-Verlauf
        /// zurueckgerechnet</b> (Delta SoC = E_ch*eta_ch - E_dis/eta_dis, und beides
        /// zugleich ist ausgeschlossen). Der Test prueft damit das Ergebnis, nicht die
        /// Innereien der Schleife.
        /// </remarks>
        [Theory]
        [InlineData(true, true, 0.90)]     // Gruen: PV + BHKW
        [InlineData(true, false, 0.90)]    // Gruen: nur PV
        [InlineData(false, true, 0.81)]    // Gruen: nur BHKW
        [InlineData(true, true, 1.00)]     // verlustfrei
        public void Bilanz_Schliesst_In_Jedem_Intervall(bool pvZulaessig, bool bhkwZulaessig, double etaRt)
        {
            const int n = 2688;   // vier Wochen im Viertelstundenraster
            (double[] last, double[] pv, double[] bhkw) = SynthetischeReihen(n);

            var eingang = new SpeicherEingang(
                last, pv, SpeicherEingang.KonstanteReihe(PreisCtKwh, n), bhkw)
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherParameter p = MiniParameter() with
            {
                CNomKwh = 200.0,
                PKw = 60.0,
                SoCMinKwh = 20.0,
                SoCMaxKwh = 180.0,
                RoundTripWirkungsgrad = etaRt,
                PvZulaessig = pvZulaessig,
                BhkwUeberschussZulaessig = bhkwZulaessig
            };

            SpeicherErgebnis r = Rechne(eingang, p);

            // Der Fall muss den Speicher wirklich bewegen - sonst waere die Bilanz
            // trivial erfuellt.
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

                // Kein Strom aus dem Nichts: nur zugelassene Quellen laden, nur das
                // Defizit wird entladen.
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

            // Jahresbilanz beider Gleichungen.
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
        }

        // ==================================================================
        // 6. Rueckwaertskompatibilitaet zur Fassung aus AP1
        // ==================================================================

        /// <summary>
        /// <b>Ohne BHKW-Reihe und mit zulaessiger PV rechnet der energetische Modus
        /// bitgleich wie vor AP2.</b> Verglichen wird gegen eine zeichengetreue Kopie
        /// der alten Schleife (Stand AP1) - ueber den vollen Referenzjahrgang.
        /// </summary>
        [Theory]
        [InlineData(0.81)]
        [InlineData(0.90)]
        [InlineData(1.00)]
        public void Ohne_BHKW_Bitgleich_Zur_Logik_Vor_AP2(double etaRt)
        {
            SpeicherParameter p = Referenzdaten.V7Parameter() with { RoundTripWirkungsgrad = etaRt };
            SpeicherEingang eingang = Referenzdaten.V7Eingang();

            SpeicherErgebnis neu = Rechne(eingang, p);
            (double[] soc, double[] eur, double summe, double lade, double entlade) alt = LogikVorAP2(eingang, p);

            Assert.Equal(alt.soc.Length, neu.Anzahl);
            for (int k = 0; k < neu.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(neu.SoCKwh[k], alt.soc[k]),
                    "SoC weicht ab in Intervall " + k + ": neu = " + V7ReferenzTests.Bits(neu.SoCKwh[k]) +
                    ", alt = " + V7ReferenzTests.Bits(alt.soc[k]));
                Assert.True(V7ReferenzTests.Bitgleich(neu.GeldwertEur[k], alt.eur[k]),
                    "Geldwert weicht ab in Intervall " + k + ": neu = " + V7ReferenzTests.Bits(neu.GeldwertEur[k]) +
                    ", alt = " + V7ReferenzTests.Bits(alt.eur[k]));
            }

            Assert.True(V7ReferenzTests.Bitgleich(neu.SummeGeldwertEur, alt.summe));
            Assert.True(V7ReferenzTests.Bitgleich(neu.LadeenergieKwh, alt.lade));
            Assert.True(V7ReferenzTests.Bitgleich(neu.EntladeenergieKwh, alt.entlade));

            // Ohne BHKW stammt die gesamte Ladeenergie aus PV.
            Assert.True(V7ReferenzTests.Bitgleich(neu.Kennzahlen.LadeenergiePvKwh, neu.LadeenergieKwh));
            Assert.Equal(0.0, neu.Kennzahlen.LadeenergieBhkwKwh);
        }

        /// <summary>
        /// Eine BHKW-Reihe aus lauter Nullen ist dasselbe wie keine BHKW-Reihe -
        /// bitgleich, nicht nur ungefaehr.
        /// </summary>
        [Fact]
        public void Nullreihe_BHKW_Ist_Wie_Keine_BHKW_Reihe()
        {
            SpeicherParameter p = MiniParameter();
            SpeicherEingang ohne = SpeicherEingang.MitFixpreis(
                new double[] { 0.0, 20.0, 0.0, 20.0 },
                new double[] { 8.0, 0.0, 8.0, 0.0 }, PreisCtKwh);
            SpeicherEingang mitNull = ohne.MitBhkw(new double[4]);

            SpeicherErgebnis a = Rechne(ohne, p);
            SpeicherErgebnis b = Rechne(mitNull, p);

            for (int k = 0; k < a.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(a.SoCKwh[k], b.SoCKwh[k]), "SoC " + k);
                Assert.True(V7ReferenzTests.Bitgleich(a.GeldwertEur[k], b.GeldwertEur[k]), "F " + k);
            }
            Assert.True(V7ReferenzTests.Bitgleich(a.SummeGeldwertEur, b.SummeGeldwertEur));
        }

        /// <summary>
        /// Der Excel-Kompatibilitaetsmodus ignoriert BHKW-Reihe, Quellenflags und
        /// c_ver - er bildet die V7-Mappe nach und bleibt damit referenzfest.
        /// </summary>
        [Fact]
        public void Kompatibilitaetsmodus_Ignoriert_Quellen_Und_C_Ver()
        {
            var strategie = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet);
            SpeicherEingang ohne = SpeicherEingang.MitFixpreis(
                new double[] { 0.0, 20.0, 0.0, 20.0 },
                new double[] { 8.0, 0.0, 8.0, 0.0 }, PreisCtKwh);
            SpeicherEingang mitBhkw = ohne.MitBhkw(new double[] { 12.0, 12.0, 12.0, 12.0 })
                                          .MitVerguetungen(1.0, 2.0);

            SpeicherParameter p = MiniParameter();
            SpeicherErgebnis a = strategie.Berechne(ohne, p);
            SpeicherErgebnis b = strategie.Berechne(
                mitBhkw, p with { BhkwUeberschussZulaessig = true, CVerEurProKwhZyklus = 0.5 });

            for (int k = 0; k < a.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(a.SoCKwh[k], b.SoCKwh[k]), "SoC " + k);
                Assert.True(V7ReferenzTests.Bitgleich(a.GeldwertEur[k], b.GeldwertEur[k]), "F " + k);
            }
            Assert.True(V7ReferenzTests.Bitgleich(a.SummeGeldwertEur, b.SummeGeldwertEur));

            // c_ver = 0 im Kompatibilitaetsmodus (Fachkonzept 5.2).
            Assert.Equal(0.0, b.Kennzahlen.VerschleisskostenEurProA);
            Assert.Equal(b.LadeenergieKwh, b.Kennzahlen.LadeenergiePvKwh);
        }

        // ==================================================================
        // 7. Vertrag des Eingangs
        // ==================================================================

        /// <summary>Auch die optionalen Reihen muessen zum Lastgang passen.</summary>
        [Fact]
        public void Optionale_Reihen_Muessen_Gleich_Lang_Sein()
        {
            double[] drei = new double[3];
            Assert.Throws<ArgumentException>(() =>
                new SpeicherEingang(drei, drei, drei, new double[2]));
            Assert.Throws<ArgumentException>(() =>
                new SpeicherEingang(drei, drei, drei, null, new double[4]));
            Assert.Throws<ArgumentException>(() =>
                new SpeicherEingang(drei, drei, drei, null, null, new double[1]));
        }

        /// <summary>Der Ausschnitt nimmt die optionalen Reihen mit.</summary>
        [Fact]
        public void Ausschnitt_Nimmt_Optionale_Reihen_Mit()
        {
            var voll = new SpeicherEingang(
                new double[] { 1, 2, 3, 4 },
                new double[] { 5, 6, 7, 8 },
                new double[] { 9, 10, 11, 12 },
                new double[] { 13, 14, 15, 16 },
                new double[] { 17, 18, 19, 20 },
                new double[] { 21, 22, 23, 24 });

            SpeicherEingang teil = voll.Ausschnitt(1, 2);

            Assert.Equal(2, teil.Anzahl);
            Assert.True(teil.HatBhkwReihe);
            Assert.Equal(new double[] { 14, 15 }, teil.BhkwKw);
            Assert.Equal(new double[] { 18, 19 }, teil.VerguetungPvCtKwh);
            Assert.Equal(new double[] { 22, 23 }, teil.VerguetungBhkwCtKwh);

            // Ohne optionale Reihen bleibt der Ausschnitt ebenfalls ohne.
            SpeicherEingang schlank = SpeicherEingang.MitFixpreis(
                new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, 20.0).Ausschnitt(0, 2);
            Assert.False(schlank.HatBhkwReihe);
            Assert.Null(schlank.VerguetungPvCtKwh);
        }

        /// <summary>Der Eingang kopiert auch die optionalen Reihen.</summary>
        [Fact]
        public void Eingang_Kopiert_Optionale_Reihen()
        {
            double[] bhkw = { 1.0, 2.0 };
            var e = new SpeicherEingang(new double[2], new double[2], new double[2], bhkw);
            bhkw[0] = 99.0;

            Assert.Equal(1.0, e.BhkwKw![0]);
        }

        // ==================================================================
        // Hilfen
        // ==================================================================

        /// <summary>Der Mini-Eingang aus der Kopfdokumentation von Test 1.</summary>
        private static SpeicherEingang MiniEingang()
            => new SpeicherEingang(
                    new double[] { 0.0, 20.0, 20.0 },
                    new double[] { 4.0, 0.0, 0.0 },
                    new double[] { PreisCtKwh, PreisCtKwh, PreisCtKwh },
                    new double[] { 8.0, 0.0, 0.0 })
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

        /// <summary>
        /// Synthetische Jahresreihen mit fester Saat: Grundlast mit Tagesgang,
        /// PV-Glocke um die Mittagszeit und ein BHKW-Block in den Morgen- und
        /// Abendstunden, jeweils mit Rauschen. Deckt Ueberschuss- und
        /// Defizitintervalle beider Quellen ab.
        /// </summary>
        private static (double[] last, double[] pv, double[] bhkw) SynthetischeReihen(int n)
        {
            var zufall = new Random(20260816);
            double[] last = new double[n];
            double[] pv = new double[n];
            double[] bhkw = new double[n];

            for (int k = 0; k < n; k++)
            {
                double stunde = (k % 96) / 4.0;

                last[k] = 20.0 + 25.0 * Math.Sin(Math.PI * stunde / 24.0) + zufall.NextDouble() * 10.0;

                pv[k] = (stunde >= 7.0 && stunde <= 19.0)
                    ? 90.0 * Math.Sin(Math.PI * (stunde - 7.0) / 12.0) * (0.5 + zufall.NextDouble())
                    : 0.0;

                bhkw[k] = ((stunde >= 5.0 && stunde < 9.0) || (stunde >= 17.0 && stunde < 22.0))
                    ? 30.0 + zufall.NextDouble() * 20.0
                    : 0.0;
            }
            return (last, pv, bhkw);
        }

        /// <summary>
        /// <b>Zeichengetreue Kopie des energetischen Modus im Stand AP1</b> (vor der
        /// Quellen-Matrix). Referenz fuer den Rueckwaertskompatibilitaetstest; die
        /// Datei <c>Dauernutzung.cs</c> hat diese Fassung ersetzt.
        /// </summary>
        private static (double[] soc, double[] eur, double summe, double lade, double entlade)
            LogikVorAP2(SpeicherEingang eingang, SpeicherParameter p)
        {
            double[] b = eingang.LastKw;
            double[] c = eingang.PvKw;
            double[] dCt = eingang.PreisCtKwh;
            int n = b.Length;

            double maxPower = p.PKw;
            double minLevel = p.SoCMinKwh;
            double maxLevel = p.SoCMaxKwh;
            double verguetung = p.VerguetungCtKwh;
            double dt = p.DtH;
            double etaCh = p.EtaCh;
            double etaDis = p.EtaDis;

            double[] soc = new double[n];
            double[] eur = new double[n];

            double ladeenergie = 0.0;
            double entladeenergie = 0.0;
            double prev = p.StartSoCEffektivKwh;

            for (int k = 0; k < n; k++)
            {
                double bk = b[k];
                double ck = c[k];
                double dk = dCt[k];

                double charge = 0.0;
                double discharge = 0.0;

                if (ck > bk)
                {
                    charge = (ck - bk) * dt;
                    if (charge > maxPower * dt) charge = maxPower * dt;
                    if (charge > (maxLevel - prev) / etaCh) charge = (maxLevel - prev) / etaCh;
                    if (charge < 0) charge = 0.0;
                }
                else
                {
                    discharge = (bk - ck) * dt;
                    if (discharge > maxPower * dt) discharge = maxPower * dt;
                    if (discharge > (prev - minLevel) * etaDis) discharge = (prev - minLevel) * etaDis;
                    if (discharge < 0) discharge = 0.0;
                }

                double newLevel = prev + charge * etaCh - discharge / etaDis;

                if (discharge > 0) eur[k] = discharge * dk / 100.0;
                else if (charge > 0) eur[k] = -charge * verguetung / 100.0;
                else eur[k] = 0.0;

                soc[k] = newLevel;
                prev = newLevel;

                ladeenergie += charge;
                entladeenergie += discharge;
            }

            return (soc, eur, Numerik.SummeSequenziell(eur), ladeenergie, entladeenergie);
        }
    }
}
