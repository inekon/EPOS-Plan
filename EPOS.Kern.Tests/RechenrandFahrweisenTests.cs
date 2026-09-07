using System;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der Anwenderentscheide W8‑O‑5d‑Q3 und ‑Q4</b> vom 07.09.2026
    /// („Empfehlung", das Nachziehen zu Q1): Der Zahlenrand <see cref="Rechenrand"/>
    /// liegt seither an <b>allen</b> Betriebsschwellen der drei BHKW‑Fahrweisen und an
    /// der Reservemarke des Pufferspeichers — nicht nur an den zwei Stellen, an denen
    /// die Umstellung auf <c>double</c> ihn erzwungen hatte.
    ///
    /// <para><b>Warum diese Proben synthetisch sind und nicht über die Testdatenbank
    /// laufen.</b> <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> führt in
    /// <c>Tab_Einstellungen.Betriebsart</c> für <b>jedes</b> der einundzwanzig Projekte
    /// den Wert 0 oder <c>NULL</c> — alle fahren WÄRMEGEFÜHRT
    /// (<c>SimulationBHKW.Fahrweise_Stunde</c>: 1 = stromgeführt, 2 = ohne Einspeisung,
    /// sonst wärmegeführt). Die zwei anderen Fahrweisen sind über die Datenbank also gar
    /// nicht erreichbar; deshalb geht jede Probe hier unmittelbar an den Stundenschritt,
    /// mit Zahlen, die die Grenze auf das letzte Bit treffen. Genau darum bleibt der
    /// Referenzlauf über die zwölf Projekte auch byte‑gleich: Er berührt keine der hier
    /// geprüften Weichen.</para>
    ///
    /// <para><b>Die Bauform jeder Probe</b> ist die von <see cref="RechenrandTests"/>:
    /// ein Stand GENAU auf der Grenze und einer um ein ulp darunter
    /// (<c>Math.BitDecrement</c>) müssen dieselbe Entscheidung liefern, und eine
    /// GEGENPROBE hält fest, dass der blanke Vergleich auf denselben zwei Zahlen
    /// verschieden ausfällt — ohne sie prüfte der Fall nur, dass zweimal dasselbe
    /// herauskommt, nicht dass der Rand es bewirkt.</para>
    ///
    /// <para><b>Die Leserichtung ist überall dieselbe</b> (Muster Q1): SCHWELLE ist die
    /// Maschinengröße — Nennleistung, Modulationsgrenze oder die aus ihr abgeleitete
    /// Ausbeute —, WERT der Rest, der ihr gegenübersteht (Wärmeraum, Reststrom). Damit
    /// entscheiden die drei Fahrweisen nach EINEM Maß.</para>
    /// </summary>
    public class RechenrandFahrweisenTests
    {
        // Krumme Kennzahlen eines Moduls: 19 kW thermisch, 9,5 kW elektrisch, 30 %
        // Mindestlast. P_th · x = 5,7 und P_el · x = 2,85 sind in Gleitkomma beide
        // KRUMM — an einer runden Zahl träte die Frage gar nicht erst auf.
        private const double P_TH = 19.0;
        private const double P_EL = 9.5;
        private const double X_MIN = 0.3;

        // =====================================================================
        //  1 — Stromgeführte Fahrweise (modeBHKW = 1)
        // =====================================================================

        /// <summary>
        /// <b>Volllast gegen Modulation, Stromseite.</b> Ein Reststrombedarf genau auf
        /// der elektrischen Nennleistung und einer um ein ulp darunter führen beide auf
        /// VOLLLAST — das Modul bucht <c>P_el</c>, nicht den um ein ulp kleineren Rest.
        ///
        /// <para>Der Fall Gleichheit ist BITGLEICH: Der Modulationszweig rechnet dort
        /// <c>restStrom / P_el == 1,0</c> und daraus dieselben zwei Zahlen.</para>
        /// </summary>
        [Fact]
        public void Stromgefuehrt_Volllastgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            Assert.Equal(P_EL, StromgefuehrteStunde(restStrom: P_EL).Strom);
            Assert.Equal(P_EL, StromgefuehrteStunde(restStrom: Math.BitDecrement(P_EL)).Strom);

            // GEGENPROBE: Ohne den Rand haette der zweite Lauf moduliert und den um ein
            // ulp kleineren Reststrom gebucht.
            Assert.False(P_EL < Math.BitDecrement(P_EL));
        }

        /// <summary>
        /// <b>Modulation gegen „Motor bleibt aus", Stromseite.</b> Hier ist der
        /// Unterschied nicht ein ulp, sondern alles: über der Grenze moduliert das Modul,
        /// darunter steht es still.
        /// </summary>
        [Fact]
        public void Stromgefuehrt_Modulationsgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            double untergrenze = P_EL * X_MIN;
            double knapp = Math.BitDecrement(untergrenze);

            Assert.Equal(untergrenze, StromgefuehrteStunde(untergrenze).Strom);
            Assert.Equal(knapp, StromgefuehrteStunde(knapp).Strom);

            // GEGENPROBE: Ohne den Rand stuende das Modul im zweiten Fall still (0 kW).
            Assert.False(untergrenze <= knapp);
        }

        // =====================================================================
        //  2 — Fahrweise ohne Einspeisung (modeBHKW = 2), wärmeseitige Zuschaltung
        // =====================================================================

        /// <summary>
        /// <b>W1 gegen W2 — die Volllastgrenze der Wärmeseite.</b> Der Vergleich ist
        /// wortgleich dem der wärmegeführten Fahrweise (Q1) und wird deshalb genauso
        /// gelesen. Bei Gleichheit rechnet W2 den Faktor <c>Raum / P_th == 1,0</c> und
        /// bucht dasselbe; ein ulp darunter tut es das ohne den Rand nicht mehr.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_W1_Volllastgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            Assert.Equal(P_TH, OhneEinspeisung(restWaerme: P_TH, restStrom: 1000).Waerme);
            Assert.Equal(P_TH, OhneEinspeisung(restWaerme: Math.BitDecrement(P_TH), restStrom: 1000).Waerme);

            Assert.False(P_TH < Math.BitDecrement(P_TH));
        }

        /// <summary>
        /// <b>Die innere Stromweiche der Zero‑Export‑Bedingung, obere Stufe.</b> Der
        /// Wärmeraum ist reichlich (50 kWh), entschieden wird an der elektrischen
        /// Nennleistung: Auf der Grenze und ein ulp darunter läuft das Modul auf
        /// VOLLLAST.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_W1_Stromvolllast_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            Assert.Equal(P_EL, OhneEinspeisung(restWaerme: 50, restStrom: P_EL).Strom);
            Assert.Equal(P_EL, OhneEinspeisung(restWaerme: 50, restStrom: Math.BitDecrement(P_EL)).Strom);

            Assert.False(P_EL < Math.BitDecrement(P_EL));
        }

        /// <summary>
        /// <b>Die innere Stromweiche, untere Stufe — und die eine Stelle, an der der Rand
        /// die GLEICHHEIT bewegt.</b> Der Vergleich war als einziger dieser Weiche streng
        /// (<c>P_el · x_min &lt; restStrom</c>): Ein Reststrom GENAU auf der Mindestlast
        /// ließ das Modul stehen, ein ulp darüber ließ es laufen — die Entscheidung hing
        /// am letzten Bit. Mit dem Rand läuft es in beiden Fällen, und das ist die Seite,
        /// auf der die vier übrigen Modulationsgrenzen des Moduls ohnehin liegen (dort
        /// steht <c>&lt;=</c>). Fachlich trägt sie: Bei
        /// <c>restStrom == P_el · x_min</c> deckt die Mindestlast den Bedarf GENAU, es
        /// wird nichts eingespeist.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_W1_Strommodulation_laeuft_auf_der_Grenze_und_ein_ulp_darunter()
        {
            double untergrenze = P_EL * X_MIN;
            double knapp = Math.BitDecrement(untergrenze);

            Assert.Equal(untergrenze, OhneEinspeisung(restWaerme: 50, restStrom: untergrenze).Strom);
            Assert.Equal(knapp, OhneEinspeisung(restWaerme: 50, restStrom: knapp).Strom);

            // GEGENPROBE: Der blanke STRENGE Vergleich entscheidet innerhalb eines ulp
            // verschieden - genau das ist der Befund, den der Rand abstellt.
            Assert.False(untergrenze > P_EL * X_MIN);
            Assert.True(Math.BitIncrement(untergrenze) > P_EL * X_MIN);
        }

        /// <summary>
        /// <b>W2 gegen „Modul bleibt aus" — die Modulationsgrenze der Wärmeseite.</b>
        /// Über der Grenze moduliert das Modul auf den Wärmeraum, darunter produziert es
        /// nichts.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_W2_Modulationsgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            double untergrenze = P_TH * X_MIN;
            double knapp = Math.BitDecrement(untergrenze);

            Assert.Equal(untergrenze, OhneEinspeisung(restWaerme: untergrenze, restStrom: 1000).Waerme);
            Assert.Equal(knapp, OhneEinspeisung(restWaerme: knapp, restStrom: 1000).Waerme);

            Assert.False(untergrenze <= knapp);
        }

        /// <summary>
        /// <b>Die Ausbeute des modulierten Laufs gegen den Reststrom (W2).</b> Der
        /// Wärmeraum liegt zwischen Mindestlast und Nennleistung, das Modul moduliert;
        /// gefragt ist, ob der Reststrom trägt, was dabei elektrisch anfällt. Auf der
        /// Grenze und ein ulp darunter bucht es den Wärmeraum ungeschmälert.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_W2_Ausbeutevergleich_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            const double raum = 10.0;
            double ausbeute = raum / P_TH * P_EL;      // dieselbe Rechnung wie im Kern

            Assert.Equal(raum, OhneEinspeisung(restWaerme: raum, restStrom: ausbeute).Waerme);
            Assert.Equal(raum, OhneEinspeisung(restWaerme: raum, restStrom: Math.BitDecrement(ausbeute)).Waerme);

            Assert.False(ausbeute < Math.BitDecrement(ausbeute));
        }

        // =====================================================================
        //  3 — Fahrweise ohne Einspeisung, stromseitige Zuschaltung (S1…S3)
        // =====================================================================
        //
        // Die zweite Schleife ist nur erreichbar, wenn die erste den Reststrom NICHT
        // aufgebraucht hat - der Bestand darf denselben Motor in beiden Durchläufen
        // zuschalten. Alle drei Proben nutzen denselben Aufbau: Der Wärmebedarf ist
        // GENAU P_th, sodass die erste Schleife ihn restlos deckt (restWaerme wird 0,
        // der Speicherstand bleibt unberührt) und der freie Speicherraum unverändert in
        // die zweite geht. Damit ist der Wärmeraum der zweiten Schleife exakt die
        // Kapazität - eine Zahl, die die Probe auf das letzte Bit setzen kann.

        /// <summary>
        /// <b>S1 — Volllast gegen die thermische Grenze.</b> Der Vergleich „trägt der
        /// Wärmeraum die Nennwärmeleistung?" ist wortgleich dem in W1. Auf der Grenze
        /// rechnet der sonst greifende Zweig S3 dasselbe (Faktor <c>Raum / P_th == 1,0</c>);
        /// ein ulp darunter bucht er ohne den Rand den kleineren Raum.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_S1_Waermegrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            Assert.Equal(2 * P_TH, ZweiteSchleife(raum: P_TH, restStromNachW1: P_EL).Waerme);
            Assert.Equal(2 * P_TH, ZweiteSchleife(Math.BitDecrement(P_TH), P_EL).Waerme);

            Assert.False(Math.BitDecrement(P_TH) > P_TH);
        }

        /// <summary>
        /// <b>S2 — Teillast auf den Reststrom, gegen die thermische Grenze.</b> Der
        /// Reststrom liegt unter der Nennleistung (S1 fällt aus), und gefragt ist, ob der
        /// Wärmeraum die anteilige Ausbeute trägt. Gebucht wird in beiden Fällen die
        /// AUSBEUTE, nicht der Raum — ohne den Rand fiele der zweite Lauf auf S3 und
        /// buchte den um ein ulp kleineren Raum.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_S2_Ausbeutegrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            const double restStrom = 5.0;
            double ausbeute = restStrom / P_EL * P_TH;

            Assert.Equal(P_TH + ausbeute, ZweiteSchleife(raum: ausbeute, restStromNachW1: restStrom).Waerme);
            Assert.Equal(P_TH + ausbeute, ZweiteSchleife(Math.BitDecrement(ausbeute), restStrom).Waerme);

            Assert.False(Math.BitDecrement(ausbeute) > ausbeute);
        }

        /// <summary>
        /// <b>S3 — Teillast bis zur thermischen Speichergrenze.</b> Die letzte Stufe der
        /// Weiche, und wie in W1 eine mit STRENGEM Vergleich: Ein Wärmeraum genau auf der
        /// Modulationsgrenze ließ das Modul stehen. Mit dem Rand läuft es auf der Grenze
        /// und ein ulp darunter — dieselbe Seite, auf der die wärmeseitige Stufe W2 mit
        /// ihrem <c>&lt;=</c> längst steht.
        /// </summary>
        [Fact]
        public void OhneEinspeisung_S3_Modulationsgrenze_laeuft_auf_der_Grenze_und_ein_ulp_darunter()
        {
            double untergrenze = P_TH * X_MIN;
            double knapp = Math.BitDecrement(untergrenze);

            Assert.Equal(P_TH + untergrenze, ZweiteSchleife(raum: untergrenze, restStromNachW1: 5.0).Waerme);
            Assert.Equal(P_TH + knapp, ZweiteSchleife(knapp, 5.0).Waerme);

            // GEGENPROBE: Der blanke strenge Vergleich laesst das Modul auf der Grenze
            // stehen und ein ulp darueber laufen.
            Assert.False(untergrenze > P_TH * X_MIN);
            Assert.True(Math.BitIncrement(untergrenze) > P_TH * X_MIN);
        }

        // =====================================================================
        //  4 — Die Reservemarke des Pufferspeichers (W8‑O‑5d‑Q4)
        // =====================================================================

        /// <summary>
        /// <b>Ohne BHKW‑Bezug bleibt alles, wie es war.</b> Der Rand darf da nie greifen,
        /// wo die Methode gar nicht rechnet: Ohne Ladeauftrag eines BHKW, ohne gepflegte
        /// Reserve und ohne Kapazität liefert sie <c>double.MaxValue</c> — auch mit einem
        /// Füllstand genau auf der Marke.
        /// </summary>
        [Fact]
        public void Die_Entnahmeobergrenze_bleibt_ohne_Reserve_unbegrenzt()
        {
            SimulationPufferspeicher ohneBhkw = Speicher();
            ohneBhkw.BhkwReserveGilt = false;
            ohneBhkw.SOC = ohneBhkw.Q_max * ohneBhkw.SchwelleReserve;
            Assert.Equal(double.MaxValue, ohneBhkw.EntnahmeObergrenze());

            SimulationPufferspeicher ohneReserve = Speicher();
            ohneReserve.SchwelleReserve = 0;
            ohneReserve.SOC = 500;
            Assert.Equal(double.MaxValue, ohneReserve.EntnahmeObergrenze());

            SimulationPufferspeicher ohneKapazitaet = Speicher();
            ohneKapazitaet.Q_max = 0;
            ohneKapazitaet.SOC = 500;
            Assert.Equal(double.MaxValue, ohneKapazitaet.EntnahmeObergrenze());
        }

        /// <summary>
        /// <b>Die Reservemarke selbst.</b> Ein Füllstand genau auf der Marke und einer um
        /// ein ulp DARÜBER geben beide nichts mehr frei — die Marke wird von oben
        /// erreicht, deshalb liegt der Rand hier auf der oberen Seite.
        /// </summary>
        [Fact]
        public void Die_Reservemarke_gibt_auf_der_Grenze_und_ein_ulp_darueber_nichts_mehr_frei()
        {
            SimulationPufferspeicher sp = Speicher();
            double marke = sp.Q_max * sp.SchwelleReserve;

            sp.SOC = marke;
            Assert.Equal(0.0, sp.EntnahmeObergrenze());

            double einUlpDarueber = Math.BitIncrement(marke);
            sp.SOC = einUlpDarueber;
            Assert.Equal(0.0, sp.EntnahmeObergrenze());

            // GEGENPROBE: Der blanke Vergleich gaebe den Rest von einem ulp frei - und
            // der Speicher entlaede in der naechsten Stunde ein Milliardstel kWh, statt
            // in den Nachladebetrieb zu gehen.
            Assert.True(einUlpDarueber - marke > 0);
            Assert.True(einUlpDarueber - marke < 1e-12);

            // Und der Vorrat OBERHALB der Reserve bleibt unangetastet: eine kWh ueber der
            // Marke ist eine kWh entnehmbar.
            sp.SOC = marke + 1.0;
            Assert.Equal(1.0, sp.EntnahmeObergrenze(), 9);
        }

        /// <summary>
        /// <b>Die Probe über den Rechenweg des Laufs</b> — das Gegenstück zur
        /// Bistabilitätsprobe der Hysterese. Der Speicher wird um GENAU den Betrag
        /// entladen, den <c>EntnahmeObergrenze</c> freigibt (so klemmt
        /// <c>Kaskadenschleife.EntladeKanal</c> den Bedarf), und genau da entsteht der
        /// Fehltritt: In Gleitkomma ist <c>a − (a − b)</c> nicht bitgleich <c>b</c>.
        /// Danach muss die Marke über mehrere Stunden als erreicht gelten.
        /// </summary>
        [Fact]
        public void Nach_dem_Entladen_auf_die_Marke_bleibt_die_Reserve_ueber_Stunden_erreicht()
        {
            SimulationPufferspeicher sp = Speicher();
            double marke = sp.Q_max * sp.SchwelleReserve;
            sp.SOC = 500.123;                       // ein krummer Anfangsstand

            double freigabe = sp.EntnahmeObergrenze();
            Assert.True(freigabe > 0);
            Assert.Equal(freigabe, sp.Entladen(freigabe, 0), 9);

            // GEGENPROBE: Der Fuellstand hat seinen Zielwert im letzten Bit VERFEHLT -
            // ohne den Rand meldete die Methode diesen Rest jede Stunde erneut.
            Assert.True(sp.SOC != marke);
            Assert.True(sp.SOC - marke > 0);
            Assert.True(sp.SOC - marke < 1e-12);

            for (int stunde = 1; stunde < 6; stunde++)
                Assert.Equal(0.0, sp.EntnahmeObergrenze());
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        /// <summary>
        /// Ein Speicher mit krummer Kapazität und 10 % Notreserve, im Bilanzraum eines
        /// BHKW (sonst wäre die Reserve unwirksam).
        /// </summary>
        private static SimulationPufferspeicher Speicher()
        {
            return new SimulationPufferspeicher
            {
                Q_max = 1234.5678,
                SchwelleEin = 0.10,
                SchwelleAus = 0.95,
                SchwelleReserve = 0.10,
                BhkwReserveGilt = true,
                SOC = 0
            };
        }

        /// <summary>Was eine Stunde EINES Moduls hinterlassen hat.</summary>
        private struct Stundenspur
        {
            public double Waerme;   // s_waerme[0]
            public double Strom;    // s_strom[0]
        }

        /// <summary>
        /// Fährt <c>Motorlauf_Stromgefuehrt</c> für EIN Modul. Der Wärmebedarf ist
        /// reichlich und spielt keine Rolle — die Fahrweise folgt dem Strom.
        ///
        /// <para>Der Aufruf geht über Reflexion, weil die Methode <c>private</c> ist; der
        /// öffentliche Weg führte über <c>Vorbereiten_Zweikanalig</c> und damit über die
        /// Datenbank, und er ließe den Reststrom nicht auf das letzte Bit setzen.</para>
        /// </summary>
        private static Stundenspur StromgefuehrteStunde(double restStrom)
        {
            object[] args =
            {
                0, 1,
                new double[1], new double[1],           // stromproduktion, waermeproduktion
                new double[1], new double[1],           // s_waerme, s_strom
                new[] { P_TH }, new[] { P_EL },
                X_MIN,
                restStrom,
                1000.0                                  // restWaerme: reichlich
            };

            Rufen("Motorlauf_Stromgefuehrt", args);
            return new Stundenspur { Waerme = ((double[])args[4])[0], Strom = ((double[])args[5])[0] };
        }

        /// <summary>
        /// Fährt <c>Motorlauf_OhneEinspeisung</c> für EIN Modul mit VOLLEM Pendelspeicher
        /// — <c>restSpeicher = 0</c>, damit der Wärmeraum GENAU der übergebene Restbedarf
        /// ist. Nur so lässt sich die Grenze auf ein ulp genau treffen; die zweite
        /// (stromseitige) Schleife bleibt dabei ohne Wärmeraum und damit wirkungslos.
        /// </summary>
        private static Stundenspur OhneEinspeisung(double restWaerme, double restStrom)
        {
            const double kapazitaet = 100.0;
            object[] args =
            {
                0, 1,
                new double[1], new double[1],
                new double[1], new double[1],
                new[] { P_TH }, new[] { P_EL },
                X_MIN,
                kapazitaet,
                kapazitaet,                              // speicher = voll => restSpeicher = 0
                restWaerme,
                restStrom
            };

            Rufen("Motorlauf_OhneEinspeisung", args);
            return new Stundenspur { Waerme = ((double[])args[4])[0], Strom = ((double[])args[5])[0] };
        }

        /// <summary>
        /// Fährt <c>Motorlauf_OhneEinspeisung</c> so, dass die ZWEITE (stromseitige)
        /// Schleife entscheidet.
        ///
        /// <para>Der Wärmebedarf ist genau <c>P_th</c>: Die erste Schleife schaltet das
        /// Modul auf Volllast (der Reststrom trägt es), deckt den Bedarf restlos und
        /// lässt den Speicherstand unberührt — <c>restWaerme</c> wird exakt 0. Der freie
        /// Speicherraum geht damit unverändert in die zweite Schleife, und ihr Wärmeraum
        /// ist auf das letzte Bit genau <paramref name="raum"/>.</para>
        /// </summary>
        /// <param name="raum">Wärmeraum, den die zweite Schleife vorfindet [kWh].</param>
        /// <param name="restStromNachW1">
        /// Reststrom, der der zweiten Schleife bleibt [kWh] — übergeben wird
        /// <c>P_el + dieser Wert</c>, weil die erste Schleife <c>P_el</c> verbraucht.
        /// </param>
        private static Stundenspur ZweiteSchleife(double raum, double restStromNachW1)
        {
            object[] args =
            {
                0, 1,
                new double[1], new double[1],
                new double[1], new double[1],
                new[] { P_TH }, new[] { P_EL },
                X_MIN,
                raum,                                    // Kapazität …
                0.0,                                     // … bei leerem Speicher => restSpeicher = raum
                P_TH,                                    // restWaerme: genau eine Volllaststunde
                P_EL + restStromNachW1
            };

            Rufen("Motorlauf_OhneEinspeisung", args);
            return new Stundenspur { Waerme = ((double[])args[4])[0], Strom = ((double[])args[5])[0] };
        }

        /// <summary>Ruft einen der privaten Stundenschritte über Reflexion.</summary>
        private static void Rufen(string name, object[] args)
        {
            MethodInfo m = typeof(SimulationBHKW).GetMethod(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);

            m.Invoke(new SimulationBHKW(), args);
        }
    }
}
