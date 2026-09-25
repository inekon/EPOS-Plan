using System;
using Xunit;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// T13: die Ursachenregeln an synthetischen Merkmalen, samt Randwerten. Reine Funktionen —
    /// keine Datenbank, kein Werkzeuglauf.
    /// </summary>
    [Collection(Vergleichssammlung.NAME)]
    public sealed class UrsachenregelnTests
    {
        /// <summary>Eine unauffällige Flächenzeile: Δ Jahr 20 %, ohne Katalogwert, Bauweise 50, Wohngebäude.</summary>
        private static Regeleingang Flaeche(double deltaJahr = 20.0) => new Regeleingang
        {
            IstFlaeche = true,
            DeltaJahrProzent = deltaJahr,
            DeltaSpitzeProzent = deltaJahr,
            NachtanteilAltProzent = 16.0,
            NachtanteilNeuProzent = 18.0,
            FensteranteilProzent = 15.0,
            BauweiseJeM2 = 50.0,
            InnereGewinneWm2 = 2.5,
            AbsenkungK = 2.0,
        };

        private static Regeleingang Verbrauch(double deltaJahr) => new Regeleingang
        {
            IstFlaeche = false,
            DeltaJahrProzent = deltaJahr,
            DeltaSpitzeProzent = deltaJahr,
            NachtanteilAltProzent = 16.0,
            NachtanteilNeuProzent = 16.0,
            BauweiseJeM2 = 50.0,
            InnereGewinneWm2 = 2.5,
            AbsenkungK = 2.0,
        };

        private static Regelbefund R(Regeleingang e) => Ursachenregeln.Anwenden(e);

        [Fact]
        public void T13_E8_mit_Band_und_Rand()
        {
            double band = Ursachenregeln.BAND_E8_PROZENT;
            Assert.True(band > 0.0 && band <= 2.0);

            Regelbefund innen = R(Verbrauch(band));
            Assert.Contains(Ursachenregeln.U_E8, innen.Codes);
            Assert.Equal(Ampel.Erklaert, innen.Ampel);
            Assert.Equal(Ampel.Erklaert, R(Verbrauch(-band)).Ampel);

            Regelbefund aussen = R(Verbrauch(band * 1.001));
            Assert.Contains(Ursachenregeln.U_E8, aussen.Codes);
            Assert.Equal(Ampel.ZuPruefen, aussen.Ampel);

            // Mit Zone bzw. fester Nennleistung: keine Rückrechnung - nur Hinweis, nie „erklärt".
            Regeleingang zone = Verbrauch(0.0);
            zone.HatZone = true;
            Regelbefund z = R(zone);
            Assert.Contains(Ursachenregeln.U_E8Z, z.Codes);
            Assert.Contains(Ursachenregeln.U_ZO, z.Codes);
            Assert.DoesNotContain(Ursachenregeln.U_E8, z.Codes);
            Assert.Equal(Ampel.ZuPruefen, z.Ampel);

            Regeleingang nn = Verbrauch(0.0);
            nn.FesteNennleistung = true;
            Regelbefund n = R(nn);
            Assert.Contains(Ursachenregeln.U_NN, n.Codes);
            Assert.DoesNotContain(Ursachenregeln.U_E8, n.Codes);
            Assert.Equal(Ampel.ZuPruefen, n.Ampel);
        }

        [Fact]
        public void T13_Modellwechsel_und_Katalogtreffer_an_den_Raendern()
        {
            Assert.Contains(Ursachenregeln.U_MW, R(Flaeche(5.0)).Codes);
            Assert.DoesNotContain(Ursachenregeln.U_MW, R(Flaeche(4.999)).Codes);
            Assert.Contains(Ursachenregeln.U_MW, R(Flaeche(50.0)).Codes);
            Assert.DoesNotContain(Ursachenregeln.U_MW, R(Flaeche(50.001)).Codes);

            Assert.Equal(Ampel.Erklaert, R(Flaeche(5.0)).Ampel);
            Assert.Equal(Ampel.Erklaert, R(Flaeche(50.0)).Ampel);
            Assert.Equal(Ampel.ZuPruefen, R(Flaeche(4.999)).Ampel);
            Assert.Equal(Ampel.ZuPruefen, R(Flaeche(50.001)).Ampel);
            Assert.Equal(Ampel.ZuPruefen, R(Flaeche(-3.0)).Ampel);

            foreach ((double treffer, bool kat) in new[] { (90.0, true), (89.999, false), (115.0, true), (115.001, false), (100.0, true) })
            {
                Regeleingang e = Flaeche(20.0);
                e.KatalogtrefferNeuProzent = treffer;
                Regelbefund b = R(e);
                Assert.Equal(kat, b.Codes.Contains(Ursachenregeln.U_KAT));
                Assert.Equal(kat ? Ampel.Erklaert : Ampel.ZuPruefen, b.Ampel);
            }

            // Verbrauchsangabe: kein Modellwechsel, kein Katalogtreffer.
            Regeleingang v = Verbrauch(20.0);
            v.KatalogtrefferNeuProzent = 100.0;
            Assert.DoesNotContain(Ursachenregeln.U_MW, R(v).Codes);
            Assert.DoesNotContain(Ursachenregeln.U_KAT, R(v).Codes);
        }

        [Fact]
        public void T13_Hinweisregeln_an_den_Raendern_verschieben_die_Ampel_nicht()
        {
            Regeleingang sp = Flaeche(20.0);
            sp.DeltaSpitzeProzent = 35.0;           // 15 Prozentpunkte über Δ Jahr
            Assert.Contains(Ursachenregeln.U_SP, R(sp).Codes);
            Assert.Equal(Ampel.Erklaert, R(sp).Ampel);
            sp.DeltaSpitzeProzent = 34.999;
            Assert.DoesNotContain(Ursachenregeln.U_SP, R(sp).Codes);
            sp.DeltaSpitzeProzent = 35.0;
            sp.AbsenkungK = 0.0;
            Assert.DoesNotContain(Ursachenregeln.U_SP, R(sp).Codes);

            Regeleingang ng = Flaeche(20.0);
            ng.NachtanteilNeuProzent = ng.NachtanteilAltProzent + 5.0;
            Assert.Contains(Ursachenregeln.U_NG, R(ng).Codes);
            ng.NachtanteilNeuProzent = ng.NachtanteilAltProzent + 4.999;
            Assert.DoesNotContain(Ursachenregeln.U_NG, R(ng).Codes);

            Regeleingang sol = Flaeche(20.0);
            sol.FensteranteilProzent = 30.0;
            Assert.Contains(Ursachenregeln.U_SOL, R(sol).Codes);
            sol.FensteranteilProzent = 29.999;
            Assert.DoesNotContain(Ursachenregeln.U_SOL, R(sol).Codes);

            Regeleingang il = Flaeche(20.0);
            il.IstNwg = true;
            Assert.Contains(Ursachenregeln.U_IL, R(il).Codes);
            il.IstNwg = false;
            foreach ((double w, bool auffaellig) in new[] { (1.5, false), (1.499, true), (5.0, false), (5.001, true) })
            {
                il.InnereGewinneWm2 = w;
                Assert.Equal(auffaellig, R(il).Codes.Contains(Ursachenregeln.U_IL));
            }

            Regeleingang flags = Flaeche(20.0);
            flags.HatZone = true;
            flags.HeizkreisAktiv = true;
            flags.KuehlungWirksam = true;
            flags.SpalteTagesbilanz = true;
            flags.DeltaSpitzeProzent = 80.0;
            flags.NachtanteilNeuProzent = 40.0;
            flags.FensteranteilProzent = 45.0;
            Regelbefund b = R(flags);
            foreach (string c in new[] { Ursachenregeln.U_ZO, Ursachenregeln.U_AK, Ursachenregeln.U_KU, Ursachenregeln.U_TB,
                                         Ursachenregeln.U_SP, Ursachenregeln.U_NG, Ursachenregeln.U_SOL })
                Assert.Contains(c, b.Codes);
            Assert.Equal(Ampel.Erklaert, b.Ampel);
        }

        [Fact]
        public void T13_Bauweise_Datenfehler_und_gescheiterte_Wege_sind_rot()
        {
            double min = Ursachenregeln.BW_MIN_WH_M2K, max = Ursachenregeln.BW_MAX_WH_M2K;
            Assert.Equal(5.0, min);
            Assert.Equal(200.0, max);

            foreach ((double bw, bool datenfehler) in new[]
                     {
                         (0.16, true), (4.99, true), (5.0, true), (5.499, true), (5.5, false), (50.0, false),
                         (180.0, false), (180.001, true), (200.0, true), (250.0, true), (double.NaN, true)
                     })
            {
                Regeleingang e = Flaeche(20.0);
                e.BauweiseJeM2 = bw;
                Regelbefund b = R(e);
                Assert.Equal(datenfehler, b.Codes.Contains(Ursachenregeln.U_BW));
                Assert.Equal(datenfehler ? Ampel.Fehler : Ampel.Erklaert, b.Ampel);
                Assert.Equal(datenfehler ? Ursachenregeln.VERMERK_DATENFEHLER : "", b.Vermerk);
            }

            Regeleingang alt = Flaeche(20.0);
            alt.AltOk = false;
            alt.DeltaJahrProzent = double.NaN;
            Assert.Equal(Ampel.Fehler, R(alt).Ampel);

            Regeleingang neu = Flaeche(20.0);
            neu.NeuOk = false;
            Assert.Equal(Ampel.Fehler, R(neu).Ampel);
            Assert.Equal("", R(neu).Vermerk);
        }

        [Fact]
        public void T13_Kennzahlen_Nachtanteil_Heizstunden_und_Treffer()
        {
            var reihe = new double[48];
            reihe[0] = 1.0;      // 0 Uhr: Nacht
            reihe[12] = 3.0;     // 12 Uhr: Tag
            reihe[22] = 1.0;     // 22 Uhr: Nacht
            reihe[29] = 0.0005;  // 5 Uhr: Nacht, aber unter 0,1 % der Spitze
            Assert.Equal(2.0005 / 5.0005 * 100.0, Kennzahlen.NachtanteilProzent(reihe), 9);
            Assert.Equal(3, Kennzahlen.Heizstunden(reihe));

            Assert.Equal(100.0, Kennzahlen.KatalogtrefferProzent(15.688, true, 212.0, 74.0).Value, 6);
            Assert.Null(Kennzahlen.KatalogtrefferProzent(15.0, true, 0.0, 74.0));
            Assert.Null(Kennzahlen.KatalogtrefferProzent(15.0, false, 212.0, 74.0));
            Assert.Equal(100.0, Kennzahlen.VerbrauchstrefferProzent(75.0, false, 75000.0).Value, 9);
            Assert.Null(Kennzahlen.VerbrauchstrefferProzent(75.0, true, 75000.0));

            Assert.Equal(25.0, Kennzahlen.DeltaProzent(80.0, 100.0), 9);
            Assert.True(double.IsNaN(Kennzahlen.DeltaProzent(0.0, 100.0)));
            Assert.Equal(0, Berichtsschreiber.Band(-0.001));
            Assert.Equal(1, Berichtsschreiber.Band(0.0));
            Assert.Equal(2, Berichtsschreiber.Band(5.0));
            Assert.Equal(3, Berichtsschreiber.Band(15.0));
            Assert.Equal(4, Berichtsschreiber.Band(50.0));
            Assert.Equal(5, Berichtsschreiber.Band(50.001));
        }

        [Fact]
        public void T13_Namensbereinigung_laengere_Werte_zuerst_in_einem_Durchgang()
        {
            Namensbereinigung b = Namensbereinigung.Aus(new[]
            {
                ("Haus", "Gebäude 11"), ("Haus Nord", "Gebäude 12"), ("Haus Nord", "Gebäude 13"),
                ("X", "Projekt 1"), ("Gebäude", "Projekt 2"), ("Mehr\nzeilig", "Projekt 3"),
            });
            Assert.Equal("Gebäude 12/13 und Gebäude 11", b.Bereinigen("Haus Nord und Haus"));
            Assert.Equal("X bleibt", b.Bereinigen("X bleibt"));                 // einstellig
            Assert.Equal("Projekt 2 3", b.Bereinigen("Gebäude 3"));              // kein zweiter Durchgang über den Ersatz
            Assert.Equal("Projekt 3", b.Bereinigen("Mehr zeilig"));             // so schreibt SimulationProtokoll
            Assert.Equal("Projekt 3", b.Bereinigen("zeilig"));                  // jede Zeile für sich
        }
    }
}
