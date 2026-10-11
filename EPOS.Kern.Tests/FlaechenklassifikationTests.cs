using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Flächenklassifikation nach Randbedingung</b> (Konzept HottCAD-Verbund 4.2): die Gruppen R0 bis R7 an den Körperproben
    /// <c>ifc4_koerper_nachbarn*.ifc</c> und <c>ifc4_koerper_bauteile.ifc</c> — gepaart, Bauteil des Raumbezugs, Rückfall, Raumgrenze
    /// vor Körper, Bilanz und Gegenprobe. Ohne Datenbank.
    /// </summary>
    public sealed class FlaechenklassifikationTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        private static AbbildGebaeude Lesen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            return a.Abbild.Gebaeude.Single();
        }

        private static string Kennung(AbbildGebaeude g, string raum) => g.Raeume.Single(r => r.Name == raum).Kennung;

        /// <summary>Die Gruppen eines Raums als sortierte Folge „Gruppe:Beleg:m²“.</summary>
        private static List<string> Gruppen(AbbildGebaeude g, string raum)
        {
            AbbildRaum r = g.Raeume.Single(x => x.Name == raum);
            return g.Flaechengruppen.Where(z => z.Raumkennung == r.Kennung)
                    .Select(z => z.Gruppe + ":" + z.Beleg + ":" + Math.Round(Flaeche(r.Koerper, z.Dreiecksindizes), 3))
                    .OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        private static double Flaeche(Dateikoerper k, IEnumerable<int> dreiecke)
            => dreiecke.Sum(t =>
            {
                double[] a = k.PunkteM[k.Dreiecke[t][0]], b = k.PunkteM[k.Dreiecke[t][1]], c = k.PunkteM[k.Dreiecke[t][2]];
                double ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
                double x = uy * vz - uz * vy, y = uz * vx - ux * vz, z = ux * vy - uy * vx;
                return Math.Sqrt(x * x + y * y + z * z) / 2.0;
            });

        [Fact]
        public void Jedes_Dreieck_eines_Raumkoerpers_hat_genau_eine_Gruppe()
        {
            foreach (string datei in new[] { "ifc4_koerper_nachbarn.ifc", "ifc4_koerper_nachbarn_grenzen.ifc", "ifc4_koerper_bauteile.ifc" })
            {
                AbbildGebaeude g = Lesen(datei);
                Assert.NotNull(g.Flaechengruppen);
                foreach (AbbildRaum r in g.Raeume.Where(x => x.Koerper != null))
                {
                    List<int> alle = g.Flaechengruppen.Where(z => z.Raumkennung == r.Kennung).SelectMany(z => z.Dreiecksindizes).OrderBy(x => x).ToList();
                    Assert.Equal(Enumerable.Range(0, r.Koerper.DreieckZahl), alle);
                }
            }
        }

        [Fact]
        public void Gepaarte_Flaechen_zwischen_beheizten_Raeumen_sind_R0_die_Huelle_faellt_auf_die_Normale_zurueck()
        {
            AbbildGebaeude g = Lesen("ifc4_koerper_nachbarn.ifc");
            Assert.Equal(new[] { "R0:PAAR:12", "R0:PAAR:20", "R1:FLAECHE_OHNE_BAUTEIL:12", "R1:FLAECHE_OHNE_BAUTEIL:15", "R1:FLAECHE_OHNE_BAUTEIL:15",
                                 "R3:FLAECHE_OHNE_BAUTEIL:20" }, Gruppen(g, "Büro"));
            Assert.Equal(new[] { "R0:PAAR:20", "R1:FLAECHE_OHNE_BAUTEIL:12", "R1:FLAECHE_OHNE_BAUTEIL:12", "R1:FLAECHE_OHNE_BAUTEIL:15",
                                 "R1:FLAECHE_OHNE_BAUTEIL:15", "R5:FLAECHE_OHNE_BAUTEIL:20" }, Gruppen(g, "Büro 2"));
            Assert.Equal(64.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R0], 6);
            Assert.Equal(126.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R1], 6);
            Assert.Equal(32.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R3], 6);
            Assert.Equal(32.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R5], 6);
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "FLAECHE_OHNE_BAUTEIL");
        }

        [Fact]
        public void Raumgrenzen_gehen_vor_den_Koerperpaaren()
        {
            AbbildGebaeude g = Lesen("ifc4_koerper_nachbarn_grenzen.ifc");
            List<string> buero = Gruppen(g, "Büro");
            Assert.Equal(2, buero.Count(x => x.StartsWith("R0:RAUMGRENZE:", StringComparison.Ordinal)));
            Assert.DoesNotContain(buero, x => x.Contains(":PAAR:"));
            Assert.Equal(64.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R0], 6);
        }

        [Fact]
        public void Huellbauteile_und_unbeheizter_Nachbar_geben_R1_R2_R3_und_R7()
        {
            AbbildGebaeude g = Lesen("ifc4_koerper_bauteile.ifc");
            string sued = g.Bauteile.Single(b => b.Name == "Außenwand Süd").Kennung;
            string boden = g.Bauteile.Single(b => b.Name == "Bodenplatte").Kennung;
            Assert.Equal(new[] { "R1:BAUTEIL:15", "R1:FLAECHE_OHNE_BAUTEIL:12", "R1:FLAECHE_OHNE_BAUTEIL:15", "R2:PAAR:12", "R3:BAUTEIL:20",
                                 "R5:FLAECHE_OHNE_BAUTEIL:20" }, Gruppen(g, "Wohnen"));
            // Aus Sicht des unbeheizten Raums ist dieselbe Trennfläche R0.
            Assert.Contains("R0:PAAR:12", Gruppen(g, "Kammer"));
            string wohnen = Kennung(g, "Wohnen");
            Assert.Contains(g.Flaechengruppen, z => z.Raumkennung == wohnen && z.Gruppe == Flaechengruppe.R1 && z.Bauteilkennung == sued);
            Assert.Contains(g.Flaechengruppen, z => z.Raumkennung == wohnen && z.Gruppe == Flaechengruppe.R3 && z.Bauteilkennung == boden);

            // Bauteilkörper: Wand R1, Fenster R7 — Zeilen ohne Raum, nicht in der Bilanz der Dreiecke.
            AbbildBauteil fenster = g.Bauteile.Single(b => b.Name == "Außenwand Süd").Oeffnungen.Single();
            Assert.Contains(g.Flaechengruppen, z => z.Raumkennung == null && z.Bauteilkennung == sued && z.Gruppe == Flaechengruppe.R1);
            Assert.Contains(g.Flaechengruppen, z => z.Raumkennung == null && z.Bauteilkennung == fenster.Kennung && z.Gruppe == Flaechengruppe.R7);

            Assert.Equal(12.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R0], 6);
            Assert.Equal(78.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R1], 6);
            Assert.Equal(12.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R2], 6);
            Assert.Equal(36.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R3], 6);
            Assert.Equal(36.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R5], 6);
            Assert.Equal(1.8, g.FlaechengruppenBilanzM2[Flaechengruppe.R7], 6);
            Assert.Equal(0.0, g.FlaechengruppenBilanzM2[Flaechengruppe.R4] + g.FlaechengruppenBilanzM2[Flaechengruppe.R6], 6);
        }

        [Fact]
        public void Die_Gegenprobe_meldet_ueber_fuenf_Prozent_einen_Hinweis_keinen_Fehler()
        {
            AbbildGebaeude g = Lesen("ifc4_koerper_bauteile.ifc");
            Assert.Equal(27.72, g.FlaechengruppenMengeM2[Flaechengruppe.R1], 6);
            Assert.Equal(36.0, g.FlaechengruppenMengeM2[Flaechengruppe.R3], 6);
            List<Flaechengruppe> ab = Flaechenklassifikation.Abweichungen(g);
            Assert.Contains(Flaechengruppe.R1, ab);       // Körper 78 m² gegen Menge 27,72 m²
            Assert.DoesNotContain(Flaechengruppe.R3, ab); // 36 gegen 36
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "FLAECHENGRUPPE_ABWEICHUNG" && m.Werte[1] == "R1"
                                              && m.Stufe == SpeicherEngine.PruefStufe.Info);
        }

        [Fact]
        public void Ohne_Raumkoerper_bleibt_die_Klassifikation_leer()
        {
            AbbildGebaeude g = Lesen("ifc4_z6_cad.ifc");
            Assert.Null(g.Flaechengruppen);
            Assert.Null(g.FlaechengruppenBilanzM2);
        }

        [Theory]
        [InlineData(10.0, 350.0, 20.0)]
        [InlineData(180.0, 170.0, 10.0)]
        [InlineData(0.0, 180.0, 180.0)]
        public void Der_Winkelabstand_geht_ueber_Nord(double a, double b, double d)
            => Assert.Equal(d, Flaechenklassifikation.Winkelabstand(a, b), 9);
    }
}
