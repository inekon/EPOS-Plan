using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Ergebnis je Zone (Stufe G6b, Welle W5; A2, A6, Festlegung 10)</b> — die Zonen am
    /// Gebäudeergebnis, die Stundenkennzahlen des Gebäudes „mindestens eine beheizte Zone", die
    /// Zeilen je Zone in den Kennzahlen und der Weg über <c>Tab_ErgebnisZone</c> (Speichern und Lesen).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErgebnisZoneTests
    {
        [Fact]
        public void Das_Gebaeudeergebnis_traegt_seine_Zonen_und_zaehlt_nach_Festlegung_10()
        {
            Mehrzonenergebnis m = ZonenschleifeTests.Rechnen(ZonenEingangTests.MitKeller(out _));
            GebaeudeModellErgebnis g = m.Gebaeude;
            Assert.Equal(2, g.Zonen.Count);
            Assert.Equal(new[] { ZonenEingangTests.WOHNEN, ZonenEingangTests.KELLER }, g.Zonen.Select(z => z.ZonenId));
            Assert.True(g.Zonen[0].IstBeheizt);
            Assert.False(g.Zonen[1].IstBeheizt);
            Assert.Equal(1.0, g.Skalierungsfaktor);

            // Überhitzung: Stunden der Nutzungszeit, in denen mindestens eine beheizte Zone über ihrer
            // oberen Raumtemperatur liegt.
            int ueber = 0;
            for (int h = 0; h < 8760; h++)
                if (g.Nachtzeit.Nutzungszeit(h)
                    && g.Zonen.Any(z => z.IstBeheizt && z.Ergebnis.OperativeTemperatur[h] > z.Ergebnis.ThetaMax))
                    ueber++;
            Assert.Equal(ueber, g.Ueberhitzungsstunden);
            Assert.Equal(g.Zonen[0].Ergebnis.Ueberhitzungsstunden, g.Ueberhitzungsstunden);
            Assert.True(double.IsFinite(g.Zonen[1].DeltaThetaMaxK) && g.Zonen[1].DeltaThetaMaxK > 0.0);
            Assert.Equal(g.Zonen[0].DeltaThetaMaxK, g.Zonen[1].DeltaThetaMaxK);
            Assert.All(g.Zonen, z => Assert.True(z.DurchlaeufeMax >= 2));

            // Die Kennzahlen des Gebäudes tragen je Zone eine Zeile; die unbeheizte ohne Energie (A2).
            ErgebnisGebaeudeModel k = GebaeudeKennzahlen.Bilden(0, 4711, "Probegebäude", DbWerte.GEBAEUDE_MODELL_VDI6007,
                                                               g.HeizlastW.Select(w => w / 1000.0).ToArray(), g);
            Assert.Equal(2, k.Zonen.Count);
            Assert.Equal(g.Zonen[0].Ergebnis.JahresheizwaermeMwh, k.Zonen[0].HeizwaermeMwh);
            Assert.Null(k.Zonen[1].HeizwaermeMwh);
            Assert.Null(k.Zonen[1].SpitzeKw);
            Assert.NotNull(k.Zonen[1].MittlereRaumtemperaturC);
            Assert.Equal(1, k.Zonen[0].Rang);
            Assert.Equal(2, k.Zonen[1].Rang);
        }

        /// <summary>
        /// Der Ergebnisexport schreibt die Zonen nur ab zwei Zonen (<c>Geb[n].Zone[k].*</c>): die
        /// unbeheizte Zone ohne Energie, Δϑ_max mit Nachbarzone; eine Einzelzone schreibt keinen
        /// Zonenschlüssel.
        /// </summary>
        [Fact]
        public void Der_Export_schreibt_die_Zonen_nur_ab_zwei_Zonen()
        {
            Mehrzonenergebnis m = ZonenschleifeTests.Rechnen(ZonenEingangTests.MitKeller(out _));
            GebaeudeExportsatz s = GebaeudeErgebnisexport.Satz(m.Gebaeude);
            string p = "Geb[" + m.Gebaeude.Index + "].Zone[";
            List<string> zonen = s.Skalare.Select(x => x.Key).Where(x => x.StartsWith(p, StringComparison.Ordinal)).ToList();
            Assert.Equal(new[]
            {
                p + "0].ID_Zone", p + "0].IstBeheizt", p + "0].JahresheizwaermeMwh", p + "0].SpitzeKw",
                p + "0].MittlereRaumtemperaturHeizzeit", p + "0].Ueberhitzungsstunden", p + "0].DeltaThetaMaxK",
                p + "1].ID_Zone", p + "1].IstBeheizt",
                p + "1].MittlereRaumtemperaturHeizzeit", p + "1].Ueberhitzungsstunden", p + "1].DeltaThetaMaxK",
            }, zonen);
            Assert.Equal(ZonenEingangTests.KELLER, s.Skalare.Single(x => x.Key == p + "1].ID_Zone").Value);
            Assert.Equal(0.0, s.Skalare.Single(x => x.Key == p + "1].IstBeheizt").Value);
            Assert.Equal(m.Gebaeude.Zonen[0].Ergebnis.JahresheizwaermeMwh,
                         s.Skalare.Single(x => x.Key == p + "0].JahresheizwaermeMwh").Value);
            Assert.DoesNotContain(s.Reihen, r => r.Key.Contains("zone", StringComparison.OrdinalIgnoreCase));   // keine Reihen je Zone

            GebaeudeModellErgebnis einzel = Vdi6007Rechenweg.Laufen(GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL), 0, 1);
            Assert.DoesNotContain(GebaeudeErgebnisexport.Satz(einzel).Skalare, x => x.Key.Contains(".Zone[", StringComparison.Ordinal));
        }

        [Fact]
        public void Eine_Einzelzone_traegt_keine_Zonenliste()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.Null(r.Zonen);
            ErgebnisGebaeudeModel k = GebaeudeKennzahlen.Bilden(0, 1, "E", DbWerte.GEBAEUDE_MODELL_VDI6007,
                                                               r.HeizlastW.Select(w => w / 1000.0).ToArray(), r);
            Assert.Empty(k.Zonen);
        }

        /// <summary>Die Zonenzeilen gehen nach <c>Tab_ErgebnisZone</c> und kommen gleich zurück; NULL bleibt null.</summary>
        [Fact]
        public void Die_Zonen_gehen_nach_Tab_ErgebnisZone_und_kommen_gleich_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1039;
            var laeufer = new SimulationRunner();
            Assert.True(laeufer.SimuliereUndSpeichere(projekt, out string fehler) > 0, fehler);

            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            Assert.All(m.Gebaeude, g => Assert.Empty(g.Zonen));
            var zonen = new List<ErgebnisZoneModel>
            {
                new ErgebnisZoneModel { ID_Zone = null, Rang = 1, Bezeichner = "Wohnen", IstBeheizt = true, HeizwaermeMwh = 12.5,
                                        SpitzeKw = 7.25, MittlereRaumtemperaturC = 20.4, UeberhitzungsstundenH = 12,
                                        DeltaThetaMaxK = 9.5, DurchlaeufeMax = 3, MusterwechselH = 4 },
                new ErgebnisZoneModel { ID_Zone = null, Rang = 2, Bezeichner = "Keller", IstBeheizt = false,
                                        MittlereRaumtemperaturC = 11.0, UeberhitzungsstundenH = 0, DeltaThetaMaxK = 9.5,
                                        DurchlaeufeMax = 3, MusterwechselH = 0 },
            };
            m.Gebaeude[1].Zonen.AddRange(zonen);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);

            ErgebnisModel geladen = new ErgebnisCtrl().Load(projekt);
            Assert.Empty(geladen.Gebaeude[0].Zonen);
            List<ErgebnisZoneModel> z = geladen.Gebaeude[1].Zonen;
            Assert.Equal(2, z.Count);
            Assert.Equal("Wohnen", z[0].Bezeichner);
            Assert.True(z[0].IstBeheizt);
            Assert.Equal(12.5, z[0].HeizwaermeMwh);
            Assert.Equal(7.25, z[0].SpitzeKw);
            Assert.Null(z[0].KuehlenergieMwh);
            Assert.Equal(9.5, z[0].DeltaThetaMaxK);
            Assert.Equal(3, z[0].DurchlaeufeMax);
            Assert.Equal(4, z[0].MusterwechselH);
            Assert.False(z[1].IstBeheizt);
            Assert.Null(z[1].HeizwaermeMwh);
            Assert.Null(z[1].SpitzeKw);
            Assert.Equal(11.0, z[1].MittlereRaumtemperaturC);
        }
    }
}
