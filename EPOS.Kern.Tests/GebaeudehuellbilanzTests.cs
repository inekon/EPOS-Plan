using System.Collections.Generic;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Altweg;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wärmeleitwerte der Gebäudehülle</b> (Stufe G1; Umsetzungskonzept
    /// Gebäudesimulation 2.5, 2.10) und die Auskunft über den Rechenweg (2.3, ADR-006) — die
    /// Rechnung, die der Gebäudedialog nur anzeigt.
    ///
    /// <para><b>Die Kernprobe:</b> Der gewichtete Zweig trifft die Transmission samt
    /// Wärmebrücken von <c>TagesbilanzPhysik.SpezWaermeverlusteC</c> auf 1e-12 — mit
    /// Luftwechsel 0 (der Lüftungsanteil des Bestands hängt an der Außentemperatur und ist
    /// keine Dialogkennzahl) und nach Division durch 100 (die Rückgabe trägt den Faktor
    /// 100).</para>
    ///
    /// <para>Ohne Datenbank, ohne Sprachbindung.</para>
    /// </summary>
    public class GebaeudehuellbilanzTests
    {
        private static IReadOnlyList<Huellzeile> Beispiel()
            => Gebaeudehuellbilanz.Zeilen(
                1.2, 520, 2.8, 128, 0.6, 310, 1.0, 310, 2.0, 8,
                0.10, 240, 0.15, 88, 0.10, 88);

        [Fact]
        public void H_T_ist_die_ungewichtete_Summe_aller_acht_Zeilen()
        {
            double erwartet = 1.2 * 520 + 2.8 * 128 + 0.6 * 310 + 1.0 * 310 + 2.0 * 8
                            + 0.10 * 240 + 0.15 * 88 + 0.10 * 88;

            Assert.Equal(erwartet, Gebaeudehuellbilanz.TransmissionWK(Beispiel()), 12);
            Assert.Equal(8, Beispiel().Count);
        }

        [Theory]
        [InlineData(1.2, 520, 2.8, 128, 0.6, 310, 1.0, 310, 2.0, 8, 0.10, 240, 0.15, 88, 0.10, 88)]
        [InlineData(0.3, 200, 1.3, 45, 0.2, 120, 0.35, 100, 0.5, 5, 0.1, 50, 0, 0, 0, 0)]
        [InlineData(0.24, 1234.5, 1.1, 311.2, 0.14, 870.3, 0.3, 870.3, 0, 0, 0.05, 612, 0.2, 140, 0.08, 140)]
        public void Der_gewichtete_Wert_trifft_SpezWaermeverlusteC(
            double uw, double aw, double uf, double af, double ud, double ad, double ug, double ag,
            double us, double @as, double psi1, double l1, double psiKeller, double lKeller,
            double psiDach, double lDach)
        {
            IReadOnlyList<Huellzeile> zeilen = Gebaeudehuellbilanz.Zeilen(
                uw, aw, uf, af, ud, ad, ug, ag, us, @as, psi1, l1, psiKeller, lKeller, psiDach, lDach);

            // Dieselbe Belegung wie im Tagesbilanz-Weg: kwb1 Fenster-Wand, kwb2 Wand-Dach,
            // kwb3 Aussenwand-Keller; Luftwechsel 0, Aussentemperatur 0.
            double bestand = TagesbilanzPhysik.SpezWaermeverlusteC(
                uw, aw, uf, af, ud, ad, ug, ag, us, @as,
                psi1, l1, psiDach, lDach, psiKeller, lKeller,
                0.0, 150.0, 2.5, 0.0) / 100.0;

            double dialog = Gebaeudehuellbilanz.TransmissionGewichtetWK(zeilen);
            Assert.True(System.Math.Abs(dialog - bestand) <= 1e-12 * System.Math.Max(1.0, System.Math.Abs(bestand)),
                        $"gewichtet {dialog} gegen SpezWaermeverlusteC {bestand}");
        }

        [Fact]
        public void H_ve_rechnet_mit_0_34_und_leere_Werte_zaehlen_als_0()
        {
            Assert.Equal(0.5 * 150 * 2.5 * 0.34, Gebaeudehuellbilanz.LueftungWK(0.5, 150, 2.5), 12);
            Assert.Equal(0.0, Gebaeudehuellbilanz.LueftungWK(null, 150, 2.5));
        }

        [Fact]
        public void Ost_West_ist_die_Summe_oder_das_Bestandsfeld_und_halb_ist_unvollstaendig()
        {
            Assert.Equal(18.0, Gebaeudehuellbilanz.FensterOstWest(6, 12, 15));
            Assert.Equal(15.0, Gebaeudehuellbilanz.FensterOstWest(null, null, 15));
            Assert.Null(Gebaeudehuellbilanz.FensterOstWest(6, null, 15));
            Assert.Null(Gebaeudehuellbilanz.FensterOstWest(null, null, null));
        }

        [Fact]
        public void Das_mittlere_U_zaehlt_nur_die_opaken_Bauteile()
        {
            double? u = Gebaeudehuellbilanz.MittleresUOpak(Beispiel());
            double erwartet = (1.2 * 520 + 0.6 * 310 + 1.0 * 310 + 2.0 * 8) / (520 + 310 + 310 + 8.0);
            Assert.Equal(erwartet, u!.Value, 12);

            Assert.Null(Gebaeudehuellbilanz.MittleresUOpak(
                Gebaeudehuellbilanz.Zeilen(1, 0, 1, 10, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0)));
        }

        [Fact]
        public void Die_Grenzen_sind_die_des_Kerns()
        {
            Assert.Equal(GebaeudeFestwerte.U_MIN, Gebaeudehuellbilanz.U_MIN);
            Assert.Equal(GebaeudeFestwerte.U_MAX, Gebaeudehuellbilanz.U_MAX);
            Assert.Equal(GebaeudeFestwerte.C_RHO_LUFT, Gebaeudehuellbilanz.C_RHO_LUFT);
            Assert.Equal(GebaeudeFestwerte.VORGABE_RAHMENANTEIL, Gebaeudemodellvorgaben.Rahmenanteil);
            Assert.Equal(GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR, Gebaeudemodellvorgaben.Kellertemperatur);
        }

        /// <summary>
        /// <b>Die Anzeige folgt der Rechnung</b> (ADR-006): <see cref="Gebaeuderechenweg"/> liest
        /// dieselbe NULL-Regel wie die Weiche, und die Weiche nimmt für jeden Spaltenwert den
        /// Weg, den die Auskunft nennt.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_VDI6007)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ)]
        public void Der_Rechenweg_der_Auskunft_ist_der_der_Weiche(string modell)
        {
            Assert.Equal(SimulationWaermebedarf.MODELL_OHNE_ANGABE, Gebaeuderechenweg.OhneAngabe);

            var sim = new SimulationWaermebedarf();
            IGebaeudeRechenweg weg = sim.RechenwegWaehlen(new ProjektGebaeudeModel { Gebaeude_Modell = modell });
            Assert.Equal(Gebaeuderechenweg.IstVdi6007(modell), weg is Vdi6007Rechenweg);
        }

        [Fact]
        public void Ein_unbekannter_Spaltenwert_rechnet_auf_dem_Tagesbilanz_Weg()
        {
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, Gebaeuderechenweg.Wirksam("UNBEKANNT"));
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, Gebaeuderechenweg.Wirksam(DbWerte.GEBAEUDE_MODELL_VDI6007));
        }
    }
}
