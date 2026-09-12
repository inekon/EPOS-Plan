using System;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids W8‑O‑5d‑Q1</b> vom 07.09.2026
    /// („Empfehlung"): Die zwei Betriebsschwellen, die bei der Umstellung auf
    /// <c>double</c> am letzten Bit entschieden, tragen seither den Zahlenrand
    /// <see cref="Rechenrand"/>.
    ///
    /// <para><b>Was hier geprüft wird — und wie.</b> Je Schwelle zwei Stände: einer
    /// GENAU auf der Grenze, einer um EIN ulp darunter (<c>Math.BitDecrement</c>). Beide
    /// müssen dieselbe Entscheidung liefern. Jeder Fall führt die Gegenprobe mit: Der
    /// blanke Vergleich <c>&gt;=</c> auf denselben zwei Zahlen fällt verschieden aus —
    /// ohne diesen Nachweis prüften die Fälle nur, dass zweimal dasselbe herauskommt,
    /// nicht dass der Rand es bewirkt.</para>
    ///
    /// <para><b>Die dritte Schwelle fehlt hier mit Absicht.</b> Sie waren zu dritt (siehe
    /// <c>protokoll.txt</c> der Basis <c>2026-09-07_R4_Double</c>); die dritte — die drei
    /// <c>int</c>-Rückgaben des BHKW-Plan-Ports — ist mit W8‑O‑5d‑Q2 ganz gefallen und
    /// steht in <see cref="BhkwPlanRueckgabeTests"/>.</para>
    ///
    /// <para>Ohne Datenbank: Beide Rechenwege werden mit gesetzten Feldern bzw. über
    /// Argumente gefahren.</para>
    /// </summary>
    public class RechenrandTests
    {
        // =====================================================================
        //  1 — Der Rand selbst
        // =====================================================================

        /// <summary>
        /// Der Rand ist immer positiv, an einer Schwelle von 0 genau
        /// <see cref="Rechenrand.ABSOLUT"/> und wächst mit der Schwelle. Ohne den
        /// absoluten Anteil verschwände er bei 0, ohne den relativen wäre er an großen
        /// Schwellen zu knapp.
        /// </summary>
        [Fact]
        public void Der_Rand_hat_eine_Untergrenze_und_waechst_mit_der_Schwelle()
        {
            Assert.Equal(Rechenrand.ABSOLUT, Rechenrand.Zu(0));
            Assert.Equal(Rechenrand.ABSOLUT, Rechenrand.Zu(-0.0));

            Assert.True(Rechenrand.Zu(100_000) > Rechenrand.Zu(10));
            Assert.True(Rechenrand.Zu(-100_000) > Rechenrand.Zu(10));   // Betrag zählt

            // 100 000 kWh: 1e-9 + 1e-12 * 1e5 = 1,01e-7 kWh — immer noch das
            // Hunderttausendfache eines ulp (1,5e-11) und zugleich winzig gegen die
            // absolute Vergleichstoleranz der Referenzsuite (0,01).
            Assert.True(Rechenrand.Zu(100_000) < 1e-6);
        }

        /// <summary>
        /// Der Rand greift dort, wo er soll — und nur dort: Gleichheit und ein ulp
        /// darunter zählen als erreicht, eine Abweichung, die man in einer Referenz-CSV
        /// überhaupt sähe, nicht mehr.
        /// </summary>
        [Fact]
        public void Der_Rand_deckt_ein_ulp_und_bleibt_unter_jeder_sichtbaren_Groesse()
        {
            const double schwelle = 1234.5678;

            Assert.True(Rechenrand.SchwelleErreicht(schwelle, schwelle));
            Assert.True(Rechenrand.SchwelleErreicht(Math.BitDecrement(schwelle), schwelle));
            Assert.True(Rechenrand.SchwelleErreicht(schwelle + 1000, schwelle));

            // 1e-7 kWh liegt bereits ausserhalb des Rands (1e-9 + 1,2e-9) - und ist
            // selbst noch 0,36 Millijoule.
            Assert.False(Rechenrand.SchwelleErreicht(schwelle - 1e-7, schwelle));
            Assert.False(Rechenrand.SchwelleErreicht(schwelle - 0.01, schwelle));
        }

        // =====================================================================
        //  2 — Die Speicherhysterese
        // =====================================================================

        /// <summary>
        /// <b>Die Abschaltschwelle des Speichers.</b> Ein Füllstand GENAU auf
        /// <c>Q_max · SchwelleAus</c> und einer um ein ulp darunter beenden beide die
        /// Nachladung. Vor W8‑O‑5d‑Q1 tat das nur der erste — und weil die Hysterese
        /// bistabil ist, trug der zweite seinen Fehltritt über Stunden weiter.
        /// </summary>
        [Fact]
        public void Die_Abschaltschwelle_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            double grenze = Speicher().Q_max * 0.95;

            SimulationPufferspeicher genau = Speicher();
            genau.SOC = grenze;
            Assert.True(genau.HystereseFortschreiben(), "Auf der Grenze muss die Nachladung enden.");
            Assert.False(genau.LaedtGerade);

            double einUlpDarunter = Math.BitDecrement(grenze);

            SimulationPufferspeicher knapp = Speicher();
            knapp.SOC = einUlpDarunter;
            Assert.True(knapp.HystereseFortschreiben(), "Ein ulp darunter muss die Nachladung ebenso enden.");
            Assert.False(knapp.LaedtGerade);

            // GEGENPROBE: Der blanke Vergleich faellt auf denselben zwei Zahlen
            // verschieden aus - ohne den Rand waere der zweite Fall oben rot. Und der
            // Abstand, um den es geht, ist genau ein ulp.
            Assert.False(einUlpDarunter >= grenze);
            Assert.True(grenze - einUlpDarunter < 1e-12);
        }

        /// <summary>
        /// <b>Die Bistabilitätsprobe.</b> Der Speicher wird über den Rechenweg des Laufs
        /// gefüllt — <see cref="SimulationPufferspeicher.Ladefaehigkeit"/> gibt die
        /// Menge, <c>Laden</c> nimmt sie auf —, und genau da entsteht der Fehltritt: In
        /// Gleitkomma ist <c>a + (b − a)</c> nicht bitgleich <c>b</c>. Danach muss die
        /// Regelung über mehrere Stunden im Entladezustand BLEIBEN.
        /// </summary>
        [Fact]
        public void Nach_dem_Fuellen_auf_die_Grenze_bleibt_die_Regelung_ueber_Stunden_stabil()
        {
            SimulationPufferspeicher sp = Speicher();
            sp.SOC = 3.7;                    // ein krummer Anfangsstand
            sp.LaedtGerade = true;

            double angebot = sp.Ladefaehigkeit(0);   // 0 => es gilt SchwelleAus
            Assert.True(angebot > 0);
            Assert.Equal(angebot, sp.Laden(angebot, 0), 9);

            for (int stunde = 0; stunde < 6; stunde++)
            {
                Assert.True(sp.HystereseFortschreiben(),
                            "Stunde " + stunde + ": Der Speicher darf entladen, er laedt nicht nach.");
                Assert.False(sp.LaedtGerade, "Stunde " + stunde + ": Die Nachladung ist wieder angesprungen.");
            }

            // Und die Hysterese lebt weiter: Unter der EINschaltschwelle beginnt sie neu.
            sp.SOC = sp.Q_max * 0.05;
            Assert.False(sp.HystereseFortschreiben());
            Assert.True(sp.LaedtGerade);
        }

        // =====================================================================
        //  3 — Die Volllast/Modulations-Grenze des BHKW
        // =====================================================================

        /// <summary>
        /// <b>Volllast gegen Modulation.</b> Ein Wärmeraum genau auf der Nennleistung und
        /// einer um ein ulp darunter führen beide auf VOLLLAST — das Modul schreibt
        /// <c>P_th</c> in seine Jahressumme, nicht den um ein ulp kleineren Raum. Der
        /// Vergleich ist bewusst ohne Toleranz: Er misst genau den einen Bit-Schritt, um
        /// den es geht.
        /// </summary>
        [Fact]
        public void Die_Volllastgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            const double pTh = 19.0;

            Assert.Equal(pTh, WaermegefuehrteStunde(restWaerme: pTh, pTh: pTh).Waerme);
            Assert.Equal(pTh, WaermegefuehrteStunde(restWaerme: Math.BitDecrement(pTh), pTh: pTh).Waerme);

            // GEGENPROBE: Ohne den Rand haette der zweite Lauf moduliert und den um ein
            // ulp kleineren Raum gebucht - der blanke Vergleich sieht die zwei Zahlen
            // verschieden.
            Assert.False(pTh < Math.BitDecrement(pTh));
        }

        /// <summary>
        /// <b>Modulation gegen „Motor bleibt aus".</b> Dieselbe Weiche eine Stufe tiefer,
        /// und hier ist der Unterschied nicht ein ulp, sondern alles: Über der Grenze
        /// moduliert das Modul, darunter bleibt es stehen und produziert NICHTS.
        /// </summary>
        [Fact]
        public void Die_Modulationsgrenze_entscheidet_gleich_auf_der_Grenze_und_ein_ulp_darunter()
        {
            const double pTh = 19.0;
            const double grenzL = 0.3;
            double untergrenze = pTh * grenzL;      // 5,699999999999999… - bewusst krumm

            Assert.Equal(untergrenze,
                         WaermegefuehrteStunde(untergrenze, pTh, grenzL).Waerme);
            Assert.Equal(Math.BitDecrement(untergrenze),
                         WaermegefuehrteStunde(Math.BitDecrement(untergrenze), pTh, grenzL).Waerme);

            // GEGENPROBE: Ohne den Rand stuende das Modul im zweiten Fall still (0 kWh).
            Assert.False(untergrenze <= Math.BitDecrement(untergrenze));
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        /// <summary>
        /// Ein Speicher mit krummer Kapazität — 1 000 l bei 20 K Spreizung ergäben eine
        /// runde Zahl, an der die Gleitkommafrage gar nicht erst aufträte.
        /// </summary>
        private static SimulationPufferspeicher Speicher()
        {
            return new SimulationPufferspeicher
            {
                Q_max = 1234.5678,
                SchwelleEin = 0.10,
                SchwelleAus = 0.95,
                SOC = 0
            };
        }

        /// <summary>Was eine wärmegeführte Stunde EINES Moduls hinterlassen hat.</summary>
        private struct Stundenspur
        {
            public double Waerme;      // s_waerme[0]
            public double Speicher;    // Füllstand des Pendelspeichers danach
            public double RestWaerme;  // offener Wärmebedarf danach
        }

        /// <summary>
        /// Fährt <c>Motorlauf_Waermegefuehrt</c> für EIN Modul mit vorgegebenem
        /// Wärmebedarf. Der Pendelspeicher steht voll (<c>restSpeicher = 0</c>), damit
        /// der Wärmeraum GENAU der übergebene Restbedarf ist — nur so lässt sich die
        /// Grenze auf ein ulp genau treffen.
        ///
        /// <para>Der Aufruf geht über Reflexion, weil die Methode <c>private</c> ist. Der
        /// öffentliche Weg (<c>Stunde_Bedarf</c>) führte über <c>Vorbereiten_Zweikanalig</c>
        /// und damit über die Datenbank — und er ließe den Wärmeraum nicht auf das letzte
        /// Bit setzen.</para>
        /// </summary>
        private static Stundenspur WaermegefuehrteStunde(double restWaerme, double pTh, double grenzL = 0.3)
        {
            var sim = new SimulationBHKW();

            MethodInfo m = typeof(SimulationBHKW).GetMethod(
                "Motorlauf_Waermegefuehrt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);

            double kapazitaet = 100.0;
            object[] args =
            {
                0, 1,
                new double[1], new double[1],       // stromproduktion, waermeproduktion
                new double[1], new double[1],       // s_waerme, s_strom
                new[] { pTh }, new[] { pTh / 2 },   // bhkwWaermeLeistung, bhkwStromLeistung
                new[] { grenzL },
                kapazitaet,
                kapazitaet,                          // speicher = voll  => restSpeicher = 0
                restWaerme
            };

            m.Invoke(sim, args);

            return new Stundenspur
            {
                Waerme     = ((double[])args[4])[0],
                Speicher   = (double)args[10],
                RestWaerme = (double)args[11]
            };
        }
    }
}
