using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle A — die Zonenprobe <c>ifc4_zonen.ifc</c></b> (<see cref="IfcProbenErzeuger.Zonenhaus"/>)
    /// vom Leser bis zum Vorschlag: Beheizungsregeln B3 und B5, Zonen der Datei (geschachtelt, mehrfach),
    /// Klassifikation, Polygonflächen einer Fassade über zwei Geschosse, Gegenstücke der Datei und der
    /// Geometrie, Grenzen ohne Gegenstück, eine zu kleine Zone und die Gegenprobe. Ohne Datenbank.
    /// </summary>
    public sealed class ZonenimportProbenTests : IDisposable
    {
        private const string PROBE = "ifc4_zonen.ifc";
        private const string I = "IMP_IFC_PROT_";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public ZonenimportProbenTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static AbbildRaum Raum(AbbildGebaeude g, string name) => Assert.Single(g.Raeume, r => r.Name == name);

        private static bool Hat(IEnumerable<PruefMeldung> meldungen, string schluessel) => meldungen.Any(m => m.Schluessel == schluessel);

        // ==================================================================
        //  Der Leser
        // ==================================================================

        [Fact]
        public void Der_Leser_liefert_Beheizungsregeln_Zonen_Klassifikation_und_Grenzflaechen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);

            // B5: das Lager im Untergeschoss ohne Außenluftgrenze; B3 vor der Namensregel beim Abstellraum.
            AbbildRaum lager = Raum(g, "Lager");
            Assert.False(lager.Beheizt);
            Assert.Equal("B5", lager.Beheizungsregel);
            Assert.Equal(BeheiztQuelle.Lage, lager.BeheiztQuelle);
            Assert.True(Hat(g.Meldungen, I + "UNBEHEIZT_LAGE"));
            AbbildRaum abstell = Raum(g, "Abstellraum");
            Assert.True(abstell.Beheizt);
            Assert.Equal("B3", abstell.Beheizungsregel);

            // Zonen der Datei: geschachtelt aufgelöst, der Abstellraum in zweien — in keiner.
            Assert.Equal("Wohnbereich", Raum(g, "Wohnen").ZonenName);
            Assert.Equal("Obergeschoss", Raum(g, "Schlafen").ZonenName);
            Assert.Equal(Raum(g, "Schlafen").ZonenKennung, Raum(g, "Bad").ZonenKennung);
            Assert.True(abstell.ZoneMehrfach);
            Assert.Null(abstell.ZonenKennung);
            Assert.Null(lager.ZonenKennung);
            Assert.Equal(3, g.ZahlZonen);
            Assert.True(Hat(a.Abbild.Meldungen, I + "RAUM_MEHRFACH"));
            Assert.Equal("Probenklassifikation|WO", Raum(g, "Küche").Klassifikation);

            // Die Fassade Süd: vier Polygone, je Raum eine Grenze mit Fläche, Schwerpunkt und Normale.
            AbbildBauteil fassade = Assert.Single(g.Bauteile, b => b.Name == "Fassade Süd");
            Assert.Equal(new[] { 16.8, 11.2, 16.8, 11.2 }, fassade.Grenzen.Select(x => Math.Round(x.FlaecheM2.Value, 6)));
            AbbildGrenze oben = fassade.Grenzen[2];
            Assert.Equal(3.0, oben.SchwerpunktM[0], 6);
            Assert.Equal(0.0, oben.SchwerpunktM[1], 6);
            Assert.Equal(4.4, oben.SchwerpunktM[2], 6);   // OG bei 3 m, Mitte der 2,8 m
            Assert.Equal(-1.0, oben.Normale[1], 9);
            Assert.NotNull(fassade.AzimutGrad);

            // Die Geschossdecke: Dicke aus den Mengen, das Gegenstück der Datei zwischen Küche und Bad.
            AbbildBauteil decke = Assert.Single(g.Bauteile, b => b.Name == "Geschossdecke");
            Assert.Equal(0.2, decke.DickeM.Value, 9);
            Assert.Equal(5, decke.Grenzen.Count);
            AbbildGrenze kueche = decke.Grenzen.Single(x => x.RaumKennung == Raum(g, "Küche").Kennung);
            AbbildGrenze bad = decke.Grenzen.Single(x => x.RaumKennung == Raum(g, "Bad").Kennung);
            Assert.Equal(bad.Kennung, kueche.GegenstueckKennung);
            Assert.Equal(kueche.Kennung, bad.GegenstueckKennung);
            Assert.Equal(30.0, bad.FlaecheM2.Value, 6);
            Assert.Equal(3.0, bad.SchwerpunktM[2], 6);
            Assert.Equal(2.8, kueche.SchwerpunktM[2], 6);
            // Das Fenster trägt seine eigene Grenze.
            AbbildBauteil fenster = Assert.Single(fassade.Oeffnungen, o => o.Name == "F-S-OG");
            Assert.Equal(Raum(g, "Schlafen").Kennung, Assert.Single(fenster.Grenzen).RaumKennung);
        }

        // ==================================================================
        //  Zonierung
        // ==================================================================

        [Fact]
        public void Je_Geschoss_teilt_sich_die_Fassade_nach_Polygonen_und_die_Decken_werden_Trennflaechen()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(BauteilvorschlagProbe.Lesen(PROBE).Abbild, 0);
            Assert.Equal("Z4", z.Regel);
            Assert.Equal(new[] { "Z1", "Z2", "Z4", "Z5" }, z.Regeln);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new double?[] { 80.0, 80.0, 79.5 }, z.Zonen.Select(x => x.FlaecheM2));
            Assert.Equal(new double?[] { 28.0, 28.0 }, z.Flaechen.Where(f => f.Bauteil.Name == "Fassade Süd").Select(f => (double?)Math.Round(f.BruttoM2.Value, 6)));
            Assert.Equal(new[] { 1, 1 }, z.Flaechen.Where(f => f.Bauteil.Name == "Fassade Süd").Select(f => f.Oeffnungen.Count));
            Zonentrennung decke = Assert.Single(z.Trennungen, t => t.ZoneA == 1 && t.ZoneB == 2);
            Assert.Equal(80.0, decke.FlaecheA, 6);
            Assert.Equal(79.5, decke.FlaecheB, 6);
            Assert.False(decke.Ungleich);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 0 && t.ZoneB == 1 && t.FlaecheA == 80.0);
            Assert.False(Hat(z.Meldungen, I + "OHNE_GEGENSTUECK"));
            Assert.False(Hat(z.Meldungen, I + "FLAECHE_AUFGETEILT"));
        }

        [Fact]
        public void Nach_den_Zonen_der_Datei_paaren_sich_Grenzen_ueber_Datei_und_Geometrie_und_der_Abstellraum_wird_zugeschlagen()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(BauteilvorschlagProbe.Lesen(PROBE).Abbild, 0, "Z1");
            foreach (PruefMeldung m in z.Meldungen) _aus.WriteLine(m.ToString());
            Assert.Equal(new[] { "ohne Zone (unbeheizt)", "Wohnbereich", "Küchenbereich", "Obergeschoss" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Schlafen", "Bad", "Abstellraum" }, z.Zonen[3].Raeume.Select(r => r.Name));
            Assert.Equal(new[] { "ohne Zone" }, z.Zonen[3].Zugeschlagen);
            Assert.True(Hat(z.Meldungen, I + "ZONE_ZUGESCHLAGEN"));

            // Wohnen/Schlafen über die Geometrie, Küche/Bad über die Datei — mit Gegenprobe 32 gegen 30 m².
            Assert.Contains(z.Trennungen, t => t.ZoneA == 1 && t.ZoneB == 3 && t.FlaecheA == 48.0 && t.FlaecheB == 48.0);
            Zonentrennung kb = Assert.Single(z.Trennungen, t => t.ZoneA == 2 && t.ZoneB == 3);
            Assert.Equal(32.0, kb.FlaecheA, 6);
            Assert.Equal(30.0, kb.FlaecheB, 6);
            Assert.True(kb.Ungleich);
            Assert.True(Hat(z.Meldungen, I + "TRENNFLAECHE_UNGLEICH"));

            // Ohne Gegenstück: die Kellerdecke (drei Zonen, keine Geometrie) und der Boden des Abstellraums.
            PruefMeldung ohne = Assert.Single(z.Meldungen, m => m.Schluessel == I + "OHNE_GEGENSTUECK");
            Assert.Equal("4", ohne.Werte[0]);
            Assert.Equal(4, z.Flaechen.Count(f => f.OhneGegenstueck));
            // Die Nordwand im EG liegt an zwei Zonen ohne Polygone — hälftig geteilt.
            Assert.Equal(new double?[] { 14.0, 14.0 }, z.Flaechen.Where(f => f.Bauteil.Name == "EG Nord").Select(f => f.BruttoM2));
            Assert.True(Hat(z.Meldungen, I + "FLAECHE_AUFGETEILT"));
            // Die geschachtelte Zone „Obergeschoss" fasst Schlafen und Bad: die Wand dazwischen ist innere Masse.
            Assert.Equal(Zonenrand.Innen, Assert.Single(z.Flaechen, f => f.Bauteil.Name == "IW OG").Rand);
        }

        [Fact]
        public void Nach_der_Klassifikation_bilden_sich_drei_Zonen()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(BauteilvorschlagProbe.Lesen(PROBE).Abbild, 0, "Z2");
            Assert.Equal(new[] { "NB", "WO", "SL" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { false, true, true }, z.Zonen.Select(x => x.IstBeheizt));
        }

        // ==================================================================
        //  Vorschlag
        // ==================================================================

        [Fact]
        public void Der_Vorschlag_je_Geschoss_traegt_die_Fassade_in_zwei_Zonen_mit_ihren_Fenstern()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            foreach (PruefMeldung m in v.Meldungen) _aus.WriteLine(m.ToString());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.Equal(3, v.Zonen.Count);
            List<GebaeudeBauteilzeile> fassade = v.Zeilen.Where(z => z.Bauteil.Bezeichner == "Fassade Süd").ToList();
            Assert.Equal(new[] { 1, 2 }, fassade.Select(z => z.Zone));
            Assert.Equal(new[] { 24.0, 25.0 }, fassade.Select(z => Math.Round(z.Bauteil.Flaeche, 6)));   // 28 − 4 und 28 − 3
            Assert.Equal(1, v.Zeilen.Single(z => z.Kennung != null && z.Bauteil.Bezeichner == "F-S-EG").Zone);
            Assert.Equal(2, v.Zeilen.Single(z => z.Kennung != null && z.Bauteil.Bezeichner == "F-S-OG").Zone);
            GebaeudeBauteilzeile gd = Assert.Single(v.Zeilen, z => z.Bauteil.Bezeichner == "Geschossdecke");
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, gd.Bauteil.Randbedingung);
            Assert.Equal(80.0, gd.Bauteil.Flaeche, 6);
            Assert.Equal(0.0, gd.Bauteil.Neigung);
        }

        [Fact]
        public void Der_Vorschlag_nach_den_Zonen_der_Datei_rechnet_mit_Grenzen_ohne_Gegenstueck_als_unbeheizt()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "Z1");
            foreach (PruefMeldung m in v.Meldungen) _aus.WriteLine(m.ToString());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.Equal(4, v.Zonen.Count);
            Assert.Equal(4, v.Zeilen.Count(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_UNBEHEIZT
                                                && (z.Bauteil.Bezeichner == "Kellerdecke" || z.Bauteil.Bezeichner == "Geschossdecke")));
            // Die Trennfläche Küche/Bad rechnet mit der größeren Beschreibung.
            GebaeudeBauteilzeile kb = Assert.Single(v.Zeilen, z => z.Bauteil.Bezeichner == "Geschossdecke" && z.Zone == 2);
            Assert.Equal(32.0, kb.Bauteil.Flaeche, 6);
            Assert.Equal(-4, kb.Bauteil.ID_Nachbarzone);
        }

        [Fact]
        public void Sechzig_Zellen_je_Raum_sind_zu_viele_Zonen_und_die_Geschossregel_wird_vorgeschlagen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_zonen_viele.xml");
            GebaeudeZonierung x3 = GebaeudeZonierung.Bilden(a.Abbild, 0, "X3");
            Assert.Equal("X2", x3.Vorgabe);
            Assert.Equal(60, x3.Zonen.Count);
            Assert.All(x3.Zonen, z => Assert.True(z.ZuKlein));   // 10 m² unter 12 m², ohne Nachbarn
            Assert.True(x3.ZuVieleZonen);
            Assert.Equal("X2", x3.Vorschlagsregel);
            Assert.Equal(new[] { "60", "50", "X2", "2" },
                         Assert.Single(x3.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_ZU_VIELE_ZONEN_VORSCHLAG").Werte);

            GebaeudeBauteilvorschlag abgelehnt = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, 'H', "X3");
            Assert.True(abgelehnt.Abgelehnt);
            Assert.Contains(abgelehnt.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.ZU_VIELE_ZONEN);

            GebaeudeBauteilvorschlag x2 = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, 'H');
            Assert.False(x2.Abgelehnt, string.Join(" | ", x2.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, x2.Zonen.Select(z => z.Bezeichner));
            Assert.Equal(new double?[] { 300.0, 300.0 }, x2.Zonen.Select(z => z.Nutzflaeche));
        }

        [Fact]
        public void Der_Einzonenfall_bleibt_der_Vorschlag_aus_G4b()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            Assert.Equal(GebaeudeBauteilvorschlag.Bilden(a, 0, null).Zeilen.Select(z => z.ToString()),
                         GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "Z5").Zeilen.Select(z => z.ToString()));
        }
    }
}
