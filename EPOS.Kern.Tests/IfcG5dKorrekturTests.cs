using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Korrektur G5-3d am IFC-Weg</b> an den synthetischen Proben aus <see cref="IfcProbenErzeuger"/> (nur im Speicher):
    /// <list type="bullet">
    /// <item><b>Befund 1:</b> Die Kellerdecke, die die Datei als Platte gegen unbeheizt mit Raumbezug zum Erdgeschoss führt,
    /// zählt einmal — keine zusätzliche Trenndecke aus den Raumkörpern zwischen Keller und Erdgeschoss; die Körperdecke
    /// zwischen Erd- und Obergeschoss nimmt ihre Vorlage aus der Platte in ihrer Höhe.</item>
    /// <item><b>Befund 2:</b> Fenster, die laut Datei an einer Wand hängen, deren Mengensatz kleiner ist als ihre Öffnungen,
    /// gehen nach Lage an Teile derselben Wand bzw. an die Wand, in der sie liegen; der Abzug ist voll, die Meldung nennt
    /// die Zahl der Wände. Eine Öffnung in keiner Wand wird benannt.</item>
    /// </list>
    /// </summary>
    public sealed class IfcG5dKorrekturTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(byte[] inhalt, string name)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(inhalt))
                Assert.True(a.Lesen(s, name, new IfcImportProfil()) > 0,
                            string.Join(" | ", a.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            return a;
        }

        private static void Nah(double erwartet, double ist, string wo, double toleranz = 1e-6)
            => Assert.True(Math.Abs(ist - erwartet) <= toleranz,
                           wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.ToString("R", CultureInfo.InvariantCulture));

        private static IEnumerable<PruefMeldung> AlleMeldungen(GebaeudeImportAblauf a)
            => a.Meldungen.Concat(a.Abbild.Gebaeude.SelectMany(g => g.Meldungen));

        // ==================================================================
        //  Befund 1: Decke über unbeheizt einmal
        // ==================================================================

        [Fact]
        public void Kellerdecke_mit_Raumbezug_zaehlt_einmal()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.Kellerdeckenhaus(), "ifc4_g5d_kellerdecke.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            string keller = g.Raeume.Single(r => r.Name == "Keller").Kennung, wohnen = g.Raeume.Single(r => r.Name == "Wohnen").Kennung;
            AbbildBauteil kd = g.Bauteile.Single(b => b.Name == "Kellerdecke");
            Assert.Equal(Randbedingung.Unbeheizt, kd.Randbedingung);

            // Keine Körperdecke zwischen Keller und Wohnen, die Meldung nennt eine.
            Assert.DoesNotContain(g.Bauteile, b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER
                                                   && b.Nachbarn.Any(n => n.Kennung == keller) && b.Nachbarn.Any(n => n.Kennung == wohnen));
            PruefMeldung m = Assert.Single(AlleMeldungen(a), x => x.Schluessel == P + "KOERPERDECKE_HUELLE");
            Assert.Equal("1", m.Werte[1]);

            // Die Grundfläche des Einzonensatzes ist die Kellerdecke, einmal.
            Huelleneinordnung e = GebaeudeHuelleneinordnung.Einordnen(a.Abbild, 0, r => r.Beheizt);
            List<Huellposten> grund = e.Huelle.Where(p => !p.Verworfen && p.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND).ToList();
            Huellposten einzig = Assert.Single(grund);
            Assert.Same(kd, einzig.Bauteil);
            Nah(50.76, grund.Sum(p => p.BruttoM2 ?? 0.0), "Grundfläche");
        }

        [Fact]
        public void Koerperdecke_nimmt_die_Vorlage_in_ihrer_Hoehe()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.Kellerdeckenhaus(), "ifc4_g5d_kellerdecke.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            string wohnen = g.Raeume.Single(r => r.Name == "Wohnen").Kennung, schlafen = g.Raeume.Single(r => r.Name == "Schlafen").Kennung;
            AbbildBauteil t = Assert.Single(g.Bauteile, b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER);
            Assert.Equal(new[] { wohnen, schlafen }.OrderBy(x => x), t.Nachbarn.Select(n => n.Kennung).OrderBy(x => x));
            // Das „Podest“ wäre der Fläche nach näher, liegt aber nicht zwischen den Körpern.
            Assert.Equal("Geschossdecke", t.Name);
            Assert.Contains(g.Bauteile, b => b.Name == "Podest");
        }

        // ==================================================================
        //  Befund 2: Öffnungen an der Wand ihrer Lage
        // ==================================================================

        private static AbbildBauteil Wirt(AbbildGebaeude g, string oeffnung)
            => Assert.Single(g.Bauteile, b => b.Oeffnungen.Any(o => o.Name == oeffnung));

        private static List<Huellposten> Aussenwand(GebaeudeImportAblauf a)
            => GebaeudeHuelleneinordnung.Einordnen(a.Abbild, 0, r => r.Beheizt).Huelle
                   .Where(p => !p.Verworfen && p.Summenfeld == GebaeudeZielfelder.FLAECHE_AUSSENWAND).ToList();

        [Fact]
        public void Oeffnungen_der_zu_kleinen_Wand_gehen_nach_Lage()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.Oeffnungslagehaus(schwebend: false), "ifc4_g5d_oeffnungslage.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);

            // Fenster D liegt in der Ostwand, Fenster A geht an einen Teil der Südwand; B und C bleiben.
            Assert.Equal("Wand Ost", Wirt(g, "Fenster D").Name);
            Assert.StartsWith("Wand Süd-", Wirt(g, "Fenster A").Name);
            Assert.Equal("Wand Süd", Wirt(g, "Fenster B").Name);
            Assert.Equal("Wand Süd", Wirt(g, "Fenster C").Name);

            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_UMGEHAENGT");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1", "2", "2" }, m.Werte.Take(3));
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_OHNE_WAND" || x.Schluessel == P + "WAND_KLEINER_OEFFNUNGEN");

            // Voller Abzug: Außenwand netto = brutto − Öffnungen, ohne Rückfall auf die Nettofläche der Datei.
            List<Huellposten> w = Aussenwand(a);
            Assert.DoesNotContain(w, p => p.NettoRueckfall || p.NettoNegativ);
            double oeffnungen = 1.5 * 1.2 + 1.2 * 1.0 + 1.0 * 1.2 + 1.0 * 1.0;
            Nah(oeffnungen, w.Sum(p => p.AbzugM2), "Abzug");
            Nah(w.Sum(p => p.BruttoM2 ?? 0.0) - oeffnungen, w.Sum(p => p.NettoM2 ?? 0.0), "Außenwand netto");
        }

        [Fact]
        public void Oeffnung_ohne_Wand_wird_benannt()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.Oeffnungslagehaus(schwebend: true), "ifc4_g5d_oeffnungslage_schwebend.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            Assert.Equal("Wand Süd", Wirt(g, "Fenster E").Name);

            PruefMeldung ohne = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_OHNE_WAND");
            Assert.Equal(PruefStufe.Warnung, ohne.Stufe);
            Assert.Equal(new[] { "1", "Fenster E" }, ohne.Werte);
            PruefMeldung klein = Assert.Single(a.Meldungen, x => x.Schluessel == P + "WAND_KLEINER_OEFFNUNGEN");
            Assert.Equal(new[] { "1", "Wand Süd" }, klein.Werte);
            Assert.Equal("4", Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_UMGEHAENGT").Werte[1]);
        }

        [Fact]
        public void Meldungstexte_in_beiden_Sprachen()
        {
            foreach (string k in new[] { "KOERPERDECKE_HUELLE", "OEFFNUNG_UMGEHAENGT", "OEFFNUNG_OHNE_WAND", "WAND_KLEINER_OEFFNUNGEN" })
                foreach (string kultur in new[] { "de-DE", "en-US" })
                    Assert.False(string.IsNullOrWhiteSpace(
                        WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(P + k, CultureInfo.GetCultureInfo(kultur))), k + " " + kultur);
        }
    }
}
