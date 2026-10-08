using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die offenen Prüfpunkte des Körperwegs</b> an den Proben aus <see cref="IfcProbenErzeuger.Pruefpunktproben"/>:
    /// <list type="bullet">
    /// <item><b>Geschossplatte mit fremdnamigen Teilen:</b> Deckt der Körper die ganze Platte, ihr eigener Mengensatz nur einen
    /// Teil, und tragen die Teile ohne Darstellung einen anderen Namensstamm, wird der Körper gegen den eigenen Mengensatz und
    /// die eine passende Teilgruppe verglichen — keine falsche Abweichung; die Flächen bleiben die der Mengensätze. Ohne
    /// passende Gruppe bleibt die Warnung (Gegenprobe).</item>
    /// <item><b>Dach mit Mengensatz, Körper nur in den Platten:</b> Fläche aus dem Mengensatz des Dachs, Neigung und Azimut je
    /// Platte aus dem Körper, Körpervergleich ohne Abweichung, Flächen je Raum aus den Körpern ohne Raumgrenzen.</item>
    /// </list>
    /// </summary>
    public sealed class IfcKoerperPruefpunkteTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Pruefpunktproben()[datei]))
                a.Lesen(s, datei, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name)
            => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Single(b => b.Name == name);

        private static void Nah(double erwartet, double? ist, string wo, double toleranz = 1e-6)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(ist.Value - erwartet) <= toleranz, wo + ": erwartet " + erwartet + ", ist " + ist.Value);
        }

        [Fact]
        public void Geschossplatte_mit_fremdnamigen_Teilen_wird_gegen_die_Teilgruppe_verglichen()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.PRUEFPUNKT_FREMDTEILE);

            // Die Flächen bleiben die der Mengensätze: Körperelement 8 m², Teile 40 und 32 m², nichts doppelt.
            AbbildBauteil platte = Bauteil(a, "Platte A1");
            Nah(80.0, platte.Koerperflaeche.FlaecheM2, "Körper Platte A1");
            Nah(8.0, platte.BruttoflaecheM2, "Platte A1");
            Assert.Equal(Flaechenherkunft.Mengensatz, platte.Flaechenherkunft);
            Nah(40.0, Bauteil(a, "Platte B1-1").BruttoflaecheM2, "Platte B1-1");
            Nah(32.0, Bauteil(a, "Platte B1-2").BruttoflaecheM2, "Platte B1-2");

            // Körper 80 m² gegen 8 + 40 + 32 m²: keine Abweichung, eine Platte mit Teilen.
            Assert.DoesNotContain(a.Meldungen, m => m.Werte.Length > 0 && m.Werte[0] == "Platte A1");
            Assert.Equal(new[] { "1" }, Assert.Single(a.Meldungen, m => m.Schluessel == P + "KOERPER_TEILE_DACH").Werte);

            // Gegenprobe: Platte C (Körper 40 m², Mengensatz 4 m²) — die übrige Gruppe „Platte X“ (30 m²) deckt ihn nicht.
            PruefMeldung c = Assert.Single(a.Meldungen, m => m.Schluessel == P + "KOERPER_ABWEICHUNG");
            Assert.Equal(new[] { "Platte C", "4", "40", "900", "25" }, c.Werte);
            Assert.Equal(new[] { "1", "900", "900", "Platte C", "2" },
                         Assert.Single(a.Meldungen, m => m.Schluessel == P + "KOERPER_ABWEICHUNGEN_DACH").Werte);
        }

        [Fact]
        public void Dach_mit_Mengensatz_und_Koerper_nur_in_den_Platten()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.PRUEFPUNKT_SATTEL_MENGE);
            AbbildGebaeude g = a.Abbild.Gebaeude[0];

            // Das Dach ist das Bauteil, seine Platten nicht; Fläche aus dem Mengensatz, Körper aus beiden Platten (2 × 37,5 m²).
            Assert.DoesNotContain(g.Bauteile, b => b.Name == "Dachfläche Süd" || b.Name == "Dachfläche Nord");
            AbbildBauteil dach = Bauteil(a, "Dach");
            Nah(75.0, dach.BruttoflaecheM2, "Dach Mengensatz");
            Assert.Equal(Flaechenherkunft.Mengensatz, dach.Flaechenherkunft);
            Nah(75.0, dach.Koerperflaeche.FlaecheM2, "Dach Körper");
            Assert.Equal(2, dach.Koerperflaeche.Teile.Count);
            Assert.All(dach.Koerperflaeche.Teile, t => Nah(37.5, t.FlaecheM2, "Dachplatte"));
            Nah(DACHNEIGUNG, dach.NeigungGrad, "Dach Neigung");

            // Körpervergleich ohne Abweichung.
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel.StartsWith(P + "KOERPER_ABWEICHUNG", StringComparison.Ordinal));

            // Ohne Raumgrenzen: je Raum und Dachfläche eine Grenze aus den Körpern (Breite × Sparrenlänge 3,375 m).
            Assert.Equal(0, g.ZahlGrenzen);
            foreach ((string raum, double breite) in new[] { ("Wohnen", 5.58), ("Bad", 3.58) })
            {
                string kennung = g.Raeume.Single(r => r.Name == raum).Kennung;
                List<AbbildGrenze> grenzen = dach.Grenzen.Where(x => x.RaumKennung == kennung).ToList();
                Assert.Equal(2, grenzen.Count);
                Assert.All(grenzen, x => Nah(breite * 3.375, x.FlaecheM2, raum + " Dachfläche"));
                Assert.All(grenzen, x => Assert.Equal(Randbedingung.Aussenluft, x.Lage));
            }

            // Der Vorschlag: je Dachfläche eine Zeile mit eigenem Azimut und der Neigung aus dem Körper, zusammen der Mengensatz.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel)));
            List<GebaeudeBauteilzeile> zeilen = v.Zeilen.Where(z => z.Bauteil.Bezeichner == "Dach").ToList();
            Assert.Equal(2, zeilen.Count);
            GebaeudeBauteilzeile sued = Assert.Single(zeilen, z => z.Bauteil.Azimut > 90.0), nord = Assert.Single(zeilen, z => z.Bauteil.Azimut < 90.0);
            Nah(180.0, sued.Bauteil.Azimut, "Dach Süd Azimut");
            Nah(0.0, nord.Bauteil.Azimut, "Dach Nord Azimut");
            Assert.All(zeilen, z => Nah(DACHNEIGUNG, z.Bauteil.Neigung, "Dach Neigung"));
            Assert.All(zeilen, z => Nah(37.5, z.Bauteil.Flaeche, "Dachzeile"));
        }
    }
}
