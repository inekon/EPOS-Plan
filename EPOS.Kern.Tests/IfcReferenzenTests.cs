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
    /// <b>Trenndecken und innere Masse über die Raumbezüge</b> (Mehrzonenkonzept 6.5) an der Probe
    /// <c>ifc2x3_referenzen.ifc</c> (<see cref="IfcProbenErzeuger.Referenzen"/>): Ein CAD-Export ohne Raumgrenzen,
    /// der je Raum ein <c>IfcRelReferencedInSpatialStructure</c> mit den angrenzenden Bauteilen schreibt. Eine Decke,
    /// die Räume zweier Geschosse referenzieren, trennt diese Geschosse — mit der Fläche ihrer Menge, je Geschosspaar,
    /// ohne Geometrie (ADR-003) —, und die Geschosszonen (Z4) sind damit ohne Handeingabe gekoppelt; Wände zwischen
    /// zwei bis vier Räumen eines Geschosses sind innere Masse, Innenwände ohne Nachbarraum zählen einseitig. Ohne
    /// Raumbezüge bleibt Z5 mit dem bisherigen Hinweis; die übrigen Proben bleiben ohne die neuen Meldungen.
    /// </summary>
    public sealed class IfcReferenzenTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string PROBE = "ifc2x3_referenzen.ifc";

        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public IfcReferenzenTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(string name)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), name);
            using (FileStream s = File.OpenRead(pfad))
                return Lesen(s, pfad);
        }

        private static GebaeudeImportAblauf Lesen(Stream s, string dateiname)
        {
            var a = new GebaeudeImportAblauf();
            a.Lesen(s, dateiname, new IfcImportProfil());
            Assert.True(a.Abbild != null && a.Abbild.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a;
        }

        private static void Nah(double erwartet, double? ist)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= 1e-9, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name)
            => a.Abbild.Gebaeude.Single().Bauteile.Single(b => b.Name == name);

        private static string Raum(GebaeudeImportAblauf a, string kennung)
            => a.Abbild.Gebaeude.Single().Raeume.Single(r => r.Kennung == kennung).Name;

        private static bool Hat(IEnumerable<PruefMeldung> meldungen, string name) => meldungen.Any(m => m.Schluessel == P + name);

        // ==================================================================
        //  Leser
        // ==================================================================

        [Fact]
        public void Eine_Decke_zwischen_Raeumen_zweier_Geschosse_ist_eine_Trenndecke()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(0, g.ZahlGrenzen);
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);
            Assert.True(g.GeschosseGekoppelt);

            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "TRENNDECKE_REFERENZ");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "Gebäude", "2", "KG/EG, EG/OG", "3" }, m.Werte);
            Assert.False(Hat(g.Meldungen, "TRENNDECKE_KLEIN"));

            // Je Geschoss der erste beheizte Raum; die Sicht aus der Geschosslage, der beheizte Raum zuerst.
            AbbildBauteil decke = Bauteil(a, "Decke EG/OG");
            Assert.Equal(new[] { "Wohnen", "Schlafen" }, decke.Nachbarn.Select(n => Raum(a, n.Kennung)));
            Assert.Equal(new[] { GebaeudeAggregation.SICHT_DECKE, GebaeudeAggregation.SICHT_BODEN }, decke.Nachbarn.Select(n => n.Sicht));
            Assert.Equal(Randbedingung.Innen, decke.Randbedingung);
            Assert.False(decke.HuelleOhneNachbar);
            Nah(60.0, decke.BruttoflaecheM2);
            Nah(1.0, decke.UWertWm2K);

            AbbildBauteil kd = Bauteil(a, "Kellerdecke");
            Assert.Equal(new[] { "Wohnen", "Keller" }, kd.Nachbarn.Select(n => Raum(a, n.Kennung)));
            Assert.Equal(new[] { GebaeudeAggregation.SICHT_BODEN, GebaeudeAggregation.SICHT_DECKE }, kd.Nachbarn.Select(n => n.Sicht));
            Assert.Equal(Randbedingung.Unbeheizt, kd.Randbedingung);
            Assert.False(kd.HuelleOhneNachbar);

            // Ein Geschoss bzw. außen: keine Trenndecke, die Hülle ohne Nachbarn wie bisher.
            AbbildBauteil oben = Bauteil(a, "Oberste Decke");
            Assert.Empty(oben.Nachbarn);
            Assert.True(oben.HuelleOhneNachbar);
            Assert.False(oben.ZonenbodenOhneNachbar);
            AbbildBauteil vordach = Bauteil(a, "Vordach");
            Assert.Empty(vordach.Nachbarn);
            Assert.True(vordach.HuelleOhneNachbar);
            Assert.Equal(Randbedingung.Aussenluft, vordach.Randbedingung);
            Assert.All(new[] { "Außenwand Süd", "Außenwand Nord", "Kellerwand" }, n => Assert.Empty(Bauteil(a, n).Nachbarn));
        }

        [Fact]
        public void Waende_zwischen_Raeumen_eines_Geschosses_sind_innere_Masse_die_uebrigen_Innenwaende_zaehlen_einseitig()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            Assert.Equal(new[] { "Wohnen", "Küche" }, Bauteil(a, "Innenwand EG").Nachbarn.Select(n => Raum(a, n.Kennung)));
            Assert.Equal(new[] { "Küche", "Abstellraum" }, Bauteil(a, "Wand Abstellraum").Nachbarn.Select(n => Raum(a, n.Kennung)));
            Assert.Equal(new[] { "Schlafen", "Kind" }, Bauteil(a, "Innenwand OG").Nachbarn.Select(n => Raum(a, n.Kennung)));
            Assert.All(new[] { "Innenwand EG", "Wand Abstellraum", "Innenwand OG" }, n => Assert.False(Bauteil(a, n).InnenEinseitig));

            // Zwei Geschosse, ein Raum, kein Bezug: ohne Nachbarn — einseitig innere Masse.
            foreach (string n in new[] { "Treppenhauswand", "Innenwand einzeln", "Innenwand frei" })
            {
                AbbildBauteil w = Bauteil(a, n);
                Assert.Empty(w.Nachbarn);
                Assert.True(w.InnenEinseitig, n);
                Assert.False(w.HuelleOhneNachbar, n);
            }
            Assert.False(Bauteil(a, "Kellerwand").InnenEinseitig);
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "INNEN_EINSEITIG");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "3", "21" }, m.Werte);
        }

        // ==================================================================
        //  Einzonenweg (Z5): Hülle unverändert, innere Masse gemessen
        // ==================================================================

        [Fact]
        public void Die_Einzonen_Zuordnung_haelt_die_Huelle_und_misst_die_innere_Masse()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Nah(120.0, satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            Nah(80.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);
            Nah(66.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);      // oberste Decke gegen unbeheizt und Vordach
            Nah(65.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);     // Kellerdecke: Boden über dem Keller
            Nah(6.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);   // Wand zum Abstellraum

            // Innere Masse: Innenwände EG/OG und die Decke EG/OG beidseitig, drei Wände einseitig — 185 m² ÷ 120 m².
            GebaeudeFeldzeile innen = satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
            Nah(185.0 / 120.0, innen.Wert);
            Assert.Equal(Importherkunft.Ifc, innen.Herkunft);
            Assert.Equal("GIMP_BELEG_INNENFLAECHENFAKTOR", innen.Beleg.Schluessel);
            Assert.Equal("6", innen.Beleg.Werte[1]);   // sechs Flächen innerer Masse

            Huelleneinordnung e = GebaeudeHuelleneinordnung.Einordnen(a.Abbild, 0, r => r.Beheizt);
            Assert.Equal(3, e.Innen.Count(p => p.PosA < 0 && p.PosB < 0));
            Nah(185.0, e.InnenflaecheM2);
        }

        // ==================================================================
        //  Zonierung (Z4) und Mehrzonenvorschlag
        // ==================================================================

        [Fact]
        public void Z4_ist_die_Vorgabe_und_die_Zonierung_rechnet_die_Trenndecke_je_Geschosspaar()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z4", z.Vorgabe);
            Assert.Equal("Z4", z.Regel);
            Assert.Equal(new[] { "Z4", "Z5" }, z.Regeln);
            Assert.False(z.HatRaumgrenzen);
            Assert.False(Hat(z.Meldungen, "KEINE_GRENZEN"));
            Assert.False(Hat(z.Meldungen, "GRENZEN_ENTKOPPELT"));
            Assert.Equal(new[] { "KG", "EG", "EG (unbeheizt)", "OG" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { false, true, false, true }, z.Zonen.Select(x => x.IstBeheizt));

            string Paar(Zonentrennung t) => z.Zonen[t.ZoneA].Name + "|" + z.Zonen[t.ZoneB].Name;
            Assert.Equal(new[] { "KG|EG", "EG|EG (unbeheizt)", "EG|OG" }, z.Trennungen.Select(Paar));
            Assert.Equal(new[] { 65.0, 6.0, 60.0 }, z.Trennungen.Select(t => t.FlaecheA));
            Assert.All(z.Trennungen, t => Assert.False(t.Ungleich));

            // Innere Masse: beidseitig in der Zone, einseitig in der Zone des Geschosses.
            List<Zonenflaeche> innen = z.Flaechen.Where(f => f.Rand == Zonenrand.Innen).ToList();
            Assert.Equal(new[] { "Innenwand EG", "Innenwand OG" }, innen.Where(f => f.Beidseitig).Select(f => f.Bauteil.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(new[] { "Innenwand einzeln", "Innenwand frei", "Treppenhauswand" },
                         innen.Where(f => !f.Beidseitig).Select(f => f.Bauteil.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.All(innen.Where(f => !f.Beidseitig), f => Assert.Equal("EG", z.Zonen[f.Zone].Name));
        }

        [Fact]
        public void Der_Mehrzonenvorschlag_fuehrt_die_Trenndecke_als_Trennflaeche()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            foreach (PruefMeldung m in v.Meldungen) _aus.WriteLine(m.ToString());
            foreach (GebaeudeBauteilzeile x in v.Zeilen) _aus.WriteLine(x + " Zone " + x.Zone + " nz " + x.Bauteil.ID_Nachbarzone);
            Assert.True(v.Mehrzonig);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            List<GebaeudeBauteilzeile> trenn = v.Zeilen.Where(x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).ToList();
            GebaeudeBauteilzeile decke = Assert.Single(trenn, x => x.Kennung == Bauteil(a, "Decke EG/OG").Kennung);
            Nah(60.0, decke.Bauteil.Flaeche);
            Assert.Equal("EG", v.Zonen[decke.Zone].Bezeichner);
            Assert.Equal(v.Zonen.Single(x => x.Bezeichner == "OG").ID, decke.Bauteil.ID_Nachbarzone);
            Nah(GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_OBEN, decke.Bauteil.Neigung);   // die Decke des Erdgeschosses
            Nah(1.0, decke.Bauteil.U_Wert);
        }

        [Fact]
        public void Eine_zu_kleine_Trenndecke_koppelt_nicht_dann_bleibt_Z5_mit_Hinweis()
        {
            GebaeudeImportAblauf a;
            using (var s = new MemoryStream(IfcProbenErzeuger.Referenzen(schwach: true)))
                a = Lesen(s, "referenzen_schwach.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);
            Assert.False(g.GeschosseGekoppelt);
            PruefMeldung klein = Assert.Single(g.Meldungen, x => x.Schluessel == P + "TRENNDECKE_KLEIN");
            Assert.Equal(PruefStufe.Warnung, klein.Stufe);
            Assert.Equal(new[] { "EG", "OG", "10", "60", "17" }, klein.Werte);

            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z5", z.Vorgabe);
            Assert.True(Hat(z.Meldungen, "KEINE_GRENZEN"));
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, "Z4");
            Assert.False(z4.Abgelehnt);
            Assert.True(Hat(z4.Meldungen, "GRENZEN_ENTKOPPELT"));
            Assert.Contains(z4.Trennungen, t => z4.Zonen[t.ZoneA].Name == "EG" && z4.Zonen[t.ZoneB].Name == "OG" && t.FlaecheA == 10.0);
        }

        [Fact]
        public void Ohne_Raumbezuege_bleibt_Z5_mit_dem_bisherigen_Hinweis()
        {
            GebaeudeImportAblauf a = Lesen("ifc2x3_enthaltensein.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(0, g.ZahlTrenndeckenReferenz);
            Assert.False(g.GeschosseGekoppelt);
            Assert.False(Hat(g.Meldungen, "TRENNDECKE_REFERENZ"));
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z5", z.Vorgabe);
            Assert.True(Hat(z.Meldungen, "KEINE_GRENZEN"));
            Assert.True(Hat(GebaeudeZonierung.Bilden(a.Abbild, 0, "Z4").Meldungen, "GRENZEN_ENTKOPPELT"));
        }

        [Fact]
        public void Gegenprobe_die_uebrigen_Proben_bleiben_ohne_die_neuen_Meldungen()
        {
            IEnumerable<string> proben = IfcProbenErzeuger.Alle().Keys.Where(k => k != PROBE && !k.EndsWith(".ifczip", StringComparison.Ordinal))
                                                          .Append("ifc4_verlust.ifc");
            foreach (string probe in proben)
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), probe);
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                var alle = a.Meldungen.Concat(a.Abbild?.Gebaeude.SelectMany(g => g.Meldungen) ?? Enumerable.Empty<PruefMeldung>()).ToList();
                Assert.False(Hat(alle, "TRENNDECKE_REFERENZ"), probe);
                Assert.False(Hat(alle, "TRENNDECKE_KLEIN"), probe);
                if (a.Abbild == null) continue;
                Assert.All(a.Abbild.Gebaeude, g => Assert.Equal(0, g.ZahlTrenndeckenReferenz));
                // Die einseitige Innenwand trägt allein die CAD-Probe ohne Raumgrenzen („Innenwand CAD", 12 m²).
                bool cad = probe == "ifc2x3_enthaltensein.ifc";
                Assert.Equal(cad, Hat(alle, "INNEN_EINSEITIG"));
                Assert.Equal(cad ? 1 : 0, a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Count(b => b.InnenEinseitig));
            }
        }
    }
}
