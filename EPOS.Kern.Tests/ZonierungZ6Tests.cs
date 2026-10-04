using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Zonenregel Z6 „nach Raumtemperatur und Nutzung"</b> (<see cref="GebaeudeZonierung"/>, Mehrzonenkonzept 6.1):
    /// Gruppen gebäudeweit aus Beheizung und gerundeter Raumsolltemperatur, Nutzungsklasse aus Raumtyp oder Raumnamen,
    /// Räume ohne Temperatur, Mindestgröße ohne Grenzflächen zur nächstliegenden Temperatur, feste Ordnung, Wählbarkeit,
    /// Übersteuerung der Beheizung — am synthetischen Abbild und an den zwei selbst erzeugten Proben
    /// <c>ifc4_z6_sollwerte.ifc</c> und <c>ifc4_z6_cad.ifc</c>. Ohne Datenbank.
    /// </summary>
    public sealed class ZonierungZ6Tests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string Z6 = IfcImportProfil.ZONENREGEL_Z6;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // ------------------------------------------------------------------
        //  Synthetisches Abbild
        // ------------------------------------------------------------------

        private static AbbildRaum R(string kennung, string name, double m2, bool beheizt, double? c, string typ = null, string geschoss = "EG")
            => new AbbildRaum { Kennung = kennung, Name = name, FlaecheM2 = m2, Beheizt = beheizt, SollHeizenC = c, Raumtyp = typ, GeschossKennung = geschoss };

        private static GebaeudeAbbild Abbild(params AbbildRaum[] raeume)
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_IFC };
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude" };
            g.Raeume.AddRange(raeume);
            a.Gebaeude.Add(g);
            return a;
        }

        /// <summary>20/15/10 °C, beheizt und unbeheizt, zwei Räume ohne Temperatur, zwei Geschosse.</summary>
        private static GebaeudeAbbild Haus() => Abbild(
            R("r1", "Büro 1", 30, true, 20.2, "mrtOffice"),
            R("r2", "Büro 2", 25, true, 19.8, "Office"),
            R("r3", "Flur", 40, true, 15.0, null),
            R("r4", "Lager", 20, false, 10.0, "mrtStore", "OG"),
            R("r5", "Abstellraum 2", 8, false, null, null, "OG"),
            R("r6", "Teeküche", 6, true, null, null, "OG"),
            R("r7", "Wohnen", 35, true, 20.0, "mrtLiving", "OG"));

        [Fact]
        public void Gruppen_nach_Beheizung_und_gerundeter_Temperatur_Raeume_ohne_Temperatur_nach_Nutzung()
        {
            GebaeudeAbbild a = Haus();
            var gruppen = GebaeudeZonierung.Z6Gruppen(a.Gebaeude[0].Raeume, r => r.Beheizt)
                                           .ToDictionary(x => x.Raum.Kennung, x => x.Kennung + "|" + (x.Warm ? "B" : "U"));
            Assert.Equal("T|20|B", gruppen["r1"]);
            Assert.Equal("T|20|B", gruppen["r2"]);
            Assert.Equal("T|15|B", gruppen["r3"]);
            Assert.Equal("T|10|U", gruppen["r4"]);
            Assert.Equal("T|10|U", gruppen["r5"]);   // ohne Temperatur, Lager wie r4, unbeheizt wie r4
            Assert.Equal("N|Kueche|B", gruppen["r6"]);  // keine Temperaturgruppe mit Küche → eigene Gruppe
            Assert.Equal("T|20|B", gruppen["r7"]);

            (IReadOnlyList<string> regeln, string vorgabe, bool grenzen) = GebaeudeZonierung.Waehlbar(a, 0);
            Assert.Equal(new[] { IfcImportProfil.ZONENREGEL_Z4, Z6, IfcImportProfil.ZONENREGEL_Z5 }, regeln);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, vorgabe);   // M7 unverändert: ohne Grenzen und Bezüge Z5
            Assert.False(grenzen);
        }

        [Fact]
        public void Zonen_in_fester_Ordnung_mit_Namen_aus_Temperatur_und_Nutzung()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(Haus(), 0, Z6);
            Assert.False(z.Abgelehnt);
            Assert.Equal(Z6, z.Regel);
            // Ohne Flächen entfällt die unbeheizte Zone (6.5); die beheizten stehen absteigend nach Temperatur, ohne Sollwert zuletzt.
            Assert.Equal(new[] { "20 °C – Büro, Wohnen", "15 °C – Verkehr", "ohne Sollwert – Küche" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "r1", "r2", "r7" }, z.Zonen[0].Raeume.Select(r => r.Kennung));
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.GRENZEN_ENTKOPPELT && m.Werte[0] == Z6);
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_OHNE_FLAECHEN && m.Werte[0] == "10 °C – Lager");
            Assert.Equal(new[] { Z6, "3", IfcImportProfil.ZONENREGEL_Z5 },
                         z.Meldungen.Last(m => m.Schluessel == I + GebaeudeZonierung.ZONENREGEL).Werte);
        }

        [Fact]
        public void Unbeheizte_Zone_gleicher_Temperatur_traegt_den_Zusatz()
        {
            GebaeudeAbbild a = Abbild(R("a", "Büro", 30, true, 20, "Office"), R("b", "Büro 2", 30, false, 20, "Office"),
                                      R("c", "Flur", 30, true, 15, "Hall"), R("d", "Flur 2", 10, true, 15, "Hall"));
            var gruppen = GebaeudeZonierung.Z6Gruppen(a.Gebaeude[0].Raeume, r => r.Beheizt).ToList();
            Assert.Equal(3, gruppen.Select(x => x.Kennung + x.Warm).Distinct().Count());
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, Z6);
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_OHNE_FLAECHEN && m.Werte[0] == "20 °C – Büro (unbeheizt)");
        }

        [Fact]
        public void Mindestgroesse_ohne_Grenzflaechen_zur_naechstliegenden_Temperatur_bei_Gleichstand_zur_groesseren()
        {
            GebaeudeAbbild a = Abbild(
                R("a", "Büro", 30, true, 20, "Office"),
                R("a2", "Büro 2", 30, true, 20, "Office"),
                R("b", "Halle", 200, true, 15, "Hall"),
                R("c", "WC", 1.5, true, 23.6, "WC"),        // 24 °C, unter max(2 m², 2 %) → zur 20-°C-Zone (näher), nicht zur größeren
                R("d", "Sauna", 1.0, true, 18, "Sauna"),     // 18 °C: 20 und 16 gleich nah → zur größeren
                R("e", "Lager", 100, true, 16, "Store"));
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, Z6);
            Assert.Equal(new[] { "20 °C – Büro", "16 °C – Lager", "15 °C – Verkehr" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "24 °C – Sanitär" }, z.Zonen[0].Zugeschlagen);
            Assert.Equal(new[] { "18 °C – Sport" }, z.Zonen[1].Zugeschlagen);
            Assert.Equal(2, z.Meldungen.Count(m => m.Schluessel == I + GebaeudeZonierung.ZONE_ZUGESCHLAGEN));
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_ZU_KLEIN);
        }

        [Fact]
        public void Zweimal_gebildet_gleiche_Reihenfolge_und_Namen()
        {
            GebaeudeZonierung z1 = GebaeudeZonierung.Bilden(Haus(), 0, Z6), z2 = GebaeudeZonierung.Bilden(Haus(), 0, Z6);
            Assert.Equal(z1.Zonen.Select(x => x.Schluessel + "=" + x.Name + ":" + string.Join(",", x.Raeume.Select(r => r.Kennung))),
                         z2.Zonen.Select(x => x.Schluessel + "=" + x.Name + ":" + string.Join(",", x.Raeume.Select(r => r.Kennung))));
            Assert.Equal(z1.Meldungen.Select(m => m.Schluessel + string.Join(";", m.Werte)), z2.Meldungen.Select(m => m.Schluessel + string.Join(";", m.Werte)));
        }

        [Fact]
        public void Nicht_waehlbar_bei_einer_Gruppe_oder_einer_Gruppe_je_Raum()
        {
            GebaeudeAbbild eine = Abbild(R("a", "Büro", 30, true, 20), R("b", "Flur", 30, true, 20.3), R("c", "Bad", 10, true, 19.6, null, "OG"));
            Assert.DoesNotContain(Z6, GebaeudeZonierung.Waehlbar(eine, 0).Regeln);
            GebaeudeAbbild jeRaum = Abbild(R("a", "Büro", 30, true, 20), R("b", "Flur", 30, true, 15), R("c", "Bad", 10, true, 24, null, "OG"));
            Assert.DoesNotContain(Z6, GebaeudeZonierung.Waehlbar(jeRaum, 0).Regeln);
            GebaeudeAbbild einRaum = Abbild(R("a", "Büro", 30, true, 20));
            Assert.DoesNotContain(Z6, GebaeudeZonierung.Waehlbar(einRaum, 0).Regeln);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(eine, 0, Z6);
            Assert.True(z.Abgelehnt);
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONENREGEL_UNGUELTIG);
            // gbXML kennt Z6 nicht.
            GebaeudeAbbild xml = Haus();
            xml.Format = GebaeudeQuelle.FORMAT_GBXML;
            Assert.DoesNotContain(Z6, GebaeudeZonierung.Waehlbar(xml, 0).Regeln);
        }

        [Theory]
        [InlineData("mrtOffice", null, "Buero")]
        [InlineData("Conference", null, "Buero")]
        [InlineData("mrtRestaurant", null, "Gastronomie")]
        [InlineData("mrtStairway", null, "Verkehr")]
        [InlineData("mrtHallWay", null, "Verkehr")]
        [InlineData("mrtConnection", "Hausanschlussraum", "Technik")]
        [InlineData("mrtAdjoiningRoom", "Nebenraum", "Sonstige")]
        [InlineData("mrtFitness", null, "Sport")]
        [InlineData("mrtSleeping", null, "Schlafen")]
        [InlineData("mrtBath", null, "Sanitaer")]
        [InlineData("mrtKitchen", null, "Kueche")]
        [InlineData("mrtLiving", null, "Wohnen")]
        [InlineData("Unbekannt", "Büro 3", "Buero")]
        [InlineData(null, "Treppenhaus", "Verkehr")]
        [InlineData(null, "Corridor 2", "Verkehr")]
        [InlineData(null, "Keller", "Lager")]
        [InlineData(null, "Plant room", "Technik")]
        [InlineData(null, "Bedroom 1", "Schlafen")]
        [InlineData(null, "Raum 12", "Sonstige")]
        [InlineData(null, null, "Sonstige")]
        public void Nutzungsklasse_aus_Raumtyp_sonst_Raumname(string typ, string name, string klasse)
        {
            Assert.Equal(klasse, GebaeudeZonierung.Nutzungsklasse(new AbbildRaum { Kennung = "x", Name = name, Raumtyp = typ }));
            Assert.Contains(klasse, GebaeudeZonierung.NUTZUNGSKLASSEN);
        }

        [Fact]
        public void Jede_Nutzungsklasse_hat_Text_in_beiden_Sprachen_und_Z6_eine_Beschriftung()
        {
            foreach (string k in GebaeudeZonierung.NUTZUNGSKLASSEN)
            {
                string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GIMP_NUTZUNG_" + k.ToUpperInvariant(), System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
                string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GIMP_NUTZUNG_" + k.ToUpperInvariant(), System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                Assert.False(string.IsNullOrEmpty(de), k);
                Assert.False(string.IsNullOrEmpty(en), k);
            }
            Assert.Equal("Z6 – nach Raumtemperatur und Nutzung", GebaeudeZuordnungsModell.ZonenregelText(Z6));
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Z6 – by room temperature and use", GebaeudeZuordnungsModell.ZonenregelText(Z6));
                Assert.Equal("20 °C – Office, Living", GebaeudeZonierung.Bilden(Haus(), 0, Z6).Zonen[0].Name);
            }
        }

        // ------------------------------------------------------------------
        //  Selbst erzeugte Proben
        // ------------------------------------------------------------------

        [Fact]
        public void Probe_mit_Sollwerten_des_Standards()
        {
            GebaeudeAbbild a = BauteilvorschlagProbe.Lesen("ifc4_z6_sollwerte.ifc").Abbild;
            AbbildGebaeude g = a.Gebaeude[0];
            Assert.Equal(20.4, g.Raeume.Single(r => r.Name == "Büro 2").SollHeizenC.Value, 6);
            Assert.Null(g.Raeume.Single(r => r.Name == "Archiv").SollHeizenC);
            (IReadOnlyList<string> regeln, string vorgabe, _) = GebaeudeZonierung.Waehlbar(a, 0);
            Assert.Equal(new[] { IfcImportProfil.ZONENREGEL_Z4, Z6, IfcImportProfil.ZONENREGEL_Z5 }, regeln);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, vorgabe);

            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, Z6);
            Assert.Equal(new[] { "20 °C – Büro", "15 °C – Lager, Verkehr", "ohne Sollwert – Sonstige" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Büro 1", "Büro 2", "Besprechung", "WC" }, z.Zonen[0].Raeume.Select(r => r.Name));
            Assert.Equal(new[] { "24 °C – Sanitär" }, z.Zonen[0].Zugeschlagen);
            Assert.Equal(new[] { "Flur", "Lager 1", "Lager 2" }, z.Zonen[1].Raeume.Select(r => r.Name));
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.GRENZEN_ENTKOPPELT && m.Werte[0] == Z6);

            // Regelwechsel von Hand wie zwischen Z4 und Z5.
            Assert.Equal(2, GebaeudeZonierung.Bilden(a, 0, IfcImportProfil.ZONENREGEL_Z4).Zonen.Count);
            Assert.Single(GebaeudeZonierung.Bilden(a, 0, IfcImportProfil.ZONENREGEL_Z5).Zonen);
        }

        [Fact]
        public void Probe_nach_dem_Muster_eines_CAD_Exports_und_Uebersteuerung()
        {
            GebaeudeAbbild a = BauteilvorschlagProbe.Lesen("ifc4_z6_cad.ifc").Abbild;
            AbbildGebaeude g = a.Gebaeude[0];
            AbbildRaum buero = g.Raeume.Single(r => r.Name == "Büroraum");
            Assert.Equal("Office", buero.Raumtyp);
            Assert.Equal(20.0, buero.RaumtemperaturC);
            Assert.Null(buero.SollHeizenC);   // die Raumtemperatur der Datei ist kein Sollwert
            Assert.Equal("Living", g.Raeume.Single(r => r.Name == "Wohnraum").Raumtyp);
            Assert.True(g.Raeume.Single(r => r.Name == "Wohnraum").Beheizt);
            Assert.False(g.Raeume.Single(r => r.Name == "Abstellraum").Beheizt);
            Assert.Contains(Z6, GebaeudeZonierung.Waehlbar(a, 0).Regeln);

            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, Z6);
            // Die unbeheizte Gruppe {Lagerraum, Abstellraum} bildet „10 °C – Lager"; ohne Raumgrenzen trägt sie keine Fläche und
            // entfällt benannt (6.5) — ihre Räume bleiben außerhalb der Zonen.
            Assert.Equal(new[] { "20 °C – Büro, Wohnen, Sanitär", "15 °C – Verkehr" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Büroraum", "Büroraum 2", "WC-Raum", "Wohnraum", "Raum 7" }, z.Zonen[0].Raeume.Select(r => r.Name));
            Assert.Equal(new[] { "10 °C – Lager", "35" },
                         Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_OHNE_FLAECHEN).Werte);
            Assert.Equal(-1, z.ZoneVon(g.Raeume.Single(r => r.Name == "Lagerraum").Kennung));
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, z.Vorgabe);   // M7: die Trenndecke aus den Raumbezügen koppelt die Geschosse

            // Übersteuerung: der Abstellraum beheizt → eigene beheizte 10-°C-Zone, der Lagerraum ohne Temperatur bleibt unbeheizt ohne Sollwert.
            string abstell = g.Raeume.Single(r => r.Name == "Abstellraum").Kennung;
            var haken = new Dictionary<string, bool>(StringComparer.Ordinal) { [abstell] = true };
            GebaeudeZonierung u = GebaeudeZonierung.Bilden(a, 0, Z6, haken);
            Importzone zehn = u.Zonen.Single(x => x.Raeume.Any(r => r.Kennung == abstell));
            Assert.True(zehn.IstBeheizt);
            Assert.StartsWith("10 °C – Lager", zehn.Name);
            Assert.DoesNotContain(u.Zonen, x => x.Raeume.Any(r => r.Name == "Lagerraum") && x.Raeume.Any(r => r.Kennung == abstell));
        }
    }
}
