using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rampenmaske in der Nutzungszeit</b> (Entwurf KP3, Welle R4; Befund B10, Festlegung 10, Teilkonzept
    /// F16): Rampenstunden (s'(h) &gt; s(h)) fallen aus der Nutzungszeit der Kennzahlen — Mittel und Überhitzung;
    /// <see cref="GebaeudeModellErgebnis.Skaliert"/> und die Zonenergebnisse tragen die Maske, das Gebäude eines
    /// Mehrzonenlaufs die Vereinigung; ohne Rampe bleibt alles bitgleich. Dazu die Skalierung der Aufheizwerte
    /// (Festlegung 16, B11) im Klassenweg und mit Verbrauchsangabe über die Fassade.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizNutzungszeitTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizNutzungszeitTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static double[] Konstant(double w)
        {
            var r = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) r[h] = w;
            return r;
        }

        /// <summary>Die Nutzungszeit ohne Rampe — der Bestandsausdruck, unabhängig nachgebaut.</summary>
        private static bool Grundnutzung(GebaeudeModellErgebnis r, int h)
            => r.Nutzungsmaske == null ? r.Nachtzeit.Nutzungszeit(h) : r.Nutzungsmaske[h];

        /// <summary>Mittel und Überhitzung über eine Stundenauswahl, in Stundenfolge summiert wie der Konstruktor.</summary>
        private static (double Mittel, int Ueber, int Stunden) Kennzahlen(GebaeudeModellErgebnis r, Func<int, bool> nutzung)
        {
            double luft = 0.0;
            int n = 0, ueber = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (!nutzung(h)) continue;
                n++;
                luft += r.Raumtemperatur[h];
                if (r.OperativeTemperatur[h] > r.ThetaMax) ueber++;
            }
            return (luft / n, ueber, n);
        }

        // =====================================================================
        //  Die Maske am Ergebnis selbst
        // =====================================================================

        /// <summary>
        /// <b>Am Ergebnis gebaut:</b> zwei überhitzte, warme Stunden der Nutzungszeit (10 und 11 Uhr am Tag 40)
        /// zählen ohne Maske mit; als Rampenstunden fallen sie aus Mittel und Überhitzung. <c>Skaliert</c> trägt
        /// die Maske (Nutzungszeit, Mittel, Überhitzung bitgleich), P_auf geht mit dem Faktor, Zeiten und
        /// Zählungen nicht. Eine leere Maske (Plan ohne Rampe) ändert nichts.
        /// </summary>
        [Fact]
        public void Rampenstunden_fallen_aus_Mittel_und_Ueberhitzung_und_Skaliert_traegt_die_Maske()
        {
            double[] luft = Konstant(20.0), op = Konstant(20.0);
            var maske = new bool[STUNDEN];
            foreach (int h in new[] { 40 * 24 + 10, 40 * 24 + 11 })
            {
                luft[h] = 30.0;
                op[h] = 30.0;
                maske[h] = true;
            }
            GebaeudeModellErgebnis Bauen(Aufheizergebnis a)
                => new GebaeudeModellErgebnis(0, 1, DbWerte.GEBAEUDE_MODELL_VDI6007, Konstant(1000.0), luft, op, null, 26.0,
                                              8760.0, 1.0, 0, 0, Konstant(20.0), aufheizung: a);
            var aufheizung = new Aufheizergebnis
            {
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizLeistungKw = 12.5, AufheizzeitMaxH = 3,
                Aufheiztage = 1, AufheizAussenC = -12.0, Rampenmaske = maske,
            };
            GebaeudeModellErgebnis ohne = Bauen(null), mitLeer = Bauen(aufheizung with { Rampenmaske = new bool[STUNDEN] });
            GebaeudeModellErgebnis mit = Bauen(aufheizung);

            Assert.Equal(2, ohne.Ueberhitzungsstunden);
            Assert.True(ohne.MittlereRaumtemperaturHeizzeit > 20.0);
            Assert.Equal(Bits(ohne.MittlereRaumtemperaturHeizzeit), Bits(mitLeer.MittlereRaumtemperaturHeizzeit));
            Assert.Equal(ohne.Ueberhitzungsstunden, mitLeer.Ueberhitzungsstunden);
            Assert.Equal(0, mit.Ueberhitzungsstunden);
            Assert.Equal(20.0, mit.MittlereRaumtemperaturHeizzeit);
            Assert.False(mit.NutzungBei(40 * 24 + 10));
            Assert.True(ohne.NutzungBei(40 * 24 + 10));

            GebaeudeModellErgebnis s = mit.Skaliert(2.5);
            Assert.Same(maske, s.Rampenmaske);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(mit.NutzungBei(h), s.NutzungBei(h));
            Assert.Equal(Bits(mit.MittlereRaumtemperaturHeizzeit), Bits(s.MittlereRaumtemperaturHeizzeit));
            Assert.Equal(mit.Ueberhitzungsstunden, s.Ueberhitzungsstunden);
            Assert.Equal(12.5 * 2.5, s.Aufheizung.AufheizLeistungKw);
            Assert.Equal(2.5, s.Aufheizung.Skalierungsfaktor);
            Assert.Equal(s.Skalierungsfaktor, s.Aufheizung.Skalierungsfaktor);
            Assert.Equal(aufheizung with { AufheizLeistungKw = null, Skalierungsfaktor = 0 },
                         s.Aufheizung with { AufheizLeistungKw = null, Skalierungsfaktor = 0 });
        }

        // =====================================================================
        //  Im Lauf: Personen ab 6 Uhr, Sprung 7 Uhr (Teilkonzept F16)
        // =====================================================================

        /// <summary>
        /// <b>Personen ab 6 Uhr, Sprung 7 Uhr</b> (Abnahme Teilkonzept F16): Die Rampe legt Stunden vor 7 Uhr in die
        /// Anwesenheit; sie fallen aus Mittel und Überhitzung — nachgerechnet über (Anwesenheit ∧ ¬Maske), bitgleich.
        /// Ohne Schalter ist die Nutzungszeit die Anwesenheit, bitgleich. <c>Skaliert</c> trägt die Maske.
        /// </summary>
        [Fact]
        public void Personen_ab_6_Uhr_Sprung_7_Uhr_Rampenstunden_fallen_aus_der_Nutzungszeit()
        {
            ProjektGebaeudeModel g = Bueroprobe.Gebaeude();
            Bueroprobe.Lauf lauf = Bueroprobe.Rechnen(g, Bueroprobe.An(), personenAb: 6);
            GebaeudeModellErgebnis r = lauf.Ergebnis;
            Assert.NotNull(r.Nutzungsmaske);
            Assert.Same(lauf.Plan.Rampenmaske, r.Rampenmaske);

            int maskiert = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Grundnutzung(r, h) && !r.Rampenmaske[h], r.NutzungBei(h));
                if (Grundnutzung(r, h) && r.Rampenmaske[h]) maskiert++;
            }
            Assert.True(maskiert > 0, "keine Rampenstunde in der Anwesenheit");
            (double mittel, int ueber, int n) = Kennzahlen(r, h => Grundnutzung(r, h) && !r.Rampenmaske[h]);
            Assert.Equal(Bits(mittel), Bits(r.MittlereRaumtemperaturHeizzeit));
            Assert.Equal(ueber, r.Ueberhitzungsstunden);
            (double mittelMitRampe, _, _) = Kennzahlen(r, h => Grundnutzung(r, h));
            Assert.True(mittelMitRampe != r.MittlereRaumtemperaturHeizzeit);

            GebaeudeModellErgebnis s = r.Skaliert(1.7);
            Assert.Equal(Bits(r.MittlereRaumtemperaturHeizzeit), Bits(s.MittlereRaumtemperaturHeizzeit));
            Assert.Equal(r.Ueberhitzungsstunden, s.Ueberhitzungsstunden);
            Assert.Same(r.Rampenmaske, s.Rampenmaske);

            // Ohne Schalter: die Anwesenheit, wörtlich.
            GebaeudeModellErgebnis aus = Bueroprobe.Rechnen(g, Aufheizvorgabe.Aus, personenAb: 6).Ergebnis;
            Assert.Null(aus.Aufheizung);
            Assert.Null(aus.Rampenmaske);
            (double mittelAus, int ueberAus, _) = Kennzahlen(aus, h => Grundnutzung(aus, h));
            Assert.Equal(Bits(mittelAus), Bits(aus.MittlereRaumtemperaturHeizzeit));
            Assert.Equal(ueberAus, aus.Ueberhitzungsstunden);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "F16: {0} Rampenstunden, davon {1} in der Anwesenheit; Nutzungsstunden {2}; Mittel {3:0.0000} °C mit Maske, " +
                "{4:0.0000} °C ohne Maske, {5:0.0000} °C ohne Schalter", lauf.Plan.MaskenstundenH, maskiert, n,
                r.MittlereRaumtemperaturHeizzeit, mittelMitRampe, aus.MittlereRaumtemperaturHeizzeit));
        }

        // =====================================================================
        //  Zonen
        // =====================================================================

        /// <summary>
        /// <b>Zonen:</b> Jedes Zonenergebnis trägt die Maske seines Plans und zählt ohne seine Rampenstunden; das
        /// Gebäude trägt die Vereinigung (Festlegung 22) und zählt Mittel und Überhitzung ohne sie; die unbeheizte
        /// Zone trägt keine Maske.
        /// </summary>
        [Fact]
        public void Zonen_tragen_ihre_Maske_und_das_Gebaeude_die_Vereinigung()
        {
            double f = 0.5 * Vdi6007Probe.Gebaeude().Nutzflaeche;
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen(
                eins: new Zoneneingaben(Nutzflaeche: f, SollTag: 21.0, SollNacht: 16.0, SollWochenende: 15.0),
                zwei: new Zoneneingaben(Nutzflaeche: f, SollTag: 20.0, SollNacht: 17.0, SollWochenende: 16.0));
            Mehrzonenergebnis m = Zonenrechnung.Rechnen(g, Bueroprobe.Klima(), false, null, 0, g.ID_Gebaeude,
                                                        aufheizvorgabe: AufheizMehrzonenTests.An());
            GebaeudeModellErgebnis geb = m.Gebaeude;
            Assert.NotNull(geb.Aufheizung);
            Assert.Same(m.Aufheizgebaeude.Rampenmaske, geb.Rampenmaske);

            var vereinigt = new bool[STUNDEN];
            int geplant = 0;
            for (int z = 0; z < m.Zonen.Count; z++)
            {
                GebaeudeModellErgebnis r = m.Zonen[z];
                Aufheizplan p = m.Eingaenge[z].Aufheizplan;
                Assert.Same(p.Rampenmaske, r.Rampenmaske);
                Assert.Equal(p.Zustand, r.Aufheizung.AufheizZustand);
                if (p.Unbeheizt)
                {
                    Assert.Null(r.Rampenmaske);
                    continue;
                }
                geplant++;
                Assert.True(p.Geaendert);
                for (int h = 0; h < STUNDEN; h++)
                {
                    Assert.Equal(Grundnutzung(r, h) && !r.Rampenmaske[h], r.NutzungBei(h));
                    if (r.Rampenmaske[h]) vereinigt[h] = true;
                }
                (double mittel, int ueber, _) = Kennzahlen(r, h => Grundnutzung(r, h) && !r.Rampenmaske[h]);
                Assert.Equal(Bits(mittel), Bits(r.MittlereRaumtemperaturHeizzeit));
                Assert.Equal(ueber, r.Ueberhitzungsstunden);
            }
            Assert.Equal(2, geplant);
            Assert.Equal(vereinigt, geb.Rampenmaske);
            (double mittelGeb, _, _) = Kennzahlen(geb, h => Grundnutzung(geb, h) && !geb.Rampenmaske[h]);
            Assert.Equal(Bits(mittelGeb), Bits(geb.MittlereRaumtemperaturHeizzeit));
            int ueberGeb = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (!geb.NutzungBei(h)) continue;
                if (geb.Zonen.Any(z => z.IstBeheizt && z.Ergebnis.OperativeTemperatur[h] > z.Ergebnis.ThetaMax)) ueberGeb++;
            }
            Assert.Equal(ueberGeb, geb.Ueberhitzungsstunden);

            // Die Gebäudewerte der Ergebniszeile (Festlegung 22): W3 als Vereinigung, Kappung als Σ_h max_z.
            Aufheizergebnis a = geb.Aufheizung;
            Assert.Equal(m.Aufheizgebaeude.Aufheiztage, a.Aufheiztage);
            Assert.Equal(m.Aufheizgebaeude.AufheizstundenH, a.AufheizstundenH);
            Assert.Equal(m.Aufheizgebaeude.Quelle, a.AufheizLeistungsquelle);
            Assert.Equal(m.Aufheizgebaeude.AufheizleistungW / 1000.0, a.AufheizLeistungKw);
            Assert.Equal(Aufheizoptimierung.HeizleistungMaxStundenH(m.Zonen.Select(z => z.HeizleistungMaxAnteil).ToList()),
                         a.HeizleistungMaxStundenH);
            var w3 = new bool[365];
            foreach (GebaeudeModellErgebnis r in m.Zonen)
                if (r.Aufheizung?.Nachweisbandtage != null)
                    for (int d = 0; d < 365; d++) w3[d] |= r.Aufheizung.Nachweisbandtage[d];
            Assert.Equal(w3, a.Nachweisbandtage);
            Assert.Equal(w3.Count(x => x), a.AufheiztageNachweisband);
        }

        // =====================================================================
        //  Skalierung über die Fassade (Festlegung 16, B11)
        // =====================================================================

        /// <summary>
        /// <b>Skalierung</b> (Festlegung 16, B11) an Projekt 1018, Gebäude 10632, über die Fassade: Mit
        /// Flächenangabe gehen P_auf und die Spitzen mit demselben Faktor nach E8, Zeiten, Zählungen und T_a,B
        /// nicht. Mit Verbrauchsangabe bleibt die Jahreswärme die angegebene — die Mehrwärme der Rampe
        /// verschwindet im Faktor, der mit Rampe kleiner ist als ohne.
        /// </summary>
        [Fact]
        public void Skalierung_P_auf_wie_die_Spitzen_und_mit_Verbrauchsangabe_bleibt_die_Jahreswaerme()
        {
            if (!_db.Vorhanden) return;
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;

            AufheizLauf.Gebaeudelauf flaeche = AufheizLauf.Projekt(1018, an, double.NaN, wahl).Single();
            GebaeudeModellErgebnis r = flaeche.Ergebnis;
            Aufheizplan p = flaeche.Plan;
            double f = r.Skalierungsfaktor;
            Assert.True(f != 1.0, "Faktor 1: die Probe skaliert nicht");
            Assert.Equal(Bits(f), Bits(r.Aufheizung.Skalierungsfaktor));
            Assert.Equal(Bits(p.Bemessung.AufheizleistungW / 1000.0 * f), Bits(r.Aufheizung.AufheizLeistungKw.Value));
            double spitzeUnskaliert = 0.0;
            // Die Spitze des Katalogbaus: die Reihe zurückgerechnet (rel., denn die Reihe ist multipliziert).
            for (int h = 0; h < STUNDEN; h++) spitzeUnskaliert = Math.Max(spitzeUnskaliert, r.HeizlastW[h] / f);
            Assert.True(Math.Abs(r.SpitzeKw / (spitzeUnskaliert / 1000.0) - r.Aufheizung.AufheizLeistungKw.Value / (p.Bemessung.AufheizleistungW / 1000.0)) < 1e-12);
            Assert.Equal(p.Aufheiztage, r.Aufheizung.Aufheiztage);
            Assert.Equal(p.AufheizstundenH, r.Aufheizung.AufheizstundenH);
            Assert.Equal(p.Bemessung.Wirksam.AufheizzeitMaxH, r.Aufheizung.AufheizzeitMaxH);
            Assert.Equal(p.Bemessung.Wirksam.AussenC, r.Aufheizung.AufheizAussenC);
            Assert.Same(p.Rampenmaske, r.Rampenmaske);

            // Verbrauchsangabe: 80 MWh/a.
            const double MWH = 80.0;
            Action<ProjektGebaeudeModel> verbrauch = x =>
            {
                x.Einheit = "Verbrauch  [MWh/a]";
                x.Z_AuswahlWohnflaeche = MWH;
            };
            AufheizLauf.Gebaeudelauf vAn = AufheizLauf.Projekt(1018, an, double.NaN, wahl, verbrauch).Single();
            AufheizLauf.Gebaeudelauf vAus = AufheizLauf.Projekt(1018, Aufheizvorgabe.Aus, double.NaN, wahl, verbrauch).Single();
            Assert.True(vAn.Gerechnet && vAus.Gerechnet);
            Assert.True(Math.Abs(vAn.Ergebnis.JahresheizwaermeMwh / MWH - 1.0) < 1e-9, "mit Rampe: " + vAn.Ergebnis.JahresheizwaermeMwh);
            Assert.True(Math.Abs(vAus.Ergebnis.JahresheizwaermeMwh / MWH - 1.0) < 1e-9, "ohne Rampe: " + vAus.Ergebnis.JahresheizwaermeMwh);
            Assert.True(vAn.Ergebnis.Skalierungsfaktor < vAus.Ergebnis.Skalierungsfaktor);
            Assert.Equal(Bits(vAn.Plan.Bemessung.AufheizleistungW / 1000.0 * vAn.Ergebnis.Skalierungsfaktor),
                         Bits(vAn.Ergebnis.Aufheizung.AufheizLeistungKw.Value));

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Skalierung 1018/10632: Faktor {0:0.######}, P_auf {1:0.000} kW (Katalogbau {2:0.000} kW), Spitze {3:0.000} kW; " +
                "Verbrauchsangabe {4} MWh/a: Faktor mit Rampe {5:0.######}, ohne {6:0.######}, Jahreswärme {7:0.######} / {8:0.######} MWh",
                f, r.Aufheizung.AufheizLeistungKw, p.Bemessung.AufheizleistungW / 1000.0, r.SpitzeKw, MWH,
                vAn.Ergebnis.Skalierungsfaktor, vAus.Ergebnis.Skalierungsfaktor, vAn.Ergebnis.JahresheizwaermeMwh,
                vAus.Ergebnis.JahresheizwaermeMwh));
        }
    }
}
