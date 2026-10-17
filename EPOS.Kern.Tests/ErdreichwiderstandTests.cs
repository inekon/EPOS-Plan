using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Erdreichwiderstand nach DIN EN ISO 13370</b> (Rechenweg RP2a, Vorschlag 4): Formeln gegen Handrechnungen,
    /// Grenzfälle, die Wirkung im Bauteil- und Klassenweg und der Export. λ_Erd = 2,0 W/(m·K), w = 0,3 m,
    /// R_si = 0,17 (Boden) bzw. 0,13 (Wand), R_se = 0,04 m²K/W in d_t und d_w.
    /// </summary>
    public class ErdreichwiderstandTests
    {
        /// <summary>
        /// <b>Handrechnung 1 — gut gedämmte Platte (d_t ≥ B′)</b>: A = 100 m², P = 40 m (Feld; der flächengleiche Kreis hätte
        /// 2·√(π·100) = 35,45 m) ⇒ B′ = 100/(0,5·40) = 5,0 m. U = 0,35 ⇒ R_f = 1/0,35 − 0,17 = 2,687143;
        /// d_t = 0,3 + 2·(0,17 + 2,687143 + 0,04) = 6,094286 m ≥ B′ ⇒ U_g = 2/(0,457·5 + 6,094286) = 0,238684;
        /// R_g = 1/0,238684 − 1/0,35 = 1,3325 m²K/W; wirksam 1/(1/0,35 + 1,3325) = U_g.
        /// </summary>
        [Fact]
        public void Handrechnung_gedaemmte_Platte()
        {
            var u = new double[1];
            Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (100.0, 180.0, 0.35) }, 100.0, 40.0, u);
            Assert.Equal(Erdreichumfangsquelle.Feld, k.Quelle);
            Assert.Equal(40.0, k.Umfang_M);
            Assert.Equal(5.0, k.B_M, 12);
            Assert.Equal(6.094285714285714, k.Dt_M, 9);
            Assert.Equal(0.23868382917057368, k.Ug_WM2K, 9);
            Assert.Equal(1.3325, k.Rg_M2KW, 9);
            Assert.Equal(0.23868382917057368, u[0], 9);
            Assert.Equal(0.23868382917057368, k.UWirksam_WM2K, 9);
            Assert.Equal(0.0, k.Tiefe_M);
        }

        /// <summary>
        /// <b>Handrechnung 2 — Platte mit d_t &lt; B′, Umfang aus dem Quadrat</b>: A = 1 000 m², kein Feld ⇒ P = 4·√1000 =
        /// 126,491 m, B′ = 15,811388 m. U = 1,0 ⇒ R_f = 0,83, d_t = 0,3 + 2·(0,17 + 0,83 + 0,04) = 2,38 m &lt; B′ ⇒
        /// U_g = 4/(π·15,811388 + 2,38)·ln(π·15,811388/2,38 + 1) = 0,237079; R_g = 1/0,237079 − 1 = 3,218009.
        /// </summary>
        [Fact]
        public void Handrechnung_Platte_mit_Quadrat()
        {
            var u = new double[1];
            Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (1000.0, 180.0, 1.0) }, 0.0, 0.0, u);
            Assert.Equal(Erdreichumfangsquelle.Quadrat, k.Quelle);
            Assert.Equal("Quadrat", k.QuelleText);
            Assert.Equal(4.0 * Math.Sqrt(1000.0), k.Umfang_M, 12);
            Assert.Equal(15.811388300841896, k.B_M, 9);
            Assert.Equal(2.38, k.Dt_M, 12);
            Assert.Equal(0.2370786923232667, k.Ug_WM2K, 9);
            Assert.Equal(3.2180087556601595, k.Rg_M2KW, 9);
            Assert.True(u[0] < 1.0);
        }

        /// <summary>
        /// <b>Keller in der Tiefe z = 2,5 m</b>: Boden 1 000 m² U 1,0, Wände am Erdreich 2,5·P = 316,23 m² U 0,5 ⇒ z = A_w/P = 2,5 m.
        /// Kellerboden: Nenner d_t + 0,5·z = 3,63 ⇒ U_bf = 4/(π·15,811388 + 3,63)·ln(π·15,811388/3,63 + 1) = 0,201622.
        /// Kellerwand: R_w = 2 − 0,13 = 1,87, d_w = 2·(0,13 + 1,87 + 0,04) = 4,08 m;
        /// U_bw = 4/(π·2,5)·(1 + 0,5·2,38/(2,38 + 2,5))·ln(2,5/4,08 + 1) = 0,302768.
        /// </summary>
        [Fact]
        public void Handrechnung_Kellerwand_und_Kellerboden()
        {
            double p = 4.0 * Math.Sqrt(1000.0);
            var u = new double[2];
            Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (1000.0, 180.0, 1.0), (2.5 * p, 90.0, 0.5) }, 0.0, 0.0, u);
            Assert.Equal(2.5, k.Tiefe_M, 12);
            Assert.Equal(0.20162180913797345, u[0], 9);
            Assert.Equal(0.30276824261037744, u[1], 9);
            Assert.Equal(0.20162180913797345, k.Ug_WM2K, 9);
            Assert.Equal(4.08, Erdreichwiderstand.Dw(2.0 - 0.13), 12);
        }

        /// <summary>Grenzfälle: B′ → ∞ ⇒ U_g → 0; d_t ≥ B′ nimmt die Formel der gedämmten Platte; Kellerwand bei z = 0 der Grenzwert.</summary>
        [Fact]
        public void Grenzfaelle()
        {
            Assert.Equal(0.0, Erdreichwiderstand.BodenU(double.PositiveInfinity, 2.38));
            Assert.True(Erdreichwiderstand.BodenU(1e9, 2.38) < 1e-7);
            Assert.True(Erdreichwiderstand.BodenU(1e3, 2.38) < Erdreichwiderstand.BodenU(1e2, 2.38));
            // d_t = B′: die gedämmte Formel λ/(0,457·B′ + d_t); knapp darunter die andere — beide nah beieinander.
            double gleich = Erdreichwiderstand.BodenU(5.0, 5.0);
            Assert.Equal(2.0 / (0.457 * 5.0 + 5.0), gleich, 12);
            double knapp = Erdreichwiderstand.BodenU(5.0 + 1e-9, 5.0);
            Assert.InRange(Math.Abs(knapp - gleich) / gleich, 0.0, 0.05);
            // z = 0: Grenzwert 3λ/(π·d_w), stetig zu kleinem z.
            double dw = 4.08;
            Assert.Equal(3.0 * 2.0 / (Math.PI * dw), Erdreichwiderstand.KellerwandU(0.0, 2.38, dw), 12);
            Assert.Equal(Erdreichwiderstand.KellerwandU(0.0, 2.38, dw), Erdreichwiderstand.KellerwandU(1e-9, 2.38, dw), 6);
            // R_g nie negativ: ein Bauteil, das schon schlechter leitet als die Norm, behält seinen U-Wert.
            Assert.Equal(0.0, Erdreichwiderstand.Rg(0.5, 0.2));
            Assert.Equal(0.2, Erdreichwiderstand.UWirksam(0.2, 0.0));
            // Ein Feld unter dem Kreisumfang fällt auf das Quadrat.
            Assert.Equal(4.0 * Math.Sqrt(946.4), Erdreichwiderstand.Umfang(946.4, 9.7, out Erdreichumfangsquelle q), 12);
            Assert.Equal(Erdreichumfangsquelle.Quadrat, q);
        }

        /// <summary>
        /// <b>Bauteilweg</b>: Der Erdreichwiderstand senkt den wirksamen U-Wert der Bodenplatte am Erdreich und Σ(U·A) um genau
        /// (U − U_wirksam)·A; Bauteile an Außenluft bleiben, und ein Satz ohne Erdreich trägt keine Kennwerte.
        /// </summary>
        [Fact]
        public void Im_Bauteilweg_sinkt_nur_das_Bauteil_am_Erdreich()
        {
            var g = new BauteilwegGebaeude("Probe", 100.0, 10000.0, 0.5, 2.5, 30.0, 100.0, 40.0);
            var boden = new BauteilEingang("Boden", Bauteilart.Bodenplatte, 100.0, Bauteilrand.Erdreich, 0.35);
            var wand = new BauteilEingang("Wand", Bauteilart.Aussenwand, 120.0, Bauteilrand.Aussenluft, 0.3, azimutGrad: 180.0);
            var fenster = new BauteilEingang("Fenster", Bauteilart.Fenster, 20.0, Bauteilrand.Aussenluft, 1.3, azimutGrad: 180.0, gWert: 0.6);
            ErsatzparameterRC mit = ErsatzparameterRC.AusBauteilweg(g, new[] { wand, boden, fenster });
            Assert.NotNull(mit.Erdreich);
            Assert.Equal(0.23868382917057368, mit.UWirksamJeBauteil_WM2K[1], 9);
            Assert.Equal(0.3, mit.UWirksamJeBauteil_WM2K[0]);
            Assert.Equal(120.0 * 0.3 + 100.0 * 0.23868382917057368, mit.SummeUA_opak_WK, 9);
            Assert.Equal(0.23868382917057368, mit.Bauteilherleitung.Single(h => h.Bezeichnung == "Boden").UWirksam_WM2K, 9);

            var aussenluft = new BauteilEingang("Boden", Bauteilart.Bodenplatte, 100.0, Bauteilrand.Aussenluft, 0.35);
            ErsatzparameterRC ohne = ErsatzparameterRC.AusBauteilweg(g, new[] { wand, aussenluft, fenster });
            Assert.Null(ohne.Erdreich);
            Assert.Equal(120.0 * 0.3 + 100.0 * 0.35, ohne.SummeUA_opak_WK, 9);
            Assert.True(mit.R_Rest_AWGruppe_KW > ohne.R_Rest_AWGruppe_KW);
        }

        private static ProjektGebaeudeModel Kellerprobe()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER;
            return g;
        }

        /// <summary>
        /// <b>Klassenweg, Lauf und Export</b>: Das Probegebäude (Grundfläche 88 m² am Erdreich, U 0,8, Feld 2,5 m — unter dem
        /// Kreisumfang, also das Quadrat) rechnet mit dem wirksamen U-Wert der Grundfläche (U_Grund bleibt der eingetragene); der Eingang trägt die Kennwerte, die
        /// Heizwärme sinkt gegenüber dem Lauf ohne Erdreichwiderstand (Grundfläche am Keller), und der Export schreibt
        /// <c>Geb[n].Erdreich_B</c>, <c>Geb[n].Erdreich_Ug</c> und <c>Geb[n].Erdreich_Umfangsquelle</c>.
        /// </summary>
        [Fact]
        public void Klassenweg_Lauf_und_Export()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            SolardatenModel[] klima = Vdi6007Probe.Klima(h => 5.0 + 10.0 * Math.Sin(2.0 * Math.PI * (h - 2400) / 8760.0));
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, klima);
            Assert.NotNull(e.Erdreich);
            Assert.Equal(Erdreichumfangsquelle.Quadrat, e.Erdreich.Quelle);
            Assert.Equal(88.0 / (0.5 * 4.0 * Math.Sqrt(88.0)), e.Erdreich.B_M, 12);
            Assert.Equal(e.Erdreich.Ug_WM2K, e.Erdreich.UWirksam_WM2K, 12);
            Assert.Equal(0.8, e.U_Grund);
            Assert.True(e.Erdreich.UWirksam_WM2K < 0.8);
            Assert.Equal(e.Parameter.SummeUA_opak_WK - 88.0 * e.Erdreich.UWirksam_WM2K,
                         ErsatzparameterRC.AusKlassenweg(GebaeudeModellEingang.Daten(Kellerprobe())).SummeUA_opak_WK - 88.0 * 0.8, 9);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            Assert.Same(e.Erdreich, r.Erdreich);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(r);
            Assert.Equal(e.Erdreich.B_M, satz.Skalare.Single(s => s.Key == "Geb[0].Erdreich_B").Value);
            Assert.Equal(e.Erdreich.Ug_WM2K, satz.Skalare.Single(s => s.Key == "Geb[0].Erdreich_Ug").Value);
            Assert.Equal("Quadrat", satz.Texte.Single(s => s.Key == "Geb[0].Erdreich_Umfangsquelle").Value);
            Assert.DoesNotContain(satz.Skalare, s => s.Key.Contains("Innenumkehr"));

            // Am Keller kein Erdreichwiderstand, kein Schlüssel.
            ProjektGebaeudeModel keller = Kellerprobe();
            GebaeudeModellEingang ek = Vdi6007Probe.Eingang(keller, klima);
            Assert.Null(ek.Erdreich);
            Assert.Equal(0.8 * 88.0 + e.Parameter.SummeUA_opak_WK - 88.0 * e.Erdreich.UWirksam_WM2K, ek.Parameter.SummeUA_opak_WK, 9);
            GebaeudeExportsatz satzKeller = GebaeudeErgebnisexport.Satz(Vdi6007Rechenweg.Laufen(ek, 0, keller.ID_Gebaeude));
            Assert.DoesNotContain(satzKeller.Skalare, s => s.Key.Contains("Erdreich"));
            Assert.DoesNotContain(satzKeller.Texte, s => s.Key.Contains("Erdreich"));
        }
    }
}
