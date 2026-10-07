using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe K4 — die aus Flächen gebildeten Körper im Verbund</b> (Datenaustauschkonzept 17.4 bis 17.6, Proben 42 und 43):
    /// die Flächenklassifikation läuft formatfrei mit der ersten Regel <c>QUELLFLAECHE</c> (gbXML und Projektdatei), die Sperren
    /// gebildeter Körper (kein Körpervergleich, keine Flächenherkunft <c>KOERPER</c>, keine Körperpaare und Körpertrennflächen,
    /// kein Weg der Bauteilflächen aus Körpern) greifen bei <see cref="Koerperquelle.AusFlaechen"/> und ändern bei
    /// <see cref="Koerperquelle.Datei"/> (IFC) nichts, und „Datei erneut lesen“ bildet die Körper neu.
    /// </summary>
    public sealed class KoerperAbleitungTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string SCHALE = "gbxml_g5_closedshell.xml";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        // ------------------------------------------------------------------
        //  Klassifikation mit QUELLFLAECHE
        // ------------------------------------------------------------------

        [Fact]
        public void GbXml_Schale_klassifiziert_ueber_die_Quellflaeche()
        {
            AbbildGebaeude g = Assert.Single(GbxmlLesen(SCHALE).Gebaeude);
            Assert.NotNull(g.Flaechengruppen);
            var erwartet = new Dictionary<string, Flaechengruppe>(StringComparer.Ordinal)
            {
                ["boden-a"] = Flaechengruppe.R3, ["dach-a"] = Flaechengruppe.R5, ["aw-a-sued"] = Flaechengruppe.R1,
                ["aw-a-west"] = Flaechengruppe.R1, ["aw-a-nord"] = Flaechengruppe.R1, ["iw-ab"] = Flaechengruppe.R0,
            };
            List<Flaechengruppenzeile> a = g.Flaechengruppen.Where(z => z.Raumkennung == "raum-a").ToList();
            foreach (KeyValuePair<string, Flaechengruppe> e in erwartet)
            {
                Flaechengruppenzeile z = a.FirstOrDefault(x => x.Bauteilkennung == e.Key && x.Beleg == Flaechenklassifikation.BELEG_QUELLFLAECHE);
                Assert.True(z != null, e.Key + ": keine Zeile mit Quellfläche");
                Assert.Equal(e.Value, z.Gruppe);
            }
            // Jede Zeile mit Quellfläche nennt ein Bauteil der Datei; Paare aus gebildeten Körpern gibt es nicht.
            Assert.All(g.Flaechengruppen.Where(z => z.Beleg == Flaechenklassifikation.BELEG_QUELLFLAECHE),
                       z => Assert.Contains(g.Bauteile, b => b.Kennung == z.Bauteilkennung));
            Assert.DoesNotContain(g.Flaechengruppen, z => z.Beleg == Flaechenklassifikation.BELEG_PAAR);
            Assert.True(g.FlaechengruppenBilanzM2[Flaechengruppe.R1] > 0.0);
        }

        [Fact]
        public void Projektdatei_klassifiziert_ueber_die_Quellflaeche_Ersatzkennungen_fallen_zurueck()
        {
            GebaeudeAbbild abbild = SqprojLesen(SqprojProbenErzeuger.Geometriehaus());
            AbbildGebaeude g = abbild.Gebaeude.Single(x => x.Raeume.Any(r => r.Kennung == "R1"));
            Assert.NotNull(g.Flaechengruppen);
            List<Flaechengruppenzeile> r1 = g.Flaechengruppen.Where(z => z.Raumkennung == "R1").ToList();
            Flaechengruppe Gruppe(string bauteil)
                => Assert.Single(r1, z => z.Bauteilkennung == bauteil && z.Beleg == Flaechenklassifikation.BELEG_QUELLFLAECHE).Gruppe;
            Assert.Equal(Flaechengruppe.R1, Gruppe("AW"));
            Assert.Equal(Flaechengruppe.R0, Gruppe("IW"));
            Assert.Equal(Flaechengruppe.R5, Gruppe("DA"));
            Assert.Contains(Gruppe("BP"), new[] { Flaechengruppe.R3, Flaechengruppe.R4 });
            // Die Wände West und Nord tragen Ersatzkennungen (Raum:Mantel…) — sie gehen über die übrigen Regeln.
            Dateikoerper k = g.Raeume.Single(r => r.Kennung == "R1").Koerper;
            Assert.Contains(k.Quellflaechen, q => q.StartsWith("R1:Mantel", StringComparison.Ordinal));
            Assert.Contains(r1, z => z.Beleg != Flaechenklassifikation.BELEG_QUELLFLAECHE);
            Assert.DoesNotContain(r1, z => z.Beleg == Flaechenklassifikation.BELEG_PAAR);
        }

        [Fact]
        public void Ifc_Klassifikation_bleibt_ohne_Quellflaeche()
        {
            GebaeudeImportAblauf a = IfcLesen("ifc4_koerper_nachbarn.ifc", Koerperquelle.Datei);
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.NotNull(g.Flaechengruppen);
            Assert.DoesNotContain(g.Flaechengruppen, z => z.Beleg == Flaechenklassifikation.BELEG_QUELLFLAECHE);
            Assert.Contains(g.Flaechengruppen, z => z.Beleg == Flaechenklassifikation.BELEG_PAAR);
        }

        // ------------------------------------------------------------------
        //  Sperren (Probe 43): greifen bei AusFlaechen, ändern bei Datei nichts
        // ------------------------------------------------------------------

        [Fact]
        public void Sperre_Koerpervergleich_gegen_den_Mengensatz()
        {
            static bool Vergleich(GebaeudeImportAblauf a)
                => a.Meldungen.Concat(a.Abbild.Gebaeude.SelectMany(g => g.Meldungen))
                    .Any(m => m.Schluessel.StartsWith(P + "KOERPER_ABWEICHUNG", StringComparison.Ordinal)
                           || m.Schluessel == P + "KOERPER_REST");
            Assert.True(Vergleich(IfcDateiLesen("ifc4_g5_abweichungen.ifc", Koerperquelle.Datei)));
            Assert.False(Vergleich(IfcDateiLesen("ifc4_g5_abweichungen.ifc", Koerperquelle.AusFlaechen)));
        }

        [Fact]
        public void Sperre_Flaechenherkunft_Koerper()
        {
            static IEnumerable<AbbildBauteil> Alle(GebaeudeImportAblauf a)
                => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).SelectMany(b => new[] { b }.Concat(b.Oeffnungen));
            GebaeudeImportAblauf datei = IfcDateiLesen(KoerperflaechenTests.OHNE, Koerperquelle.Datei);
            GebaeudeImportAblauf ab = IfcDateiLesen(KoerperflaechenTests.OHNE, Koerperquelle.AusFlaechen);
            Assert.Contains(Alle(datei), b => b.Flaechenherkunft == Flaechenherkunft.Koerper);
            Assert.DoesNotContain(Alle(ab), b => b.Flaechenherkunft == Flaechenherkunft.Koerper);
        }

        [Fact]
        public void Sperre_Bauteilflaechen_je_Raum_aus_Raum_und_Bauteilkoerpern()
        {
            static bool Koerperweg(GebaeudeImportAblauf a)
                => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Any(b => b.Grenzen.Any(x => x.Herkunft == Grenzherkunft.Bauteilkoerper));
            static bool RaumflaecheAusKoerper(GebaeudeImportAblauf a)
                => a.Abbild.Gebaeude.SelectMany(g => g.Meldungen).Any(m => m.Schluessel == P + "RAUMFLAECHE_KOERPER");
            GebaeudeImportAblauf datei = IfcDateiLesen(KoerperflaechenTests.OHNE, Koerperquelle.Datei);
            GebaeudeImportAblauf ab = IfcDateiLesen(KoerperflaechenTests.OHNE, Koerperquelle.AusFlaechen);
            Assert.True(Koerperweg(datei));
            Assert.False(Koerperweg(ab));
            Assert.False(RaumflaecheAusKoerper(ab));
        }

        [Fact]
        public void Sperre_Koerperpaare_und_Koerpertrennflaechen()
        {
            AbbildGebaeude datei = IfcLesen("ifc4_koerper_nachbarn.ifc", Koerperquelle.Datei).Abbild.Gebaeude.Single();
            AbbildGebaeude ab = IfcLesen("ifc4_koerper_nachbarn.ifc", Koerperquelle.AusFlaechen).Abbild.Gebaeude.Single();
            Assert.Equal(2, datei.ZahlKoerperpaare);
            Assert.Contains(datei.Bauteile, b => b.Grenzen.Any(x => x.Herkunft == Grenzherkunft.Koerper));

            Assert.Equal(0, ab.ZahlKoerperpaare);
            Assert.False(ab.KoerperpaareGebildet);
            Assert.DoesNotContain(ab.Bauteile, b => b.Grenzen.Any(x => x.Herkunft == Grenzherkunft.Koerper));
            Assert.DoesNotContain(ab.Flaechengruppen ?? new List<Flaechengruppenzeile>(), z => z.Beleg == Flaechenklassifikation.BELEG_PAAR);
            // Die Körper selbst bleiben für die Ansicht da.
            Assert.All(ab.Raeume, r => Assert.Equal(Koerperquelle.AusFlaechen, r.Koerper.Quelle));
        }

        [Fact]
        public void Koerpernachbarschaft_paart_nur_Koerper_der_Datei()
        {
            AbbildGebaeude g = IfcLesen("ifc4_koerper_nachbarn.ifc", Koerperquelle.Datei).Abbild.Gebaeude.Single();
            List<Dateikoerper> datei = g.Raeume.Select(r => r.Koerper).ToList();
            List<Dateikoerper> ab = datei.Select(k => k.MitQuelle(Koerperquelle.AusFlaechen)).ToList();
            Assert.NotEmpty(Koerpernachbarschaft.Paare(datei));
            Assert.Empty(Koerpernachbarschaft.Paare(ab));
            Assert.True(datei[0].IstBeleg);
            Assert.False(ab[0].IstBeleg);
            Assert.Equal(datei[0].Text(), ab[0].MitQuelle(Koerperquelle.Datei).Text());
        }

        [Fact]
        public void Ifc_Koerper_unveraendert_mit_der_Vorgabe_der_Pruefnaht()
        {
            GebaeudeImportAblauf a = IfcLesen("ifc4_koerper_nachbarn.ifc", Koerperquelle.Datei);
            GebaeudeImportAblauf b;
            var abl = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Raumkoerperproben()["ifc4_koerper_nachbarn.ifc"]))
                abl.Lesen(s, "ifc4_koerper_nachbarn.ifc", new IfcImportProfil());
            b = abl;
            string Text(GebaeudeImportAblauf x) => string.Join("\n", x.Abbild.Gebaeude.SelectMany(g => g.Raeume).Select(r => r.Koerper?.Text()));
            Assert.Equal(Text(b), Text(a));
            Assert.DoesNotContain("Quelle ", Text(a), StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------
        //  Neulesen
        // ------------------------------------------------------------------

        [Fact]
        public void Neulesen_GbXml_bildet_die_Koerper_neu()
        {
            string pfad = GbxmlImportTests.Probe(SCHALE);
            NeulesenErgebnis e1 = Neulesen(pfad, DbWerte.IMPORT_FORMAT_GBXML);
            NeulesenErgebnis e2 = Neulesen(pfad, DbWerte.IMPORT_FORMAT_GBXML);
            Assert.Equal(NeulesenZustand.Passend, e1.Zustand);
            Pruefen(e1, e2);
        }

        [Fact]
        public void Neulesen_Projektdatei_bildet_die_Koerper_neu()
        {
            string pfad = SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("k4neulesen"));
            _dateien.Add(pfad);
            NeulesenErgebnis e1 = Neulesen(pfad, DbWerte.IMPORT_FORMAT_SQPROJ);
            NeulesenErgebnis e2 = Neulesen(pfad, DbWerte.IMPORT_FORMAT_SQPROJ);
            Assert.Equal(NeulesenZustand.Passend, e1.Zustand);
            Pruefen(e1, e2);
        }

        private static void Pruefen(NeulesenErgebnis e1, NeulesenErgebnis e2)
        {
            AbbildGebaeude g = Assert.IsType<AbbildGebaeude>(e1.Gebaeude);
            Assert.Contains(g.Raeume, r => r.Koerper != null);
            Assert.All(g.Raeume.Where(r => r.Koerper != null), r => Assert.Equal(Koerperquelle.AusFlaechen, r.Koerper.Quelle));
            Assert.Contains(g.Flaechengruppen, z => z.Beleg == Flaechenklassifikation.BELEG_QUELLFLAECHE);
            Assert.True(e1.Geometrie.HatDateikoerper);
            string Text(NeulesenErgebnis e) => string.Join("\n", e.Gebaeude.Raeume.Select(r => r.Kennung + ":" + r.Koerper?.Text()));
            Assert.Equal(Text(e1), Text(e2));
        }

        // ------------------------------------------------------------------
        //  Hilfen
        // ------------------------------------------------------------------

        private static GebaeudeAbbild GbxmlLesen(string name)
        {
            using (FileStream s = File.OpenRead(GbxmlImportTests.Probe(name)))
                return new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
        }

        private GebaeudeAbbild SqprojLesen(SqprojProbenErzeuger e)
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("k4"));
            _dateien.Add(pfad);
            var profil = new SqprojImportProfil();
            using (FileStream f = File.OpenRead(pfad))
                return profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
        }

        private static GebaeudeImportAblauf IfcLesen(string probe, Koerperquelle quelle)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Raumkoerperproben()[probe]))
                a.Lesen(s, probe, new IfcImportProfil { KoerperquellePruefung = quelle });
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static GebaeudeImportAblauf IfcDateiLesen(string datei, Koerperquelle quelle)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil { KoerperquellePruefung = quelle });
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static NeulesenErgebnis Neulesen(string pfad, string format)
        {
            byte[] b = File.ReadAllBytes(pfad);
            var q = new ImportquelleModel
            {
                ID = 1, ID_Gebaeude = 1, Format = format, Dateiname = Path.GetFileName(pfad),
                Hash = Convert.ToHexStringLower(SHA256.HashData(b)), Groesse = b.LongLength, Zeitpunkt = "2026-10-07T10:00:00+02:00",
            };
            using FileStream s = File.OpenRead(pfad);
            return GebaeudeNeulesen.Lesen(s, pfad, q, "", false);
        }
    }
}
