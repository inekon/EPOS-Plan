using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>Die synthetische Projektdatei mit Bauteiltabellen (BA-4b) und das passende IFC-Abbild.</summary>
    internal static class SqprojAufbauProbe
    {
        internal const string GA = "{a1000000-0000-0000-0000-00000000000a}";
        internal const string GC = "{a1000000-0000-0000-0000-00000000000c}";
        internal const string AW1 = "{b0000000-0000-0000-0000-000000000001}";
        internal const string AW2 = "{b0000000-0000-0000-0000-000000000002}";
        internal const string AW2B = "{b0000000-0000-0000-0000-00000000002b}";
        internal const string AW3A = "{b0000000-0000-0000-0000-00000000003a}";
        internal const string AW3B = "{b0000000-0000-0000-0000-00000000003b}";
        internal const string AWX = "{b0000000-0000-0000-0000-0000000000ff}";
        internal const string DE = "{b0000000-0000-0000-0000-0000000000de}";
        internal const string NULLKENNUNG = "{aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa}";

        /// <summary>Die GId der Hüllfläche <paramref name="n"/>.</summary>
        internal static string G(int n) => "{c0000000-0000-0000-0000-0000000000" + n.ToString("00", CultureInfo.InvariantCulture) + "}";

        /// <summary>Die Normalform einer Kennung.</summary>
        internal static string N(string k) => IfcAbbildBauer.GuidNormalform(k);

        // U der Aufbauten aus Rsi 0,13 + Σd/λ + Rse 0,04 (Wand) bzw. 0,10/0,10 (Decke).
        internal static readonly double U_AW1 = 1.0 / (0.13 + 0.2 / 2.0 + 0.06 / 0.04 + 0.04);
        internal static readonly double U_AW2 = 1.0 / (0.13 + 0.2 / 2.0 + 0.12 / 0.04 + 0.04);

        /// <summary>
        /// Räume A (EG) und C (OG); Aufbauten AW1 (Beton innen, Dämmung außen), AW2 (dicker gedämmt, unbenutzt), AW2B (U 0,5 % über
        /// AW2, andere Schichten, unbenutzt), AW3A/AW3B
        /// (beide U 0,8, verschiedene Schichten, unbenutzt), AWX (Rohdichte als Platzhalter, unbenutzt), DE (Decke: Dämmung oben);
        /// Hüllflächen G1…G7 (G4 mit Null-Kennung als Aufbau, G5 Decke zwischen A und C, G6 doppelt mit verschiedenen Aufbauten).
        /// </summary>
        internal static SqprojProbenErzeuger Erzeuger()
            => new SqprojProbenErzeuger()
                .Geschoss("F1", "EG").Geschoss("F2", "OG")
                .Raum("R1", "Raum A", "F1", GA, 50.0)
                .Raum("R2", "Raum C", "F2", GC, 50.0)
                .Aufbau(AW1, "Wand alt", U_AW1)
                .Schicht("L12", AW1, 1, "Daemmstoff", 0.06, 0.04, 30.0, 1.5, daemmung: true)
                .Schicht("L11", AW1, 0, "Beton", 0.2, 2.0, 2400.0, 1.0)
                .Aufbau(AW2, "Wand neu", U_AW2)
                .Schicht("L21", AW2, 0, "Beton", 0.2, 2.0, 2400.0, 1.0)
                .Schicht("L22", AW2, 1, "Daemmstoff", 0.12, 0.04, 30.0, 1.5, daemmung: true)
                .Aufbau(AW2B, "Wand neu B", U_AW2 * 1.005).Schicht("L2B", AW2B, 0, "Mauerwerk", 0.5, 0.2, 800.0, 1.0)
                .Aufbau(AW3A, "Wand A", 0.8).Schicht("L3A", AW3A, 0, "Mauerwerk", 0.3, 0.5, 1200.0, 1.0)
                .Aufbau(AW3B, "Wand B", 0.8).Schicht("L3B", AW3B, 0, "Mauerwerk", 0.36, 0.6, 1400.0, 1.0)
                .Aufbau(AWX, "Wand X", 0.7).Schicht("LX", AWX, 0, "Mauerwerk", 0.3, 0.5, SqprojProbenErzeuger.PLATZHALTER, 1.0)
                .Aufbau(DE, "Decke", 1.0 / (0.1 + 0.04 / 0.04 + 0.16 / 2.0 + 0.1), 0.1, 0.1)
                .Schicht("LD1", DE, 0, "Trittschall", 0.04, 0.04, 30.0, 1.5, daemmung: true)
                .Schicht("LD2", DE, 1, "Beton", 0.16, 2.0, 2400.0, 1.0)
                .Huellflaeche("H1", G(1), 1, AW1, U_AW1, 10.0).Bezug("B1", "R1", "H1", 1)
                .Huellflaeche("H2", G(2), 1, AW1, U_AW1, 10.0).Bezug("B2", "R1", "H2", 1)
                .Huellflaeche("H3", G(3), 1, AW1, U_AW1, 10.0).Bezug("B3", "R1", "H3", 1)
                .Huellflaeche("H4", G(4), 1, NULLKENNUNG, 0.9, 10.0).Bezug("B4", "R1", "H4", 1)
                .Huellflaeche("H5", G(5), 4, DE, 1.0, 50.0, 3).Bezug("B5a", "R1", "H5", 7, 0).Bezug("B5b", "R2", "H5", 8, 1)
                .Huellflaeche("H6", G(6), 1, AW1, U_AW1, 10.0).Huellflaeche("H6b", G(6), 1, AW2, U_AW2, 10.0)
                .Huellflaeche("H7", G(7), 1, AW1, U_AW1, 10.0).Bezug("B7", "R1", "H7", 1);

        internal static SqprojAbbild Lesen(SqprojProbenErzeuger e, string name)
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad(name));
            try { return SqprojLeser.Lesen(pfad); }
            finally { File.Delete(pfad); }
        }

        internal static AbbildBauteil Bauteil(string kennung, Bauteilart art, Randbedingung rand, double flaeche, double? u, double? neigung,
                                              double? azimut, string guid, params string[] nachbarn)
        {
            AbbildBauteil b = BauteilvorschlagProbe.Flaeche(kennung, art, rand, flaeche, u, neigung, azimut, nachbarn);
            b.HottcadGuid = guid == null ? null : N(guid);
            return b;
        }

        /// <summary>Ein IFC-Gebäude mit den Räumen A und C (über die GUID abgleichbar) und Bauteilen zu jeder Rangstufe.</summary>
        internal static AbbildGebaeude Ifc()
        {
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Probe" };
            g.Raeume.Add(new AbbildRaum { Kennung = "RA", Name = "Raum A", HottcadGuid = N(GA), FlaecheM2 = 50, VolumenM3 = 125 });
            g.Raeume.Add(new AbbildRaum { Kennung = "RC", Name = "Raum C", HottcadGuid = N(GC), FlaecheM2 = 50, VolumenM3 = 125 });
            g.Bauteile.Add(Bauteil("W1", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, U_AW1, 90, 0, G(1), "RA"));
            g.Bauteile.Add(Bauteil("W2", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, U_AW2, 90, 90, G(2), "RA"));
            g.Bauteile.Add(Bauteil("W3", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.8, 90, 180, G(3), "RA"));
            g.Bauteile.Add(Bauteil("W4", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.9, 90, 270, G(4), "RA"));
            g.Bauteile.Add(Bauteil("D5", Bauteilart.Decke, Randbedingung.Innen, 50, null, null, null, G(5), "RA", "RC"));
            g.Bauteile.Add(Bauteil("W6", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, U_AW1, 90, 0, G(6), "RA"));
            g.Bauteile.Add(Bauteil(SqprojRaumabgleich.IfcKennung(G(7)), Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, U_AW1, 90, 0, null, "RA"));
            g.Bauteile.Add(Bauteil("W8", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, U_AW1, 90, 0, "{d0000000-0000-0000-0000-000000000008}", "RA"));
            g.Bauteile.Add(Bauteil("F9", Bauteilart.Fenster, Randbedingung.Aussenluft, 2, 1.1, 90, 0, G(1), "RA"));
            return g;
        }
    }

    /// <summary>
    /// <b>BA-4b — Aufbauten aus der HottCAD-Projektdatei</b> (Konzept Bauteilaufbau 5.5, Befund Projektdatei N.1–N.10,
    /// Entscheid E97): Leser der vier Bauteiltabellen (Platzhalter, Null-Kennung, c · 1 000, Richtung, zwei Räume je Zeile,
    /// fehlende Tabellen), Zuordnung über die GUID mit Ausweich GlobalId, Rangfolge mit U-Abgleich samt mehrdeutigem
    /// Katalogtreffer, und der Bauteilvorschlag mit allen vier Rangstufen.
    /// </summary>
    public class SqprojAufbauTests
    {
        private static SqprojAbbild Lesen() => SqprojAufbauProbe.Lesen(SqprojAufbauProbe.Erzeuger(), "ba4b_leser.sqproj");

        [Fact]
        public void Der_Leser_liest_Huellflaechen_Bezuege_und_Aufbauten_innen_nach_aussen()
        {
            SqprojAbbild a = Lesen();
            Assert.False(a.Abgelehnt, a.Ablehnung?.ToString());
            Assert.True(a.BauteileGelesen);
            Assert.Equal(8, a.Huellflaechen.Count);
            Assert.Equal(7, a.Aufbauten.Count);
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.BAUTEILE);

            // SortNum aufsteigend = innen → außen; c in kJ/(kg·K) · 1 000.
            SqprojAufbau aw1 = a.Aufbauten[SqprojAufbauProbe.N(SqprojAufbauProbe.AW1)];
            Assert.Equal(new[] { "Beton", "Daemmstoff" }, aw1.Schichten.Select(s => s.Name));
            Assert.Equal(1000.0, aw1.Schichten[0].CpJkgK);
            Assert.Equal(1500.0, aw1.Schichten[1].CpJkgK);
            Assert.True(aw1.Schichten[1].IstDaemmung);
            Assert.False(aw1.Schichten[0].IstDaemmung);
            Assert.Equal(0.13, aw1.RsiM2KW);
            Assert.Equal(0.04, aw1.RseM2KW);
            Assert.True(aw1.HatSchichten);

            // Platzhalter und Null-Kennung heißen „nicht gesetzt“.
            SqprojSchicht x = a.Aufbauten[SqprojAufbauProbe.N(SqprojAufbauProbe.AWX)].Schichten.Single();
            Assert.Null(x.RhoKgM3);
            Assert.False(x.Vollstaendig);
            Assert.Null(a.Huellflaechen.Single(h => h.Uuid == "H4").AufbauKennung);

            // Eine Zeile mit zwei Räumen: innen ist der obere Raum (Rolle 8).
            SqprojHuellflaeche h5 = a.Huellflaechen.Single(h => h.Uuid == "H5");
            Assert.Equal(2, h5.Bezuege.Count);
            Assert.Equal("R2", h5.InnenRaum);
            Assert.Equal(SqprojAufbauProbe.N(SqprojAufbauProbe.G(5)), h5.Gid);
        }

        [Theory]
        [InlineData("BmElement")]
        [InlineData("TcBuildingElementDimensionLayer")]
        public void Ohne_Bauteiltabelle_bleibt_es_beim_Stand_ohne_Aufbauten(string tabelle)
        {
            SqprojAbbild a = SqprojAufbauProbe.Lesen(SqprojAufbauProbe.Erzeuger().Ohne(tabelle), "ba4b_ohne.sqproj");
            Assert.False(a.Abgelehnt, a.Ablehnung?.ToString());
            Assert.False(a.BauteileGelesen);
            Assert.Empty(a.Huellflaechen);
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == SqprojProtokoll.BAUTEILE_FEHLEN);
            Assert.Contains(tabelle, m.ToString());
            Assert.Equal(2, a.Raeume.Count);

            // Die Aufbauwahl ist dann leer.
            SqprojAufbauwahl w = SqprojAufbauwahl.Bilden(a, SqprojRaumabgleich.Bilden(a, SqprojAufbauProbe.Ifc()), SqprojAufbauProbe.Ifc());
            Assert.Empty(w.Entscheide);
        }

        [Fact]
        public void Eine_Probe_ohne_Bauteile_traegt_die_Tabellen_nicht()
        {
            SqprojAbbild a = SqprojAufbauProbe.Lesen(SqprojProbenErzeuger.Standard(), "ba4b_standard.sqproj");
            Assert.False(a.Abgelehnt);
            Assert.False(a.BauteileGelesen);
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.BAUTEILE_FEHLEN);
        }

        [Fact]
        public void Die_Rangfolge_E97_mit_U_Abgleich_Katalogsuche_und_Richtung()
        {
            SqprojAbbild a = Lesen();
            AbbildGebaeude g = SqprojAufbauProbe.Ifc();
            SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(a, g);
            Assert.Equal(2, abgleich.UeberGuid);
            SqprojAufbauwahl w = SqprojAufbauwahl.Bilden(a, abgleich, g);
            SqprojAufbauentscheid E(string k) => w.Entscheid(g.Bauteile.Single(b => b.Kennung == k));

            // Rang 1: U passt auf 1 %.
            SqprojAufbauentscheid w1 = E("W1");
            Assert.Equal(Aufbaurang.Projektdatei, w1.Rang);
            Assert.Equal(SqprojAufbauProbe.N(SqprojAufbauProbe.AW1), w1.Aufbau.Kennung);
            Assert.False(w1.UAbweichend);
            Assert.True(w1.UeberGuid);

            // Rang 2: anderer Stand — der Katalog trifft das U der IFC (auch unbenutzt); von zwei Aufbauten im Band entscheidet
            // das gleiche U.
            SqprojAufbauentscheid w2 = E("W2");
            Assert.Equal(Aufbaurang.Projektkatalog, w2.Rang);
            Assert.Equal(SqprojAufbauProbe.N(SqprojAufbauProbe.AW2), w2.Aufbau.Kennung);
            Assert.True(w2.UAbweichend);

            // Mehrdeutiger Katalogtreffer: nicht raten.
            SqprojAufbauentscheid w3 = E("W3");
            Assert.Equal(Aufbaurang.Keiner, w3.Rang);
            Assert.Null(w3.Aufbau);
            Assert.True(w3.KatalogMehrdeutig);

            // Ohne Aufbau (Null-Kennung) und ohne Katalogtreffer: bleibt beim IFC-Weg.
            SqprojAufbauentscheid w4 = E("W4");
            Assert.Equal(Aufbaurang.Keiner, w4.Rang);
            Assert.False(w4.KatalogMehrdeutig);

            // Decke ohne IFC-U: direkt; innen ist der obere Raum C, der im IFC-Bauteil an zweiter Stelle steht → umgedreht.
            SqprojAufbauentscheid d5 = E("D5");
            Assert.Equal(Aufbaurang.Projektdatei, d5.Rang);
            Assert.True(d5.Umgekehrt);
            Assert.False(d5.RichtungAngenommen);
            AbbildAufbau ab = w.Abbild(d5);
            Assert.Equal(Schichtrichtung.AussenNachInnen, ab.Richtung);
            Assert.Equal(Aufbaustatus.Vollstaendig, ab.Status);
            Assert.All(ab.Schichten, s => Assert.True(s.AusProjektdatei));
            Assert.Equal(1500.0, ab.Schichten[0].CpJkgK);
            Assert.Same(ab, w.Abbild(d5));

            // Zwei Hüllflächen mit derselben GId und verschiedenen Aufbauten: nicht geraten.
            Assert.Null(E("W6"));
            Assert.Contains(w.Mehrdeutig, b => b.Kennung == "W6");

            // Ausweich über die dekodierte GlobalId; ohne Gegenstück; Fenster werden nicht zugeordnet.
            SqprojAufbauentscheid w7 = E(SqprojRaumabgleich.IfcKennung(SqprojAufbauProbe.G(7)));
            Assert.Equal(Aufbaurang.Projektdatei, w7.Rang);
            Assert.False(w7.UeberGuid);
            Assert.Equal(1, w.UeberGlobalId);
            Assert.Equal(5, w.UeberGuid);
            Assert.Contains(w.OhneGegenstueck, b => b.Kennung == "W8");
            Assert.Null(E("F9"));
        }

        [Fact]
        public void Die_Toleranz_ist_ein_Prozent()
        {
            Assert.True(SqprojAufbauwahl.Passt(1.0, 1.01));
            Assert.True(SqprojAufbauwahl.Passt(0.99, 1.0));
            Assert.False(SqprojAufbauwahl.Passt(1.0, 1.0102));
            Assert.False(SqprojAufbauwahl.Passt(0.0, 1.0));
        }

        /// <summary>Der Vorschlag eines Raums mit Dach (Rang 1), Boden (Rang 2), Wand mit IFC-Schichten (Rang 3) und Wand ohne Aufbau (Rang 4).</summary>
        internal static GebaeudeBauteilvorschlag Vorschlag(bool mitProjektdatei)
        {
            SqprojAbbild pd = Lesen();
            var a = new GbxmlAbbild();
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Probe" };
            g.Raeume.Add(new AbbildRaum { Kennung = "RA", Quelltyp = "Space", Name = "Raum A", HottcadGuid = SqprojAufbauProbe.N(SqprojAufbauProbe.GA),
                                          FlaecheM2 = 50, VolumenM3 = 125 });
            g.Bauteile.Add(SqprojAufbauProbe.Bauteil("dach", Bauteilart.Dach, Randbedingung.Aussenluft, 50, SqprojAufbauProbe.U_AW1, 0, null,
                                                     SqprojAufbauProbe.G(1), "RA"));
            g.Bauteile.Add(SqprojAufbauProbe.Bauteil("boden", Bauteilart.Bodenplatte, Randbedingung.Erdreich, 50, SqprojAufbauProbe.U_AW2, 180, null,
                                                     SqprojAufbauProbe.G(2), "RA"));
            AbbildBauteil w3 = SqprojAufbauProbe.Bauteil("wand3", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 20, 0.9, 90, 0, null, "RA");
            w3.Aufbau = BauteilvorschlagProbe.Massiv("massiv");
            g.Bauteile.Add(w3);
            g.Bauteile.Add(SqprojAufbauProbe.Bauteil("wand4", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 30, 0.8, 90, 180, null, "RA"));
            a.Gebaeude.Add(g);
            SqprojStand stand = mitProjektdatei
                ? new SqprojStand("probe.sqproj", null, 0, pd, SqprojRaumabgleich.Bilden(pd, g), pd.Meldungen, null) : null;
            return GebaeudeBauteilvorschlag.Bilden(a, 0, 'F', null, new GbxmlImportProfil(), projektdatei: stand);
        }

        [Fact]
        public void Der_Vorschlag_uebernimmt_die_Aufbauten_der_Projektdatei_in_vier_Rangstufen()
        {
            GebaeudeBauteilvorschlag v = Vorschlag(true);
            Assert.False(v.Abgelehnt, string.Join("; ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            GebaeudeBauteilzeile Z(string k) => v.Zeilen.Single(z => z.Kennung == k);

            GebaeudeBauteilzeile dach = Z("dach");
            Assert.Equal(Aufbaurang.Projektdatei, dach.Aufbaurang);
            Assert.Equal(Bauteilzuordnungsstufe.A, dach.Stufe);
            Assert.Equal(SqprojAufbauProbe.U_AW1, dach.Bauteil.U_Wert.Value, 9);
            GebaeudeAufbauzeile ad = BauteilvorschlagProbe.AufbauZeile(v, dach);
            Assert.True(ad.AusProjektdatei);
            Assert.Equal(GebaeudeBauteilvorschlag.QUELLTYP_PD_AUFBAU, ad.Quelltyp);
            Assert.Equal("probe.sqproj", ad.Aufbau.Quelle);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, ad.Aufbau.Herkunft);
            Assert.Null(ad.Aufbau.Typaufbau);
            // innen → außen aus Sicht des Raums: Beton, dann Dämmung; c · 1 000.
            Assert.Equal(new double?[] { 2.0, 0.04 }, ad.Aufbau.Schichten.Select(s => s.Lambda));
            Assert.Equal(new double?[] { 1000.0, 1500.0 }, ad.Aufbau.Schichten.Select(s => s.Cp));
            Assert.Equal(2, ad.Projektstoffe.Count);
            Assert.All(ad.Projektstoffe, s =>
            {
                Assert.Equal(DbWerte.HERKUNFT_IFC, s.Herkunft);
                Assert.Equal(GebaeudeBauteilvorschlag.QUELLE_PROJEKTDATEI, s.Quelle);
            });
            Assert.Equal("Beton", ad.Projektstoffe[0].Bezeichner);
            Assert.Equal(1500.0, ad.Projektstoffe[1].Cp);

            GebaeudeBauteilzeile boden = Z("boden");
            Assert.Equal(Aufbaurang.Projektkatalog, boden.Aufbaurang);
            Assert.Equal(Bauteilzuordnungsstufe.A, boden.Stufe);
            Assert.Equal(SqprojAufbauProbe.U_AW2, boden.Bauteil.U_Wert.Value, 9);
            Assert.Equal(0.12, BauteilvorschlagProbe.AufbauZeile(v, boden).Aufbau.Schichten.Sum(s => s.Dicke) - 0.2, 9);

            Assert.Equal(Aufbaurang.IfcSchichten, Z("wand3").Aufbaurang);
            Assert.False(BauteilvorschlagProbe.AufbauZeile(v, Z("wand3")).AusProjektdatei);
            Assert.Equal(Aufbaurang.Ersatz, Z("wand4").Aufbaurang);
            Assert.NotEqual(Bauteilzuordnungsstufe.A, Z("wand4").Stufe);

            string[] info = BauteilvorschlagProbe.Schluessel(v, PruefStufe.Info);
            foreach (string k in new[] { GebaeudeBauteilvorschlag.PD_ZUORDNUNG, GebaeudeBauteilvorschlag.PD_RANG1, GebaeudeBauteilvorschlag.PD_RANG2,
                                         GebaeudeBauteilvorschlag.PD_RANG3, GebaeudeBauteilvorschlag.PD_RANG4 })
                Assert.Contains(k, info);
            Assert.Contains(GebaeudeBauteilvorschlag.PD_U_ABWEICHUNG, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Warnung));
        }

        [Fact]
        public void Ohne_Projektdatei_bleibt_der_Vorschlag_wie_er_war()
        {
            GebaeudeBauteilvorschlag v = Vorschlag(false);
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel.StartsWith("IMP_BAUTEIL_PROT_PD_", StringComparison.Ordinal));
            Assert.Equal(Aufbaurang.Ersatz, v.Zeilen.Single(z => z.Kennung == "dach").Aufbaurang);
            Assert.Equal(Aufbaurang.IfcSchichten, v.Zeilen.Single(z => z.Kennung == "wand3").Aufbaurang);
            Assert.DoesNotContain(v.Aufbauten, a => a.AusProjektdatei);
        }
    }

    /// <summary>BA-4b — die Stoffe der Projektdatei werden beim Schreiben Projektkopien mit Herkunft IFC und Quelle „Projektdatei“.</summary>
    [Collection("Testdatenbank")]
    public class SqprojAufbauDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Stoffe_der_Projektdatei_werden_Projektkopien()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];
            GebaeudeBauteilvorschlag v = SqprojAufbauTests.Vorschlag(true);
            Assert.False(v.Abgelehnt);

            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);

            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Bezeichner\", \"Lambda\", \"Rho\", \"cp\", \"Herkunft\" FROM \"Tab_Baustoff\" WHERE \"ID_Projekt\" = ? AND \"Quelle\" = ?",
                new DbParam("@p", PROJEKT), new DbParam("@q", GebaeudeBauteilvorschlag.QUELLE_PROJEKTDATEI));
            // Beton und Dämmstoff je einmal (gleiche Werte in Dach und Boden), Dämmstoff von AW2 mit gleichen Werten → derselbe.
            Assert.Equal(2, t.Rows.Count);
            Assert.All(t.Rows.Cast<DataRow>(), r => Assert.Equal(DbWerte.HERKUNFT_IFC, Convert.ToString(r["Herkunft"], CultureInfo.InvariantCulture)));
            Assert.Contains(t.Rows.Cast<DataRow>(), r => Convert.ToDouble(r["cp"], CultureInfo.InvariantCulture) == 1500.0);

            // Jede Schicht der übernommenen Aufbauten zeigt auf eine solche Kopie.
            long ohne = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + BauteilaufbauSchema.TAB_SCHICHT + "\" s JOIN \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" a ON a.\"ID\" = s.\"ID_Aufbau\" " +
                "WHERE a.\"ID_Projekt\" = ? AND a.\"Quelle\" = ? AND s.\"ID_Baustoff\" IS NULL",
                new DbParam("@p", PROJEKT), new DbParam("@q", "probe.sqproj")), CultureInfo.InvariantCulture);
            Assert.Equal(0, ohne);
        }
    }
}
