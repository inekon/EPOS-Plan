using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Trenndecke ohne Raumgrenzen und ohne Raumbezug</b> (Mehrzonenkonzept 6.5, Rest der Stufe G6c) an der Probe
    /// <see cref="IfcProbenErzeuger.Uebereinander"/>: Zwei übereinanderliegende Räume, deren Grundrisse sich auf 20 m²
    /// überdecken, bekommen eine Trenndecke als Paar mit der Überlappung als Fläche (Herkunft Grundriss); ohne Grundriss
    /// trennt die Decke im Geschoss den ersten beheizten Raum je Geschoss (Herkunft Geschoss); ohne beides bleibt Z5,
    /// und Z4 warnt <c>GRENZEN_ENTKOPPELT</c>. Dazu die Überlappung selbst (konvex, L-förmig, getrennt). Zählproben ohne
    /// Datenbank, Rundlauf bis zum Bauteilvorschlag mit Trennflächen <c>ZONE</c>.
    /// </summary>
    public sealed class IfcTrenndeckeGrundrissTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public IfcTrenndeckeGrundrissTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(bool grundriss, bool decke)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Uebereinander(grundriss, decke)))
                a.Lesen(s, "ifc4_uebereinander.ifc", new IfcImportProfil());
            return a;
        }

        private static double[] P(double x, double y) => new[] { x, y };

        [Fact]
        public void Ueberlappung_konvex_L_foermig_und_getrennt()
        {
            var a = new List<double[]> { P(0, 0), P(6, 0), P(6, 5), P(0, 5) };
            var b = new List<double[]> { P(2, 0), P(8, 0), P(8, 5), P(2, 5) };
            Assert.Equal(20.0, Grundrissueberlappung.Ueberlappung(a, b), 6);
            // L-förmig, im Uhrzeigersinn: 6 × 5 ohne das Feld 3…6 × 3…5 = 24 m²; gegen a die ganze Fläche.
            var l = new List<double[]> { P(0, 0), P(0, 5), P(3, 5), P(3, 3), P(6, 3), P(6, 0) };
            Assert.Equal(24.0, Math.Abs(Grundrissueberlappung.Flaeche(l)), 6);
            Assert.Equal(24.0, Grundrissueberlappung.Ueberlappung(a, l), 6);
            Assert.Equal(14.0, Grundrissueberlappung.Ueberlappung(l, b), 6);   // x 2…6: 4 × 3 + 1 × 2
            Assert.Equal(0.0, Grundrissueberlappung.Ueberlappung(a, new List<double[]> { P(10, 0), P(12, 0), P(12, 2) }));
        }

        [Fact]
        public void Zwei_Raeume_uebereinander_ohne_Grenze_bilden_ein_Paar_mit_der_Ueberlappung()
        {
            GebaeudeImportAblauf a = Lesen(grundriss: true, decke: true);
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.Equal(0, g.ZahlGrenzen);
            Assert.All(g.Raeume, r => Assert.NotNull(r.GrundrissM));
            AbbildBauteil paar = Assert.Single(g.Bauteile, b => b.Trenndeckenherkunft != null);
            // Beide Räume tragen einen Körper: Das Körperpaar geht dem Grundriss vor (Mehrzonenkonzept 6.2), mit derselben Überlappung.
            Assert.Equal(AbbildBauteil.TRENNDECKE_KOERPER, paar.Trenndeckenherkunft);
            Assert.Equal(20.0, paar.BruttoflaecheM2.Value, 3);
            Assert.Equal(1.0, paar.UWertWm2K.Value, 6);
            Assert.Equal(2, paar.Nachbarn.Count);
            Assert.DoesNotContain(g.Bauteile, b => b.Name == "Decke EG/OG" && b.Trenndeckenherkunft == null);   // in dem Paar aufgegangen
            Assert.True(g.GeschosseGekoppelt);
            Assert.Contains(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_AUS_KOERPER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_GRUNDRISS");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_REFERENZ");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_GESCHAETZT");

            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            foreach (PruefMeldung m in z.Meldungen) _aus.WriteLine(m.ToString());
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, z.Vorgabe);
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel.EndsWith(GebaeudeZonierung.GRENZEN_ENTKOPPELT) || m.Schluessel.EndsWith(GebaeudeZonierung.KEINE_GRENZEN));
            Zonentrennung t = Assert.Single(z.Trennungen);
            Assert.Equal(20.0, Math.Max(t.FlaecheA, t.FlaecheB), 3);
        }

        [Fact]
        public void Rundlauf_bis_zum_Bauteilvorschlag_mit_Trennflaechen()
        {
            GebaeudeImportAblauf a = Lesen(grundriss: true, decke: true);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Assert.True(v.Mehrzonig);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            List<GebaeudeBauteilzeile> trenn = v.Zeilen.Where(x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).ToList();
            Assert.NotEmpty(trenn);
            Assert.All(trenn, x => Assert.Equal(20.0, x.Bauteil.Flaeche, 3));
            Assert.All(trenn, x => Assert.Equal(1.0, x.Bauteil.U_Wert.Value, 6));
        }

        [Fact]
        public void Ohne_Grundriss_trennt_die_Decke_des_Geschosses()
        {
            GebaeudeImportAblauf a = Lesen(grundriss: false, decke: true);
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            AbbildBauteil decke = Assert.Single(g.Bauteile, b => b.Trenndeckenherkunft != null);
            Assert.Equal(AbbildBauteil.TRENNDECKE_GESCHOSS, decke.Trenndeckenherkunft);
            Assert.Equal("Decke EG/OG", decke.Name);
            Assert.Equal(40.0, decke.BruttoflaecheM2.Value, 3);
            Assert.True(g.GeschosseGekoppelt);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, GebaeudeZonierung.Bilden(a.Abbild, 0).Vorgabe);
        }

        [Fact]
        public void Ohne_Grundriss_und_Decke_bleibt_Z5_und_Z4_warnt()
        {
            GebaeudeImportAblauf a = Lesen(grundriss: false, decke: false);
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.DoesNotContain(g.Bauteile, b => b.Trenndeckenherkunft != null);
            Assert.False(g.GeschosseGekoppelt);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, GebaeudeZonierung.Bilden(a.Abbild, 0).Vorgabe);
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            Assert.Contains(z4.Meldungen, m => m.Schluessel.EndsWith(GebaeudeZonierung.GRENZEN_ENTKOPPELT));
        }

        [Fact]
        public void Grundriss_ohne_Decke_bildet_das_Paar_ohne_U_Wert()
        {
            GebaeudeImportAblauf a = Lesen(grundriss: true, decke: false);
            AbbildBauteil paar = Assert.Single(a.Abbild.Gebaeude[0].Bauteile, b => b.Trenndeckenherkunft != null);
            Assert.Equal(20.0, paar.BruttoflaecheM2.Value, 3);
            Assert.Null(paar.UWertWm2K);
            Assert.True(a.Abbild.Gebaeude[0].GeschosseGekoppelt);
        }
    }
}
