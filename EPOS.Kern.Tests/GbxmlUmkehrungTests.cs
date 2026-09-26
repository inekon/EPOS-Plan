using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7a, Welle 1 — die Umkehrtabelle</b> (<see cref="GbxmlUmkehrung"/>; Umsetzungsauftrag
    /// G7a, 2.3 „Umkehrtabelle"): vollständig 9 Bauteilarten × 5 Randbedingungen × 3 Wärmestromrichtungen,
    /// jede Zelle in sich stimmig, die ausdrücklich geregelten Zellen einzeln — und die <b>Rückkehr jeder
    /// Zelle am eigenen Import gemessen</b>: Das Probengebäude wird geschrieben, mit dem gbXML-Leser
    /// gelesen und über den Bauteilvorschlag (<see cref="GebaeudeBauteilvorschlag"/>) wieder zu
    /// EPOS-Zeilen; deren Art und wirksame Randbedingung müssen die erwartete Rückkehr der Zelle sein.
    /// </summary>
    public sealed class GbxmlUmkehrungTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public GbxmlUmkehrungTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        [Fact]
        public void Die_Tabelle_ist_vollstaendig_neun_mal_fuenf_mal_drei()
        {
            Assert.Equal(9, GbxmlUmkehrung.Arten.Count);
            Assert.Equal(5, GbxmlUmkehrung.Spalten.Count);
            Assert.Equal(3, GbxmlUmkehrung.Richtungen.Count);
            Assert.Equal(135, GbxmlUmkehrung.Tabelle.Count);
            foreach (Bauteilart art in GbxmlUmkehrung.Arten)
                foreach (Umkehrspalte spalte in GbxmlUmkehrung.Spalten)
                    foreach (Waermestromrichtung r in GbxmlUmkehrung.Richtungen)
                        Assert.Single(GbxmlUmkehrung.Tabelle, z => z.Art == art && z.Spalte == spalte && z.Richtung == r);
            Assert.Equal(DbWerte.BAUTEILARTEN.Count, GbxmlUmkehrung.Arten.Count);
        }

        [Fact]
        public void Jede_Zelle_ist_in_sich_stimmig()
        {
            foreach (Umkehrzelle z in GbxmlUmkehrung.Tabelle)
            {
                string wo = z.ToString();
                Assert.Equal(GebaeudeZonenabbildung.RandAusZeile(z.Art, GbxmlUmkehrung.Wert(z.Spalte)), z.Rand);
                if (z.Spalte == Umkehrspalte.Zone)
                {
                    Assert.True(z.Ergebnis == Umkehrergebnis.Ablehnung, wo);
                    Assert.Equal(GbxmlUmkehrung.GRUND_ZONE, z.Grund);
                    Assert.Null(z.Flaechenart);
                    Assert.Null(z.Oeffnungsart);
                    Assert.Null(z.RueckArt);
                    continue;
                }
                Assert.True(z.Ergebnis != Umkehrergebnis.Ablehnung, wo);
                Assert.True((z.Flaechenart == null) != (z.Oeffnungsart == null), wo + ": genau eine Flächen- oder Öffnungsart");
                if (z.Flaechenart != null) Assert.True(GbxmlVokabular.Flaechenarten.ContainsKey(z.Flaechenart), wo);
                if (z.Oeffnungsart != null)
                {
                    Assert.True(GbxmlVokabular.Oeffnungsarten.ContainsKey(z.Oeffnungsart), wo);
                    Assert.Equal(Umkehrnachbarn.ImWirt, z.Nachbarn);
                }
                // Gleich ohne Namen, jeder Wechsel benannt.
                Assert.True((z.Ergebnis == Umkehrergebnis.Gleich) == (z.Grund == null), wo);
                // Die Nachbarn tragen die Randbedingung.
                if (z.Flaechenart != null)
                {
                    Umkehrnachbarn erwartet = z.Rand == Bauteilrand.Unbeheizt ? Umkehrnachbarn.MitPlatzhalter
                                            : z.Rand == Bauteilrand.Innen ? Umkehrnachbarn.InnenBeidseitig
                                            : Umkehrnachbarn.EigenerRaum;
                    Assert.True(erwartet == z.Nachbarn, wo);
                    Assert.Equal(z.Rand, z.RueckRand);
                }
                // Die Sicht nennt Boden oder Decke, nie eine Wand; bei innerer Masse die Gegensicht dazu.
                if (z.Sicht != null) Assert.NotNull(GebaeudeAggregation.SichtIstBoden(z.Sicht));
                if (z.GegenSicht != null) Assert.Equal(!GebaeudeAggregation.SichtIstBoden(z.Sicht), GebaeudeAggregation.SichtIstBoden(z.GegenSicht));
            }
        }

        [Theory]
        [InlineData(nameof(Bauteilart.Aussenwand), nameof(Umkehrspalte.Unbeheizt), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.InteriorWall, nameof(Bauteilart.Innenwand), nameof(Bauteilrand.Unbeheizt), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Dach), nameof(Umkehrspalte.Erdreich), nameof(Waermestromrichtung.Aufwaerts), GbxmlVokabular.UndergroundCeiling, nameof(Bauteilart.Decke), nameof(Bauteilrand.Erdreich), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Dach), nameof(Umkehrspalte.Unbeheizt), nameof(Waermestromrichtung.Aufwaerts), GbxmlVokabular.Ceiling, nameof(Bauteilart.Decke), nameof(Bauteilrand.Unbeheizt), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Innenwand), nameof(Umkehrspalte.Aussenluft), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.ExteriorWall, nameof(Bauteilart.Aussenwand), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Innenwand), nameof(Umkehrspalte.Erdreich), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.UndergroundWall, nameof(Bauteilart.Aussenwand), nameof(Bauteilrand.Erdreich), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Innenwand), nameof(Umkehrspalte.Unbeheizt), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.InteriorWall, nameof(Bauteilart.Innenwand), nameof(Bauteilrand.Unbeheizt), nameof(Umkehrergebnis.Gleich))]
        [InlineData(nameof(Bauteilart.Innenwand), nameof(Umkehrspalte.Leer), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.InteriorWall, nameof(Bauteilart.Innenwand), nameof(Bauteilrand.Innen), nameof(Umkehrergebnis.Gleich))]
        [InlineData(nameof(Bauteilart.Decke), nameof(Umkehrspalte.Leer), nameof(Waermestromrichtung.Abwaerts), GbxmlVokabular.InteriorFloor, nameof(Bauteilart.Decke), nameof(Bauteilrand.Innen), nameof(Umkehrergebnis.Gleich))]
        [InlineData(nameof(Bauteilart.Decke), nameof(Umkehrspalte.Aussenluft), nameof(Waermestromrichtung.Aufwaerts), GbxmlVokabular.Roof, nameof(Bauteilart.Dach), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Aussenwand), nameof(Umkehrspalte.Leer), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.ExteriorWall, nameof(Bauteilart.Aussenwand), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Gleich))]
        [InlineData(nameof(Bauteilart.Bodenplatte), nameof(Umkehrspalte.Unbeheizt), nameof(Waermestromrichtung.Abwaerts), GbxmlVokabular.InteriorFloor, nameof(Bauteilart.Decke), nameof(Bauteilrand.Unbeheizt), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Sonstiges), nameof(Umkehrspalte.Aussenluft), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.ExteriorWall, nameof(Bauteilart.Aussenwand), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Sonstiges), nameof(Umkehrspalte.Aussenluft), nameof(Waermestromrichtung.Aufwaerts), GbxmlVokabular.Roof, nameof(Bauteilart.Dach), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Sonstiges), nameof(Umkehrspalte.Leer), nameof(Waermestromrichtung.Abwaerts), GbxmlVokabular.ExposedFloor, nameof(Bauteilart.Bodenplatte), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Vorhangfassade), nameof(Umkehrspalte.Aussenluft), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.FixedWindow, nameof(Bauteilart.Fenster), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Fenster), nameof(Umkehrspalte.Erdreich), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.FixedWindow, nameof(Bauteilart.Fenster), nameof(Bauteilrand.Aussenluft), nameof(Umkehrergebnis.Wechsel))]
        [InlineData(nameof(Bauteilart.Tuer), nameof(Umkehrspalte.Erdreich), nameof(Waermestromrichtung.Horizontal), GbxmlVokabular.NonSlidingDoor, nameof(Bauteilart.Tuer), nameof(Bauteilrand.Erdreich), nameof(Umkehrergebnis.Gleich))]
        public void Die_ausdruecklich_geregelten_Zellen(string art, string spalte, string r, string ziel,
                                                        string rueckArt, string rueckRand, string ergebnis)
        {
            // Die Aufzählungen sind intern; die Theorie trägt ihre Namen.
            Umkehrzelle z = GbxmlUmkehrung.Zelle(Enum.Parse<Bauteilart>(art), Enum.Parse<Umkehrspalte>(spalte), Enum.Parse<Waermestromrichtung>(r));
            Assert.Equal(ziel, z.Flaechenart ?? z.Oeffnungsart);
            Assert.Equal(Enum.Parse<Bauteilart>(rueckArt), z.RueckArt);
            Assert.Equal(Enum.Parse<Bauteilrand>(rueckRand), z.RueckRand);
            Assert.Equal(Enum.Parse<Umkehrergebnis>(ergebnis), z.Ergebnis);
        }

        [Fact]
        public void Die_Zelle_einer_Zeile_folgt_LeerHeisstInnen_und_der_wirksamen_Neigung()
        {
            Assert.Equal(Bauteilrand.Innen, GbxmlUmkehrung.Zelle(Bauteilart.Innenwand, null, 90.0).Rand);
            Assert.Equal(Bauteilrand.Innen, GbxmlUmkehrung.Zelle(Bauteilart.Decke, null, 0.0).Rand);
            Assert.Equal(Bauteilrand.Aussenluft, GbxmlUmkehrung.Zelle(Bauteilart.Aussenwand, null, 90.0).Rand);
            Assert.Equal(GbxmlVokabular.Ceiling, GbxmlUmkehrung.Zelle(Bauteilart.Decke, DbWerte.RANDBEDINGUNG_UNBEHEIZT, 59.0).Flaechenart);
            Assert.Equal(GbxmlVokabular.Ceiling, GbxmlUmkehrung.Zelle(Bauteilart.Decke, DbWerte.RANDBEDINGUNG_UNBEHEIZT, 120.0).Flaechenart);
            Assert.Equal(GbxmlVokabular.InteriorFloor, GbxmlUmkehrung.Zelle(Bauteilart.Decke, DbWerte.RANDBEDINGUNG_UNBEHEIZT, 121.0).Flaechenart);
            Assert.Equal(Umkehrergebnis.Ablehnung, GbxmlUmkehrung.Zelle(Bauteilart.Aussenwand, DbWerte.RANDBEDINGUNG_ZONE, 90.0).Ergebnis);
            Assert.Throws<ArgumentException>(() => GbxmlUmkehrung.Zelle(Bauteilart.Aussenwand, "KELLER", 90.0));
            Assert.Throws<GebaeudeModellException>(() => GbxmlUmkehrung.Zelle(Bauteilart.Aussenwand, null, 181.0));
        }

        // ==================================================================
        //  Die Rückkehr — am eigenen Import gemessen
        // ==================================================================

        [Fact]
        public void Die_Rueckkehr_jeder_Zelle_wird_am_Leser_gemessen()
        {
            var abweichungen = new List<string>();
            int gemessen = 0;
            foreach (Umkehrzelle z in GbxmlUmkehrung.Tabelle.Where(x => x.Ergebnis != Umkehrergebnis.Ablehnung))
            {
                List<(Bauteilart Art, Bauteilrand Rand)> rueck = Rueckkehr(z);
                gemessen++;
                int erwartetZeilen = z.Nachbarn == Umkehrnachbarn.InnenBeidseitig ? 2 : 1;
                if (rueck.Count != erwartetZeilen)
                {
                    abweichungen.Add(z + ": " + rueck.Count + " Zeilen statt " + erwartetZeilen);
                    continue;
                }
                foreach ((Bauteilart art, Bauteilrand rand) in rueck)
                    if (art != z.RueckArt || rand != z.RueckRand)
                        abweichungen.Add(z + ": zurück " + art + "/" + rand);
            }
            _ausgabe.WriteLine(gemessen + " Zellen gemessen, " + abweichungen.Count + " Abweichungen.");
            Assert.Equal(108, gemessen);
            Assert.True(abweichungen.Count == 0, string.Join("\n", abweichungen));
        }

        /// <summary>Schreibt das Probengebäude einer Zelle, liest es und liefert Art und wirksame Randbedingung der Zeilen der Probe.</summary>
        private static List<(Bauteilart, Bauteilrand)> Rueckkehr(Umkehrzelle z)
        {
            const int GEB = 1, ZONE = 1;
            string raum = GebaeudeExportKennung.Raum(ZONE), platz = GebaeudeExportKennung.Unbeheizt(GEB);
            var a = new GbxmlAbbild { CampusKennung = GebaeudeExportKennung.Campus(GEB) };
            var g = new AbbildGebaeude { Kennung = GebaeudeExportKennung.Gebaeude(GEB), Name = "Umkehrprobe", Art = "SingleFamily" };
            a.Gebaeude.Add(g);
            g.Raeume.Add(new AbbildRaum
            {
                Kennung = raum, Name = "Zone", FlaecheM2 = 100.0, VolumenM3 = 250.0, Beheizt = true,
                ZonenKennung = GebaeudeExportKennung.Zone(ZONE), SollHeizenC = 20.0,
            });
            g.Raeume.Add(new AbbildRaum { Kennung = platz, Name = "unbeheizt", Beheizt = false });

            // Die Hülle drumherum: Dach, Bodenplatte, eine Außenwand.
            g.Bauteile.Add(GbxmlExportProbe.Flaeche(901, GbxmlVokabular.Roof, Massiv("d"), 10.0, 10.0, 180.0, 0.0,
                                                    GbxmlExportProbe.N(raum, GbxmlVokabular.Roof)));
            g.Bauteile.Add(GbxmlExportProbe.Flaeche(902, GbxmlVokabular.SlabOnGrade, Massiv("b"), 10.0, 10.0, 180.0, 180.0,
                                                    GbxmlExportProbe.N(raum, GbxmlVokabular.SlabOnGrade)));
            g.Bauteile.Add(GbxmlExportProbe.Flaeche(903, GbxmlVokabular.ExteriorWall, Massiv("w"), 10.0, 2.5, 0.0, 90.0,
                                                    GbxmlExportProbe.N(raum)));

            double neigung = z.Richtung == Waermestromrichtung.Aufwaerts ? 0.0 : z.Richtung == Waermestromrichtung.Abwaerts ? 180.0 : 90.0;
            string kennung;
            if (z.Oeffnungsart == null)
            {
                AbbildBauteil probe = GbxmlExportProbe.Flaeche(1, z.Flaechenart, Massiv("p"), 10.0, 10.0, 180.0, neigung,
                                                               Nachbarn(z, raum, platz));
                g.Bauteile.Add(probe);
                kennung = probe.Kennung;
            }
            else
            {
                // Der Wirt: eine Außenwand derselben Spalte, senkrecht.
                Umkehrzelle w = GbxmlUmkehrung.Zelle(Bauteilart.Aussenwand, z.Spalte, Waermestromrichtung.Horizontal);
                AbbildBauteil wirt = GbxmlExportProbe.Flaeche(1, w.Flaechenart, Massiv("p"), 10.0, 10.0, 180.0, 90.0,
                                                              Nachbarn(w, raum, platz));
                bool fenster = z.Oeffnungsart != GbxmlVokabular.NonSlidingDoor;
                var oeffnung = new AbbildBauteil
                {
                    Kennung = GebaeudeExportKennung.Oeffnung(2), Quelltyp = "Opening", Name = "Öffnung", Quellart = z.Oeffnungsart,
                    Art = GbxmlVokabular.Oeffnungsarten[z.Oeffnungsart], UWertWm2K = fenster ? 1.1 : 1.8, GWert = fenster ? 0.6 : (double?)null,
                    FenstertypKennung = fenster ? GebaeudeExportKennung.Fenstertyp(2) : null,
                    BreiteM = 2.0, HoeheM = 1.5, BruttoflaecheM2 = 3.0, AzimutGrad = 180.0, NeigungGrad = 90.0,
                };
                wirt.Oeffnungen.Add(oeffnung);
                g.Bauteile.Add(wirt);
                kennung = oeffnung.Kennung;
            }

            GbxmlAbbild zurueck = GbxmlExportProbe.Lesen(GbxmlExportProbe.Schreiben(a));
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(zurueck, 0, 'E', null, new GbxmlImportProfil());
            return v.Zeilen.Where(r => r.Kennung == kennung)
                    .Select(r =>
                    {
                        Bauteilart art = GebaeudeZonenabbildung.ArtAusZeile(r.Bauteil.Bauteilart).Value;
                        return (art, GebaeudeZonenabbildung.RandAusZeile(art, r.Bauteil.Randbedingung).Value);
                    }).ToList();
        }

        private static AbbildNachbar[] Nachbarn(Umkehrzelle z, string raum, string platz)
        {
            switch (z.Nachbarn)
            {
                case Umkehrnachbarn.MitPlatzhalter: return new[] { GbxmlExportProbe.N(raum, z.Sicht), GbxmlExportProbe.N(platz) };
                case Umkehrnachbarn.InnenBeidseitig: return new[] { GbxmlExportProbe.N(raum, z.Sicht), GbxmlExportProbe.N(raum, z.GegenSicht) };
                default: return new[] { GbxmlExportProbe.N(raum, z.Sicht) };
            }
        }

        /// <summary>Ein vollständiger, symmetrischer Aufbau (Putz, Kalksandstein, Putz) mit eigenen Kennungen.</summary>
        private static AbbildAufbau Massiv(string zeichen)
        {
            var a = new AbbildAufbau { Kennung = "k-" + zeichen, Name = "Massiv " + zeichen, UWertWm2K = 2.0 };
            for (int i = 1; i <= 3; i++)
            {
                bool putz = i != 2;
                a.Schichten.Add(new AbbildSchicht
                {
                    Kennung = "s-" + zeichen + "-" + i, BaustoffKennung = "m-" + zeichen + "-" + i, Name = putz ? "Putz" : "Kalksandstein",
                    DickeM = putz ? 0.01 : 0.175, LambdaWmK = putz ? 0.7 : 0.99, RhoKgM3 = putz ? 1400.0 : 1800.0, CpJkgK = 1000.0,
                });
            }
            return a;
        }
    }
}
