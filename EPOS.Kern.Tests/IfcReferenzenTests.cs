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
    /// Raumbezüge bleibt Z5 mit dem bisherigen Hinweis; die übrigen Proben bleiben ohne die neuen Meldungen. Dazu
    /// die zwei Hinweise aus dem Dateinamen: Ein Platzhaltername des Gebäudes weicht im Namensvorschlag dem
    /// Dateinamen, und ein Jahr im Dateinamen wird nur genannt, nie als Baujahr übernommen.
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
            Assert.Equal(new[] { "Z4", "Z6", "Z5" }, z.Regeln);   // Z6: Temperaturen bzw. Beheizung bilden Gruppen
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
        public void Referenziert_die_Datei_nicht_alle_Deckenteile_kommt_die_Flaeche_aus_den_Raummengen()
        {
            GebaeudeImportAblauf a;
            using (var s = new MemoryStream(IfcProbenErzeuger.Referenzen(schwach: true)))
                a = Lesen(s, "referenzen_schwach.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "TRENNDECKE_GESCHAETZT");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "EG", "OG", "60", "10" }, m.Werte);   // die kleinere beheizte Grundfläche statt der referenzierten
            Assert.False(Hat(g.Meldungen, "TRENNDECKE_KLEIN"));
            AbbildBauteil decke = Bauteil(a, "Decke EG/OG");
            Nah(60.0, decke.BruttoflaecheM2);
            Nah(60.0, decke.NettoflaecheM2);
            Nah(1.0, decke.UWertWm2K);                                  // U-Wert des referenzierten Teils

            // Das Paar koppelt: Z4 ist die Vorgabe, die Trennfläche trägt die geschätzte Fläche.
            Assert.True(g.GeschosseGekoppelt);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z4", z.Vorgabe);
            Assert.False(Hat(z.Meldungen, "GRENZEN_ENTKOPPELT"));
            Assert.Contains(z.Trennungen, t => z.Zonen[t.ZoneA].Name == "EG" && z.Zonen[t.ZoneB].Name == "OG" && t.FlaecheA == 60.0);
            // Die Kellerdecke grenzt an ein Geschoss ohne beheizten Raum: nicht geschätzt.
            Nah(65.0, Bauteil(a, "Kellerdecke").BruttoflaecheM2);
        }

        [Fact]
        public void Bei_vollstaendigen_Bezuegen_wird_nicht_geschaetzt()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            Assert.False(Hat(a.Abbild.Gebaeude.Single().Meldungen, "TRENNDECKE_GESCHAETZT"));
            Nah(60.0, Bauteil(a, "Decke EG/OG").BruttoflaecheM2);
        }

        [Fact]
        public void Die_Erklaerung_der_Datei_gilt_vor_dem_Bezug_und_der_Widerspruch_wird_benannt()
        {
            GebaeudeImportAblauf a;
            using (var s = new MemoryStream(IfcProbenErzeuger.Referenzen(spitzboden: true)))
                a = Lesen(s, "referenzen_spitzboden.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.True(g.Raeume.Single(r => r.Name == "Spitzboden").Beheizt);
            AbbildBauteil oben = Bauteil(a, "Oberste Decke");
            Assert.Empty(oben.Nachbarn);                 // keine Trenndecke OG/DG
            Assert.True(oben.HuelleOhneNachbar);         // die Hülle gegen unbeheizt bleibt
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "ERKLAERUNG_VOR_BEZUG");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "Oberste Decke", "Spitzboden" }, m.Werte);   // der Raum der Seite, die die Datei unbeheizt nennt
            // Das beheizte DG ist nicht gekoppelt: Z5 bleibt die Vorgabe.
            Assert.False(g.GeschosseGekoppelt);
            Assert.Equal("Z5", GebaeudeZonierung.Bilden(a.Abbild, 0).Vorgabe);
            // Ohne Spitzboden kein Hinweis.
            Assert.False(Hat(Lesen(PROBE).Abbild.Gebaeude.Single().Meldungen, "ERKLAERUNG_VOR_BEZUG"));
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

        // ==================================================================
        //  Anwenderdateien eines CAD-Exports: Beheizungsart, Kopplung, nicht bewertete Platten
        // ==================================================================

        private static GebaeudeImportAblauf Abwandlung(string name, byte[] inhalt)
        {
            using (var s = new MemoryStream(inhalt))
                return Lesen(s, name);
        }

        [Fact]
        public void Die_Beheizungsart_des_CAD_Exports_gilt_vor_Namensregel_und_Lage()
        {
            GebaeudeImportAblauf a = Abwandlung("referenzen_beheizungsart.ifc", IfcProbenErzeuger.Referenzen(beheizungsart: true));
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            AbbildRaum keller = g.Raeume.Single(r => r.Name == "Keller");
            Assert.False(keller.Beheizt);
            Assert.Equal(BeheiztQuelle.Attribut, keller.BeheiztQuelle);
            Assert.Equal("B3", keller.Beheizungsregel);
            Assert.Equal("CAD_RaumAllgemein.HeatingType = bhtUnHeated", keller.Zustandsangabe);
            AbbildRaum abstell = g.Raeume.Single(r => r.Name == "Abstellraum");
            Assert.True(abstell.Beheizt);                                    // die Datei vor der Namensregel
            Assert.Equal(BeheiztQuelle.Attribut, abstell.BeheiztQuelle);
            AbbildRaum kind = g.Raeume.Single(r => r.Name == "Kind");
            Assert.True(kind.Beheizt);                                       // getrennt beheizt entscheidet nicht: Annahme
            Assert.Equal(BeheiztQuelle.Annahme, kind.BeheiztQuelle);
            Assert.False(Hat(g.Meldungen, "UNBEHEIZT_NAME"));
            Assert.False(Hat(g.Meldungen, "UNBEHEIZT_LAGE"));
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "BEHEIZUNGSART");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "2", "1", "CAD_RaumAllgemein.HeatingType" }, m.Werte);
            PruefMeldung offen = Assert.Single(g.Meldungen, x => x.Schluessel == P + "BEHEIZUNGSART_OFFEN");
            Assert.Equal(new[] { "1", "CAD_RaumAllgemein.HeatingType", "bhtSeparatelyHeated" }, offen.Werte);

            // Ohne Beheizungsart bleiben Namensregel und Lage.
            AbbildGebaeude ohne = Lesen(PROBE).Abbild.Gebaeude.Single();
            Assert.False(ohne.Raeume.Single(r => r.Name == "Abstellraum").Beheizt);
            Assert.False(Hat(ohne.Meldungen, "BEHEIZUNGSART"));
        }

        [Fact]
        public void Eine_Decke_nur_zum_unbeheizten_Teil_eines_Geschosses_koppelt_nicht()
        {
            GebaeudeImportAblauf a = Abwandlung("referenzen_kellerteil.ifc", IfcProbenErzeuger.Referenzen(kellerTeil: true));
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.True(g.Raeume.Single(r => r.Name == "Hobbyraum").Beheizt);
            Assert.False(g.Raeume.Single(r => r.Name == "Keller").Beheizt);
            // Die Kellerdecke trennt KG und EG, grenzt aber nur an den unbeheizten Keller: Das beheizte KG bleibt ohne
            // Verbindung, nicht geschätzt, und die Vorgabe ist Z5 — nicht Z4 mit entkoppelter Zone.
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);
            Assert.False(g.GeschosseGekoppelt);
            Assert.DoesNotContain(g.Meldungen, x => x.Schluessel == P + "TRENNDECKE_GESCHAETZT");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z5", z.Vorgabe);
            Assert.True(Hat(z.Meldungen, "KEINE_GRENZEN"));
            Assert.True(Hat(GebaeudeZonierung.Bilden(a.Abbild, 0, "Z4").Meldungen, "GRENZEN_ENTKOPPELT"));

            // Gegenprobe: Ohne den Hobbyraum hat das KG keinen beheizten Raum, und EG/OG koppeln wie bisher.
            Assert.True(Lesen(PROBE).Abbild.Gebaeude.Single().GeschosseGekoppelt);
        }

        [Fact]
        public void Eine_Platte_mit_U_Wert_null_und_ohne_Aufbau_trennt_keine_Geschosse()
        {
            GebaeudeImportAblauf a = Abwandlung("referenzen_bodenoeffnung.ifc", IfcProbenErzeuger.Referenzen(bodenoeffnung: true));
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            AbbildBauteil loch = Bauteil(a, "Bodenöffnung");
            Assert.Null(loch.UWertWm2K);
            Assert.Null(loch.Aufbau);
            Assert.Empty(loch.Nachbarn);
            Assert.False(loch.HuelleOhneNachbar);
            Assert.Equal(2, g.ZahlTrenndeckenReferenz);                  // Kellerdecke und Decke EG/OG wie in der Probe
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "UWERT_NICHT_POSITIV");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1", "CAD_Bauteilreferenzen", "UValue (W/(m² K))", "0" }, m.Werte);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "Z4");
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(x => x.Stufe == PruefStufe.Fehler)));
            Assert.DoesNotContain(v.Zeilen, x => x.Bauteil.Bezeichner == "Bodenöffnung");
            Assert.False(Hat(Lesen(PROBE).Meldungen, "UWERT_NICHT_POSITIV"));
        }

        [Fact]
        public void Eine_unbeheizte_Zone_ohne_jede_Flaeche_entfaellt_benannt()
        {
            GebaeudeImportAblauf a = Abwandlung("referenzen_speicher.ifc", IfcProbenErzeuger.Referenzen(speicher: true));
            Assert.False(a.Abbild.Gebaeude.Single().Raeume.Single(r => r.Name == "Speicher").Beheizt);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, "Z4");
            Assert.DoesNotContain(z.Zonen, x => x.Raeume.Any(r => r.Name == "Speicher"));
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == P + "ZONE_OHNE_FLAECHEN");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "OG (unbeheizt)", "6" }, m.Werte);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "Z4");
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(x => x.Stufe == PruefStufe.Fehler)));
            Assert.DoesNotContain(v.Zonen, x => x.Bezeichner == "OG (unbeheizt)");

            // Die Zonen der Probe bleiben: Der unbeheizte Keller trägt die Kellerdecke als Trennfläche.
            GebaeudeZonierung probe = GebaeudeZonierung.Bilden(Lesen(PROBE).Abbild, 0, "Z4");
            Assert.False(Hat(probe.Meldungen, "ZONE_OHNE_FLAECHEN"));
            Assert.Equal(z.Zonen.Select(x => x.Name), probe.Zonen.Select(x => x.Name));
        }

        // ==================================================================
        //  Dateiname: Platzhaltername und Jahr — nur Hinweise
        // ==================================================================

        [Theory]
        [InlineData("Gebäude", true)]
        [InlineData(" gebäude ", true)]
        [InlineData("Gebaeude", true)]
        [InlineData("Building", true)]
        [InlineData("DEFAULT BUILDING", true)]
        [InlineData("Haus", true)]
        [InlineData("Haus A", false)]
        [InlineData("Probengebäude", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Platzhalternamen_werden_ohne_Gross_und_Kleinschreibung_erkannt(string name, bool platzhalter)
            => Assert.Equal(platzhalter, GebaeudeZuordnungsModell.IstPlatzhaltername(name));

        [Fact]
        public void Ein_Platzhaltername_weicht_im_Vorschlag_dem_Dateinamen_mit_Hinweis()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal("Gebäude", g.Name);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "NAME_PLATZHALTER");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "Gebäude", "ifc2x3_referenzen" }, m.Werte);
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal("ifc2x3_referenzen", GebaeudeZuordnungsModell.Vorschlagsname(satz));
            Assert.Contains(satz.Meldungen, x => x.Schluessel == P + "NAME_PLATZHALTER");

            // Ein eigener Name bleibt, ohne Hinweis.
            GebaeudeImportAblauf e = Lesen("ifc2x3_enthaltensein.ifc");
            Assert.Equal("Probengebäude", GebaeudeZuordnungsModell.Vorschlagsname(e.Zuordnen(0, null)));
            Assert.False(Hat(e.Abbild.Gebaeude.Single().Meldungen, "NAME_PLATZHALTER"));
        }

        [Theory]
        [InlineData("MFH_mittel_1984.ifc", 1984)]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc", 2026)]
        [InlineData("/daten/2019/Haus_1984.ifc", 1984)]
        [InlineData("Haus_1984_2020.ifc", null)]
        [InlineData("Haus_19845.ifc", null)]
        [InlineData("Haus_1499.ifc", null)]
        [InlineData("ifc2x3_referenzen.ifc", null)]
        [InlineData("", null)]
        public void Der_Dateiname_nennt_genau_ein_Jahr_oder_keines(string datei, int? jahr)
            => Assert.Equal(jahr, Baujahrregel.JahrImDateinamen(datei));

        [Fact]
        public void Das_Jahr_im_Dateinamen_ist_nur_ein_Hinweis_wenn_die_Datei_kein_Baujahr_fuehrt()
        {
            GebaeudeImportAblauf a;
            using (var s = new MemoryStream(File.ReadAllBytes(Path.Combine(IfcProbenTests.Ordner(), PROBE))))
                a = Lesen(s, "Haus_1984.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Null(g.Baujahr);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "BAUJAHR_DATEINAME");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1984" }, m.Werte);
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Null(satz.Baujahr);
            Assert.False(satz.Zeile(GebaeudeZielfelder.BAUJAHR).HatWert);   // nie übernommen
            Assert.Null(GebaeudeZuordnungsModell.KlasseDerDatei(satz));
            Assert.Equal("Haus_1984", GebaeudeZuordnungsModell.Vorschlagsname(satz));

            // Die Datei führt ein Baujahr: kein Hinweis aus dem Dateinamen.
            GebaeudeImportAblauf e;
            using (var s = new MemoryStream(File.ReadAllBytes(Path.Combine(IfcProbenTests.Ordner(), "ifc2x3_enthaltensein.ifc"))))
                e = Lesen(s, "Haus_1990.ifc");
            Assert.Equal(1984, e.Abbild.Gebaeude.Single().Baujahr);
            Assert.False(Hat(e.Abbild.Gebaeude.Single().Meldungen, "BAUJAHR_DATEINAME"));
        }

        /// <summary>
        /// <b>Datei und Dateiname nennen verschiedene Jahre</b>: genau ein Hinweis „Datei/Name", es gilt das Baujahr der
        /// Datei; nennt der Name dasselbe Jahr oder mehrere Jahre, schweigt das Protokoll.
        /// </summary>
        [Theory]
        [InlineData("Haus_1990.ifc", "1984/1990")]
        [InlineData("Haus_1984.ifc", "")]
        [InlineData("Haus_1990_2014.ifc", "")]
        [InlineData("Haus.ifc", "")]
        public void Ein_abweichendes_Jahr_im_Dateinamen_wird_benannt_es_gilt_die_Datei(string datei, string erwartet)
        {
            GebaeudeImportAblauf e;
            using (var s = new MemoryStream(File.ReadAllBytes(Path.Combine(IfcProbenTests.Ordner(), "ifc2x3_enthaltensein.ifc"))))
                e = Lesen(s, datei);
            AbbildGebaeude g = e.Abbild.Gebaeude.Single();
            Assert.Equal(1984, g.Baujahr);
            List<PruefMeldung> m = g.Meldungen.Where(x => x.Schluessel == P + "BAUJAHR_WIDERSPRUCH").ToList();
            Assert.Equal(erwartet, string.Join(";", m.Select(x => x.Werte[0] + "/" + x.Werte[1])));
            Assert.All(m, x => Assert.Equal(PruefStufe.Info, x.Stufe));
            Assert.False(Hat(g.Meldungen, "BAUJAHR_DATEINAME"));
            Assert.Equal("1984", e.Zuordnen(0, null).Zeile(GebaeudeZielfelder.BAUJAHR).Wert?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        // ==================================================================
        //  „Getrennt beheizt" nach der Raumtemperatur, Wärmekapazität masseloser Schichten
        // ==================================================================

        /// <summary>
        /// <b>„Getrennt beheizt" mit Raumtemperatur</b>: über 12 °C beheizt, sonst unbeheizt (Regel B3, Quelle Attribut),
        /// benannt mit Raum und Temperatur; die Temperatur wird nicht als Sollwert übernommen, und der Raum ist nicht mehr offen.
        /// </summary>
        [Theory]
        [InlineData(20.0, true)]
        [InlineData(8.0, false)]
        public void Getrennt_beheizt_entscheidet_die_Raumtemperatur_der_Datei(double temperatur, bool beheizt)
        {
            GebaeudeImportAblauf a = Abwandlung("referenzen_temperatur.ifc", IfcProbenErzeuger.Referenzen(beheizungsart: true, kindTemperatur: temperatur));
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            AbbildRaum kind = g.Raeume.Single(r => r.Name == "Kind");
            Assert.Equal(beheizt, kind.Beheizt);
            Assert.Equal(BeheiztQuelle.Attribut, kind.BeheiztQuelle);
            Assert.Equal("B3", kind.Beheizungsregel);
            Assert.Null(kind.SollHeizenC);
            string grad = temperatur.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal("CAD_RaumAllgemein.HeatingType = bhtSeparatelyHeated, CAD_RaumAllgemein.InsideTemperature (°C) = " + grad + " °C",
                         kind.Zustandsangabe);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == P + "BEHEIZUNGSART_TEMPERATUR");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { beheizt ? "1" : "0", beheizt ? "0" : "1", "CAD_RaumAllgemein.HeatingType = bhtSeparatelyHeated", "Kind " + grad + " °C" },
                         m.Werte);
            Assert.False(Hat(g.Meldungen, "BEHEIZUNGSART_OFFEN"));
            // Die übrigen Räume entscheidet die Beheizungsart wie ohne Temperatur.
            Assert.Equal(new[] { "2", "1", "CAD_RaumAllgemein.HeatingType" },
                         Assert.Single(g.Meldungen, x => x.Schluessel == P + "BEHEIZUNGSART").Werte);
        }

        /// <summary>
        /// <b>Masselose IFC4-Schichten</b>: Putz und Mauerwerk führen Dichte und λ, aber keine Wärmekapazität — c kommt aus
        /// dem Katalog der Auslieferung bzw. der Stofftabelle, die Aufbauten werden vollständig, je Aufbau eine Meldung.
        /// Eine Schicht ohne Dichte bleibt masselos mit der bestehenden Meldung.
        /// </summary>
        [Fact]
        public void Schichten_ohne_Waermekapazitaet_fallen_auf_Katalog_und_Stofftabelle_zurueck()
        {
            GebaeudeImportAblauf a = Abwandlung("ifc4_schichten_ohne_c.ifc",
                IfcProbenErzeuger.Schichten(Xbim.Common.Step21.XbimSchemaVersion.Ifc4, "ifc4_schichten_ohne_c.ifc", nullwerte: false, ohneWaermekapazitaet: true));
            List<PruefMeldung> m = a.Meldungen.Where(x => x.Schluessel == P + "WAERMEKAPAZITAET_RUECKFALL").ToList();
            foreach (PruefMeldung x in m) _aus.WriteLine(string.Join(" | ", x.Werte));
            Assert.Equal(new[] { "Außenwand außen zuerst", "Außenwand innen zuerst" }, m.Select(x => x.Werte[0]).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(m, x => Assert.Equal(PruefStufe.Info, x.Stufe));
            Assert.All(m, x => Assert.Equal("2", x.Werte[1]));
            foreach (AbbildBauteil b in a.Abbild.Gebaeude.Single().Bauteile.Where(x => x.Aufbau != null))
            {
                Assert.Equal(Aufbaustatus.Vollstaendig, b.Aufbau.Status);
                Assert.All(b.Aufbau.Schichten, x => Assert.True(x.CpJkgK >= 800.0 && x.CpJkgK <= 1700.0, x.Name + ": c = " + x.CpJkgK));
            }

            // Ohne Dichte bleibt die Schicht masselos, benannt wie bisher — kein Rückfall.
            GebaeudeImportAblauf n = Lesen("ifc4_schichten_nullwerte.ifc");
            Assert.True(Hat(n.Meldungen, "STOFFWERT_NULL"));
            Assert.False(Hat(n.Meldungen, "WAERMEKAPAZITAET_RUECKFALL"));
        }

        /// <summary>Die Stofftabelle des Rückfalls: Stoffgruppe je Materialname, Dämmstoffe vor den mineralischen Gruppen.</summary>
        [Theory]
        [InlineData("Beton armiert mit 1% Stahl  (DIN 12524)", "Beton", 1000.0)]
        [InlineData("Leichtbauplatten mit Mineralfaserschicht", "Dämmstoff (Mineralfaser)", 1030.0)]
        [InlineData("Polystyrol PS -Extruderschaum  (WLG 040)", "Dämmstoff (Schaumkunststoff)", 1450.0)]
        [InlineData("PUR/PIR-Hartschaum", "Dämmstoff (Schaumkunststoff)", 1450.0)]
        [InlineData("Konstruktionsholz", "Holz", 1600.0)]
        [InlineData("Gipskartonplatten  (DIN 18180)", "Gips", 1000.0)]
        [InlineData("Normalmörtel NM", "Putz", 1000.0)]
        [InlineData("Hochlochziegel Lochung A+B", "Mauerwerk", 1000.0)]
        [InlineData("Fantasiestoff 7", null, 0.0)]
        public void Die_Stofftabelle_ordnet_Materialnamen_einer_Gruppe_zu(string name, string gruppe, double cp)
        {
            Waermekapazitaetsrueckfall.Stoffgruppe g = Waermekapazitaetsrueckfall.Gruppe(name);
            Assert.Equal(gruppe, g?.Name);
            if (g != null) Assert.Equal(cp, g.CpJkgK);
            Assert.Null(Waermekapazitaetsrueckfall.Bestimmen("Fantasiestoff 7", new Baustoffabgleich(BaustoffabgleichDaten.AusSaat())));
        }

        [Fact]
        public void Gegenprobe_die_uebrigen_Proben_bleiben_ohne_die_neuen_Meldungen()
        {
            IEnumerable<string> proben = IfcProbenErzeuger.Alle().Keys.Where(k => k != PROBE && k != "ifc4_z6_cad.ifc" && !k.EndsWith(".ifczip", StringComparison.Ordinal))
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
                Assert.False(Hat(alle, "TRENNDECKE_GESCHAETZT"), probe);
                Assert.False(Hat(alle, "ERKLAERUNG_VOR_BEZUG"), probe);
                Assert.False(Hat(alle, "BAUJAHR_DATEINAME"), probe);
                Assert.False(Hat(alle, "NAME_PLATZHALTER"), probe);
                Assert.False(Hat(alle, "BAUJAHR_WIDERSPRUCH"), probe);
                Assert.False(Hat(alle, "BEHEIZUNGSART_TEMPERATUR"), probe);
                Assert.False(Hat(alle, "WAERMEKAPAZITAET_RUECKFALL"), probe);
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
