using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Befunde der Importproben 13–18</b> an der Probe <see cref="IfcProbenErzeuger.Importbefunde"/>: die Raumfläche aus
    /// dem Grundriss als Rückfall (Mehrzonenkonzept 6.5), Regel B2 nicht gegen <c>PredefinedType = INTERNAL</c> (6.1), die
    /// Grenzen eines zerlegten Dachs auf seiner einzigen Platte und die Grenzen ohne Bauteil benannt (6.2, P14), der äußere
    /// Splitter und die Bodenplatte an einer Grenze <c>EXTERNAL</c> (6.2, P16). Zählproben ohne Datenbank.
    /// </summary>
    public sealed class IfcImportbefundeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen()
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Importbefunde()))
                a.Lesen(s, "ifc4_importbefunde.ifc", new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static AbbildRaum Raum(AbbildGebaeude g, string name) => g.Raeume.Single(r => r.Name == name);

        private static AbbildBauteil Bauteil(AbbildGebaeude g, string name) => g.Bauteile.Single(b => b.Name == name);

        [Fact]
        public void Raum_ohne_Flaechenmenge_erhaelt_die_Flaeche_seines_Grundrisses()
        {
            AbbildGebaeude g = Lesen().Abbild.Gebaeude[0];
            AbbildRaum arbeit = Raum(g, "Arbeitsraum");
            Assert.True(arbeit.FlaecheAusGrundriss);
            Assert.Equal(20.0, arbeit.FlaecheM2.Value, 6);
            // Die Flächenmenge geht vor: Wohnen bleibt bei 30 m², obwohl sein Grundriss 36 m² misst.
            AbbildRaum wohnen = Raum(g, "Wohnen");
            Assert.NotNull(wohnen.GrundrissM);
            Assert.False(wohnen.FlaecheAusGrundriss);
            Assert.Equal(30.0, wohnen.FlaecheM2.Value, 6);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_FLAECHE_GRUNDRISS");
            Assert.Equal("1", m.Werte[0]);
            Assert.Equal("20", m.Werte[1]);
            Assert.Equal(PruefStufe.Info, m.Stufe);
        }

        [Fact]
        public void IsExternal_gegen_PredefinedType_INTERNAL_entscheidet_nicht()
        {
            AbbildGebaeude g = Lesen().Abbild.Gebaeude[0];
            AbbildRaum buero = Raum(g, "Büro");
            Assert.True(buero.Beheizt);
            Assert.Equal("B6", buero.Beheizungsregel);
            // Ohne Widerspruch gilt B2 weiter.
            AbbildRaum terrasse = Raum(g, "Terrasse");
            Assert.False(terrasse.Beheizt);
            Assert.Equal("B2", terrasse.Beheizungsregel);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_AUSSEN_WIDERSPRUCH");
            Assert.Equal("1", m.Werte[0]);
            Assert.Equal("Büro", m.Werte[1]);
        }

        [Fact]
        public void Grenzen_eines_zerlegten_Dachs_gelten_fuer_seine_einzige_Platte()
        {
            GebaeudeImportAblauf a = Lesen();
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.DoesNotContain(g.Bauteile, b => b.Name == "Dach");
            AbbildBauteil platte = Bauteil(g, "Dachplatte");
            AbbildGrenze grenze = Assert.Single(platte.Grenzen);
            Assert.Equal(30.0, grenze.FlaecheM2.Value, 6);
            Assert.Equal(Bauteilart.Dach, platte.Art);
            PruefMeldung m = Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_GRENZEN_DACHPLATTE");
            Assert.Equal(new[] { "1", "1" }, m.Werte.Take(2).ToArray());
        }

        [Fact]
        public void Grenzen_ohne_Bauteil_werden_je_Art_mit_Flaeche_gemeldet()
        {
            GebaeudeImportAblauf a = Lesen();
            PruefMeldung m = Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_GRENZEN_OHNE_BAUTEIL");
            Assert.Equal(new[] { "1", "IfcColumn", "0.6" }, m.Werte.Take(3).ToArray());
            // Jede andere Grenze ist von einem Bauteil übernommen und trägt ein Polygon.
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.Equal(g.ZahlGrenzen - 1, g.Bauteile.Sum(b => b.Grenzen.Count));
            Assert.All(g.Bauteile.SelectMany(b => b.Grenzen), x => Assert.True(x.FlaecheM2.HasValue));
        }

        [Fact]
        public void Aeusserer_Splitter_macht_keine_Decke_zum_Aussenbauteil()
        {
            GebaeudeImportAblauf a = Lesen();
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            AbbildBauteil decke = Bauteil(g, "Geschossdecke");
            Assert.Equal(Randbedingung.Innen, decke.Randbedingung);
            Assert.Equal(Bauteilart.Decke, decke.Art);
            // Eine äußere Grenze über der Schwelle (14 %) entscheidet weiter.
            Assert.Equal(Randbedingung.Aussenluft, Bauteil(g, "Kragplatte").Randbedingung);
            PruefMeldung m = Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_AUSSEN_SPLITTER");
            Assert.Equal(new[] { "1", "Geschossdecke" }, m.Werte.Take(2).ToArray());
        }

        [Fact]
        public void Bodenplatte_an_einer_Grenze_EXTERNAL_ist_erdberuehrt()
        {
            AbbildGebaeude g = Lesen().Abbild.Gebaeude[0];
            AbbildBauteil boden = Bauteil(g, "Bodenplatte");
            Assert.Equal(Randbedingung.Erdreich, boden.Randbedingung);
            Assert.Equal(Bauteilart.Bodenplatte, boden.Art);
        }

        [Fact]
        public void Einzonenweg_zaehlt_die_Geschossdecke_nicht_als_Dach()
        {
            GebaeudeImportAblauf a = Lesen();
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, IfcImportProfil.ZONENREGEL_Z5);
            Assert.DoesNotContain(v.Zeilen, z => z.Bauteil.Bezeichner == "Geschossdecke" && z.Summenfeld == GebaeudeZielfelder.FLAECHE_DACH);
            Assert.Contains(v.Zeilen, z => z.Bauteil.Bezeichner == "Bodenplatte" && z.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND);
        }
    }
}
