using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Teilflächen je Richtung und Öffnungen über zwei Räume</b> (Körperweg ohne Raumgrenzen, Zonenweg nach Regel Z3): die
    /// Proben aus <see cref="IfcProbenErzeuger.Teilflaechenproben"/> gegen die Handrechnung — eine gegliederte Außenwand über
    /// Eck führt je Fassade eine Zeile mit eigenem Azimut, ein Satteldach über einem Raum je Dachfläche eine Zeile mit Azimut
    /// und Neigung, ein Fensterband über zwei Räume teilt sich nach seiner Überlappung mit den Raumseiten, und jede Wandzeile
    /// zieht nur ihren Teil ab. Dazu die Gliederung ohne Datei (<see cref="Teilflaechen"/>).
    /// </summary>
    public sealed class TeilflaechenTests
    {
        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            byte[] inhalt = IfcProbenErzeuger.Teilflaechenproben()[datei];
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(inhalt))
                a.Lesen(s, datei, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        /// <summary>Der Vorschlag nach <paramref name="regel"/> (<c>null</c> = die Vorgabe), ohne Fehler, mit <paramref name="zonen"/> Zonen.</summary>
        private static GebaeudeBauteilvorschlag Vorschlag(GebaeudeImportAblauf a, string regel, int zonen)
        {
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, regel);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel + "(" + string.Join(";", m.Werte) + ")")));
            Assert.Equal(zonen, v.Zonen.Count);
            return v;
        }

        /// <summary>Die Zeilen eines Bauteils (im Zonenweg in der Zone des Raums), die größte zuerst.</summary>
        private static List<GebaeudeBauteilzeile> Zeilen(GebaeudeBauteilvorschlag v, string bauteil, string raum = null)
            => v.Zeilen.Where(z => z.Bauteil.Bezeichner == bauteil
                                   && (raum == null || v.Zonierung == null || v.Zonierung.Zonen[z.Zone].Raeume.Any(r => r.Name == raum)))
                       .OrderByDescending(z => z.Bauteil.Flaeche).ToList();

        private static void Nah(double erwartet, double? ist, string wo, double toleranz = 1e-6)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(ist.Value - erwartet) <= toleranz,
                wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void Zeile(GebaeudeBauteilzeile z, double? flaeche, double azimut, double neigung, string wo)
        {
            if (flaeche.HasValue) Nah(flaeche.Value, z.Bauteil.Flaeche, wo + " Fläche");
            Nah(azimut, z.Bauteil.Azimut, wo + " Azimut");
            Nah(neigung, z.Bauteil.Neigung, wo + " Neigung");
        }

        [Fact]
        public void Wand_ueber_Eck_fuehrt_im_Zonenweg_je_Fassade_eine_Zeile_mit_eigenem_Azimut_und_ihren_Fenstern()
        {
            GebaeudeBauteilvorschlag v = Vorschlag(Lesen(IfcProbenErzeuger.TEILFLAECHEN_FLACHBAU), null, 2);
            List<GebaeudeBauteilzeile> wand = Zeilen(v, "Wand Eck", "Wohnen");
            Assert.Equal(2, wand.Count);
            // Nord: 5,0 × 2,5 − 1,5 × 1,2; West: 4,0 × 2,5 − 1,0 × 1,2.
            Zeile(wand[0], 12.5 - 1.8, 0.0, 90.0, "Wand Eck Nord");
            Zeile(wand[1], 10.0 - 1.2, 270.0, 90.0, "Wand Eck West");
            Zeile(Assert.Single(Zeilen(v, "Fenster Nord")), 1.8, 0.0, 90.0, "Fenster Nord");
            Zeile(Assert.Single(Zeilen(v, "Fenster West")), 1.2, 270.0, 90.0, "Fenster West");
        }

        [Fact]
        public void Wand_ueber_Eck_fuehrt_im_Einzonenweg_je_Fassade_eine_Zeile_im_Verhaeltnis_ihrer_Raumseiten()
        {
            GebaeudeBauteilvorschlag v = Vorschlag(Lesen(IfcProbenErzeuger.TEILFLAECHEN_FLACHBAU), IfcImportProfil.ZONENREGEL_Z5, 1);
            List<GebaeudeBauteilzeile> wand = Zeilen(v, "Wand Eck");
            Assert.Equal(2, wand.Count);
            GebaeudeBauteilzeile nord = Assert.Single(wand, z => z.Bauteil.Azimut < 90.0), west = Assert.Single(wand, z => z.Bauteil.Azimut > 180.0);
            Zeile(nord, null, 0.0, 90.0, "Wand Eck Nord");
            Zeile(west, null, 270.0, 90.0, "Wand Eck West");
            // Die Bruttofläche teilt sich wie die Raumseiten 12,5 : 10,0; je Teil zieht nur sein Fenster ab.
            Nah(1.25, (nord.Bauteil.Flaeche + 1.8) / (west.Bauteil.Flaeche + 1.2), "Verhältnis");
            Zeile(Assert.Single(Zeilen(v, "Fenster Nord")), 1.8, 0.0, 90.0, "Fenster Nord");
            Zeile(Assert.Single(Zeilen(v, "Fenster West")), 1.2, 270.0, 90.0, "Fenster West");
        }

        [Fact]
        public void Satteldach_fuehrt_je_Dachflaeche_eine_Zeile_mit_Azimut_und_Neigung()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.TEILFLAECHEN_SATTEL);
            // Die Grenzen je Raum: Breite × Sparrenlänge √(2,7² + 2,025²) = 3,375 m, je Dachfläche.
            AbbildBauteil platte = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Name == "Dachplatte");
            foreach ((string raum, double breite) in new[] { ("Wohnen", 5.58), ("Bad", 3.58) })
            {
                string kennung = a.Abbild.Gebaeude[0].Raeume.Single(r => r.Name == raum).Kennung;
                List<AbbildGrenze> g = platte.Grenzen.Where(x => x.RaumKennung == kennung).ToList();
                Assert.Equal(2, g.Count);
                Assert.All(g, x => Nah(breite * 3.375, x.FlaecheM2, raum + " Dachfläche"));
            }
            GebaeudeBauteilvorschlag v = Vorschlag(a, null, 1);
            List<GebaeudeBauteilzeile> dach = Zeilen(v, "Dachplatte");
            Assert.Equal(2, dach.Count);
            GebaeudeBauteilzeile sued = Assert.Single(dach, z => z.Bauteil.Azimut > 90.0), nord = Assert.Single(dach, z => z.Bauteil.Azimut < 90.0);
            Zeile(sued, null, 180.0, DACHNEIGUNG, "Dach Süd");
            Zeile(nord, null, 0.0, DACHNEIGUNG, "Dach Nord");
            Nah(sued.Bauteil.Flaeche, nord.Bauteil.Flaeche, "gleiche Dachflächen");
        }

        [Fact]
        public void Fensterband_ueber_zwei_Raeume_teilt_sich_nach_der_Ueberlappung()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.TEILFLAECHEN_FLACHBAU);
            // Handrechnung: 4,0 × 2,8 = 11,2 m²; Überlappung mit Wohnen 1,3 × 1,0, mit Bad 2,46 × 1,0, mit Schlafen 4,0 × 1,5 m².
            const double brutto = 11.2, summe = 1.3 + 2.46 + 6.0;
            AbbildBauteil band = a.Abbild.Gebaeude[0].Bauteile.SelectMany(b => b.Oeffnungen).Single(o => o.Name == "Fensterband");
            foreach ((string raum, double teil) in new[] { ("Wohnen", 1.3), ("Bad", 2.46), ("Schlafen", 6.0) })
            {
                string kennung = a.Abbild.Gebaeude[0].Raeume.Single(r => r.Name == raum).Kennung;
                Nah(brutto * teil / summe, Assert.Single(band.Grenzen, g => g.RaumKennung == kennung).FlaecheM2, "Fensterband " + raum);
            }
            Assert.Equal(3, band.Grenzen.Count);

            GebaeudeBauteilvorschlag v = Vorschlag(a, null, 2);
            double eg = brutto * (1.3 + 2.46) / summe, og = brutto * 6.0 / summe;
            Zeile(Assert.Single(Zeilen(v, "Fensterband", "Wohnen")), eg, 180.0, 90.0, "Fensterband EG");
            Zeile(Assert.Single(Zeilen(v, "Fensterband", "Schlafen")), og, 180.0, 90.0, "Fensterband OG");
            // Je Zone zieht die Wand nur ihren Teil ab: EG (5,0 + 4,16) × 2,5, OG 9,4 × 2,5.
            Zeile(Assert.Single(Zeilen(v, "Wand Süd", "Wohnen")), 9.16 * 2.5 - eg, 180.0, 90.0, "Wand Süd EG");
            Zeile(Assert.Single(Zeilen(v, "Wand Süd", "Schlafen")), 9.4 * 2.5 - og, 180.0, 90.0, "Wand Süd OG");
        }

        // ==================================================================
        //  Die Gliederung ohne Datei
        // ==================================================================

        private static AbbildGrenze Grenze(double flaeche, double nx, double ny, double nz)
            => new AbbildGrenze { FlaecheM2 = flaeche, Normale = new[] { nx, ny, nz } };

        [Fact]
        public void Gliederung_fasst_Richtungen_bis_fuenf_Grad_zusammen_und_trennt_die_uebrigen()
        {
            double c = Math.Cos(4.0 * Math.PI / 180.0), s = Math.Sin(4.0 * Math.PI / 180.0);
            var grenzen = new List<AbbildGrenze> { Grenze(10.0, 0, -1, 0), Grenze(5.0, s, -c, 0), Grenze(8.0, -1, 0, 0) };
            List<Teilflaeche> teile = Teilflaechen.Gliedern(grenzen, 0.0);
            Assert.Equal(2, teile.Count);
            Nah(15.0, teile[0].BruttoM2, "Süd");
            Nah(8.0, teile[1].BruttoM2, "West");
            Nah(270.0, teile[1].AzimutGrad, "Azimut West");
            Nah(90.0, teile[1].NeigungGrad, "Neigung West");
            // Mit Drehung 30° gegen Nord: der Azimut aus der Normale dreht mit.
            Nah(240.0, Teilflaechen.Gliedern(grenzen, 30.0)[1].AzimutGrad, "Azimut West gedreht");
            // Eine Richtung allein und eine Grenze ohne Normale sind keine Gliederung.
            Assert.Null(Teilflaechen.Gliedern(grenzen.Take(2).ToList(), 0.0));
            Assert.Null(Teilflaechen.Gliedern(new List<AbbildGrenze> { grenzen[0], new AbbildGrenze { FlaecheM2 = 3.0 } }, 0.0));
        }
    }
}
