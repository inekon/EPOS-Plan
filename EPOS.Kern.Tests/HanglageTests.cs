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
    /// <b>Teilweise eingegrabene Wände</b> (Hanglage, Souterrain) im Körperweg ohne Raumgrenzen: Die Proben aus
    /// <see cref="IfcProbenErzeuger.Hanglageproben"/> gegen die Handrechnung — die Wand eines Raums, dessen Körper die Geländehöhe
    /// schneidet, teilt sich in einen Teil am Erdreich und einen an der Außenluft; das Kellerfenster über Gelände bleibt an der
    /// Außenluft; die Geländehöhe kommt aus der Datei, sonst gilt z = 0 mit Info. Dazu die Teilung ohne Datei
    /// (<see cref="Koerperflaechen.Zuordnen"/>) samt Zeichentoleranz.
    /// </summary>
    public sealed class HanglageTests
    {
        private const string P = IfcImportProfil.MELDUNGSPRAEFIX;

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            byte[] inhalt = IfcProbenErzeuger.Hanglageproben()[datei];
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(inhalt))
                a.Lesen(s, datei, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static AbbildGebaeude Gebaeude(GebaeudeImportAblauf a) => Assert.Single(a.Abbild.Gebaeude);

        private static List<AbbildGrenze> Grenzen(GebaeudeImportAblauf a, string bauteil, string raum)
        {
            AbbildGebaeude g = Gebaeude(a);
            string kennung = g.Raeume.Single(r => r.Name == raum).Kennung;
            return g.Bauteile.Single(b => b.Name == bauteil).Grenzen.Where(x => x.RaumKennung == kennung).ToList();
        }

        private static void Nah(double erwartet, double? ist, string wo, double toleranz = 1e-6)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(ist.Value - erwartet) <= toleranz,
                wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>Die Fläche der Grenzen eines Bauteils an einem Raum mit der Lage <paramref name="lage"/>.</summary>
        private static double Flaeche(GebaeudeImportAblauf a, string bauteil, string raum, Randbedingung lage)
            => Grenzen(a, bauteil, raum).Where(x => x.Lage == lage).Sum(x => x.FlaecheM2 ?? 0.0);

        [Fact]
        public void Wand_des_Untergeschosses_teilt_sich_an_der_Gelaendehoehe_der_Datei()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_HANG);
            // Gelände −0,5 m: Hobby (z −1,5 … 1,0 m) liegt 1,0 m im Erdreich, 1,5 m darüber.
            Nah(9.4 * 1.0, Flaeche(a, "Wand Süd UG", "Hobby", Randbedingung.Erdreich), "Süd Erdreich");
            Nah(9.4 * 1.5, Flaeche(a, "Wand Süd UG", "Hobby", Randbedingung.Aussenluft), "Süd Außenluft");
            Nah(9.4 * 1.0, Flaeche(a, "Wand Nord UG", "Hobby", Randbedingung.Erdreich), "Nord Erdreich");
            Nah(5.4 * 1.0, Flaeche(a, "Wand West UG", "Hobby", Randbedingung.Erdreich), "West Erdreich");
            Nah(5.4 * 1.5, Flaeche(a, "Wand Ost UG", "Hobby", Randbedingung.Aussenluft), "Ost Außenluft");
            Assert.Equal(2, Grenzen(a, "Wand Süd UG", "Hobby").Count);
            // Die Tiefe des eingegrabenen Teils: Geländehöhe minus Unterkante = −0,5 − (−1,5) = 1,0 m.
            AbbildGrenze erde = Grenzen(a, "Wand Süd UG", "Hobby").Single(x => x.Lage == Randbedingung.Erdreich);
            Nah(1.0, erde.UnterGelaendeM, "Tiefe unter Gelände");
            Assert.Null(Grenzen(a, "Wand Süd UG", "Hobby").Single(x => x.Lage == Randbedingung.Aussenluft).UnterGelaendeM);
            // Das Erdgeschoss liegt ganz über Gelände: keine Teilung.
            AbbildGrenze eg = Assert.Single(Grenzen(a, "Wand Süd EG", "Wohnen"));
            Assert.Equal(Randbedingung.Aussenluft, eg.Lage);
            Nah(9.4 * 2.5, eg.FlaecheM2, "Süd EG");
            // Das Kellerfenster liegt über Gelände.
            Assert.All(Gebaeude(a).Bauteile.SelectMany(b => b.Oeffnungen).Single(b => b.Name == "Kellerfenster").Grenzen, x => Assert.Equal(Randbedingung.Aussenluft, x.Lage));
            PruefMeldung m = Assert.Single(Gebaeude(a).Meldungen, x => x.Schluessel == P + "GELAENDE_DATEI");
            Assert.DoesNotContain(Gebaeude(a).Meldungen, x => x.Schluessel == P + "GELAENDE_NULL");
            Assert.Equal("4", m.Werte[2]);
        }

        [Fact]
        public void Zonenvorschlag_fuehrt_je_Wand_eine_Zeile_am_Erdreich_und_eine_an_der_Aussenluft_mit_dem_Kellerfenster()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_HANG);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel)));
            List<GebaeudeBauteilzeile> sued = v.Zeilen.Where(z => z.Bauteil.Bezeichner == "Wand Süd UG").ToList();
            Assert.Equal(2, sued.Count);
            Nah(9.4 * 1.0, sued.Single(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ERDREICH).Bauteil.Flaeche, "Zeile Erdreich");
            Nah(9.4 * 1.5 - 0.8, sued.Single(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_AUSSENLUFT).Bauteil.Flaeche, "Zeile Außenluft ohne Fenster");
            GebaeudeBauteilzeile fenster = Assert.Single(v.Zeilen, z => z.Bauteil.Bezeichner == "Kellerfenster");
            Nah(0.8, fenster.Bauteil.Flaeche, "Kellerfenster");
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, fenster.Bauteil.Randbedingung);
        }

        [Fact]
        public void Einzonenweg_fuehrt_je_Wand_einen_Posten_am_Erdreich_und_einen_an_der_Aussenluft()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_HANG);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel)));
            // Wie im Zonenweg: Süd 1,0 m im Erdreich, 1,5 m darüber mit dem Kellerfenster an der Außenluft.
            List<GebaeudeBauteilzeile> sued = v.Zeilen.Where(z => z.Bauteil.Bezeichner == "Wand Süd UG").ToList();
            Assert.Equal(2, sued.Count);
            GebaeudeBauteilzeile erde = sued.Single(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ERDREICH);
            GebaeudeBauteilzeile luft = sued.Single(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_AUSSENLUFT);
            // Der Einzonenweg trägt die Bruttofläche des Bauteils (Körper); sie teilt sich nach den Flächen der Stücke am Raum
            // (1,0 : 1,5), das Kellerfenster geht vom Teil an der Außenluft ab.
            double brutto = erde.Bauteil.Flaeche + luft.Bauteil.Flaeche + 0.8;
            Nah(brutto * 1.0 / 2.5, erde.Bauteil.Flaeche, "Posten Erdreich");
            Nah(brutto * 1.5 / 2.5 - 0.8, luft.Bauteil.Flaeche, "Posten Außenluft ohne Fenster");
            Assert.Equal(erde.Bauteil.Azimut, luft.Bauteil.Azimut);
            GebaeudeBauteilzeile fenster = Assert.Single(v.Zeilen, z => z.Bauteil.Bezeichner == "Kellerfenster");
            Nah(0.8, fenster.Bauteil.Flaeche, "Kellerfenster");
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, fenster.Bauteil.Randbedingung);
            // Die übrigen Wände des Untergeschosses: je ein Posten am Erdreich und einer an der Außenluft.
            foreach (string w in new[] { "Wand Nord UG", "Wand West UG", "Wand Ost UG" })
            {
                List<GebaeudeBauteilzeile> z = v.Zeilen.Where(x => x.Bauteil.Bezeichner == w).ToList();
                Assert.Equal(2, z.Count);
                Assert.Single(z, x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ERDREICH);
                Assert.Single(z, x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_AUSSENLUFT);
            }
            // Das Erdgeschoss liegt ganz über Gelände: ein Posten an der Außenluft.
            GebaeudeBauteilzeile eg = Assert.Single(v.Zeilen, x => x.Bauteil.Bezeichner == "Wand Süd EG");
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, eg.Bauteil.Randbedingung);
        }

        [Fact]
        public void Gliederung_am_Gelaende_teilt_nur_Stuecke_des_Koerperwegs()
        {
            AbbildGrenze Stueck(Randbedingung lage, double flaeche, double? tiefe, Grenzherkunft herkunft) => new AbbildGrenze
            {
                Kennung = lage + "|" + flaeche, RaumKennung = "R", Lage = lage, FlaecheM2 = flaeche, Normale = new[] { 0.0, -1.0, 0.0 },
                UnterGelaendeM = tiefe, Herkunft = herkunft,
            };
            List<Teilflaeche> teile = Teilflaechen.GliedernAmGelaende(new[]
            {
                Stueck(Randbedingung.Erdreich, 10.0, 1.0, Grenzherkunft.Bauteilkoerper),
                Stueck(Randbedingung.Aussenluft, 15.0, null, Grenzherkunft.Bauteilkoerper),
            }, 0.0);
            Assert.Equal(2, teile.Count);
            Assert.Equal(Randbedingung.Erdreich, teile[0].Rand);
            Assert.Equal(10.0, teile[0].BruttoM2, 9);
            Assert.Equal(Randbedingung.Aussenluft, teile[1].Rand);
            Assert.Equal(180.0, teile[1].AzimutGrad.Value, 6);
            // Raumgrenzen der Datei teilen sich nicht am Gelände; eine Richtung allein ist keine Gliederung.
            Assert.Null(Teilflaechen.GliedernAmGelaende(new[]
            {
                Stueck(Randbedingung.Erdreich, 10.0, 1.0, Grenzherkunft.Raumgrenze),
                Stueck(Randbedingung.Aussenluft, 15.0, null, Grenzherkunft.Raumgrenze),
            }, 0.0));
        }

        [Fact]
        public void Ohne_Gelaendehoehe_in_der_Datei_gilt_z_null_mit_Info()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_OHNE);
            // z = 0: Hobby liegt 1,5 m im Erdreich, 1,0 m darüber.
            Nah(9.4 * 1.5, Flaeche(a, "Wand Süd UG", "Hobby", Randbedingung.Erdreich), "Süd Erdreich");
            Nah(9.4 * 1.0, Flaeche(a, "Wand Süd UG", "Hobby", Randbedingung.Aussenluft), "Süd Außenluft");
            Nah(1.5, Grenzen(a, "Wand Süd UG", "Hobby").Single(x => x.Lage == Randbedingung.Erdreich).UnterGelaendeM, "Tiefe unter Gelände");
            Assert.All(Gebaeude(a).Bauteile.SelectMany(b => b.Oeffnungen).Single(b => b.Name == "Kellerfenster").Grenzen, x => Assert.Equal(Randbedingung.Aussenluft, x.Lage));
            Assert.Single(Gebaeude(a).Meldungen, x => x.Schluessel == P + "GELAENDE_NULL");
            Assert.DoesNotContain(Gebaeude(a).Meldungen, x => x.Schluessel == P + "GELAENDE_DATEI");
        }

        [Fact]
        public void Wand_ganz_unter_oder_ganz_ueber_Gelaende_wird_nicht_geteilt()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_TIEF);
            // Gelände +1,2 m: Hobby (bis 1,0 m) ganz darunter, Wohnen (ab 1,3 m) ganz darüber.
            AbbildGrenze ug = Assert.Single(Grenzen(a, "Wand Süd UG", "Hobby"));
            Assert.Equal(Randbedingung.Erdreich, ug.Lage);
            Nah(9.4 * 2.5, ug.FlaecheM2, "Süd UG");
            Assert.Null(ug.UnterGelaendeM);
            AbbildGrenze eg = Assert.Single(Grenzen(a, "Wand Süd EG", "Wohnen"));
            Assert.Equal(Randbedingung.Aussenluft, eg.Lage);
            Nah(9.4 * 2.5, eg.FlaecheM2, "Süd EG");
            Assert.Equal("0", Assert.Single(Gebaeude(a).Meldungen, x => x.Schluessel == P + "GELAENDE_DATEI").Werte[2]);
        }

        [Fact]
        public void Die_Geschosslage_steht_am_Raum()
        {
            GebaeudeImportAblauf a = Lesen(IfcProbenErzeuger.HANGLAGE_HANG);
            Nah(-1.5, Gebaeude(a).Raeume.Single(r => r.Name == "Hobby").GeschossLageM, "Hobby");
            Nah(1.3, Gebaeude(a).Raeume.Single(r => r.Name == "Wohnen").GeschossLageM, "Wohnen");
        }

        // ==================================================================
        //  Ohne Datei
        // ==================================================================

        private static Dateikoerper Kasten(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            var punkte = new List<double[]>
            {
                new[] { x0, y0, z0 }, new[] { x1, y0, z0 }, new[] { x1, y1, z0 }, new[] { x0, y1, z0 },
                new[] { x0, y0, z1 }, new[] { x1, y0, z1 }, new[] { x1, y1, z1 }, new[] { x0, y1, z1 },
            };
            var dreiecke = new List<int[]>
            {
                new[] { 0, 2, 1 }, new[] { 0, 3, 2 }, new[] { 4, 5, 6 }, new[] { 4, 6, 7 },
                new[] { 0, 1, 5 }, new[] { 0, 5, 4 }, new[] { 1, 2, 6 }, new[] { 1, 6, 5 },
                new[] { 2, 3, 7 }, new[] { 2, 7, 6 }, new[] { 3, 0, 4 }, new[] { 3, 4, 7 },
            };
            return new Dateikoerper { PunkteM = punkte, Dreiecke = dreiecke };
        }

        [Theory]
        [InlineData(-0.5, 2.0 * 1.0, 2.0 * 3.0)]   // geteilt: 1,0 m unter, 3,0 m über Gelände
        [InlineData(-1.44, 0.0, 2.0 * 4.0)]        // 0,06 von 4,0 m = 1,5 % unter Gelände: keine Teilung
        [InlineData(2.44, 2.0 * 4.0, 0.0)]         // 0,06 von 4,0 m = 1,5 % über Gelände: keine Teilung
        public void Teilung_ohne_Datei_mit_Zeichentoleranz(double gelaende, double erdreich, double luft)
        {
            // Wand x 0 … 0,3 m, Raum x 0,3 … 4 m, y 1 … 3 m, z −1,5 … 2,5 m.
            var raum = new Koerperflaechenraum { Koerper = Kasten(0.3, 1, -1.5, 4, 3, 2.5) };
            List<Koerperflaechenstueck> s = Koerperflaechen.Zuordnen(new[] { raum }, new[] { Kasten(0, 0, -1.5, 0.3, 5, 2.5) }, 0.3,
                                                                    innen: false, gelaendeM: gelaende).Stuecke;
            Nah(erdreich, s.Where(x => x.Lage == Randbedingung.Erdreich).Sum(x => x.FlaecheM2), "Erdreich");
            Nah(luft, s.Where(x => x.Lage == Randbedingung.Aussenluft).Sum(x => x.FlaecheM2), "Außenluft");
            Assert.Equal(erdreich > 0.0 && luft > 0.0 ? 2 : 1, s.Count);
            // Ohne Geländehöhe teilt nichts.
            Assert.Single(Koerperflaechen.Zuordnen(new[] { raum }, new[] { Kasten(0, 0, -1.5, 0.3, 5, 2.5) }, 0.3, innen: false).Stuecke);
        }
    }
}
