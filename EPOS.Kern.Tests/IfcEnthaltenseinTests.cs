using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Räume über das Enthaltensein und der Mengenrückfall</b> (Mehrzonenkonzept 6.5) an der Probe
    /// <c>ifc2x3_enthaltensein.ifc</c> (<see cref="IfcProbenErzeuger.Enthaltensein"/>): Geschosse und Räume,
    /// die über <c>IfcRelContainedInSpatialStructure</c> statt <c>IfcRelAggregates</c> hängen, werden Räume
    /// des Gebäudes — einmal, auch wenn beide Wege sie erreichen; Fläche, Volumen und Höhe fallen auf
    /// <c>Area</c>/<c>Volume</c>/<c>Height</c> eines fremden Mengensatzes zurück und werden benannt. Die
    /// Gegenprobe hält den Weg der Zerlegung (<c>ifc2x3_haus.ifc</c>) ohne die neuen Meldungen.
    /// </summary>
    public sealed class IfcEnthaltenseinTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string PROBE = "ifc2x3_enthaltensein.ifc";

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(string name)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), name);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.True(a.Abbild != null && a.Abbild.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a;
        }

        private static void Nah(double erwartet, double? ist)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= 1e-9, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        private static List<PruefMeldung> Meldungen(AbbildGebaeude g, string schluessel)
            => g.Meldungen.Where(m => m.Schluessel == P + schluessel).ToList();

        [Fact]
        public void Raeume_ueber_das_Enthaltensein_werden_einmal_Raeume_des_Gebaeudes()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();

            Assert.Equal("Probengebäude", g.Name);
            Assert.Equal(new[] { "EG", "OG" }, g.Geschosse.Select(s => s.Name).ToArray());
            Assert.Equal(new[] { "Abstellraum", "Bad", "Küche", "Schlafen", "Wohnen" },
                         g.Raeume.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
            Assert.Equal(g.Raeume.Count, g.Raeume.Select(r => r.Kennung).Distinct().Count());
            Assert.Equal(2, g.ZahlGeschosseMitRaeumen);
            Assert.All(g.Raeume, r => Assert.NotNull(r.GeschossKennung));

            AbbildRaum abstell = g.Raeume.Single(r => r.Name == "Abstellraum");
            Assert.False(abstell.Beheizt);
            Assert.Equal(BeheiztQuelle.Name, abstell.BeheiztQuelle);
            Assert.Equal(4, g.Raeume.Count(r => r.Beheizt));

            PruefMeldung struktur = Assert.Single(Meldungen(g, "STRUKTUR_ENTHALTEN"));
            Assert.Equal(PruefStufe.Info, struktur.Stufe);
            Assert.Equal(new[] { "Probengebäude", "2", "4" }, struktur.Werte);   // die Küche hängt auch über die Zerlegung
            Assert.Empty(Meldungen(g, "KEINE_RAEUME"));
        }

        [Fact]
        public void Mengen_fallen_auf_Area_Volume_Height_eines_fremden_Satzes_zurueck_und_werden_benannt()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            AbbildRaum Raum(string name) => g.Raeume.Single(r => r.Name == name);

            Nah(40.0, Raum("Wohnen").FlaecheM2);
            Nah(100.0, Raum("Wohnen").VolumenM3);
            Nah(2.5, Raum("Wohnen").HoeheM);
            Nah(20.0, Raum("Küche").FlaecheM2);
            Nah(30.0, Raum("Schlafen").FlaecheM2);
            Nah(2.4, Raum("Schlafen").HoeheM);
            Nah(5.0, Raum("Abstellraum").FlaecheM2);
            // Der Standard geht vor: 10 m² aus BaseQuantities, nicht 999 m² aus dem fremden Satz.
            Nah(10.0, Raum("Bad").FlaecheM2);
            Nah(24.0, Raum("Bad").VolumenM3);
            Nah(2.4, Raum("Bad").HoeheM);

            List<PruefMeldung> rueckfall = Meldungen(g, "MENGE_RUECKFALL");
            Assert.All(rueckfall, m => Assert.Equal(PruefStufe.Warnung, m.Stufe));
            Assert.Equal(new[]
            {
                "Probengebäude|4|NetFloorArea|CAD_RaumQuantities|Area",
                "Probengebäude|4|NetVolume|CAD_RaumQuantities|Volume",
                "Probengebäude|4|Height|CAD_RaumQuantities|Height",
            }, rueckfall.Select(m => string.Join("|", m.Werte)).ToArray());

            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Nah(100.0, satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            Nah(246.0, satz.Zeile(GebaeudeZielfelder.VOLUMEN).Wert);
            Nah(30.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);
        }

        [Fact]
        public void Der_Bauteilvorschlag_findet_die_beheizten_Raeume()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            GebaeudeBauteilvorschlag einzone = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            GebaeudeBauteilvorschlag zonen = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Assert.DoesNotContain(einzone.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.KEINE_BEHEIZTEN_RAEUME);
            Assert.DoesNotContain(zonen.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.KEINE_BEHEIZTEN_RAEUME);
        }

        [Fact]
        public void Gegenprobe_der_Weg_der_Zerlegung_bleibt_ohne_die_neuen_Meldungen()
        {
            foreach (string probe in new[] { "ifc2x3_haus.ifc", "ifc4_haus.ifc", "ifc4_zonen.ifc", "ifc4_ohne_mengen.ifc" })
            {
                GebaeudeImportAblauf a = Lesen(probe);
                foreach (AbbildGebaeude g in a.Abbild.Gebaeude)
                {
                    Assert.Empty(Meldungen(g, "STRUKTUR_ENTHALTEN"));
                    Assert.Empty(Meldungen(g, "MENGE_RUECKFALL"));
                }
            }
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_neuen_Meldungen_haben_Texte_in_beiden_Sprachen(string kultur)
        {
            CultureInfo c = CultureInfo.GetCultureInfo(kultur);
            foreach (string schluessel in new[] { P + "STRUKTUR_ENTHALTEN", P + "MENGE_RUECKFALL" })
            {
                string text = R.ResourceManager.GetString(schluessel, c);
                Assert.False(string.IsNullOrWhiteSpace(text), schluessel + " fehlt in " + kultur);
                Assert.Contains("{0}", text);
            }
            Assert.NotEqual(R.ResourceManager.GetString(P + "MENGE_RUECKFALL", CultureInfo.GetCultureInfo("de-DE")),
                            R.ResourceManager.GetString(P + "MENGE_RUECKFALL", CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
