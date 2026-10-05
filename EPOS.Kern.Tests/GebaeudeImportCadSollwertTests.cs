using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Heizsollwert importierter Gebäude und Zonen</b> (<see cref="GebaeudeCadSollwert"/>, Mehrzonenkonzept 6.5): Vorgabe
    /// ist die Normtemperatur, die Zonen erben das Gebäude; nur auf Wunsch (Schalter „Raumtemperatur der Datei als
    /// Heizsollwert übernehmen") wird die CAD-Raumtemperatur flächengewichtet gemittelt — für das Gebäude und je beheizter
    /// Zone, gerundet auf 0,1 °C, mit Beleg und Spannenhinweis. Mit Norm-Sollwert gilt der Weg über ihn. Ohne Datenbank.
    /// </summary>
    public sealed class GebaeudeImportCadSollwertTests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string PROBE = "ifc4_z6_cad.ifc";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportCadSollwertTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        // ------------------------------------------------------------------
        //  Synthetisches Abbild
        // ------------------------------------------------------------------

        private static AbbildRaum R(string kennung, double m2, bool beheizt, double? cad, double? norm = null, string geschoss = "EG")
            => new AbbildRaum { Kennung = kennung, Name = kennung, FlaecheM2 = m2, Beheizt = beheizt, RaumtemperaturC = cad,
                                SollHeizenC = norm, GeschossKennung = geschoss };

        private static GebaeudeAbbild Abbild(params AbbildRaum[] raeume)
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_IFC };
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude" };
            g.Raeume.AddRange(raeume);
            a.Gebaeude.Add(g);
            return a;
        }

        /// <summary>20 °C auf 100 m², 22 °C auf 50 m², ein beheizter Raum ohne Temperatur, ein unbeheizter mit 10 °C.</summary>
        private static GebaeudeAbbild Haus() => Abbild(
            R("a", 100, true, 20.0), R("b", 50, true, 22.0), R("c", 30, true, null), R("d", 40, false, 10.0));

        private static GebaeudeImportSatz Satz(GebaeudeAbbild a, bool schalter)
            => GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil(), null, schalter);

        private static GebaeudeFeldzeile Tag(GebaeudeImportSatz s) => s.Zeile(GebaeudeZielfelder.SOLL_TAG);

        [Fact]
        public void Schalter_aus_laesst_die_Normtemperatur_als_Vorgabe()
        {
            GebaeudeImportSatz s = Satz(Haus(), false);
            Assert.True(s.CadSollwertMoeglich);
            Assert.False(s.CadSollwertAktiv);
            Assert.Equal(GebaeudeStammCtrl.SOLLTEMPERATUR_TAG_VORGABE, Tag(s).Wert);
            Assert.Equal(Importherkunft.Vorgabe, Tag(s).Herkunft);
            Assert.Equal("GIMP_BELEG_SOLLWERT_VORGABE", Tag(s).Beleg.Schluessel);
            Assert.Equal(18.0, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);
            Assert.DoesNotContain(s.Meldungen, m => m.Schluessel.Contains("SOLLWERT_CAD", StringComparison.Ordinal));
        }

        [Fact]
        public void Schalter_ein_mittelt_flaechengewichtet_mit_Beleg_und_Rundung()
        {
            GebaeudeImportSatz s = Satz(Haus(), true);
            Assert.True(s.CadSollwertAktiv);
            // (20·100 + 22·50) / 150 = 20,666… → 20,7; der Raum ohne Temperatur bleibt außen vor, der unbeheizte auch.
            Assert.Equal(20.7, Tag(s).Wert);
            Assert.Equal(Importherkunft.Ifc, Tag(s).Herkunft);
            Assert.Equal(GebaeudeCadSollwert.BELEG, Tag(s).Beleg.Schluessel);
            Assert.Equal(new[] { "2", "20", "22", "1" }, Tag(s).Beleg.Werte);
            Assert.Equal(GebaeudeStammCtrl.SOLLTEMPERATUR_TAG_VORGABE, Tag(s).VorgabeWert);
            Assert.Equal("Mittel der Raumtemperaturen der Datei, flächengewichtet, von 2 beheizten Räumen (20 bis 22 °C; 1 Räume ohne Angabe)",
                         GebaeudeZuordnungsModell.BelegText(Tag(s).Beleg));
            Assert.Equal(18.0, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);
            Assert.True(Tag(s).Eingebbar);
            // Spanne 2 K: kein Hinweis.
            Assert.DoesNotContain(s.Meldungen, m => m.Schluessel == I + GebaeudeCadSollwert.SOLLWERT_CAD_SPANNE);
        }

        [Fact]
        public void Spanne_ueber_2_K_gibt_einen_Hinweis_und_der_Nachtwert_bleibt_unter_dem_Tag()
        {
            GebaeudeImportSatz s = Satz(Abbild(R("a", 10, true, 15.0), R("b", 10, true, 17.1)), true);
            Assert.Equal(16.1, Tag(s).Wert);   // 16,05 → 16,1 (kaufmännisch)
            PruefMeldung m = Assert.Single(s.Meldungen, x => x.Schluessel == I + GebaeudeCadSollwert.SOLLWERT_CAD_SPANNE);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "Gebäude", "15", "17.1", "2" }, m.Werte);
            Assert.Equal(16.1, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);
        }

        [Fact]
        public void Ohne_CAD_Temperatur_wirkt_der_Schalter_nicht()
        {
            GebaeudeImportSatz s = Satz(Abbild(R("a", 50, true, null), R("b", 40, false, 12.0)), true);
            Assert.False(s.CadSollwertMoeglich);
            Assert.False(s.CadSollwertAktiv);
            Assert.Equal(GebaeudeStammCtrl.SOLLTEMPERATUR_TAG_VORGABE, Tag(s).Wert);
            Assert.Equal(Importherkunft.Vorgabe, Tag(s).Herkunft);
        }

        [Fact]
        public void Mit_Norm_Sollwert_bleibt_der_Weg_ueber_ihn()
        {
            // Einheitlich: der Norm-Sollwert gilt, der Schalter ist nicht wählbar.
            GebaeudeImportSatz s = Satz(Abbild(R("a", 50, true, 18.0, 21.0), R("b", 40, true, 25.0, 21.0)), true);
            Assert.False(s.CadSollwertMoeglich);
            Assert.Equal(21.0, Tag(s).Wert);
            Assert.Equal("GIMP_BELEG_SOLLWERT", Tag(s).Beleg.Schluessel);
            // Uneinheitlich: die Info bleibt, wo sie entsteht, der Tag bleibt bei der Vorgabe — auch mit Schalter.
            s = Satz(Abbild(R("a", 50, true, 18.0, 21.0), R("b", 40, true, 25.0, 19.0)), true);
            Assert.Contains(s.Meldungen, m => m.Schluessel == GebaeudeImportAblauf.MELDUNG + "SOLLWERT_UNEINHEITLICH");
            Assert.Equal(GebaeudeStammCtrl.SOLLTEMPERATUR_TAG_VORGABE, Tag(s).Wert);
        }

        [Fact]
        public void Das_Mittel_ohne_Flaechen_ist_ungewichtet_und_ohne_Temperatur_leer()
        {
            Assert.Null(GebaeudeCadSollwert.Bilden(new[] { R("a", 10, true, null) }));
            GebaeudeCadSollwert.Mittel m = GebaeudeCadSollwert.Bilden(new[]
            {
                new AbbildRaum { Kennung = "x", RaumtemperaturC = 20.0 }, new AbbildRaum { Kennung = "y", RaumtemperaturC = 21.0 },
                new AbbildRaum { Kennung = "z", RaumtemperaturC = double.NaN },
            }).Value;
            Assert.Equal(20.5, m.Wert);
            Assert.Equal(2, m.Raeume);
            Assert.Equal(1, m.OhneTemperatur);
        }

        // ------------------------------------------------------------------
        //  Zonen an der Probe ifc4_z6_cad.ifc
        // ------------------------------------------------------------------

        private static GebaeudeBauteilvorschlag Vorschlag(string regel, bool schalter)
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, regel);
            return GebaeudeBauteilvorschlag.Bilden(a, 0, 'E', null, null, z, schalter);
        }

        private static string Werte(GebaeudeBauteilvorschlag v)
            => string.Join("; ", v.Zonen.Select(z => z.Bezeichner + "=" + (z.Raumsolltemperatur_Tag?.ToString("0.0", CultureInfo.InvariantCulture) ?? "NULL")));

        [Fact]
        public void Probe_Gebaeudemittel_aus_der_Raumtemperatur()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeImportSatz aus = a.Zuordnen(0, 'E'), ein = a.Zuordnen(0, 'E', null, true);
            Assert.True(aus.CadSollwertMoeglich);
            Assert.Equal(20.0, Tag(aus).Wert);
            // (20·40 + 20·30 + 15·20 + 20·6 + 20·35 + 24·3) / 134 = 19,34 → 19,3; „Raum 7" ohne Temperatur.
            Assert.Equal(19.3, Tag(ein).Wert);
            Assert.Equal(new[] { "6", "15", "24", "1" }, Tag(ein).Beleg.Werte);
            Assert.Contains(ein.Meldungen, m => m.Schluessel == I + GebaeudeCadSollwert.SOLLWERT_CAD_SPANNE);
        }

        [Fact]
        public void Probe_Zonenwerte_unter_Z6_und_Z4()
        {
            GebaeudeBauteilvorschlag z6aus = Vorschlag(IfcImportProfil.ZONENREGEL_Z6, false);
            Assert.True(z6aus.Mehrzonig, Werte(z6aus));
            Assert.All(z6aus.Zonen, z => Assert.Null(z.Raumsolltemperatur_Tag));
            Assert.DoesNotContain(z6aus.Meldungen, m => m.Schluessel == I + GebaeudeCadSollwert.ZONE_SOLLWERT_CAD);

            GebaeudeBauteilvorschlag z6 = Vorschlag(IfcImportProfil.ZONENREGEL_Z6, true);
            _aus.WriteLine("Z6: " + Werte(z6));
            // Die 20-°C-Gruppe mit der zugeschlagenen Dusche (24 °C, 3 m²): 2292 / 114 = 20,1; der Flur 15,0.
            Assert.Equal(new double?[] { 20.1, 15.0 }, z6.Zonen.Select(z => z.Raumsolltemperatur_Tag));
            Assert.All(z6.Zonen, z => Assert.Null(z.Raumsolltemperatur_Nachtabsenkung));
            Assert.Equal(2, z6.Meldungen.Count(m => m.Schluessel == I + GebaeudeCadSollwert.ZONE_SOLLWERT_CAD && m.Stufe == PruefStufe.Info));
            PruefMeldung beleg = z6.Meldungen.First(m => m.Schluessel == I + GebaeudeCadSollwert.ZONE_SOLLWERT_CAD);
            Assert.Equal(new[] { z6.Zonen[0].Bezeichner, "20.1", "5", "1" }, beleg.Werte);
            Assert.Contains(z6.Meldungen, m => m.Schluessel == I + GebaeudeCadSollwert.SOLLWERT_CAD_SPANNE && m.Werte[0] == z6.Zonen[0].Bezeichner);

            GebaeudeBauteilvorschlag z4 = Vorschlag(IfcImportProfil.ZONENREGEL_Z4, true);
            _aus.WriteLine("Z4: " + Werte(z4));
            Assert.True(z4.Mehrzonig, Werte(z4));
            Assert.All(z4.Zonen.Where(z => !z.IstBeheizt), z => Assert.Null(z.Raumsolltemperatur_Tag));
            // EG: (20·40 + 20·30 + 15·20 + 20·6 + 24·3) / 99 = 19,1; OG: Wohnraum 20 °C, „Raum 7" ohne.
            Assert.Equal(new double?[] { 19.1, 20.0 }, z4.Zonen.Where(z => z.IstBeheizt).Select(z => z.Raumsolltemperatur_Tag));
        }

        [Fact]
        public void Gleiche_Eingabe_gibt_gleiche_Werte()
        {
            Assert.Equal(Werte(Vorschlag(IfcImportProfil.ZONENREGEL_Z6, true)), Werte(Vorschlag(IfcImportProfil.ZONENREGEL_Z6, true)));
            Assert.Equal(Tag(Satz(Haus(), true)).Wert, Tag(Satz(Haus(), true)).Wert);
        }
    }
}
