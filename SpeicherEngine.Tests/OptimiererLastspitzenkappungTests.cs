using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der dritten Berechnungsart der Auslegungsoptimierung: LASTSPITZENKAPPUNG
    /// (Anwenderentscheid W11b-E-3 vom 10.09.2026, Fachkonzept 6.3 / 6.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Woher der Auftrag kommt.</b> Projekt 1050 ("Stromspeicher Optimierung")
    /// fuehrt genau einen Speicher, keine PV und kein BHKW. Dauer- und Nachtnutzung
    /// bewerten den genutzten Erzeugungsueberschuss; ohne Erzeugung ist der 0, und mit
    /// Modulkosten 0 war auch der Kapitaldienst 0 - die Rasterkarte war einfarbig, alle
    /// 120 Punkte trugen dJ = 0 (Befund W11b-B-25, Windows-Abnahme 09.09.2026). Der
    /// Test <see cref="Dauernutzung_Ohne_Erzeugung_Liefert_Ueberall_Null"/> haelt genau
    /// diesen Befund fest.
    /// </para>
    /// <para>
    /// <b>Der Pruefstand.</b> Ein Jahr in Stundenwerten (dt = 1 h): nachts 100 kW,
    /// tagsueber 300 kW, und an EINER Stunde des Jahres eine Spitze von 700 kW. Die
    /// eine Stunde ist Absicht - sie macht die erreichbare Kappung STETIG in der
    /// Auslegung. Der nachziehende Schwellenmodus (Fachkonzept 6.4) senkt die Schwelle
    /// nie wieder; bei einem mehrstuendigen Spitzenblock reisst ein zu kleiner Speicher
    /// deshalb ab der Stunde, in der er leer ist, und die Schwelle springt auf den
    /// vollen Spitzenwert. Die Kappung waere dann eine Stufe (0 oder alles) statt einer
    /// Kurve, und ein inneres Optimum gaebe es nicht.
    /// </para>
    /// <para>
    /// <b>Die Handrechnung.</b> Verlustfrei (eta_RT = 1), Preisreihe 0 (also keine
    /// Verlustkosten), Zins 0 und N = 20 a - damit ist a = 1/N = 0,05 und
    /// E_a,aeq = E_a,1. Gekappt wird, was der Speicher in der Spitzenstunde liefern
    /// kann: <c>dP = min(P, C, 400)</c> - erst die Leistung, dann der Energieinhalt,
    /// hoechstens aber die 400 kW zwischen Spitze (700) und Tageslast (300). Mit
    /// L_P = 100 EUR/(kW*a), c_cap = 300 EUR/kWh und c_pow = 100 EUR/kW ist
    /// </para>
    /// <code>
    /// dJ(C, r) = 100 * min(r*C, C, 400) - 0,05 * (300*C + 100*r*C)
    /// </code>
    /// <para>
    /// Das Maximum liegt bei r = 1 (darunter begrenzt die Leistung, darueber kostet
    /// sie nur noch) und dort bei C = 400 kWh mit dJ = 32.000 EUR/a - INNEN im
    /// Suchraum 100 … 1.000 kWh x 0,5 … 3,0 C, also ohne Randwarnung.
    /// </para>
    /// </remarks>
    public sealed class OptimiererLastspitzenkappungTests
    {
        private const double LeistungspreisEurProKwA = 100.0;
        private const int Stunden = 8760;

        /// <summary>Stunde der Jahresspitze - Tag 100, 12 Uhr.</summary>
        private const int SpitzenStunde = 100 * 24 + 12;

        // ==================================================================
        // Pruefstand
        // ==================================================================

        /// <summary>
        /// Jahreslastgang in Stundenwerten: 100 kW nachts (0 … 7 und 20 … 23),
        /// 300 kW tagsueber, 700 kW in <see cref="SpitzenStunde"/>.
        /// </summary>
        private static double[] Lastgang()
        {
            double[] last = new double[Stunden];
            for (int i = 0; i < Stunden; i++)
            {
                int stunde = i % 24;
                last[i] = stunde >= 8 && stunde < 20 ? 300.0 : 100.0;
            }
            last[SpitzenStunde] = 700.0;
            return last;
        }

        /// <summary>Eingang ohne Erzeugung und mit Preis 0 - die Kappung braucht beides nicht.</summary>
        private static SpeicherEingang Eingang()
        {
            double[] last = Lastgang();
            return SpeicherEingang.MitFixpreis(last, new double[last.Length], 0.0);
        }

        /// <summary>
        /// Basisauslegung 400 kWh / 400 kW, Band 0 … 400 kWh (also voll nutzbar),
        /// verlustfrei, Zins 0, N = 20 a, c_cap = 300, c_pow = 100, I_fix = 0.
        /// </summary>
        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 400.0,
            PKw = 400.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 400.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 1.0,
            CCapEurProKwh = 300.0,
            CPowEurProKw = 100.0,
            IFixEur = 0.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0
        };

        /// <summary>Suchraum 100 … 1.000 kWh in 10 Schritten, 0,5 … 3,0 C, ohne Feinraster.</summary>
        private static OptimiererOptionen Optionen() => new OptimiererOptionen
        {
            CMinKwh = 100.0,
            CMaxKwh = 1000.0,
            Stuetzstellen = 10,
            RMin = 0.5,
            RMax = 3.0,
            RSchritt = 0.5,
            Feinraster = false,
            Strategie = OptimiererStrategie.Lastspitzenkappung,
            LeistungspreisEurProKwA = LeistungspreisEurProKwA
        };

        /// <summary>Die Handrechnung zu einem Rasterpunkt [EUR/a].</summary>
        private static double Handrechnung(double cNomKwh, double cRate)
        {
            double pKw = cRate * cNomKwh;
            double kappung = Math.Min(Math.Min(pKw, cNomKwh), 400.0);
            return LeistungspreisEurProKwA * kappung
                   - 0.05 * (300.0 * cNomKwh + 100.0 * pKw);
        }

        // ==================================================================
        // Zielfunktion und inneres Optimum
        // ==================================================================

        [Fact]
        public void Optimum_Liegt_Innen_Bei_400_kWh_Und_1_C()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());

            Assert.Equal(400.0, erg.BestPunkt.CNomKwh, 6);
            Assert.Equal(1.0, erg.BestPunkt.CRate, 6);
            Assert.Equal(400.0, erg.BestPunkt.PKw, 6);
            Assert.Equal(32000.0, erg.BestPunkt.ZielfunktionEur, 6);
        }

        [Fact]
        public void Am_Inneren_Optimum_Warnt_Keine_Randlage()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());

            Assert.False(erg.Randlage.KapazitaetUnten);
            Assert.False(erg.Randlage.KapazitaetOben);
            Assert.False(erg.Randlage.CRateUnten);
            Assert.False(erg.Randlage.CRateOben);
            Assert.False(erg.Randlage.Vorhanden);
        }

        [Fact]
        public void Die_Zielfunktion_Steigt_Bis_Zur_Kappung_Und_Faellt_Danach()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());

            int spalte = erg.Grobraster.IndexCRate(1.0);
            double[] schnitt = erg.Grobraster.Schnittkurve(spalte);

            // 100 … 400 kWh: die Kappung waechst mit der Kapazitaet (80 EUR je kWh).
            Assert.Equal(8000.0, schnitt[0], 6);
            Assert.Equal(16000.0, schnitt[1], 6);
            Assert.Equal(24000.0, schnitt[2], 6);
            Assert.Equal(32000.0, schnitt[3], 6);

            // ab 400 kWh: die Spitze ist gekappt, jede weitere kWh kostet nur noch
            // ihren Kapitaldienst (20 EUR je kWh).
            Assert.Equal(30000.0, schnitt[4], 6);
            Assert.Equal(20000.0, schnitt[9], 6);

            for (int i = 1; i <= 3; i++) Assert.True(schnitt[i] > schnitt[i - 1]);
            for (int i = 5; i < schnitt.Length; i++) Assert.True(schnitt[i] < schnitt[i - 1]);
        }

        [Fact]
        public void Jeder_Rasterpunkt_Trifft_Die_Handrechnung()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());

            for (int i = 0; i < erg.Grobraster.Zeilen; i++)
                for (int s = 0; s < erg.Grobraster.Spalten; s++)
                {
                    OptimiererPunkt p = erg.Grobraster.Punkte[i][s];
                    Assert.Equal(Handrechnung(p.CNomKwh, p.CRate), p.ZielfunktionEur, 6);
                }
        }

        // ==================================================================
        // Sekundaerkennzahlen
        // ==================================================================

        [Fact]
        public void Der_Bestpunkt_Traegt_Spitze_Kappung_Und_Ersparnis()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());
            OptimiererPunkt best = erg.BestPunkt;

            Assert.Equal(700.0, best.SpitzeOhneSpeicherKw, 6);
            Assert.Equal(300.0, best.SpitzeMitSpeicherKw, 6);
            Assert.Equal(400.0, best.KappungKw, 6);
            Assert.Equal(40000.0, best.LeistungspreisersparnisEur, 6);
            Assert.Equal(300.0, best.ErreichteSchwelleKw, 6);
            Assert.False(best.SchwelleGerissen);

            // Der Ertrag des Referenzjahres IST die Leistungspreisersparnis: Die
            // Preisreihe ist 0, also gibt es keine Verlustkosten (Fachkonzept 6.4).
            Assert.Equal(40000.0, best.ErtragReferenzjahrEur, 6);
            Assert.Equal(40000.0, best.ErtragAequivalentEur, 6);
        }

        [Fact]
        public void Ein_Zu_Kleiner_Speicher_Kappt_Nur_Seinen_Energieinhalt()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Optionen());

            // 200 kWh bei 1 C: die Spitzenstunde braucht 400 kWh, geliefert werden 200.
            OptimiererPunkt p = erg.Grobraster.Punkte[1][erg.Grobraster.IndexCRate(1.0)];
            Assert.Equal(200.0, p.CNomKwh, 6);
            Assert.Equal(200.0, p.KappungKw, 6);
            Assert.Equal(500.0, p.SpitzeMitSpeicherKw, 6);
            Assert.Equal(20000.0, p.LeistungspreisersparnisEur, 6);
        }

        // ==================================================================
        // Der Befund: ohne Erzeugung bewerten die anderen Strategien nichts
        // ==================================================================

        [Fact]
        public void Dauernutzung_Ohne_Erzeugung_Liefert_Ueberall_Null()
        {
            // Genau die Lage aus Projekt 1050: keine Erzeugung, Modulkosten 0.
            SpeicherParameter basis = Basis() with
            {
                CCapEurProKwh = 0.0,
                CPowEurProKw = 0.0,
                IFixEur = 0.0
            };

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(
                Eingang(), basis,
                Optionen() with
                {
                    Strategie = OptimiererStrategie.Dauernutzung,
                    LeistungspreisEurProKwA = 0.0
                });

            foreach (OptimiererPunkt[] zeile in erg.Grobraster.Punkte)
                foreach (OptimiererPunkt p in zeile)
                {
                    Assert.Equal(0.0, p.ZielfunktionEur, 9);
                    Assert.Equal(0.0, p.KappungKw, 9);
                    Assert.Equal(0.0, p.LeistungspreisersparnisEur, 9);
                }
        }

        // ==================================================================
        // Pruefung
        // ==================================================================

        [Fact]
        public void Ohne_Leistungspreis_Wirft_Pruefe()
        {
            OptimiererOptionen ohne = Optionen() with { LeistungspreisEurProKwA = 0.0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => ohne.Pruefe());
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SpeicherOptimierer().Optimiere(Eingang(), Basis(), ohne));
        }

        [Fact]
        public void Ein_Negativer_Leistungspreis_Wirft_Auch_Ohne_Kappung()
        {
            OptimiererOptionen negativ = Optionen() with
            {
                Strategie = OptimiererStrategie.Dauernutzung,
                LeistungspreisEurProKwA = -1.0
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => negativ.Pruefe());
        }

        [Fact]
        public void Die_Kappung_Nutzt_Denselben_Parametersatz_Wie_Die_Maske()
        {
            // Nachziehende Schwelle, P_ziel = 0 - der gemeinsame Abbildungsweg, den
            // auch PeakShavingEingaben.AlsPeakShavingParameter verwendet.
            PeakShavingParameter ps = PeakShavingParameter.Nachziehend(LeistungspreisEurProKwA, 24.0);

            Assert.True(ps.Adaptiv);
            Assert.Equal(0.0, ps.PZielKw, 9);
            Assert.Equal(LeistungspreisEurProKwA, ps.LeistungspreisEurProKwA, 9);
            Assert.Equal(24.0, ps.BezugspreisMittelCtKwh, 9);
        }
    }
}
