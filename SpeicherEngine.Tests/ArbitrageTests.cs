using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Preissteuerung (AP10, Fachkonzept 6.5): <see cref="ArbitragePlaner"/>
    /// und Strategie <see cref="Arbitrage"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum diese Tests die einzige Absicherung sind.</b> Die Arbitragelogik der
    /// V7-Mappe war zur Laufzeit nicht ausfuehrbar und ihre gespeicherten Ergebnisse
    /// mit keinem Datenstand reproduzierbar; sie wurde ausdruecklich <b>nicht
    /// portiert</b> (Fachkonzept 6.5). Einen Excel-Verifikationsanker wie bei der
    /// Dauernutzung gibt es also nicht - die Korrektheit haengt an Handrechnungen,
    /// Bilanzidentitaeten und dem Aequivalenzanker gegen die Dauernutzung.
    /// </para>
    /// <para>Die Abdeckung ist entsprechend geschnitten:</para>
    /// <list type="number">
    ///   <item><description><b>Anti-G3</b> - der konstruierte Fall, in dem stilles
    ///     Klemmen ein anderes Ergebnis ergaebe; der Pfadpruefer muss die Paarung
    ///     verwerfen.</description></item>
    ///   <item><description><b>Rentabilitaetsbedingung</b> samt k_ver-Grenzfall
    ///     (5.4: nicht abschaltbar).</description></item>
    ///   <item><description><b>Reihenfolge</b> - Ladung zeitlich vor Entladung,
    ///     Slots paaren nie ueber Fenstergrenzen hinweg.</description></item>
    ///   <item><description><b>Zyklenbudget</b> - Reduktion und Abbruch.</description></item>
    ///   <item><description><b>Aequivalenzanker</b> - ohne Netzpfade bitgleich zur
    ///     Dauernutzung, ueber volle Jahreslaeufe.</description></item>
    ///   <item><description><b>Handrechnung</b>, Bilanz, Determinismus und ein
    ///     synthetischer Graustrom-Jahreslauf im AP4-Spotreihenformat.</description></item>
    /// </list>
    /// <para>
    /// Mini-Parametersatz wie in <c>NachtnutzungTests</c>: dt = 0,25 h,
    /// P*dt = 2,5 kWh, Band 0 .. 10 kWh. Wo es auf exakte Zahlen ankommt, steht
    /// eta_RT = 1 (dann sind eta_ch = eta_dis = 1 und alle Zwischenwerte exakt);
    /// sonst eta_RT = 0,81, weil eta_ch = eta_dis = 0,9 als Double exakt ist.
    /// </para>
    /// </remarks>
    public sealed class ArbitrageTests
    {
        private const double Dt = 0.25;
        private const double PBezugCtKwh = 20.0;
        private const double VPvCtKwh = 5.0;
        private const double VBhkwCtKwh = 12.0;

        // ==================================================================
        // Testdaten
        // ==================================================================

        /// <summary>Mini-Speicher: 10 kWh Band 0 .. 10, 10 kW, c_ver = 0 (k_ver = 0).</summary>
        private static SpeicherParameter MiniParameter(double etaRt = 1.0) => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = etaRt,
            DtH = Dt,
            VerguetungCtKwh = VPvCtKwh,
            CCapEurProKwh = 500.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0,
            Betriebsart = SpeicherBetriebsart.Graustrom
        };

        private static SpeicherEingang Eingang(double[] last, double[] pv, double[]? bhkw = null,
                                               double[]? bezug = null)
            => new SpeicherEingang(last, pv,
                                   bezug ?? SpeicherEingang.KonstanteReihe(PBezugCtKwh, last.Length), bhkw)
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

        /// <summary>Reihe aus Nullen der Laenge <paramref name="n"/>.</summary>
        private static double[] Null(int n) => new double[n];

        private static ArbitrageOptionen Optionen(double[] pNetz, double[] erloes,
                                                  bool netzladung = true, bool netzentladung = true,
                                                  double schwelle = 0.0, double budget = 0.0,
                                                  int fenster = 4)
            => new ArbitrageOptionen(pNetz, erloes, netzladung, netzentladung, schwelle, budget,
                                     ArbitrageOptionen.ReservepufferKwhStandard, fenster);

        private static ArbitrageErgebnis Rechne(SpeicherEingang e, SpeicherParameter p, ArbitrageOptionen o)
            => new Arbitrage(o).BerechneMitPlan(e, p);

        // ==================================================================
        // 1. Handrechnung
        // ==================================================================

        /// <summary>
        /// Vollstaendig von Hand nachgerechneter Minifall mit bekannten Spotpreisen
        /// (dt = 0,25 h, P*dt = 2,5 kWh, eta_ch = eta_dis = 0,9, Band 0 .. 10,
        /// Start-SoC 0, weder Last noch PV, k_ver = 0, Fenster = 4):
        /// <code>
        /// p_netzlade = [ 2, 30, 30, 30]   erloes = [0, 0, 0, 20]   p_bezug = 20
        ///
        /// Bestes Paar: guenstigste Ladung k=0 (2 ct) x teuerster Verkauf k=3 (20 ct)
        ///   Spread = 20 - 2/0,81 = 17,53 &gt; k_ver = 0        -&gt; angenommen
        ///   E_ch  = min(2,5 ; (10-0)/0,9 = 11,11) = 2,5
        ///   E_dis = 2,5 * 0,81 = 2,025
        /// k=0  Netzladung 2,5   -&gt; SoC = 0 + 2,5*0,9 = 2,25 ; F = -2,5*2/100  = -0,050
        /// k=1  nichts           -&gt; SoC = 2,25              ; F = 0
        /// k=2  nichts           -&gt; SoC = 2,25              ; F = 0
        /// k=3  Verkauf 2,025    -&gt; SoC = 2,25 - 2,025/0,9 = 0 ; F = +2,025*20/100 = +0,405
        ///
        /// Zweite Runde: bestes Restpaar k=1 x k=2, Spread = 0 - 30/0,81 &lt; 0 -&gt; Abbruch
        /// Ungepaarter Verkauf: Referenz = max(p_bezug) = 20 &gt; erloes         -&gt; keiner
        /// </code>
        /// </summary>
        [Fact]
        public void Minifall_Rechnet_Von_Hand_Nach()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            ArbitrageOptionen o = Optionen(new double[] { 2.0, 30.0, 30.0, 30.0 },
                                           new double[] { 0.0, 0.0, 0.0, 20.0 });

            ArbitrageErgebnis r = Rechne(e, MiniParameter(0.81), o);

            Assert.Equal(1, r.Plan.PaareAngenommen);
            Assert.Equal(0, r.Plan.VerkaufsslotsAngenommen);
            Assert.Equal(0, r.Plan.VerworfenPfad);

            ReihenGleich(new double[] { 2.5, 0.0, 0.0, 0.0 }, r.LadungNetzAcKwh);
            ReihenGleich(new double[] { 0.0, 0.0, 0.0, 2.025 }, r.VerkaufAcKwh);
            ReihenGleich(new double[] { 2.25, 2.25, 2.25, 0.0 }, r.Ergebnis.SoCKwh);
            ReihenGleich(new double[] { -0.05, 0.0, 0.0, 0.405 }, r.Ergebnis.GeldwertEur);

            Assert.Equal(0.355, r.Ergebnis.SummeGeldwertEur, 12);

            Assert.Equal(2.5, r.Kennzahlen.LadungNetzKwh, 12);
            Assert.Equal(2.025, r.Kennzahlen.VerkaufKwh, 12);
            Assert.Equal(0.05, r.Kennzahlen.LadekostenEur, 12);
            Assert.Equal(0.405, r.Kennzahlen.NetzerloesEur, 12);
            Assert.Equal(0.0, r.Kennzahlen.BezugsersparnisEur, 12);
            Assert.Equal(0.0, r.Kennzahlen.EntgangeneVerguetungEur, 12);

            // Der Eigenverbrauchsfluss bleibt unberuehrt: seine beiden Reihen sind leer.
            Assert.Equal(0.0, r.Ergebnis.LadeenergieKwh, 12);
            Assert.Equal(0.0, r.Ergebnis.EntladeenergieKwh, 12);

            // Verluste = 2,5 (AC hinein) - 2,025 (AC heraus), SoC-Aenderung 0.
            Assert.Equal(0.475, r.Ergebnis.Kennzahlen.SpeicherverlusteKwh, 12);
            Assert.Equal(2.25, r.Kennzahlen.EntladeenergieDcGesamtKwh, 12);
            Assert.Equal(0.225, r.Ergebnis.Kennzahlen.AequivalenteVollzyklen, 12);

            // Nichts wurde geklemmt.
            Assert.Equal(0.0, r.Kennzahlen.AbweichungVomPlanKwh, 12);
        }

        // ==================================================================
        // 2. Anti-G3: kein stilles Klemmen
        // ==================================================================

        /// <summary>
        /// <b>Der Kernfall dieses Pakets.</b> Konstruiert ist eine zweite Paarung, deren
        /// Ladung <b>vor</b> der bereits angenommenen liegt und ihr damit den SoC-Kopf
        /// wegnimmt. Wuerde der Planer still auf die Bandgrenze klemmen - der V7-Fehler
        /// G3 -, entstuende ein anderer, schlechterer Fahrplan; die Pfadpruefung muss die
        /// Paarung deshalb verwerfen.
        /// </summary>
        /// <remarks>
        /// <code>
        /// Band 0 .. 2,5 kWh (nur EINE Ladung passt hinein), eta_RT = 1, P*dt = 2,5
        /// p_netzlade = [ 5,  1, 99, 99]      erloes = [0, 0, 50, 40]
        ///
        /// Runde 1: bestes Paar k=1 (1 ct) x k=2 (50 ct), Spread 49  -&gt; angenommen
        ///          E_ch = 2,5 bei k=1 ; E_dis = 2,5 bei k=2
        /// Runde 2: bestes Restpaar k=0 (5 ct) x k=3 (40 ct), Spread 35 &gt; 0
        ///          Probelauf: k=0 laedt 2,5 -&gt; SoC = 2,5
        ///                     k=1 haette 0 Kopf -&gt; die GEPLANTE Ladung 2,5 wuerde auf 0
        ///                         geklemmt, ebenso der Verkauf bei k=3
        ///          -&gt; Pfad unzulaessig, Paarung verworfen, Verkaufsslot k=3 gesperrt
        ///
        /// Ergebnis mit Pfadpruefung: -2,5*1/100 + 2,5*50/100 = +1,225 EUR
        /// Ergebnis bei stillem Klemmen: -2,5*5/100 + 2,5*50/100 = +1,125 EUR
        /// </code>
        /// </remarks>
        [Fact]
        public void Pfadpruefung_Verwirft_Paarung_Statt_Still_Zu_Klemmen()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            SpeicherParameter p = MiniParameter() with { SoCMaxKwh = 2.5, CNomKwh = 2.5 };
            ArbitrageOptionen o = Optionen(new double[] { 5.0, 1.0, 99.0, 99.0 },
                                           new double[] { 0.0, 0.0, 50.0, 40.0 });

            ArbitrageErgebnis r = Rechne(e, p, o);

            // Genau eine Paarung angenommen, genau eine an der Pfadpruefung gescheitert.
            Assert.Equal(1, r.Plan.PaareAngenommen);
            Assert.Equal(1, r.Plan.VerworfenPfad);

            // Geladen wird beim guenstigen Preis, nicht beim teuren.
            ReihenGleich(new double[] { 0.0, 2.5, 0.0, 0.0 }, r.LadungNetzAcKwh);
            ReihenGleich(new double[] { 0.0, 0.0, 2.5, 0.0 }, r.VerkaufAcKwh);

            Assert.Equal(1.225, r.Ergebnis.SummeGeldwertEur, 12);

            // Das Ergebnis des stillen Klemmens ist ein anderes - der Test traegt also.
            Assert.NotEqual(1.125, r.Ergebnis.SummeGeldwertEur, 6);

            // Und der Dispatch musste nichts nachtraeglich beschneiden.
            Assert.Equal(0.0, r.Kennzahlen.AbweichungVomPlanKwh, 12);
        }

        /// <summary>
        /// Gegenprobe: Passt das Band, wird die zweite Paarung angenommen - die
        /// Ablehnung oben liegt also am Pfad und nicht daran, dass der Planer nach der
        /// ersten Paarung aufhoert.
        /// </summary>
        [Fact]
        public void Bei_Ausreichendem_Band_Werden_Beide_Paarungen_Angenommen()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            SpeicherParameter p = MiniParameter();   // Band 0 .. 10, zwei Ladungen passen
            ArbitrageOptionen o = Optionen(new double[] { 5.0, 1.0, 99.0, 99.0 },
                                           new double[] { 0.0, 0.0, 50.0, 40.0 });

            ArbitrageErgebnis r = Rechne(e, p, o);

            Assert.Equal(2, r.Plan.PaareAngenommen);
            Assert.Equal(0, r.Plan.VerworfenPfad);
            ReihenGleich(new double[] { 2.5, 2.5, 0.0, 0.0 }, r.LadungNetzAcKwh);
            ReihenGleich(new double[] { 0.0, 0.0, 2.5, 2.5 }, r.VerkaufAcKwh);
            Assert.Equal(0.0, r.Kennzahlen.AbweichungVomPlanKwh, 12);
        }

        // ==================================================================
        // 3. Rentabilitaetsbedingung und k_ver
        // ==================================================================

        /// <summary>
        /// Rentabilitaetsbedingung nach 6.5: <c>Erloes - p_netzlade/eta_RT - k_ver &gt; 0</c>.
        /// Bei eta_RT = 0,81 traegt ein Erloes von 24 ct eine Ladung zu 20 ct nicht
        /// (20/0,81 = 24,69), ein Erloes von 26 ct schon.
        /// </summary>
        [Theory]
        [InlineData(24.0, 0)]
        [InlineData(26.0, 1)]
        public void Spread_Wird_Auf_Den_Roundtrip_Bezogen(double erloes, int erwartetePaare)
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            ArbitrageOptionen o = Optionen(new double[] { 20.0, 20.0, 20.0, 20.0 },
                                           new double[] { 0.0, 0.0, 0.0, erloes });

            ArbitrageErgebnis r = Rechne(e, MiniParameter(0.81), o);
            Assert.Equal(erwartetePaare, r.Plan.PaareAngenommen);
        }

        /// <summary>
        /// Grenzfall k_ver: Bei eta_RT = 1, C_nom = C_nutz = 10 und
        /// c_ver = 0,0625 EUR/(kWh*Zyklus) ist
        /// <c>k_ver = 100*0,0625*10/(10*1) = 6,25 ct/kWh</c> - exakt, weil 0,0625 = 1/16
        /// als Double exakt ist. Mit kostenloser Ladung ist der Spread gleich dem
        /// Erloes; die Bedingung ist <b>strikt</b>, ein Erloes von genau 6,25 ct traegt
        /// also nicht.
        /// </summary>
        /// <remarks>
        /// Der Verschleissterm ist hier <b>nicht abschaltbar</b> (Fachkonzept 5.4,
        /// Verwendung 1) - es gibt keinen Schalter, der ihn aus der Bedingung nimmt.
        /// </remarks>
        [Theory]
        [InlineData(6.24, 0)]
        [InlineData(6.25, 0)]
        [InlineData(6.30, 1)]
        public void KVer_Ist_Die_Untere_Schranke_Des_Spreads(double erloes, int erwartetePaare)
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            SpeicherParameter p = MiniParameter() with { CVerEurProKwhZyklus = 0.0625 };

            Assert.Equal(6.25, ArbitrageOptionen.VerschleissCtKwh(p), 12);

            ArbitrageOptionen o = Optionen(Null(4), new double[] { 0.0, 0.0, 0.0, erloes });
            ArbitrageErgebnis r = Rechne(e, p, o);

            Assert.Equal(erwartetePaare, r.Plan.PaareAngenommen);
            Assert.Equal(6.25, r.Plan.VerschleissCtKwh, 12);
        }

        /// <summary>
        /// k_ver nach Fachkonzept 5.4 mit den dortigen Zahlen: c_ver = 0,025,
        /// C_nom = 5.000 kWh, C_nutz = 4.000 kWh, eta_dis = sqrt(0,9)
        /// ergibt rund 3,29 ct/kWh.
        /// </summary>
        [Fact]
        public void KVer_Trifft_Den_Wert_Des_Fachkonzepts()
        {
            SpeicherParameter p = new SpeicherParameter
            {
                CNomKwh = 5000.0,
                PKw = 2500.0,
                SoCMinKwh = 500.0,
                SoCMaxKwh = 4500.0,
                RoundTripWirkungsgrad = 0.90,
                CVerEurProKwhZyklus = 0.025
            };

            Assert.Equal(3.294, ArbitrageOptionen.VerschleissCtKwh(p), 3);
        }

        // ==================================================================
        // 4. Reihenfolge: Ladung vor Entladung, Fenstergrenzen
        // ==================================================================

        /// <summary>
        /// Die guenstige Ladung liegt <b>hinter</b> dem teuren Verkauf - gepaart wird
        /// nicht. Erst wenn ein Verkaufsslot dahinter existiert, kommt die Paarung
        /// zustande.
        /// </summary>
        [Fact]
        public void Ladung_Muss_Zeitlich_Vor_Der_Entladung_Liegen()
        {
            double[] pNetz = { 30.0, 1.0, 99.0, 99.0 };
            SpeicherEingang e = Eingang(Null(4), Null(4));

            // Teurer Verkauf VOR der guenstigen Ladung: kein Paar.
            ArbitrageErgebnis vorher = Rechne(e, MiniParameter(),
                Optionen(pNetz, new double[] { 20.0, 0.0, 0.0, 0.0 }));
            Assert.Equal(0, vorher.Plan.PaareAngenommen);
            Assert.Equal(0.0, vorher.Kennzahlen.VerkaufKwh, 12);

            // Derselbe Verkauf NACH der Ladung: Paar.
            ArbitrageErgebnis nachher = Rechne(e, MiniParameter(),
                Optionen(pNetz, new double[] { 0.0, 0.0, 0.0, 20.0 }));
            Assert.Equal(1, nachher.Plan.PaareAngenommen);
            Assert.Equal(2.5, nachher.LadungNetzAcKwh[1], 12);
            Assert.Equal(2.5, nachher.VerkaufAcKwh[3], 12);
        }

        /// <summary>
        /// Slots paaren nie ueber Fenstergrenzen hinweg: Dieselben Reihen ergeben mit
        /// zwei 4er-Fenstern kein Paar und mit einem 8er-Fenster genau eines.
        /// </summary>
        [Fact]
        public void Slots_Paaren_Nie_Ueber_Fenstergrenzen_Hinweg()
        {
            SpeicherEingang e = Eingang(Null(8), Null(8));
            double[] pNetz = { 30.0, 1.0, 30.0, 30.0, 30.0, 30.0, 30.0, 30.0 };
            double[] erloes = { 0.0, 0.0, 0.0, 0.0, 0.0, 20.0, 0.0, 0.0 };

            ArbitrageErgebnis getrennt = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, fenster: 4));
            Assert.Equal(2, getrennt.Plan.Fensteranzahl);
            Assert.Equal(0, getrennt.Plan.PaareAngenommen);
            Assert.Equal(0.0, getrennt.Kennzahlen.LadungNetzKwh, 12);

            ArbitrageErgebnis zusammen = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, fenster: 8));
            Assert.Equal(1, zusammen.Plan.Fensteranzahl);
            Assert.Equal(1, zusammen.Plan.PaareAngenommen);
            Assert.Equal(2.5, zusammen.LadungNetzAcKwh[1], 12);
            Assert.Equal(2.5, zusammen.VerkaufAcKwh[5], 12);
        }

        /// <summary>
        /// Der Ladezustand wandert ueber die Fenstergrenze: Das zweite Fenster startet
        /// mit dem Endwert des ersten (vollstaendige Uebernahme, Fachkonzept 6.5).
        /// </summary>
        [Fact]
        public void Fenster_Uebergibt_Den_Ladezustand_Vollstaendig()
        {
            SpeicherEingang e = Eingang(Null(8), Null(8));
            // Fenster 1 laedt (k=0) und verkauft nicht; Fenster 2 verkauft (k=7).
            double[] pNetz = { 1.0, 99.0, 99.0, 99.0, 99.0, 99.0, 99.0, 99.0 };
            double[] erloes = { 0.0, 0.0, 0.0, 40.0, 0.0, 0.0, 0.0, 40.0 };

            ArbitrageErgebnis r = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, fenster: 4));

            // Fenster 1: Paar (0 -> 3). Fenster 2 findet keine bezahlbare Ladung mehr.
            Assert.Equal(2.5, r.LadungNetzAcKwh[0], 12);
            Assert.Equal(2.5, r.VerkaufAcKwh[3], 12);
            Assert.Equal(2.5, r.Ergebnis.SoCKwh[0], 12);
            Assert.Equal(0.0, r.Ergebnis.SoCKwh[3], 12);
            Assert.Equal(0.0, r.Ergebnis.SoCKwh[7], 12);
        }

        // ==================================================================
        // 5. Zyklenbudget
        // ==================================================================

        /// <summary>
        /// Das Zyklenbudget begrenzt die kumulierte Entladeenergie (Fachkonzept 6.5):
        /// Zwei Fenster wuerden je 2,5 kWh verkaufen; bei einem Budget von 3,0 kWh
        /// bleibt fuer das zweite nur noch 0,5 kWh, danach endet die Planung.
        /// </summary>
        [Fact]
        public void Zyklenbudget_Reduziert_Und_Beendet_Die_Planung()
        {
            SpeicherEingang e = Eingang(Null(8), Null(8));
            double[] pNetz = { 0.0, 99.0, 99.0, 99.0, 0.0, 99.0, 99.0, 99.0 };
            double[] erloes = { 0.0, 0.0, 0.0, 50.0, 0.0, 0.0, 0.0, 50.0 };

            ArbitrageErgebnis ohne = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, fenster: 4));
            Assert.Equal(5.0, ohne.Kennzahlen.VerkaufKwh, 12);
            Assert.False(ohne.Plan.BudgetErschoepft);
            Assert.Equal(0.0, ohne.Kennzahlen.BudgetauslastungProzent, 12);

            ArbitrageErgebnis mit = Rechne(e, MiniParameter(),
                                           Optionen(pNetz, erloes, budget: 3.0, fenster: 4));

            Assert.Equal(2.5, mit.VerkaufAcKwh[3], 12);
            Assert.Equal(0.5, mit.VerkaufAcKwh[7], 12);
            Assert.Equal(3.0, mit.Kennzahlen.VerkaufKwh, 12);
            Assert.Equal(3.0, mit.Kennzahlen.EntladeenergieDcGesamtKwh, 12);
            Assert.True(mit.Plan.BudgetErschoepft);
            Assert.Equal(100.0, mit.Kennzahlen.BudgetauslastungProzent, 9);
            Assert.Equal(0.0, mit.Kennzahlen.AbweichungVomPlanKwh, 12);
        }

        /// <summary>
        /// Jahresbudget nach dem gesetzten Default <c>N_zyk * C_nutz / N</c>; ohne
        /// gepflegte Angaben 0 = unbegrenzt.
        /// </summary>
        [Fact]
        public void Jahresbudget_Folgt_Dem_Gesetzten_Default()
        {
            // Fachkonzept 5.4: N_zyk = 10.000, C_nutz = 4.000 kWh, N = 20 a
            // -> 500 Vollzyklen und damit 2.000.000 kWh DC je Jahr.
            Assert.Equal(2000000.0, ArbitrageOptionen.JahresbudgetDcKwh(10000.0, 4000.0, 20.0), 9);
            Assert.Equal(0.0, ArbitrageOptionen.JahresbudgetDcKwh(0.0, 4000.0, 20.0), 12);
            Assert.Equal(0.0, ArbitrageOptionen.JahresbudgetDcKwh(10000.0, 0.0, 20.0), 12);
            Assert.Equal(0.0, ArbitrageOptionen.JahresbudgetDcKwh(10000.0, 4000.0, 0.0), 12);
        }

        // ==================================================================
        // 6. Betriebsart, Ladeschwellwert, Netzentladung
        // ==================================================================

        /// <summary>
        /// Gruenstrom: <b>nie</b> Netzladung (Fachkonzept 2.1) - der Verkauf bleibt
        /// erlaubt, findet hier aber keine rentable Gelegenheit, weil die
        /// Vergleichsgroesse der volle Bezugspreis ist.
        /// </summary>
        [Fact]
        public void Gruenstrom_Laedt_Nie_Aus_Dem_Netz()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            ArbitrageOptionen o = Optionen(new double[] { 0.0, 0.0, 0.0, 0.0 },
                                           new double[] { 0.0, 0.0, 0.0, 50.0 },
                                           netzladung: false);

            ArbitrageErgebnis r = Rechne(e, MiniParameter(), o);

            Assert.Equal(0, r.Plan.PaareAngenommen);
            Assert.Equal(0.0, r.Kennzahlen.LadungNetzKwh, 12);
            // Ohne Ladezustand gibt es auch nichts zu verkaufen.
            Assert.Equal(0.0, r.Kennzahlen.VerkaufKwh, 12);
        }

        /// <summary>
        /// Gruenstrom mit Netzentladung: Aus PV-Ueberschuss geladene Energie <b>darf</b>
        /// verkauft werden, wenn der Erloes selbst den hoechsten noch vermeidbaren
        /// Bezugspreis des Fensters plus k_ver uebersteigt (gesetzter Default, siehe
        /// <c>ArbitragePlaner.Lauf.BesterVerkaufsslot</c>).
        /// </summary>
        [Fact]
        public void Gruenstrom_Verkauft_Nur_Oberhalb_Der_Eigenverbrauchsalternative()
        {
            // k=0 laedt aus PV (8 kW ueber 0,25 h = 2 kWh), danach passiert nichts mehr.
            double[] last = { 0.0, 0.0, 0.0, 0.0 };
            double[] pv = { 8.0, 0.0, 0.0, 0.0 };
            SpeicherEingang e = Eingang(last, pv);

            // Erloes 15 ct liegt UNTER dem Bezugspreis 20 ct -> kein Verkauf.
            ArbitrageErgebnis zuBillig = Rechne(e, MiniParameter(),
                Optionen(Null(4), new double[] { 0.0, 0.0, 0.0, 15.0 }, netzladung: false));
            Assert.Equal(0.0, zuBillig.Kennzahlen.VerkaufKwh, 12);

            // Erloes 50 ct liegt darueber -> Verkauf aus dem PV-Ladezustand.
            ArbitrageErgebnis teuerGenug = Rechne(e, MiniParameter(),
                Optionen(Null(4), new double[] { 0.0, 0.0, 0.0, 50.0 }, netzladung: false));
            Assert.Equal(1, teuerGenug.Plan.VerkaufsslotsAngenommen);
            Assert.Equal(2.0, teuerGenug.VerkaufAcKwh[3], 12);
            Assert.Equal(0.0, teuerGenug.Kennzahlen.LadungNetzKwh, 12);
            Assert.Equal(1.0, teuerGenug.Kennzahlen.NetzerloesEur, 12);
        }

        /// <summary>
        /// Der Ladeschwellwert (Fachkonzept 5.6) ist eine <b>zusaetzliche</b> Schranke:
        /// Er kann Ladeslots ausschliessen, aber keine unrentablen zulassen. 0 = keine
        /// Schranke.
        /// </summary>
        [Fact]
        public void Ladeschwellwert_Sperrt_Zu_Teure_Ladeslots()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            double[] pNetz = { 12.0, 8.0, 99.0, 99.0 };
            double[] erloes = { 0.0, 0.0, 0.0, 50.0 };

            // Ohne Schranke: der guenstigste Slot (8 ct bei k=1) wird geladen.
            ArbitrageErgebnis ohne = Rechne(e, MiniParameter(), Optionen(pNetz, erloes));
            Assert.Equal(2.5, ohne.LadungNetzAcKwh[1], 12);

            // Schranke 10 ct: k=0 (12 ct) faellt weg, k=1 (8 ct) bleibt - gleiches Bild.
            ArbitrageErgebnis mittel = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, schwelle: 10.0));
            Assert.Equal(2.5, mittel.LadungNetzAcKwh[1], 12);

            // Schranke 5 ct: kein Ladeslot mehr, obwohl der Spread truege.
            ArbitrageErgebnis eng = Rechne(e, MiniParameter(), Optionen(pNetz, erloes, schwelle: 5.0));
            Assert.Equal(0, eng.Plan.PaareAngenommen);
            Assert.Equal(0.0, eng.Kennzahlen.LadungNetzKwh, 12);
        }

        /// <summary>
        /// Abgeschaltete Netzentladung: Es wird weder gepaart noch verkauft - eine
        /// Netzladung ohne Verkaufsmoeglichkeit waere sinnlos.
        /// </summary>
        [Fact]
        public void Ohne_Netzentladung_Wird_Nicht_Geplant()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            ArbitrageErgebnis r = Rechne(e, MiniParameter(),
                Optionen(Null(4), new double[] { 0.0, 0.0, 0.0, 50.0 }, netzentladung: false));

            Assert.Equal(0, r.Plan.PaareAngenommen);
            Assert.Equal(0.0, r.Kennzahlen.LadungNetzKwh, 12);
            Assert.Equal(0.0, r.Kennzahlen.VerkaufKwh, 12);
        }

        /// <summary>
        /// Der Eigenverbrauch hat Vorrang: In einem Intervall, in dem der Speicher die
        /// Residuallast deckt, greift kein Netzpfad (Fachkonzept 6.2, 2.2).
        /// </summary>
        [Fact]
        public void Netzpfade_Greifen_Nur_Wo_Der_Eigenverbrauch_Nichts_Tut()
        {
            // k=0 PV-Ueberschuss (Ladung), k=1 Defizit (Entladung), k=2/3 frei.
            double[] last = { 0.0, 20.0, 0.0, 0.0 };
            double[] pv = { 8.0, 0.0, 0.0, 0.0 };
            SpeicherEingang e = Eingang(last, pv);

            ArbitrageOptionen o = Optionen(new double[] { 0.0, 0.0, 0.0, 0.0 },
                                           new double[] { 99.0, 99.0, 0.0, 99.0 });

            ArbitrageErgebnis r = Rechne(e, MiniParameter(), o);

            // In k=0 (Ladung) und k=1 (Entladung) darf kein Netzpfad stehen.
            Assert.Equal(0.0, r.LadungNetzAcKwh[0], 12);
            Assert.Equal(0.0, r.VerkaufAcKwh[0], 12);
            Assert.Equal(0.0, r.LadungNetzAcKwh[1], 12);
            Assert.Equal(0.0, r.VerkaufAcKwh[1], 12);

            // Der Eigenverbrauchsfluss selbst bleibt unveraendert.
            Assert.Equal(2.0, r.Ergebnis.LadungAcKwh[0], 12);
            Assert.True(r.Ergebnis.EntladungAcKwh[1] > 0.0);
        }

        // ==================================================================
        // 7. Aequivalenzanker: ohne Netzpfade identisch zur Dauernutzung
        // ==================================================================

        /// <summary>
        /// <b>Der Anker.</b> Ohne Netzpfade rechnet die Arbitrage <b>bitgleich</b> zur
        /// Dauernutzung - ueber den vollen Referenzjahrgang (35.137 Intervalle) und in
        /// allen drei Abschaltvarianten: keine Optionen, beide Schalter aus, Optionen
        /// mit Preisreihen aber ohne Freigabe.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void OhneNetzpfade_Ist_Bitgleich_Zur_Dauernutzung(int variante)
        {
            SpeicherEingang e = Referenzdaten.V7Eingang().MitVerguetungen(VPvCtKwh, VBhkwCtKwh);
            SpeicherParameter p = Referenzdaten.V7Parameter() with
            {
                RoundTripWirkungsgrad = 0.90,
                CVerEurProKwhZyklus = 0.025,
                Betriebsart = SpeicherBetriebsart.Graustrom
            };

            int n = e.Anzahl;
            ArbitrageOptionen? o = variante switch
            {
                0 => null,
                1 => ArbitrageOptionen.Konstant(0.0, 999.0, n, netzladung: false, netzentladung: false),
                _ => new ArbitrageOptionen(SpeicherEingang.KonstanteReihe(1.0, n),
                                           SpeicherEingang.KonstanteReihe(500.0, n))
            };

            SpeicherErgebnis a = new Arbitrage(o).Berechne(e, p);
            SpeicherErgebnis d = new Dauernutzung(SpeicherModus.Energetisch).Berechne(e, p);

            Assert.Equal(d.Anzahl, a.Anzahl);
            for (int k = 0; k < n; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(a.SoCKwh[k], d.SoCKwh[k]),
                    "SoC weicht ab in Intervall " + k + ": Arbitrage = " + V7ReferenzTests.Bits(a.SoCKwh[k]) +
                    ", Dauer = " + V7ReferenzTests.Bits(d.SoCKwh[k]));
                Assert.True(V7ReferenzTests.Bitgleich(a.GeldwertEur[k], d.GeldwertEur[k]),
                    "Geldwert weicht ab in Intervall " + k);
                Assert.True(V7ReferenzTests.Bitgleich(a.LadungAcKwh[k], d.LadungAcKwh[k]),
                    "Ladung weicht ab in Intervall " + k);
                Assert.True(V7ReferenzTests.Bitgleich(a.EntladungAcKwh[k], d.EntladungAcKwh[k]),
                    "Entladung weicht ab in Intervall " + k);
            }

            Assert.True(V7ReferenzTests.Bitgleich(a.SummeGeldwertEur, d.SummeGeldwertEur));
            Assert.True(V7ReferenzTests.Bitgleich(a.LadeenergieKwh, d.LadeenergieKwh));
            Assert.True(V7ReferenzTests.Bitgleich(a.EntladeenergieKwh, d.EntladeenergieKwh));
            Assert.True(V7ReferenzTests.Bitgleich(a.Kennzahlen.EntladeenergieDcKwh,
                                                  d.Kennzahlen.EntladeenergieDcKwh));
            Assert.True(V7ReferenzTests.Bitgleich(a.Kennzahlen.SpeicherverlusteKwh,
                                                  d.Kennzahlen.SpeicherverlusteKwh));
            Assert.True(V7ReferenzTests.Bitgleich(a.Kennzahlen.NetzbezugMitSpeicherKwh,
                                                  d.Kennzahlen.NetzbezugMitSpeicherKwh));
            Assert.True(V7ReferenzTests.Bitgleich(a.Wirtschaftlichkeit.JahresueberschussEur,
                                                  d.Wirtschaftlichkeit.JahresueberschussEur));

            // Der Anker muss den Speicher wirklich bewegen.
            Assert.True(a.EntladeenergieKwh > 0.0, "keine Entladung - der Aequivalenzanker traegt nicht");
        }

        /// <summary>
        /// Gegenprobe: <b>Mit</b> freigegebenen Netzpfaden weicht die Arbitrage am
        /// Referenzjahrgang nachweislich ab - die Bitgleichheit oben ist eine
        /// Eigenschaft des netzpfadfreien Falls und kein Artefakt.
        /// </summary>
        [Fact]
        public void Mit_Netzpfaden_Weicht_Die_Arbitrage_Von_Der_Dauernutzung_Ab()
        {
            SpeicherEingang e = Referenzdaten.V7Eingang().MitVerguetungen(VPvCtKwh, VBhkwCtKwh);
            SpeicherParameter p = Referenzdaten.V7Parameter() with
            {
                RoundTripWirkungsgrad = 0.90,
                CVerEurProKwhZyklus = 0.025,
                Betriebsart = SpeicherBetriebsart.Graustrom
            };

            ArbitrageOptionen o = ArbitrageOptionen.Konstant(1.0, 500.0, e.Anzahl,
                                                             netzladung: true, netzentladung: true);

            ArbitrageErgebnis a = new Arbitrage(o).BerechneMitPlan(e, p);
            SpeicherErgebnis d = new Dauernutzung(SpeicherModus.Energetisch).Berechne(e, p);

            Assert.True(a.Kennzahlen.LadungNetzKwh > 0.0);
            Assert.True(a.Kennzahlen.VerkaufKwh > 0.0);
            Assert.False(V7ReferenzTests.Bitgleich(a.Ergebnis.SummeGeldwertEur, d.SummeGeldwertEur));
            Assert.Equal(0.0, a.Kennzahlen.AbweichungVomPlanKwh, 9);
        }

        // ==================================================================
        // 8. Synthetischer Graustrom-Jahreslauf im AP4-Spotreihenformat
        // ==================================================================

        /// <summary>
        /// End-zu-End ueber ein volles Jahr: eine 8.760-Stunden-Spotreihe im Format des
        /// AP4-Preismodells wird ueber <see cref="PreisModell.ZuViertelstunden"/> auf
        /// 35.040 Intervalle gebracht und als Netzladepreis <b>und</b> Erloes
        /// verwendet; der Bezugspreis traegt zusaetzlich den Aufschlag aus 4.2.
        /// </summary>
        /// <remarks>
        /// Geprueft wird das Muster, nicht ein Einzelwert: billig laden, teuer
        /// verkaufen (mittlerer Ladepreis deutlich unter mittlerem Verkaufserloes), der
        /// Fahrplan wird ungekuerzt gefahren, und die Energiebilanz schliesst.
        /// </remarks>
        [Fact]
        public void Graustrom_Jahreslauf_Laedt_Billig_Und_Verkauft_Teuer()
        {
            int n = RasterAdapter.ViertelstundenJahr;
            double[] spot = PreisModell.ZuViertelstunden(Tagesgang());
            double[] bezug = PreisModell.MitAufschlag(spot, 11.746);

            SpeicherEingang e = new SpeicherEingang(Null(n), Null(n), bezug)
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherParameter p = new SpeicherParameter
            {
                CNomKwh = 1000.0,
                PKw = 500.0,
                SoCMinKwh = 100.0,
                SoCMaxKwh = 900.0,
                RoundTripWirkungsgrad = 0.90,
                DtH = Dt,
                VerguetungCtKwh = VPvCtKwh,
                CCapEurProKwh = 400.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 20.0,
                CVerEurProKwhZyklus = 0.025,
                Betriebsart = SpeicherBetriebsart.Graustrom
            };

            ArbitrageOptionen o = new ArbitrageOptionen(spot, spot, true, true, 0.0,
                                                        ArbitrageOptionen.JahresbudgetDcKwh(10000.0, p.CNutzKwh, 20.0));

            ArbitrageErgebnis r = Rechne(e, p, o);

            Assert.True(r.Plan.PaareAngenommen > 300, "zu wenige Paarungen: " + r.Plan.PaareAngenommen);
            Assert.True(r.Kennzahlen.LadungNetzKwh > 0.0);
            Assert.True(r.Kennzahlen.NetzerloesEur > r.Kennzahlen.LadekostenEur,
                        "der Handel traegt sich nicht: Erloes " + r.Kennzahlen.NetzerloesEur +
                        " gegen Kosten " + r.Kennzahlen.LadekostenEur);

            double ladepreisMittel = 100.0 * r.Kennzahlen.LadekostenEur / r.Kennzahlen.LadungNetzKwh;
            double erloesMittel = 100.0 * r.Kennzahlen.NetzerloesEur / r.Kennzahlen.VerkaufKwh;
            Assert.True(erloesMittel - ladepreisMittel / p.RoundTripWirkungsgrad > r.Plan.VerschleissCtKwh,
                        "mittlerer Spread traegt k_ver nicht: " + erloesMittel + " gegen " + ladepreisMittel);

            // Der Plan wurde ungekuerzt gefahren, und das Band wurde nie verlassen.
            Assert.Equal(0.0, r.Kennzahlen.AbweichungVomPlanKwh, 6);
            BandEingehalten(r, p);
            BilanzSchliesst(r, p);
        }

        /// <summary>
        /// Derselbe Jahreslauf mit PV und Last: Der Eigenverbrauchsfluss laeuft weiter,
        /// die Netzpfade legen sich darueber. Geprueft werden Band, Plantreue und die
        /// Jahresbilanz.
        /// </summary>
        [Fact]
        public void Graustrom_Jahreslauf_Mit_Eigenverbrauch_Schliesst_Die_Bilanz()
        {
            int n = RasterAdapter.ViertelstundenJahr;
            double[] spot = PreisModell.ZuViertelstunden(Tagesgang());
            double[] bezug = PreisModell.MitAufschlag(spot, 11.746);

            double[] last = new double[n];
            double[] pv = new double[n];
            for (int i = 0; i < n; i++)
            {
                int stunde = (i / 4) % 24;
                last[i] = 120.0 + 60.0 * (stunde >= 7 && stunde < 19 ? 1.0 : 0.0);
                pv[i] = stunde >= 9 && stunde < 16 ? 400.0 : 0.0;
            }

            SpeicherEingang e = new SpeicherEingang(last, pv, bezug).MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherParameter p = new SpeicherParameter
            {
                CNomKwh = 1000.0,
                PKw = 500.0,
                SoCMinKwh = 100.0,
                SoCMaxKwh = 900.0,
                RoundTripWirkungsgrad = 0.90,
                DtH = Dt,
                VerguetungCtKwh = VPvCtKwh,
                CCapEurProKwh = 400.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 20.0,
                CVerEurProKwhZyklus = 0.025,
                Betriebsart = SpeicherBetriebsart.Graustrom
            };

            ArbitrageErgebnis r = Rechne(e, p, new ArbitrageOptionen(spot, spot, true, true));

            Assert.Equal(0.0, r.Kennzahlen.AbweichungVomPlanKwh, 6);
            Assert.True(r.Ergebnis.EntladeenergieKwh > 0.0, "kein Eigenverbrauchsfluss - der Fall traegt nicht");
            BandEingehalten(r, p);
            BilanzSchliesst(r, p);

            // Die vier Geldsummanden der Bewertungszeile 6.2 ergeben wieder Sigma F.
            double summe = r.Kennzahlen.BezugsersparnisEur
                           - r.Kennzahlen.EntgangeneVerguetungEur
                           - r.Kennzahlen.LadekostenEur
                           + r.Kennzahlen.NetzerloesEur;
            Assert.Equal(r.Ergebnis.SummeGeldwertEur, summe, 6);
        }

        // ==================================================================
        // 9. Vertrag, Abgrenzung, Determinismus
        // ==================================================================

        /// <summary>
        /// Der Excel-Kompatibilitaetsmodus wird abgelehnt: Die Arbitragelogik der
        /// V7-Mappe war nicht ausfuehrbar, es gibt also keine Referenz (Fachkonzept 6.5).
        /// </summary>
        [Fact]
        public void Excel_Kompatibilitaetsmodus_Wird_Abgelehnt()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            var strategie = new Arbitrage(null, SpeicherModus.ExcelKompatibilitaet);

            Assert.Equal(SpeicherModus.ExcelKompatibilitaet, strategie.Modus);
            Assert.Equal("Arbitrage", strategie.Name);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => strategie.Berechne(e, MiniParameter()));
            Assert.Equal(Arbitrage.SchluesselOhneExcelReferenz, ex.Message);
            Assert.Equal("ARB_OHNE_EXCEL_REFERENZ", Arbitrage.SchluesselOhneExcelReferenz);
        }

        /// <summary>Fehlende oder unpassende Angaben werden abgewiesen, bevor gerechnet wird.</summary>
        [Fact]
        public void Fehlende_Pflichtangaben_Werden_Abgewiesen()
        {
            SpeicherEingang e = Eingang(Null(4), Null(4));
            var strategie = new Arbitrage(Optionen(Null(4), Null(4)));

            Assert.Throws<ArgumentNullException>(() => strategie.Berechne(null!, MiniParameter()));
            Assert.Throws<ArgumentNullException>(() => strategie.Berechne(e, null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => strategie.Berechne(e, MiniParameter() with { DtH = 0.0 }));

            // Reihenlaenge der Optionen passt nicht zum Eingang.
            Assert.Throws<ArgumentException>(
                () => new Arbitrage(Optionen(Null(3), Null(3))).Berechne(e, MiniParameter()));

            Assert.Throws<ArgumentException>(() => new ArbitrageOptionen(Null(4), Null(3)));
            Assert.Throws<ArgumentNullException>(() => new ArbitrageOptionen(null!, Null(3)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ArbitrageOptionen(Null(4), Null(4), fensterIntervalle: 0));
        }

        /// <summary>
        /// Planer und Strategie sind zustandsfrei: Zwei Laeufe derselben Instanzen auf
        /// demselben Eingang liefern bitgleiche Ergebnisse (Voraussetzung fuer
        /// <c>Parallel.For</c>, Fachkonzept 8.1).
        /// </summary>
        [Fact]
        public void Wiederholter_Lauf_Liefert_Bitgleiches_Ergebnis()
        {
            int n = 2688;
            double[] spot = new double[n];
            for (int i = 0; i < n; i++) spot[i] = 10.0 + 25.0 * Math.Sin(i * 0.13) + 5.0 * Math.Cos(i * 0.031);

            SpeicherEingang e = new SpeicherEingang(Null(n), Null(n),
                                                    PreisModell.MitAufschlag(spot, 11.746))
                .MitVerguetungen(VPvCtKwh, VBhkwCtKwh);

            SpeicherParameter p = MiniParameter(0.81) with { CNomKwh = 10.0, PKw = 10.0 };
            ArbitrageOptionen o = new ArbitrageOptionen(spot, spot, true, true);

            var strategie = new Arbitrage(o);
            ArbitrageErgebnis a = strategie.BerechneMitPlan(e, p);
            ArbitrageErgebnis b = strategie.BerechneMitPlan(e, p);

            Assert.True(a.Plan.PaareAngenommen > 0, "der Determinismustest traegt nur mit Netzpfaden");
            Assert.True(V7ReferenzTests.Bitgleich(a.Ergebnis.SummeGeldwertEur, b.Ergebnis.SummeGeldwertEur));
            for (int k = 0; k < n; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(a.Ergebnis.SoCKwh[k], b.Ergebnis.SoCKwh[k]));
                Assert.True(V7ReferenzTests.Bitgleich(a.LadungNetzAcKwh[k], b.LadungNetzAcKwh[k]));
                Assert.True(V7ReferenzTests.Bitgleich(a.VerkaufAcKwh[k], b.VerkaufAcKwh[k]));
            }
        }

        /// <summary>
        /// Der Planer liefert fuer denselben Eingang denselben Plan - unabhaengig davon,
        /// ob er ueber die Strategie oder direkt aufgerufen wird.
        /// </summary>
        [Fact]
        public void Planer_Und_Strategie_Fahren_Denselben_Plan()
        {
            SpeicherEingang e = Eingang(Null(8), Null(8));
            SpeicherParameter p = MiniParameter(0.81);
            ArbitrageOptionen o = Optionen(new double[] { 1.0, 99.0, 99.0, 99.0, 1.0, 99.0, 99.0, 99.0 },
                                           new double[] { 0.0, 0.0, 0.0, 50.0, 0.0, 0.0, 0.0, 50.0 },
                                           fenster: 4);

            ArbitragePlan direkt = new ArbitragePlaner().Plane(e, p, o);
            ArbitrageErgebnis ueberStrategie = Rechne(e, p, o);

            for (int k = 0; k < e.Anzahl; k++)
            {
                Assert.Equal(direkt.NetzladungAcKwh[k], ueberStrategie.Plan.NetzladungAcKwh[k], 12);
                Assert.Equal(direkt.NetzladungAcKwh[k], ueberStrategie.LadungNetzAcKwh[k], 12);
                Assert.Equal(direkt.VerkaufAcKwh[k], ueberStrategie.VerkaufAcKwh[k], 12);
            }
        }

        // ==================================================================
        // Hilfsmittel
        // ==================================================================

        /// <summary>
        /// Synthetische Spotreihe im AP4-Format: 8.760 Stundenwerte mit billiger Nacht
        /// (2 ct), teurem Abend (40 ct) und einem Mittelband (12 ct). Die Werte sind
        /// bewusst grob - geprueft wird das Verhalten des Planers, nicht die Preisreihe.
        /// </summary>
        private static double[] Tagesgang()
        {
            double[] stunden = new double[RasterAdapter.StundenJahr];
            for (int h = 0; h < stunden.Length; h++)
            {
                int tagesstunde = h % 24;
                if (tagesstunde >= 1 && tagesstunde < 5) stunden[h] = 2.0;
                else if (tagesstunde >= 18 && tagesstunde < 21) stunden[h] = 40.0;
                else stunden[h] = 12.0;
            }
            return stunden;
        }

        /// <summary>Der Ladezustand verlaesst das Band in keinem Intervall.</summary>
        private static void BandEingehalten(ArbitrageErgebnis r, SpeicherParameter p)
        {
            double toleranz = 1e-9 * Math.Max(1.0, p.CNutzKwh);
            double[] soc = r.Ergebnis.SoCKwh;
            for (int k = 0; k < soc.Length; k++)
            {
                Assert.True(soc[k] >= p.SoCMinKwh - toleranz, "SoC unter dem Band in Intervall " + k);
                Assert.True(soc[k] <= p.SoCMaxKwh + toleranz, "SoC ueber dem Band in Intervall " + k);
            }
        }

        /// <summary>
        /// Jahresbilanz einschliesslich Netzpfade:
        /// <c>Erzeugung + Netzbezug = Last + Einspeisung + Verluste + SoC-Aenderung</c>.
        /// </summary>
        private static void BilanzSchliesst(ArbitrageErgebnis r, SpeicherParameter p)
        {
            SpeicherKennzahlen k = r.Ergebnis.Kennzahlen;
            double socEnde = r.Ergebnis.SoCKwh[r.Ergebnis.Anzahl - 1];
            double socStart = p.StartSoCEffektivKwh;

            double links = k.ErzeugungKwh + k.NetzbezugMitSpeicherKwh;
            double rechts = k.LastKwh + k.EinspeisungMitSpeicherKwh + k.SpeicherverlusteKwh
                            + (socEnde - socStart);

            Assert.Equal(links, rechts, 6);
        }

        /// <summary>
        /// Vergleicht zwei Intervallreihen elementweise auf 12 Nachkommastellen -
        /// <c>Assert.Equal</c> kennt fuer Sammlungen keine Stellenangabe.
        /// </summary>
        private static void ReihenGleich(double[] soll, double[] ist)
        {
            Assert.Equal(soll.Length, ist.Length);
            for (int k = 0; k < soll.Length; k++) Assert.Equal(soll[k], ist[k], 12);
        }
    }
}
