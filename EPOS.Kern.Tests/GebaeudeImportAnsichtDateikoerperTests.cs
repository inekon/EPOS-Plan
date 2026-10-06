using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7f-2 — die Dateikörper in den Daten der Ansicht</b> (Datenaustauschkonzept 15.4): Die Hülle
    /// <see cref="GebaeudeImportAnsicht"/> trägt den Körper der Datei je Raum in das DTO — Punkte relativ zum Bezugspunkt
    /// des Gebäudes als <c>float</c>, Indizes flach, Art, Vermerke als Schlüssel, Herkunft als Wert —, zählt die
    /// Dreiecke, hält sie gegen die Dreiecksgrenze und bildet das Bytefeld für das Modul deterministisch. Eingang ist ein
    /// künstlicher Körper mit georeferenzierten Koordinaten, die Kleinstprobe <c>ifc2x3_koerper_brep.ifc</c> über den
    /// Leser und das Zweizonenhaus über den eigenen Schreiber. Ohne Datenbank.
    /// </summary>
    public sealed class GebaeudeImportAnsichtDateikoerperTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportAnsichtDateikoerperTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        /// <summary>Ein Quader 4,25 × 2,5 × 3 m an georeferenzierter Stelle (Ost 1 000 000 m, Nord 2 000 000 m).</summary>
        private static Dateikoerper Quader(int dreieckeZusatz = 0, params Koerpervermerk[] vermerke)
        {
            double x0 = 1_000_000.0, y0 = 2_000_000.0, z0 = 100.0;
            var punkte = new List<double[]>();
            foreach (double z in new[] { z0, z0 + 3.0 })
                foreach ((double dx, double dy) in new[] { (0.0, 0.0), (4.25, 0.0), (4.25, 2.5), (0.0, 2.5) })
                    punkte.Add(new[] { x0 + dx, y0 + dy, z });
            var dreiecke = new List<int[]>
            {
                new[] { 0, 2, 1 }, new[] { 0, 3, 2 }, new[] { 4, 5, 6 }, new[] { 4, 6, 7 },
                new[] { 0, 1, 5 }, new[] { 0, 5, 4 }, new[] { 1, 2, 6 }, new[] { 1, 6, 5 },
                new[] { 2, 3, 7 }, new[] { 2, 7, 6 }, new[] { 3, 0, 4 }, new[] { 3, 4, 7 },
            };
            for (int i = 0; i < dreieckeZusatz; i++) dreiecke.Add(new[] { 0, 1, 2 });
            var kanten = new List<int[]>
            {
                new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 }, new[] { 0, 3 }, new[] { 4, 5 }, new[] { 5, 6 },
                new[] { 6, 7 }, new[] { 4, 7 }, new[] { 0, 4 }, new[] { 1, 5 }, new[] { 2, 6 }, new[] { 3, 7 },
            };
            return new Dateikoerper
            {
                PunkteM = punkte, Dreiecke = dreiecke, Normalen = dreiecke.Select(_ => new[] { 0.0, 0.0, 1.0 }).ToList(),
                Randkanten = kanten, Art = "FacetedBrep", Vermerke = vermerke,
            };
        }

        /// <summary>Das Gebäude aus <see cref="GebaeudeImportAnsichtKoerperTests.Eingang"/>, Raum A mit dem Körper <paramref name="koerper"/>.</summary>
        private static Zonengeometrie Geometrie(Dateikoerper koerper)
        {
            Umrisseingang e = GebaeudeImportAnsichtKoerperTests.Eingang(lageOg: 3.2);
            Umrissraum a0 = e.Raeume[0];
            var a = new Umrissraum
            {
                Kennung = a0.Kennung, Name = a0.Name, GeschossKennung = a0.GeschossKennung, Zone = a0.Zone, Beheizt = a0.Beheizt,
                FlaecheM2 = a0.FlaecheM2, VolumenM3 = a0.VolumenM3, HoeheM = a0.HoeheM, Koerper = koerper,
            };
            a.Seiten.AddRange(a0.Seiten);
            e.Raeume[0] = a;
            return Zonengeometrie.AusFlaechen(e);
        }

        [Fact]
        public void Die_Huelle_traegt_den_Dateikoerper_relativ_zum_Bezugspunkt()
        {
            Zonengeometrie z = Geometrie(Quader(0, Koerpervermerk.Bogen, Koerpervermerk.Offen));
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(z, umhaengbar: true);

            Assert.True(d.HatDateikoerper);
            Assert.Equal(z.DateikoerperDreiecke, (int)d.DateikoerperDreiecke);
            Assert.Equal(12L, d.DateikoerperDreiecke);
            Assert.False(d.DateikoerperZuGross);
            Assert.Equal(Dateikoerper.DREIECKSGRENZE, d.Dreiecksgrenze);
            Assert.Equal(GebaeudeAnsichtDaten.DREIECKSGRENZE, Dateikoerper.DREIECKSGRENZE);
            Assert.Equal(new[] { 1_000_000.0, 2_000_000.0, 100.0 }, d.Bezugspunkt);

            GebaeudeAnsichtKoerperraum a = d.Koerperraum("A")!;
            GebaeudeAnsichtDateikoerper k = a.Dateikoerper!;
            Assert.Equal(Koerperherkunft.Datei, a.Herkunft);
            Assert.Equal("FacetedBrep", k.Art);
            Assert.Equal(new[] { "Bogen", "Offen" }, k.Vermerke);
            Assert.Equal(12, k.DreieckZahl);
            Assert.Equal(24, k.Punkte.Count);
            Assert.Equal(36, k.Dreiecke.Count);
            Assert.Equal(24, k.Randkanten.Count);
            // Relativ zum Bezugspunkt bleiben die Zentimeter (float träfe bei 10⁶ m nur auf 0,06 m).
            Assert.Equal(new float[] { 0f, 0f, 0f, 4.25f, 0f, 0f, 4.25f, 2.5f, 0f }, k.Punkte.Take(9));
            Assert.Equal(new float[] { 0f, 2.5f, 3f }, k.Punkte.Skip(21));
            Assert.Equal(new[] { 0, 2, 1, 0, 3, 2 }, k.Dreiecke.Take(6));
            Assert.Equal(new[] { 0, 1, 1, 2 }, k.Randkanten.Take(4));

            // Die übrigen Räume: Prisma aus dem Umriss bzw. schematisch, je nach Herkunft des Umrisses.
            foreach (Raumumriss r in z.Raeume.Where(r => r.RaumKennung != "A"))
                Assert.Equal(r.Herkunft == Geometrieherkunft.Schematisch ? Koerperherkunft.Schematisch : Koerperherkunft.Umriss,
                             d.Koerperraum(r.RaumKennung)!.Herkunft);
            Assert.Null(d.Koerperraum("B")!.Dateikoerper);

            // Die Ansicht „Dateikörper": A aus der Datei, die anderen als Prisma.
            IReadOnlyList<GebaeudeAnsichtDateiraum> ansicht = d.Dateiansicht();
            GebaeudeAnsichtDateiraum ra = ansicht.Single(r => r.Raum.Kennung == "A");
            Assert.Equal(Koerperherkunft.Datei, ra.Herkunft);
            Assert.Same(k, ra.Datei);
            Assert.Null(ra.Prisma);
            Assert.All(ansicht.Where(r => r.Raum.Kennung != "A"), r => { Assert.Null(r.Datei); Assert.NotNull(r.Prisma); });
            Assert.Equal(d.Koerper().Count, ansicht.Count);
        }

        [Fact]
        public void Ohne_Dateikoerper_kein_Bezugspunkt_und_kein_Feld()
        {
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(Zonengeometrie.AusFlaechen(GebaeudeImportAnsichtKoerperTests.Eingang()), true);
            Assert.False(d.HatDateikoerper);
            Assert.Equal(0L, d.DateikoerperDreiecke);
            Assert.Equal(new double[3], d.Bezugspunkt);
            Assert.All(d.Koerperraeume, k => Assert.NotEqual(Koerperherkunft.Datei, k.Herkunft));
            GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
            Assert.Empty(f.Bytes);
            Assert.Empty(f.Verzeichnis);
        }

        [Fact]
        public void Ueber_der_Dreiecksgrenze_stehen_alle_Raeume_als_Prisma()
        {
            // Über die Grenzvariable: zwölf Dreiecke gegen eine Grenze von elf.
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(Geometrie(Quader()), true);
            Assert.False(d.DateikoerperZuGross);
            GebaeudeAnsichtDaten eng = d with { Dreiecksgrenze = 11 };
            Assert.True(eng.DateikoerperZuGross);
            Assert.True(eng.HatDateikoerper);
            Assert.All(eng.Dateiansicht(), r => Assert.NotEqual(Koerperherkunft.Datei, r.Herkunft));
            Assert.True((d with { Dreiecksgrenze = 12 }).DateikoerperZuGross is false);

            // Über die echte Grenze: ein Körper mit 300 001 Dreiecken.
            GebaeudeAnsichtDaten gross = GebaeudeImportAnsicht.AnsichtDaten(
                Geometrie(Quader(Dateikoerper.DREIECKSGRENZE + 1 - 12)), true);
            Assert.Equal(Dateikoerper.DREIECKSGRENZE + 1L, gross.DateikoerperDreiecke);
            Assert.True(gross.DateikoerperZuGross);
            Assert.DoesNotContain(gross.Dateiansicht(), r => r.Herkunft == Koerperherkunft.Datei);
        }

        [Fact]
        public void Das_Bytefeld_ist_Little_Endian_ausgerichtet_und_deterministisch()
        {
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(Geometrie(Quader()), true);
            GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
            GebaeudeAnsichtKoerperfeldEintrag e = Assert.Single(f.Verzeichnis);
            Assert.Equal("A", e.Raum);
            Assert.Equal(4 * (24 + 36 + 24), f.Bytes.Length);
            Assert.Equal((0, 8, 96, 12, 240, 12), (e.PunkteAb, e.PunktZahl, e.DreieckeAb, e.DreieckZahl, e.KantenAb, e.KantenZahl));
            Assert.Equal(4.25f, BinaryPrimitives.ReadSingleLittleEndian(f.Bytes.AsSpan(e.PunkteAb + 12, 4)));
            Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(f.Bytes.AsSpan(e.DreieckeAb + 4, 4)));
            Assert.Equal(7, BinaryPrimitives.ReadInt32LittleEndian(f.Bytes.AsSpan(e.KantenAb + 4 * 23, 4)));
            Assert.All(new[] { e.PunkteAb, e.DreieckeAb, e.KantenAb }, o => Assert.Equal(0, o % 4));

            // Zweimal aus derselben Geometrie gebildet: byteweise gleich, auch unter anderer Kultur.
            byte[] zweite;
            using (new Kulturvorrichtung("en-US"))
                zweite = GebaeudeImportAnsicht.AnsichtDaten(Geometrie(Quader()), true).Koerperfeld().Bytes;
            Assert.Equal(f.Bytes, zweite);
            Assert.Equal(f.Bytes, d.Koerperfeld().Bytes);
        }

        [Fact]
        public void Ein_Koerper_der_Kleinstprobe_kommt_ueber_den_Leser_in_der_Huelle_an()
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc2x3_koerper_brep.ifc");
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, 0);
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(zg, true);

            AbbildRaum raum = a.Abbild.Gebaeude[0].Raeume[0];
            GebaeudeAnsichtKoerperraum k = d.Koerperraum(raum.Kennung)!;
            Assert.Equal(Koerperherkunft.Datei, k.Herkunft);
            Assert.Equal("FacetedBrep", k.Dateikoerper!.Art);
            Assert.Equal(12, k.Dateikoerper.DreieckZahl);
            Assert.Equal(raum.Koerper.PunkteM.Count * 3, k.Dateikoerper.Punkte.Count);
            for (int i = 0; i < raum.Koerper.PunkteM.Count; i++)
                for (int x = 0; x < 3; x++)
                    Assert.Equal(raum.Koerper.PunkteM[i][x] - d.Bezugspunkt[x], k.Dateikoerper.Punkte[3 * i + x], 5);
            Assert.Equal(raum.Koerper.Dreiecke.SelectMany(t => t), k.Dateikoerper.Dreiecke);
            Assert.Equal(raum.Koerper.Randkanten.SelectMany(t => t), k.Dateikoerper.Randkanten);
            Assert.Equal(0f, Enumerable.Range(0, k.Dateikoerper.Punkte.Count / 3).Min(i => k.Dateikoerper.Punkte[3 * i]));
        }

        [Fact]
        public void Probe30_Zweizonenhaus_ueber_den_eigenen_Schreiber_Zahlen_des_Bytefelds()
        {
            GebaeudeAbbild quelle = GbxmlStufe2Tests.Zweizonen();
            byte[] datei = IfcExportProbe.Schreiben(quelle, IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz _);
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(IfcExportProbe.Lesen(datei), 0);
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(zg, true);
            GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();

            Assert.True(d.HatDateikoerper);
            Assert.Equal(zg.DateikoerperDreiecke, (int)d.DateikoerperDreiecke);
            Assert.Equal(zg.Raeume.Count(r => r.Koerper != null), f.Verzeichnis.Count);
            Assert.Equal(4 * f.Verzeichnis.Sum(e => 3 * e.PunktZahl + 3 * e.DreieckZahl + 2 * e.KantenZahl), f.Bytes.Length);
            Assert.Equal(f.Bytes, GebaeudeImportAnsicht.AnsichtDaten(GebaeudeGrundriss.Bilden(IfcExportProbe.Lesen(datei), 0), true).Koerperfeld().Bytes);
            Assert.All(d.Dateiansicht(), r => Assert.Equal(Koerperherkunft.Datei, r.Herkunft));
            _aus.WriteLine("Zweizonenhaus: " + f.Verzeichnis.Count + " Räume, " + d.DateikoerperDreiecke + " Dreiecke, Bytefeld "
                           + f.Bytes.Length.ToString(CultureInfo.InvariantCulture) + " Byte");
        }

        [Fact]
        public void Probe30_Quelldatei_MFH_klein_ueber_den_Leser_Zahlen_des_Bytefelds()
        {
            string pfad = Path.GetFullPath(Path.Combine(IfcProbenTests.Ordner(), "..", "..", "Quellen", "MFH-Klein-unsaniert-1964.ifc"));
            if (!File.Exists(pfad) || new FileInfo(pfad).Length < 1000) { _aus.WriteLine("Quelldatei fehlt: " + pfad); return; }
            var uhr = Stopwatch.StartNew();
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, 0);
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(zg, true);
            GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
            uhr.Stop();

            Assert.True(d.HatDateikoerper);
            Assert.False(d.DateikoerperZuGross);
            Assert.Equal(zg.Raeume.Count, f.Verzeichnis.Count);
            Assert.Equal(zg.DateikoerperDreiecke, (int)d.DateikoerperDreiecke);
            // Georeferenziert oder nicht: relativ zum Bezugspunkt ist jede Koordinate nicht negativ und klein.
            Assert.All(d.Koerperraeume, k => Assert.All(k.Dateikoerper!.Punkte, p => Assert.InRange(p, 0f, 1000f)));
            _aus.WriteLine("MFH-Klein: " + f.Verzeichnis.Count + " Räume, " + d.DateikoerperDreiecke + " Dreiecke, Bytefeld "
                           + f.Bytes.Length.ToString(CultureInfo.InvariantCulture) + " Byte, Bezugspunkt "
                           + string.Join(" ", d.Bezugspunkt.Select(x => x.ToString("R", CultureInfo.InvariantCulture)))
                           + ", " + uhr.ElapsedMilliseconds + " ms");
        }

        [Fact]
        public void Jeder_Koerpervermerk_hat_Schluessel_und_Text_in_beiden_Sprachen()
        {
            Assert.Equal(Enum.GetNames(typeof(Koerpervermerk)), GebaeudeAnsichtDateikoerper.VERMERKE);
            foreach (string sprache in new[] { "de-DE", "en-US" })
            {
                using (new Kulturvorrichtung(sprache))
                {
                    var texte = new GebaeudeAnsichtTexte();
                    foreach (string v in GebaeudeAnsichtDateikoerper.VERMERKE)
                    {
                        string schluessel = "GANS_VERMERK_" + v.ToUpperInvariant();
                        string text = Resource.ResourceManager.GetString(schluessel, CultureInfo.CurrentUICulture);
                        Assert.False(string.IsNullOrWhiteSpace(text), schluessel + " " + sprache);
                        Assert.Equal(text, texte.Vermerk(v));
                    }
                }
            }
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("open shell", new GebaeudeAnsichtTexte().Vermerk("Offen"));
            Assert.Equal("offene Schale", new GebaeudeAnsichtTexte().Vermerk("Offen"));
        }

        // =====================================================================
        //  HC-2 Teil A — Gruppen nach Randbedingung (HottCAD-Verbund 4.3)
        // =====================================================================

        /// <summary>Liest eine Körperprobe und gibt Abbild, Gebäude und die Daten der Ansicht mit Gruppen.</summary>
        private static (GebaeudeAbbild Abbild, AbbildGebaeude Gebaeude, Zonengeometrie Geometrie, GebaeudeAnsichtDaten Daten) MitGruppen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, 0);
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            return (a.Abbild, g, zg, GebaeudeImportAnsicht.AnsichtDaten(zg, true, null, g));
        }

        [Theory]
        [InlineData("ifc4_koerper_bauteile.ifc")]
        [InlineData("ifc4_koerper_nachbarn.ifc")]
        [InlineData("ifc4_koerper_nachbarn_grenzen.ifc")]
        public void HC2_Gruppenbyte_je_Dreieck_und_Gruppenliste_gegen_die_Bilanz(string datei)
        {
            (_, AbbildGebaeude g, Zonengeometrie zg, GebaeudeAnsichtDaten d) = MitGruppen(datei);

            Assert.True(d.HatRandgruppen);
            Assert.True(d.RandbedingungWaehlbar);
            Assert.Equal(Enumerable.Range(0, 8).Select(x => (Randgruppe)x), d.Flaechengruppen.Select(x => x.Gruppe));
            foreach (GebaeudeAnsichtFlaechengruppe x in d.Flaechengruppen)
                Assert.Equal(g.FlaechengruppenBilanzM2[(Flaechengruppe)(int)x.Gruppe], x.FlaecheM2, 9);

            // Je Raumkörper ein Byte je Dreieck, gleich der Gruppe seiner Zeile; die Zählung der Liste geht über alle Bytes.
            int[] zahl = new int[8];
            foreach (AbbildRaum r in g.Raeume.Where(x => x.Koerper != null))
            {
                IReadOnlyList<byte> gruppen = d.Koerperraum(r.Kennung)!.Dateikoerper!.Gruppen!;
                Assert.Equal(r.Koerper.DreieckZahl, gruppen.Count);
                foreach (Flaechengruppenzeile z in g.Flaechengruppen.Where(z => z.Raumkennung == r.Kennung))
                    Assert.All(z.Dreiecksindizes, t => Assert.Equal((byte)z.Gruppe, gruppen[t]));
                foreach (byte b in gruppen) zahl[b]++;
            }
            Assert.Equal(zahl, d.Flaechengruppen.Select(x => x.DreieckZahl));

            // Das Bytefeld: der erste Teil ist byteweise der ohne Gruppen, dahinter Bauteilkörper und Gruppenbytes.
            GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
            byte[] ohne = GebaeudeImportAnsicht.AnsichtDaten(zg, true).Koerperfeld().Bytes;
            Assert.Equal(ohne, f.Bytes.Take(ohne.Length));
            Assert.Equal(0, f.Bytes.Length % 4);
            foreach (GebaeudeAnsichtKoerperfeldEintrag e in f.Verzeichnis)
            {
                IReadOnlyList<byte> gruppen = d.Koerperraum(e.Raum)!.Dateikoerper!.Gruppen!;
                Assert.True(e.GruppenAb >= ohne.Length);
                Assert.Equal(gruppen, f.Bytes.Skip(e.GruppenAb).Take(e.DreieckZahl));
            }
            Assert.Equal(d.Bauteilkoerper.Select(b => (b.Bauteil, (int)b.Gruppe)), f.Bauteile.Select(e => (e.Bauteil!, e.Gruppe)));
            Assert.All(f.Bauteile, e => Assert.True(e.PunkteAb >= ohne.Length && e.KantenAb + 8 * e.KantenZahl <= f.Verzeichnis.Min(v => v.GruppenAb)));
            Assert.Equal(f.Bytes, MitGruppen(datei).Daten.Koerperfeld().Bytes);
            _aus.WriteLine(datei + ": " + string.Join(", ", d.Flaechengruppen.Select(x => x.Gruppe + " " + x.FlaecheM2.ToString("0.##", CultureInfo.InvariantCulture)
                                                                                       + " m² / " + x.DreieckZahl))
                           + "; Bauteilkörper " + d.Bauteilkoerper.Count + ", Bytefeld " + f.Bytes.Length + " Byte (ohne Gruppen " + ohne.Length + ")");
        }

        [Fact]
        public void HC2_Bauteilkoerper_tragen_Gruppe_und_Kennung_relativ_zum_Bezugspunkt()
        {
            (_, AbbildGebaeude g, _, GebaeudeAnsichtDaten d) = MitGruppen("ifc4_koerper_bauteile.ifc");
            AbbildBauteil sued = g.Bauteile.Single(b => b.Name == "Außenwand Süd");
            AbbildBauteil fenster = sued.Oeffnungen.Single();

            Assert.Equal(g.Flaechengruppen.Count(z => z.Raumkennung == null), d.Bauteilkoerper.Count);
            Assert.Contains(d.Bauteilkoerper, b => b.Bauteil == sued.Kennung && b.Gruppe == Randgruppe.R1);
            GebaeudeAnsichtBauteilkoerper f = d.Bauteilkoerper.Single(b => b.Bauteil == fenster.Kennung);
            Assert.Equal(Randgruppe.R7, f.Gruppe);
            Assert.Equal(fenster.Koerper.DreieckZahl, f.Koerper.DreieckZahl);
            for (int a = 0; a < 3; a++)
                Assert.Equal(fenster.Koerper.PunkteM[0][a] - d.Bezugspunkt[a], f.Koerper.Punkte[a], 4);
            Assert.Null(f.Koerper.Gruppen);
        }

        private static string Kantentext(GebaeudeAnsichtKoerperraum k)
            => "Kanten " + string.Join(" ", k.Kantengruppen!.SelectMany(p => p).Select(x => x?.ToString() ?? "-")) + ", Boden " + k.Bodengruppe
               + ", Marken " + string.Join(" ", k.Kantenmarken.Select(m => m.Polygon + ":" + m.Kante + " "
                                              + m.Von.ToString("0.###", CultureInfo.InvariantCulture) + "–"
                                              + m.Bis.ToString("0.###", CultureInfo.InvariantCulture)));

        [Fact]
        public void HC2_Grundriss_schematisch_Kanten_nach_Richtung_Boden_ohne_erfundene_Marke()
        {
            // HottCAD ohne Raumgrenzen: die Umrisse sind schematisch, ihre Lage erfunden — die Kanten gehen nach ihrer Richtung.
            (_, AbbildGebaeude g, Zonengeometrie zg, GebaeudeAnsichtDaten d) = MitGruppen("ifc4_koerper_bauteile.ifc");
            string wohnen = g.Raeume.Single(r => r.Name == "Wohnen").Kennung;
            Assert.Equal(Geometrieherkunft.Schematisch, zg.Raeume.Single(r => r.RaumKennung == wohnen).Herkunft);
            GebaeudeAnsichtKoerperraum k = d.Koerperraum(wohnen)!;
            List<Randgruppe?> kanten = k.Kantengruppen!.SelectMany(p => p).ToList();
            _aus.WriteLine("Wohnen: " + Kantentext(k));

            // Vier Kanten: drei gegen außen (R1), die zur Kammer gegen unbeheizt (R2); der Boden auf dem Erdreich (R3).
            Assert.Equal(4, kanten.Count);
            Assert.Equal(3, kanten.Count(x => x == Randgruppe.R1));
            Assert.Equal(1, kanten.Count(x => x == Randgruppe.R2));
            Assert.Equal(Randgruppe.R3, k.Bodengruppe);
            // Die Lage des Fensters auf einer erfundenen Kante wäre erfunden: keine Marke, R7 steht in der Legende.
            Assert.Empty(k.Kantenmarken);
            Assert.True(d.Flaechengruppe(Randgruppe.R7)!.FlaecheM2 > 0);

            // Die Kammer: ihre Trennfläche zu Wohnen ist aus ihrer Sicht R0.
            GebaeudeAnsichtKoerperraum kammer = d.Koerperraum(g.Raeume.Single(r => r.Name == "Kammer").Kennung)!;
            _aus.WriteLine("Kammer: " + Kantentext(kammer));
            Assert.Contains(Randgruppe.R0, kammer.Kantengruppen!.SelectMany(p => p));
        }

        [Fact]
        public void HC2_Grundriss_aus_Raumgrenzen_Kanten_nach_Lage()
        {
            (_, AbbildGebaeude g, Zonengeometrie zg, GebaeudeAnsichtDaten d) = MitGruppen("ifc4_koerper_nachbarn_grenzen.ifc");
            foreach (AbbildRaum r in g.Raeume.Where(x => x.Koerper != null))
            {
                Raumumriss u = zg.Raeume.Single(x => x.RaumKennung == r.Kennung);
                GebaeudeAnsichtKoerperraum k = d.Koerperraum(r.Kennung)!;
                _aus.WriteLine(r.Name + " (" + u.Herkunft + "): " + Kantentext(k) + "; Polygon "
                               + string.Join(" ", u.Polygone.SelectMany(p => p.Punkte).Select(p => p[0].ToString("0.###", CultureInfo.InvariantCulture) + "," + p[1].ToString("0.###", CultureInfo.InvariantCulture))));
            }
            // Büro: die Wand zum beheizten Nachbarn neutral (R0), die übrigen gegen außen, der Boden auf dem Erdreich.
            GebaeudeAnsichtKoerperraum buero = d.Koerperraum(g.Raeume.Single(r => r.Name == "Büro").Kennung)!;
            Assert.Equal(new Randgruppe?[] { Randgruppe.R1, Randgruppe.R0, Randgruppe.R1, Randgruppe.R1 }, buero.Kantengruppen!.Single());
            Assert.Equal(Randgruppe.R3, buero.Bodengruppe);
            Assert.Equal(Geometrieherkunft.Raumgrenzen, zg.Raeume.Single(x => x.RaumKennung == g.Raeume.Single(r => r.Name == "Büro").Kennung).Herkunft);
            // Büro 2 liegt darüber (Umriss schematisch, Kanten nach Richtung): alle Wände gegen außen, sein Boden ist die
            // Trenndecke zum beheizten Büro (R0, weiß).
            GebaeudeAnsichtKoerperraum buero2 = d.Koerperraum(g.Raeume.Single(r => r.Name == "Büro 2").Kennung)!;
            Assert.All(buero2.Kantengruppen!.Single(), x => Assert.Equal(Randgruppe.R1, x));
            Assert.Equal(Randgruppe.R0, buero2.Bodengruppe);
        }

        [Fact]
        public void HC2_Ohne_Gebaeude_oder_Koerper_keine_Gruppen_und_Randbedingung_nicht_waehlbar()
        {
            (_, _, Zonengeometrie zg, _) = MitGruppen("ifc4_koerper_bauteile.ifc");
            GebaeudeAnsichtDaten ohne = GebaeudeImportAnsicht.AnsichtDaten(zg, true);
            Assert.False(ohne.HatRandgruppen);
            Assert.False(ohne.RandbedingungWaehlbar);
            Assert.Empty(ohne.Bauteilkoerper);
            Assert.All(ohne.Koerperraeume, k => Assert.Null(k.Kantengruppen));

            // Ein Gebäude ohne Raumkörper (die CAD-Probe): keine Klassifikation, keine Gruppen.
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc4_z6_cad.ifc");
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            GebaeudeAnsichtDaten cad = GebaeudeImportAnsicht.AnsichtDaten(GebaeudeGrundriss.Bilden(a.Abbild, 0), true, null, a.Abbild.Gebaeude[0]);
            Assert.False(cad.HatRandgruppen);
            Assert.False(cad.RandbedingungWaehlbar);
        }

        [Fact]
        public void HC2_Prismenrueckfall_ueber_der_Dreiecksgrenze_ohne_Gruppen()
        {
            (_, _, _, GebaeudeAnsichtDaten d) = MitGruppen("ifc4_koerper_bauteile.ifc");
            // Über der Grenze der Ansicht: die Gruppen bleiben in den Daten, wählbar ist „Randbedingung“ nicht.
            GebaeudeAnsichtDaten klein = d with { Dreiecksgrenze = 1 };
            Assert.True(klein.DateikoerperZuGross);
            Assert.True(klein.HatRandgruppen);
            Assert.False(klein.RandbedingungWaehlbar);

            // Über der Grenze der Hülle (Raum- und Bauteilkörper zusammen): keine Gruppen, benannt.
            GebaeudeAnsichtDaten gross = Gross();
            Assert.True(gross.RandgruppenZuGross);
            Assert.False(gross.HatRandgruppen);
            Assert.False(gross.RandbedingungWaehlbar);
            Assert.Empty(gross.Bauteilkoerper);
            Assert.All(gross.Koerperraeume, k => Assert.Null(k.Dateikoerper?.Gruppen));
        }

        /// <summary>
        /// Die Bauteilprobe mit einem Fenster, dessen Körper die Dreiecksgrenze allein übersteigt (die Raumkörper bleiben darunter).
        /// </summary>
        private static GebaeudeAnsichtDaten Gross()
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc4_koerper_bauteile.ifc");
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            AbbildBauteil fenster = g.Bauteile.Single(b => b.Name == "Außenwand Süd").Oeffnungen.Single();
            Dateikoerper k = fenster.Koerper;
            fenster.Koerper = new Dateikoerper
            {
                PunkteM = k.PunkteM, Normalen = k.Normalen, Randkanten = k.Randkanten, Art = k.Art, Vermerke = k.Vermerke,
                Dreiecke = Enumerable.Repeat(k.Dreiecke[0], Dateikoerper.DREIECKSGRENZE + 1).ToList(),
            };
            return GebaeudeImportAnsicht.AnsichtDaten(GebaeudeGrundriss.Bilden(a.Abbild, 0), true, null, g);
        }

        [Fact]
        public void HC2_Jede_Gruppe_hat_Farbe_und_Namen_in_beiden_Sprachen()
        {
            Assert.Equal(GebaeudeAnsichtRandgruppen.ZAHL, GebaeudeAnsichtRandgruppen.FARBEN.Count);
            Assert.Equal(GebaeudeAnsichtRandgruppen.ZAHL, GebaeudeAnsichtRandgruppen.FARBEN.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(GebaeudeAnsichtRandgruppen.FARBEN, f => Assert.Matches("^#[0-9a-f]{6}$", f));
            Assert.Equal(Enum.GetNames(typeof(Flaechengruppe)).OrderBy(x => x), Enum.GetNames(typeof(Randgruppe)).OrderBy(x => x));
            foreach (Randgruppe x in Enum.GetValues(typeof(Randgruppe)))
                Assert.Equal((int)Enum.Parse<Flaechengruppe>(x.ToString()), (int)x);
            foreach (string sprache in new[] { "de-DE", "en-US" })
                using (new Kulturvorrichtung(sprache))
                    Assert.All(new GebaeudeAnsichtTexte().Randgruppen, t => Assert.False(string.IsNullOrWhiteSpace(t)));
            Assert.Equal("R1 Wand gegen außen", new GebaeudeAnsichtTexte().Gruppenname(Randgruppe.R1));
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("R1 wall to outside", new GebaeudeAnsichtTexte().Gruppenname(Randgruppe.R1));
        }
    }
}
