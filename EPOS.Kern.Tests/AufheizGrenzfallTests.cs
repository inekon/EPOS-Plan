using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Lauf eines Referenzprojekts Gebäude für Gebäude über die Fassade
    /// (<see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/>) — mit gesetzter Aufheizvorgabe und
    /// Testnaht, denn <see cref="SimulationWaermebedarf.KlimakalenderLesen"/> setzt die Vorgabe zurück.
    /// Je Lauf frische Gebäudezeilen: die Fassade trägt Bewohner und Bezugsfläche nach.
    /// </summary>
    internal static class AufheizLauf
    {
        /// <summary>Die sechzehn Referenzprojekte der Basis R30.</summary>
        internal static readonly int[] Referenzprojekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047, 1049, 1050 };

        internal sealed class Gebaeudelauf
        {
            internal int Projekt;
            internal int Gebaeude;
            internal bool Gerechnet;
            internal double[] Ziel;
            internal GebaeudeModellErgebnis Ergebnis;
            internal Aufheizplan Plan;

            public override string ToString()
                => string.Format(CultureInfo.InvariantCulture, "Projekt {0}, Gebäude {1}", Projekt, Gebaeude);
        }

        /// <summary>Rechnet alle Gebäude des Projekts auf dem VDI-Weg mit <paramref name="vorgabe"/>.</summary>
        internal static List<Gebaeudelauf> Projekt(int idProjekt, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN,
                                                   Func<ProjektGebaeudeModel, bool> auswahl = null)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            sim.AufheizvorgabeProjekt = vorgabe;
            sim.Vdi6007weg.AufheizleistungTestW = aufheizleistungTestW;

            var liste = new List<Gebaeudelauf>();
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            for (int i = 0; i < ctrl.rows; i++)
            {
                ProjektGebaeudeModel item = ctrl.items[i];
                if (!Gebaeuderechenweg.IstVdi6007(item.Gebaeude_Modell)) continue;
                if (auswahl != null && !auswahl(item)) continue;
                var ziel = new double[8760];
                bool ok = sim.HeizwaermeEinesGebaeudes(item, i, ziel);
                liste.Add(new Gebaeudelauf
                {
                    Projekt = idProjekt,
                    Gebaeude = item.ID_Gebaeude,
                    Gerechnet = ok,
                    Ziel = ziel,
                    Ergebnis = sim.GebaeudeErgebnisse.Ergebnis(i),
                    Plan = sim.Vdi6007weg.LetzterAufheizplan,
                });
            }
            return liste;
        }
    }

    /// <summary>
    /// <b>N-AH8 Grenzfall ohne Zonen</b> (Entwurf KP3 Abschnitt 7, Welle R2; Teilkonzept 4.8): Alle
    /// Einzonengebäude der sechzehn Referenzprojekte auf dem VDI-Weg rechnen mit eingeschalteter
    /// Aufheizoptimierung und P_auf = +∞ (Testnaht) <b>bitgleich</b> zu „aus" — Heizreihe, Raum- und
    /// operative Temperatur, Kälte, Sollwertreihe und jede Kennzahl des Ergebnisses; die Planung lief
    /// (Zustand gesetzt), schrieb aber keine Stunde (n = 1 überall, Aufheiztage 0). Mit „aus" läuft
    /// die Planung gar nicht (Grundsatz 3).
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizGrenzfallTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizGrenzfallTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        [Fact]
        public void N_AH8_Schalter_an_mit_unendlicher_Aufheizleistung_rechnet_bitgleich_zu_aus()
        {
            if (!_db.Vorhanden) return;

            var an = new Aufheizvorgabe(true, null, null, null, null);
            int gebaeude = 0, gekoppelt = 0, spruenge = 0;
            foreach (int projekt in AufheizLauf.Referenzprojekte)
            {
                List<AufheizLauf.Gebaeudelauf> aus = AufheizLauf.Projekt(projekt, Aufheizvorgabe.Aus);
                List<AufheizLauf.Gebaeudelauf> mit = AufheizLauf.Projekt(projekt, an, double.PositiveInfinity);
                Assert.Equal(aus.Count, mit.Count);
                for (int i = 0; i < aus.Count; i++)
                {
                    AufheizLauf.Gebaeudelauf a = aus[i], m = mit[i];
                    string wo = m.ToString();
                    Assert.True(a.Gerechnet && m.Gerechnet, "nicht gerechnet: " + wo);
                    Assert.Null(a.Plan);
                    Assert.NotNull(m.Plan);
                    Assert.False(m.Plan.Geaendert, wo);
                    Assert.Equal(0, m.Plan.Aufheiztage);
                    Assert.Equal(0, m.Plan.AufheizstundenH);
                    if (m.Plan.Gekoppelt)
                        gekoppelt++;
                    else
                    {
                        Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, m.Plan.Zustand);
                        Assert.Equal(0, m.Plan.Bemessung.VarianteA.AufheizzeitMaxH);
                        Assert.Equal(0, m.Plan.Bemessung.VarianteB.AufheizzeitMaxH);
                        Assert.Equal(0, m.Plan.TageUnerreichbar);
                        Assert.Equal(0, m.Plan.TageBegrenzt);
                        foreach (Aufheizsprung sp in m.Plan.Spruenge) Assert.Equal(1, sp.N);
                        spruenge += m.Plan.Spruenge.Count;
                    }
                    Bitgleich(a.Ziel, m.Ziel, wo + ", Heizreihe");
                    Bitgleich(a.Ergebnis, m.Ergebnis, wo);
                    gebaeude++;
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH8: {0} Gebäude bitgleich, davon {1} gekoppelt (W5), {2} Sprünge mit n = 1", gebaeude, gekoppelt, spruenge));
            Assert.True(gebaeude >= 17, "Weniger VDI-Gebäude als erwartet: " + gebaeude);
            Assert.True(spruenge > 0);
        }

        internal static void Bitgleich(GebaeudeModellErgebnis a, GebaeudeModellErgebnis b, string wo)
        {
            Assert.NotNull(a);
            Assert.NotNull(b);
            Bitgleich(a.HeizlastW, b.HeizlastW, wo + ", Heizlast");
            Bitgleich(a.Raumtemperatur, b.Raumtemperatur, wo + ", Raumtemperatur");
            Bitgleich(a.OperativeTemperatur, b.OperativeTemperatur, wo + ", operative Temperatur");
            Bitgleich(a.KuehlbedarfKwh, b.KuehlbedarfKwh, wo + ", Kälte");
            Bitgleich(a.Heizsollwert, b.Heizsollwert, wo + ", Heizsollwert");
            double[] skalare =
            {
                a.JahresheizwaermeMwh, a.SpitzeKw, a.SpitzeTagesmittelKw, a.Spitze95Kw, a.KuehlenergieMwh ?? double.NaN,
                a.StundenMitKuehlbedarf ?? -1, a.MittlereRaumtemperaturHeizzeit, a.Ueberhitzungsstunden, a.VerbrauchAltKwh,
                a.Skalierungsfaktor, a.StundenMitUmschaltung, a.StundenHeizenUndKuehlen, a.StundenMitSommerlueftung,
                a.StundenMitNachtauskuehlung ?? -1,
            };
            double[] skalareB =
            {
                b.JahresheizwaermeMwh, b.SpitzeKw, b.SpitzeTagesmittelKw, b.Spitze95Kw, b.KuehlenergieMwh ?? double.NaN,
                b.StundenMitKuehlbedarf ?? -1, b.MittlereRaumtemperaturHeizzeit, b.Ueberhitzungsstunden, b.VerbrauchAltKwh,
                b.Skalierungsfaktor, b.StundenMitUmschaltung, b.StundenHeizenUndKuehlen, b.StundenMitSommerlueftung,
                b.StundenMitNachtauskuehlung ?? -1,
            };
            Bitgleich(skalare, skalareB, wo + ", Kennzahlen");
        }

        internal static void Bitgleich(double[] a, double[] b, string wo)
        {
            if (a == null || b == null)
            {
                Assert.True(a == null && b == null, wo + ": nur eine Reihe vorhanden");
                return;
            }
            Assert.Equal(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[i]) == BitConverter.DoubleToInt64Bits(b[i]),
                    string.Format(CultureInfo.InvariantCulture, "{0}, Stelle {1}: {2:R} gegen {3:R}", wo, i, a[i], b[i]));
        }
    }
}
