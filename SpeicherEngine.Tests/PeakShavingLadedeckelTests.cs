using System;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// <b>Der LADEDECKEL der Lastspitzenkappung</b> (Anwenderbefund 17.09.2026,
    /// Entscheide LS-E-1 (a) und LS-E-3).
    ///
    /// <para><b>Was er trennt.</b> Bis dahin war die Zielschwelle beides in einem:
    /// Oberhalb wurde entladen, unterhalb bis zu ihr geladen. Fuer den Einzelspeicher im
    /// Projektlauf reicht das nicht. Ein GRUENSTROMSPEICHER darf ueberhaupt nicht aus dem
    /// Netz laden, und ein zu hoch gewaehltes Ziel darf die Spitze nicht ANHEBEN.
    /// <c>PeakShavingParameter.LadedeckelKw</c> ist die eine Schranke, die beides
    /// traegt: die hoechste Netzlast, die das LADEN erzeugen darf.</para>
    ///
    /// <para><b>Jede Zahl ist hergeleitet.</b> Die Reihen unten sind so klein, dass sich
    /// jeder Schritt von Hand nachrechnen laesst: dt = 1 h, eta = 1 (RoundTrip 1,0),
    /// SoC-Band 0..100 kWh, P = 50 kW. Der Speicher startet leer, wenn nichts anderes
    /// dasteht.</para>
    /// </summary>
    public class PeakShavingLadedeckelTests
    {
        /// <summary>Speicher mit rundem Band: 100 kWh, 50 kW, verlustfrei, Stundenraster.</summary>
        private static SpeicherParameter Speicher(double startSoCKwh = 0.0) => new SpeicherParameter
        {
            CNomKwh = 100,
            PKw = 50,
            SoCMinKwh = 0,
            SoCMaxKwh = 100,
            RoundTripWirkungsgrad = 1.0,
            StartSoCKwh = startSoCKwh,
            DtH = 1.0,
            Kapitalzins = 0.04,
            NutzungsdauerA = 15
        };

        private static PeakShavingParameter Steuerung(double zielKw, double? deckelKw,
                                                      bool adaptiv = false) =>
            new PeakShavingParameter
            {
                PZielKw = zielKw,
                Adaptiv = adaptiv,
                LeistungspreisEurProKwA = 100.0,
                BezugspreisMittelCtKwh = 30.0,
                LadedeckelKw = deckelKw
            };

        // =====================================================================
        // 1 - Der Bestand bleibt, wie er war
        // =====================================================================

        /// <summary>
        /// OHNE Deckel (<c>null</c>) rechnet die Strategie Wert fuer Wert wie bisher:
        /// Die Ladeschranke ist die Zielschwelle selbst. Das ist die Zusage an Maske,
        /// Flotte, Rastersuche und den Excel-Regressionstest - sie setzen das Feld nicht
        /// und duerfen sich nicht bewegen.
        /// </summary>
        [Fact]
        public void Ohne_Deckel_rechnet_die_Strategie_wie_bisher()
        {
            // Last 80 / 20 / 80 / 20 kW, Ziel 50 kW.
            double[] last = { 80, 20, 80, 20 };

            PeakShavingErgebnis ohneFeld = new PeakShaving(
                new PeakShavingParameter
                {
                    PZielKw = 50,
                    LeistungspreisEurProKwA = 100.0,
                    BezugspreisMittelCtKwh = 30.0
                }).BerechnePeakShaving(last, Speicher());

            PeakShavingErgebnis mitDeckelGleichZiel = new PeakShaving(
                Steuerung(50, 50)).BerechnePeakShaving(last, Speicher());

            Assert.Equal(ohneFeld.PNeuMaxKw, mitDeckelGleichZiel.PNeuMaxKw, 12);
            for (int i = 0; i < last.Length; i++)
                Assert.Equal(ohneFeld.PNeuKw[i], mitDeckelGleichZiel.PNeuKw[i], 12);
        }

        // =====================================================================
        // 2 - Die Kappung selbst
        // =====================================================================

        /// <summary>
        /// WINTERSPITZE OHNE ERZEUGUNG, Ziel unter der Spitze: Die neue Spitze IST das
        /// Ziel, solange P_max und SoC sie tragen.
        ///
        /// <para>Herleitung: Last 20 / 20 / 20 / 90 kW, Ziel 60 kW, Deckel 60 kW. Die
        /// ersten drei Stunden laedt der Speicher mit min(50, 60-20) = 40 kW, also
        /// 40 kWh je Stunde, und steht nach zwei Stunden bei 80 kWh, nach der dritten am
        /// Bandende 100 kWh (die dritte Stunde nimmt nur noch 20 kWh auf). In Stunde 4
        /// verlangt die Kappung 90 - 60 = 30 kW; moeglich sind min(50, 100) = 50 kW.
        /// Die Spitze faellt damit genau auf 60 kW.</para>
        /// </summary>
        [Fact]
        public void Ziel_unter_der_Spitze_kappt_auf_das_Ziel()
        {
            double[] last = { 20, 20, 20, 90 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(60, 60))
                .BerechnePeakShaving(last, Speicher());

            Assert.Equal(60.0, r.PNeuKw[0], 9);   // 20 + 40 geladen
            Assert.Equal(60.0, r.PNeuKw[1], 9);
            Assert.Equal(40.0, r.PNeuKw[2], 9);   // Band voll: nur noch 20 kWh
            Assert.Equal(60.0, r.PNeuKw[3], 9);   // 90 - 30 entladen
            Assert.Equal(60.0, r.PNeuMaxKw, 9);
            Assert.Equal(90.0, r.PAltMaxKw, 9);
            Assert.False(r.SchwelleGerissen);
        }

        // =====================================================================
        // 3 - Gruenstrom: Deckel 0
        // =====================================================================

        /// <summary>
        /// GRUENSTROM (Deckel 0): Geladen wird nur, solange die NETZLAST negativ ist -
        /// also allein aus Erzeugungsueberschuss. Aus dem Netz kommt nichts.
        ///
        /// <para>Herleitung: Netzlast -30 / 10 / 10 / 70 kW (die erste Stunde hat 30 kW
        /// Ueberschuss). Mit Deckel 0 laedt Stunde 1 mit min(50, 0-(-30)) = 30 kW; die
        /// Stunden 2 und 3 liegen mit 10 kW UEBER dem Deckel und laden nicht. Stunde 4
        /// entlaedt min(50, 70-40, 30 kWh vorhanden) = 30 kW → 40 kW. Der Speicher hat
        /// damit keine kWh aus dem Netz gezogen: Ladung = 30 kWh = Ueberschuss.</para>
        /// </summary>
        [Fact]
        public void Gruenstrom_laedt_ausschliesslich_aus_dem_Ueberschuss()
        {
            double[] netzlast = { -30, 10, 10, 70 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(40, 0.0))
                .BerechnePeakShaving(netzlast, Speicher());

            Assert.Equal(0.0, r.PNeuKw[0], 9);    // -30 + 30 = 0: der Ueberschuss ist weg
            Assert.Equal(10.0, r.PNeuKw[1], 9);   // keine Netzladung
            Assert.Equal(10.0, r.PNeuKw[2], 9);
            Assert.Equal(40.0, r.PNeuKw[3], 9);   // gekappt auf das Ziel
            Assert.Equal(30.0, r.LadeenergieKwh, 9);
            Assert.Equal(30.0, r.EntladeenergieKwh, 9);

            // KEIN Intervall zieht durch das Laden mehr aus dem Netz als ohne Speicher.
            for (int i = 0; i < netzlast.Length; i++)
                Assert.True(Math.Max(0.0, r.PNeuKw[i]) <= Math.Max(0.0, netzlast[i]) + 1e-9,
                            "Intervall " + i + " hat aus dem Netz geladen.");
        }

        /// <summary>
        /// GEGENPROBE zum Gruenstromfall: Haengt man den Deckel aus (Ladeschranke = Ziel,
        /// wie im Bestand), laedt derselbe Speicher in Stunde 2 und 3 aus dem NETZ - und
        /// der Fall oben faellt. Genau das ist der Unterschied, den das Feld macht.
        /// </summary>
        [Fact]
        public void Gegenprobe_ohne_Deckel_laedt_der_Gruenstromfall_aus_dem_Netz()
        {
            double[] netzlast = { -30, 10, 10, 70 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(40, null))
                .BerechnePeakShaving(netzlast, Speicher());

            Assert.Equal(40.0, r.PNeuKw[1], 9);   // 10 + 30 aus dem Netz
            Assert.True(r.LadeenergieKwh > 30.0);
        }

        // =====================================================================
        // 4 - Graustrom: Netzladung bis zum Ziel, nie darueber
        // =====================================================================

        /// <summary>
        /// GRAUSTROM (Deckel = Ziel): Der Speicher darf aus dem Netz nachladen, aber die
        /// Netzlast steigt dabei NIE ueber das Ziel.
        ///
        /// <para>Herleitung: Netzlast 10 / 10 / 10 / 100 kW, Ziel 50 kW. Stunden 1 bis 3
        /// laden mit min(50, 50-10) = 40 kW → 40 / 80 / 100 kWh (die dritte Stunde
        /// nimmt nur noch 20 kWh auf, also 20 kW). Stunde 4 verlangt 100 - 50 = 50 kW
        /// und hat 100 kWh im Band: Die Spitze faellt auf 50 kW. In keinem Intervall
        /// liegt die neue Netzlast ueber dem Ziel.</para>
        /// </summary>
        [Fact]
        public void Graustrom_laedt_aus_dem_Netz_aber_nie_ueber_das_Ziel()
        {
            double[] netzlast = { 10, 10, 10, 100 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(50, 50))
                .BerechnePeakShaving(netzlast, Speicher());

            Assert.Equal(50.0, r.PNeuKw[0], 9);
            Assert.Equal(50.0, r.PNeuKw[1], 9);
            Assert.Equal(30.0, r.PNeuKw[2], 9);   // Band voll
            Assert.Equal(50.0, r.PNeuKw[3], 9);
            Assert.Equal(50.0, r.PNeuMaxKw, 9);

            foreach (double p in r.PNeuKw)
                Assert.True(p <= 50.0 + 1e-9, "Die Netzladung hat das Ziel gerissen.");
        }

        // =====================================================================
        // 5 - LS-E-3: Ein zu hohes Ziel hebt die Spitze nicht an
        // =====================================================================

        /// <summary>
        /// ZIEL UEBER DER REFERENZSPITZE (LS-E-3): Der Deckel steht dann auf der
        /// Referenzspitze, und die Spitze bleibt, wie sie war - sie steigt NICHT.
        ///
        /// <para>Herleitung: Netzlast 10 / 10 / 10 / 55 kW, Referenzspitze also 55 kW.
        /// Das Ziel 200 kW kappt nichts (die Last erreicht es nie). Mit Deckel
        /// min(200, 55) = 55 kW laedt Stunde 1 mit min(50, 55-10) = 45 kW, Stunde 2
        /// ebenso (Band 90 kWh), Stunde 3 nur noch 10 kW; Stunde 4 hat keine Luft mehr.
        /// Die Spitze bleibt damit 55 kW - sie steigt nicht.</para>
        /// </summary>
        [Fact]
        public void Ziel_ueber_der_Referenzspitze_hebt_die_Spitze_nicht_an()
        {
            double[] netzlast = { 10, 10, 10, 55 };
            const double referenzspitze = 55.0;

            PeakShavingErgebnis r = new PeakShaving(Steuerung(200, referenzspitze))
                .BerechnePeakShaving(netzlast, Speicher());

            Assert.Equal(55.0, r.PNeuKw[0], 9);
            Assert.Equal(55.0, r.PNeuKw[1], 9);
            Assert.Equal(20.0, r.PNeuKw[2], 9);
            Assert.Equal(55.0, r.PAltMaxKw, 9);
            Assert.Equal(55.0, r.PNeuMaxKw, 9);
        }

        /// <summary>
        /// GEGENPROBE: OHNE den Deckel laedt derselbe Speicher gegen das Ziel von
        /// 200 kW und schafft eine NEUE, hoehere Spitze - aus einer Kappung wird eine
        /// Spitzenerzeugung. Genau diesen Fall verhindert LS-E-3.
        /// </summary>
        [Fact]
        public void Gegenprobe_ohne_Deckel_erzeugt_das_zu_hohe_Ziel_eine_neue_Spitze()
        {
            double[] netzlast = { 10, 10, 10, 55 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(200, null))
                .BerechnePeakShaving(netzlast, Speicher());

            // Ohne Deckel laedt Stunde 1 mit der vollen Leistung: 10 + 50 = 60 kW - eine
            // NEUE Spitze ueber der Referenz von 55 kW.
            Assert.Equal(55.0, r.PAltMaxKw, 9);
            Assert.Equal(60.0, r.PNeuKw[0], 9);
            Assert.Equal(60.0, r.PNeuMaxKw, 9);
        }

        // =====================================================================
        // 6 - Adaptiv
        // =====================================================================

        /// <summary>
        /// ADAPTIV mit Deckel: Die Schwelle zieht nach, der Ladepfad bleibt gedeckelt.
        ///
        /// <para>Herleitung: Netzlast -30 / 200 kW, Deckel 0 (Gruenstrom), Speicher
        /// leer. Stunde 1 hat 30 kW Ueberschuss; die Schwelle steht noch auf 0, die
        /// Ladeschranke ist 0 - (-30) = 30 kW, also laedt der Speicher 30 kWh und die
        /// Netzlast wird 0. Stunde 2: dMax = min(50, 30) = 30 kW, die Schwelle zieht auf
        /// 200 - 30 = 170 kW nach, entladen werden 30 kW → 170 kW. Die erreichte
        /// Schwelle ist 170 kW und per Konstruktion nicht gerissen.</para>
        /// <para>Die ANLAUFSCHWAECHE des Greedy ist hier gut zu sehen und gewollt: Aus
        /// dem Netz haette der Speicher sich nie vorladen koennen, weil die Schwelle bei
        /// 0 beginnt und die Ladeschranke damit 0 ist. Genau deshalb hat die eigene
        /// Maske <c>MinimaleSchwelleKw</c> daneben.</para>
        /// </summary>
        [Fact]
        public void Adaptiv_zieht_die_Schwelle_nach_und_haelt_den_Deckel()
        {
            double[] netzlast = { -30, 200 };

            PeakShavingErgebnis r = new PeakShaving(Steuerung(0, 0.0, adaptiv: true))
                .BerechnePeakShaving(netzlast, Speicher());

            Assert.Equal(0.0, r.PNeuKw[0], 9);
            Assert.Equal(170.0, r.PNeuKw[1], 9);
            Assert.Equal(170.0, r.ErreichteSchwelleKw, 9);
            Assert.Equal(30.0, r.LadeenergieKwh, 9);
            Assert.False(r.SchwelleGerissen);
        }

        // =====================================================================
        // 7 - Die Pruefung
        // =====================================================================

        /// <summary>Ein negativer Deckel ist keine Schranke, sondern ein Fehler.</summary>
        [Fact]
        public void Ein_negativer_Deckel_wird_abgewiesen()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Steuerung(50, -1.0).Pruefe());
        }

        /// <summary>Der Deckel 0 ist zulaessig - er IST der Gruenstromfall.</summary>
        [Fact]
        public void Der_Deckel_null_ist_zulaessig()
        {
            Steuerung(50, 0.0).Pruefe();
        }
    }
}
