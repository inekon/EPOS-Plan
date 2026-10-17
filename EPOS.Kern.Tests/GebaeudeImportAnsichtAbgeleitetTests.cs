using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 42 — gleicher Umfang wie IFC</b> (Datenaustauschkonzept 17.8), Hülle: Ein Gebäude aus gbXML und aus der
    /// Projektdatei kommt mit gebildeten Körpern in der Ansicht an — Herkunft „aus Flächen gebildet“, Farbmodus Randbedingung
    /// wählbar, je Dreieck Gruppe und Bauteil der Quellfläche (Farbmodi Aufbau und Befund in 3D), der Weg des Körpers je Bauteil
    /// für die Zeile „Körper“ des Steckbriefs; Rechenzeit von Lesen, Bildner und Hülle gemessen. Ohne Datenbank.
    /// </summary>
    public sealed class GebaeudeImportAnsichtAbgeleitetTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _ausgabe;
        private readonly List<string> _dateien = new List<string>();

        public GebaeudeImportAnsichtAbgeleitetTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        [Fact]
        public void Probe42_GbXml_mit_gebildeten_Koerpern_in_der_Ansicht()
        {
            var uhr = Stopwatch.StartNew();
            GebaeudeAbbild abbild;
            using (FileStream s = File.OpenRead(GbxmlImportTests.Probe("gbxml_g5_closedshell.xml")))
                abbild = new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
            (GebaeudeAnsichtDaten d, AbbildGebaeude g) = Ansicht(abbild);
            uhr.Stop();
            _ausgabe.WriteLine("gbXML: Lesen, Bildner, Klassifikation und Hülle " + uhr.Elapsed.TotalMilliseconds.ToString("0.0") + " ms");

            Pruefen(d, g, "raum-a");
            GebaeudeAnsichtKoerperweg wand = d.Koerperwege["aw-a-sued"];
            Assert.True(wand.Abgeleitet);
            Assert.Equal(Koerperbildner.ART_FLAECHENEXTRUSION, wand.Art);
            Assert.Equal(GbxmlKoerper.ART_CLOSEDSHELL, d.Koerperraum("raum-a")!.Dateikoerper!.Art);
        }

        [Fact]
        public void Probe42_Projektdatei_mit_gebildeten_Koerpern_in_der_Ansicht()
        {
            string pfad = SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("k4ansicht"));
            _dateien.Add(pfad);
            var profil = new SqprojImportProfil();
            GebaeudeAbbild abbild;
            using (FileStream f = File.OpenRead(pfad))
                abbild = profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
            (GebaeudeAnsichtDaten d, AbbildGebaeude g) = Ansicht(abbild, "R1");

            Pruefen(d, g, "R1");
            Assert.Equal(Koerperbildner.ART_RAUMPOLYGON, d.Koerperraum("R1")!.Dateikoerper!.Art);
            Assert.True(d.Koerperwege["IW"].Abgeleitet);
            // Ein Raum ohne Raumpolygon (R3) fällt auf das Umrissprisma zurück; die Meldung steht im Protokoll.
            GebaeudeAnsichtKoerperraum r3 = d.Koerperraum("R3");
            Assert.True(r3 == null || r3.Dateikoerper == null);
            Assert.Contains(abbild.Meldungen.Concat(g.Meldungen), m => m.Schluessel == SqprojGeometrie.RAUM_OHNE_POLYGON);
        }

        /// <summary>Abbild → Grundriss → Hülle, das Gebäude des Raums <paramref name="raum"/> (sonst das erste).</summary>
        private static (GebaeudeAnsichtDaten, AbbildGebaeude) Ansicht(GebaeudeAbbild abbild, string raum = null)
        {
            int gi = raum == null ? 0 : abbild.Gebaeude.FindIndex(x => x.Raeume.Any(r => r.Kennung == raum));
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(abbild, gi);
            AbbildGebaeude g = abbild.Gebaeude[gi];
            return (GebaeudeImportAnsicht.AnsichtDaten(zg, true, null, g), g);
        }

        private static void Pruefen(GebaeudeAnsichtDaten d, AbbildGebaeude g, string raum)
        {
            Assert.True(d.HatDateikoerper);
            Assert.True(d.RandbedingungWaehlbar);
            GebaeudeAnsichtKoerperraum k = d.Koerperraum(raum)!;
            Assert.Equal(Koerperherkunft.Abgeleitet, k.Herkunft);
            Assert.Contains(d.Dateiansicht(), x => x.Raum.Kennung == raum && x.Herkunft == Koerperherkunft.Abgeleitet);
            Assert.DoesNotContain(d.Dateiansicht(), x => x.Herkunft == Koerperherkunft.Datei);

            // Jedes Dreieck mit Gruppe; die Dreiecke einer Quellfläche tragen ihr Bauteil — Grundlage von Aufbau und Befund in 3D.
            IReadOnlyList<byte> gruppen = k.Dateikoerper!.Gruppen!;
            Assert.All(gruppen, x => Assert.True(x < GebaeudeAnsichtRandgruppen.ZAHL));
            IReadOnlyList<string> bauteile = k.Dreiecksbauteile!;
            Dateikoerper koerper = g.Raeume.Single(r => r.Kennung == raum).Koerper;
            var kennungen = new HashSet<string>(g.Bauteile.Select(b => b.Kennung), StringComparer.Ordinal);
            for (int t = 0; t < koerper.DreieckZahl; t++)
                if (kennungen.Contains(koerper.Quellflaechen[t])) Assert.Equal(koerper.Quellflaechen[t], bauteile[t]);
            Assert.Contains(bauteile, b => b != null);

            // Mit Stufen und Befunden je Bauteil (wie aus dem Vorschlag) sind Aufbau und Befund wählbar und färben jedes Dreieck
            // einer Quellfläche mit dem Befund ihres Bauteils.
            GebaeudeAnsichtDaten mit = d with
            {
                Bauteilstufen = kennungen.ToDictionary(x => x, _ => Aufbaustufe.A, StringComparer.Ordinal),
                Bauteilbefunde = kennungen.ToDictionary(x => x, _ => Bauteilbefundstufe.OhneEigenschaften, StringComparer.Ordinal),
            };
            Assert.True(mit.AufbauWaehlbar);
            Assert.True(mit.BefundWaehlbar);
            for (int t = 0; t < gruppen.Count; t++)
                if (bauteile[t] != null && gruppen[t] != (byte)Randgruppe.R0)
                    Assert.Equal((byte)Bauteilbefundstufe.OhneEigenschaften, mit.Befundbyte(gruppen[t], bauteile[t]));

            // Der Weg je Bauteil für den Steckbrief: alle Körper aus Flächen gebildet.
            Assert.NotEmpty(d.Koerperwege);
            Assert.All(d.Koerperwege.Values, w => Assert.True(w.Abgeleitet));
        }
    }
}
