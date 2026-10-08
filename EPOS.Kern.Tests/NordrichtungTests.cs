using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>G5-N — Nordrichtung, Umrechnung (N1)</b> ohne Datei und Datenbank: „Planoberseite zeigt nach α“ ↔ Nordwinkel
    /// (360° − α) mod 360°, an den Rändern und rückwärts; Drehung eines Azimuts; Schnellwahl.
    /// </summary>
    public sealed class NordrichtungUmrechnungTests
    {
        [Fact]
        public void Planoberseite_und_Nordwinkel_rechnen_in_beide_Richtungen_auch_an_den_Raendern()
        {
            var faelle = new (double Planoberseite, double Nordwinkel)[]
            {
                (0.0, 0.0), (90.0, 270.0), (180.0, 180.0), (270.0, 90.0), (359.9, 0.1), (360.0, 0.0), (-90.0, 90.0), (450.0, 270.0), (0.1, 359.9),
            };
            foreach ((double p, double n) in faelle)
            {
                Assert.Equal(n, Nordrichtung.NordwinkelAusPlanoberseite(p).Value, 9);
                double zurueck = Nordrichtung.PlanoberseiteAusNordwinkel(n).Value;
                Assert.Equal(Nordrichtung.Normiert(p).Value, zurueck, 9);
                Assert.InRange(Nordrichtung.NordwinkelAusPlanoberseite(p).Value, 0.0, 359.999999999);
            }
            Assert.Null(Nordrichtung.NordwinkelAusPlanoberseite(null));
            Assert.Null(Nordrichtung.NordwinkelAusPlanoberseite(double.NaN));
            Assert.Null(Nordrichtung.NordwinkelAusPlanoberseite(double.PositiveInfinity));
            Assert.Null(Nordrichtung.PlanoberseiteAusNordwinkel(null));
        }

        [Fact]
        public void Ein_Azimut_dreht_um_den_Unterschied_und_NULL_bleibt_NULL()
        {
            // Nordwinkel 0 → 270 (Planoberseite nach Ost): Süd (180) wird West (270), Nord (0) wird Ost (90).
            Assert.Equal(270.0, Nordrichtung.Gedreht(180.0, 0.0, 270.0).Value, 9);
            Assert.Equal(90.0, Nordrichtung.Gedreht(0.0, 0.0, 270.0).Value, 9);
            Assert.Equal(180.0, Nordrichtung.Gedreht(270.0, 270.0, 0.0).Value, 9);   // zurück
            Assert.Equal(0.0, Nordrichtung.Gedreht(350.0, 30.0, 20.0).Value, 9);     // über 360 hinweg
            Assert.Null(Nordrichtung.Gedreht(null, 0.0, 90.0));
        }

        [Fact]
        public void Die_Schnellwahl_kennt_acht_Richtungen_im_Uhrzeigersinn()
        {
            Assert.Equal(new[] { "N", "NO", "O", "SO", "S", "SW", "W", "NW" }, Nordrichtung.Schnellwahl.Select(s => s.Kuerzel));
            Assert.Equal(new[] { 0.0, 45, 90, 135, 180, 225, 270, 315 }, Nordrichtung.Schnellwahl.Select(s => s.PlanoberseiteGrad));
            Assert.Equal("O", Nordrichtung.KuerzelZu(450.0));
            Assert.Equal("N", Nordrichtung.KuerzelZu(360.0));
            Assert.Null(Nordrichtung.KuerzelZu(10.0));
            Assert.Null(Nordrichtung.KuerzelZu(null));
        }
    }

    /// <summary>
    /// <b>G5-N — Import mit Vorgabe (N2–N4)</b> an den Importproben: Die Vorgabe dreht alle Azimute genau einmal — Abbild
    /// (Raumgrenzen, Mengensätze, Körper G5-1, Öffnungen G5-2, gbXML) und Bauteilvorschlag —, ersetzt den Dateiwert, und
    /// ohne Dateiwert und Vorgabe gilt die Annahme mit Meldung. Das Neu-Lesen aus dem Puffer gleicht einem frischen Lauf.
    /// </summary>
    public sealed class NordrichtungImportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static string Probe(string name) => Path.Combine(IfcProbenTests.Ordner(), name);

        private static GebaeudeImportAblauf Lesen(byte[] daten, string name, double? vorgabeNordwinkel)
        {
            var a = new GebaeudeImportAblauf { NordwinkelVorgabeGrad = vorgabeNordwinkel };
            using (var s = new MemoryStream(daten))
                a.Lesen(s, name, GebaeudeImportProfil.FuerDatei(name));
            Assert.True(a.Abbild != null, name + ": " + string.Join(" | ", a.Meldungen.Select(m => m.ToString())));
            return a;
        }

        private static GebaeudeImportAblauf Lesen(string name, double? vorgabeNordwinkel = null)
            => Lesen(File.ReadAllBytes(Probe(name)), name, vorgabeNordwinkel);

        /// <summary>Das Probenhaus ohne <c>TrueNorth</c> — derselbe Eingriff wie in <c>IfcImportTests</c>.</summary>
        private static byte[] HausOhneNord()
        {
            string text = File.ReadAllText(Probe("ifc4_haus.ifc"));
            string ohne = Regex.Replace(text, @"(IFCGEOMETRICREPRESENTATIONCONTEXT\([^;]*),#\d+\);", "$1,$);");
            Assert.NotEqual(text, ohne);
            return Encoding.ASCII.GetBytes(ohne);
        }

        /// <summary>Alle Azimute des Abbilds — Bauteile samt Öffnungen je Kennung, in Dateireihenfolge.</summary>
        private static List<(string Kennung, double? Azimut)> Azimute(GebaeudeAbbild abbild)
        {
            var liste = new List<(string, double?)>();
            void Sammeln(AbbildBauteil b, string vor)
            {
                liste.Add((vor + b.Kennung, b.AzimutGrad));
                foreach (AbbildBauteil o in b.Oeffnungen) Sammeln(o, vor + b.Kennung + "/");
            }
            foreach (AbbildGebaeude g in abbild.Gebaeude)
                foreach (AbbildBauteil b in g.Bauteile) Sammeln(b, "");
            foreach (AbbildBauteil b in abbild.BauteileOhneGebaeude) Sammeln(b, "-");
            return liste;
        }

        /// <summary>
        /// Prüft „genau einmal gedreht“: jeder Azimut von <paramref name="mit"/> ist der von <paramref name="ohne"/> minus den
        /// Unterschied der wirksamen Nordwinkel — im Abbild und in den Zeilen des Bauteilvorschlags.
        /// </summary>
        private static int GenauEinmalGedreht(GebaeudeImportAblauf ohne, GebaeudeImportAblauf mit)
        {
            double alt = ohne.Abbild.NordwinkelWirksamGrad ?? 0.0, neu = mit.Abbild.NordwinkelWirksamGrad ?? 0.0;
            List<(string Kennung, double? Azimut)> a = Azimute(ohne.Abbild), b = Azimute(mit.Abbild);
            Assert.Equal(a.Select(x => x.Kennung), b.Select(x => x.Kennung));
            int gedreht = 0;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Azimut.HasValue, b[i].Azimut.HasValue);
                if (!a[i].Azimut.HasValue) continue;
                Assert.True(Math.Abs(Winkelabstand(Nordrichtung.Gedreht(a[i].Azimut, alt, neu).Value, b[i].Azimut.Value)) < 1e-6,
                            a[i].Kennung + ": " + a[i].Azimut + " → " + b[i].Azimut);
                gedreht++;
            }

            GebaeudeBauteilvorschlag va = GebaeudeBauteilvorschlag.Bilden(ohne, 0, 'E'), vb = GebaeudeBauteilvorschlag.Bilden(mit, 0, 'E');
            Assert.Equal(va.Zeilen.Count, vb.Zeilen.Count);
            for (int i = 0; i < va.Zeilen.Count; i++)
            {
                double? x = va.Zeilen[i].Bauteil.Azimut, y = vb.Zeilen[i].Bauteil.Azimut;
                Assert.Equal(x.HasValue, y.HasValue);
                if (x.HasValue)
                    Assert.True(Math.Abs(Winkelabstand(Nordrichtung.Gedreht(x, alt, neu).Value, y.Value)) < 1e-6,
                                va.Zeilen[i].Bauteil.Bezeichner + ": " + x + " → " + y);
            }
            return gedreht;
        }

        private static double Winkelabstand(double a, double b)
        {
            double d = (a - b) % 360.0;
            if (d > 180.0) d -= 360.0;
            if (d < -180.0) d += 360.0;
            return d;
        }

        [Fact]
        public void Ohne_Nordrichtung_gilt_die_Annahme_mit_Meldung_und_die_Vorgabe_dreht_Sued_nach_West()
        {
            byte[] daten = HausOhneNord();
            GebaeudeImportAblauf ohne = Lesen(daten, "ohne_norden.ifc", null);
            Assert.Null(ohne.Abbild.NordwinkelGrad);
            Assert.Null(ohne.Abbild.NordwinkelWirksamGrad);
            Assert.Equal(Nordwinkelherkunft.Annahme, ohne.Abbild.NordwinkelHerkunft);
            Assert.Null(ohne.Quelle.NordwinkelGrad);
            Assert.Equal(Nordwinkelherkunft.Annahme, ohne.Quelle.NordwinkelHerkunft);
            Assert.Contains(ohne.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_KEIN_NORDEN" && m.Stufe == PruefStufe.Warnung);
            Assert.Contains("Ausrichtung", WindowsFormsApplication1.MyResource.Resource.IMP_IFC_PROT_KEIN_NORDEN);   // N3: die Annahme ist änderbar
            Assert.Contains("Planoberseite", WindowsFormsApplication1.MyResource.Resource.IMP_IFC_PROT_KEIN_NORDEN);

            // Planoberseite = Ost → Nordwinkel 270°: eine Wand nach Modell-Süd zeigt nach West.
            double vorgabe = Nordrichtung.NordwinkelAusPlanoberseite(90.0).Value;
            GebaeudeImportAblauf mit = Lesen(daten, "ohne_norden.ifc", vorgabe);
            Assert.Equal(270.0, mit.Abbild.NordwinkelVorgabeGrad.Value, 9);
            Assert.Equal(Nordwinkelherkunft.Eingabe, mit.Abbild.NordwinkelHerkunft);
            Assert.Equal(270.0, mit.Quelle.NordwinkelGrad.Value, 9);
            Assert.Equal(Nordwinkelherkunft.Eingabe, mit.Quelle.NordwinkelHerkunft);
            Assert.DoesNotContain(mit.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_KEIN_NORDEN");
            PruefMeldung info = Assert.Single(mit.Meldungen, m => m.Schluessel == GebaeudeImportAblauf.MELDUNG + "NORD_VORGABE");
            Assert.Equal(new[] { "90", "270" }, info.Werte);

            List<(string Kennung, double? Azimut)> a = Azimute(ohne.Abbild), b = Azimute(mit.Abbild);
            int sued = a.FindIndex(x => x.Azimut is double w && Math.Abs(w - 180.0) < 1e-6);
            Assert.True(sued >= 0, "Das Probenhaus ohne Nordrichtung hat eine Wand nach Süd.");
            Assert.Equal(270.0, b[sued].Azimut.Value, 6);
            Assert.True(GenauEinmalGedreht(ohne, mit) >= 4);
        }

        [Fact]
        public void Der_Dateiwert_ist_Vorgabe_und_eine_Eingabe_ueberschreibt_ihn()
        {
            foreach (string probe in new[] { "ifc4_haus.ifc", "ifc4_mapconversion.ifc", "ifc4_g5_wand_extrusion.ifc", "ifc4_g5_oeffnungen.ifc" })
            {
                GebaeudeImportAblauf datei = Lesen(probe);
                Assert.True(datei.Abbild.NordwinkelGrad.HasValue, probe);
                Assert.Equal(Nordwinkelherkunft.Datei, datei.Abbild.NordwinkelHerkunft);
                Assert.Equal(datei.Abbild.NordwinkelGrad, datei.Quelle.NordwinkelGrad);

                // Eingabe Planoberseite = Nord (0°) ersetzt den Dateiwert; die Neigungen bleiben.
                GebaeudeImportAblauf mit = Lesen(probe, 0.0);
                Assert.Equal(0.0, mit.Abbild.NordwinkelWirksamGrad.Value, 9);
                Assert.Equal(datei.Abbild.NordwinkelGrad, mit.Abbild.NordwinkelGrad);           // der Dateiwert bleibt als Angabe
                Assert.Equal(Nordwinkelherkunft.Eingabe, mit.Quelle.NordwinkelHerkunft);
                Assert.DoesNotContain(mit.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_NORDDREHUNG");
                Assert.True(GenauEinmalGedreht(datei, mit) > 0, probe);
                Assert.Equal(datei.Abbild.Gebaeude[0].Bauteile.Select(x => x.NeigungGrad), mit.Abbild.Gebaeude[0].Bauteile.Select(x => x.NeigungGrad));
            }
        }

        [Fact]
        public void Beim_gbXML_Weg_dreht_erst_die_Eingabe_und_die_Warnung_der_Datei_entfaellt()
        {
            GebaeudeImportAblauf datei = Lesen("gbxml_norddrehung.xml");
            Assert.Equal(60.0, datei.Abbild.NordwinkelGrad.Value, 9);
            Assert.Null(datei.Abbild.NordwinkelWirksamGrad);                      // nie still angewandt
            Assert.Equal(Nordwinkelherkunft.Annahme, datei.Abbild.NordwinkelHerkunft);
            Assert.Contains(datei.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_NORDDREHUNG");

            GebaeudeImportAblauf mit = Lesen("gbxml_norddrehung.xml", 60.0);
            Assert.Equal(Nordwinkelherkunft.Eingabe, mit.Abbild.NordwinkelHerkunft);
            Assert.DoesNotContain(mit.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_NORDDREHUNG" || m.Schluessel == "IMP_GBXML_PROT_KEIN_NORDEN");
            Assert.Equal(60.0, mit.Quelle.NordwinkelGrad.Value, 9);
            Assert.True(GenauEinmalGedreht(datei, mit) >= 3);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(mit, 0, 'E');
            Assert.Equal(120.0, BauteilvorschlagProbe.Zeile(v, "aw-sued").Bauteil.Azimut.Value, 6);   // 180 − 60
            Assert.True(v.NordwinkelAngewandt);

            // Grundriss und Ansicht folgen derselben Drehung.
            Umrisseingang e = GebaeudeGrundriss.Eingang(mit.Abbild, 0);
            Assert.True(e.NordwinkelAngewandt);
            Assert.Equal(60.0, e.NordwinkelGrad.Value, 9);

            GebaeudeImportAblauf ohneNord = Lesen("gbxml_haus_si.xml");
            Assert.Null(ohneNord.Abbild.NordwinkelGrad);
            Assert.Contains(ohneNord.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_KEIN_NORDEN");
        }

        [Fact]
        public void Neu_lesen_aus_dem_Puffer_gleicht_einem_frischen_Lauf_mit_der_Vorgabe()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen.ifc");
            string hash = a.Quelle.Hash;
            Assert.True(a.NordwinkelVorgeben(Nordrichtung.NordwinkelAusPlanoberseite(135.0)) > 0);
            GebaeudeImportAblauf frisch = Lesen("ifc4_g5_oeffnungen.ifc", Nordrichtung.NordwinkelAusPlanoberseite(135.0));
            Assert.Equal(hash, a.Quelle.Hash);
            Assert.Equal(225.0, a.Abbild.NordwinkelWirksamGrad.Value, 9);
            Assert.Equal(Azimute(frisch.Abbild), Azimute(a.Abbild));
            Assert.Equal(GebaeudeGrundriss.Eingang(frisch.Abbild, 0).NordwinkelGrad, GebaeudeGrundriss.Eingang(a.Abbild, 0).NordwinkelGrad);

            // Zurück auf den Dateiwert.
            Assert.True(a.NordwinkelVorgeben(null) > 0);
            Assert.Equal(Nordwinkelherkunft.Datei, a.Abbild.NordwinkelHerkunft);
            Assert.Equal(Azimute(Lesen("ifc4_g5_oeffnungen.ifc").Abbild), Azimute(a.Abbild));
            Assert.Throws<InvalidOperationException>(() => new GebaeudeImportAblauf().NordwinkelVorgeben(0.0));
        }

        [Fact]
        public void Datei_erneut_lesen_uebernimmt_den_eingegebenen_Winkel()
        {
            byte[] daten = HausOhneNord();
            var quelle = new ImportquelleModel
            {
                ID = 1, Format = GebaeudeQuelle.FORMAT_IFC, Dateiname = "ohne_norden.ifc",
                Hash = Convert.ToHexStringLower(SHA256.HashData(daten)), NordwinkelGrad = 270.0,
                NordwinkelHerkunft = Nordwinkelherkunft.Eingabe,
            };
            NeulesenErgebnis e;
            using (var s = new MemoryStream(daten)) e = GebaeudeNeulesen.Lesen(s, "ohne_norden.ifc", quelle, "", false);
            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            Assert.Equal(270.0, e.Abbild.NordwinkelWirksamGrad.Value, 9);
            Assert.True(GebaeudeImportCtrl.NordwinkelGleich(quelle.NordwinkelGrad, e.Abbild.NordwinkelWirksamGrad));
            Assert.Equal(Azimute(Lesen(daten, "ohne_norden.ifc", 270.0).Abbild), Azimute(e.Abbild));

            // Ohne gespeicherten Winkel bleibt es bei Datei bzw. Annahme.
            quelle.NordwinkelGrad = null;
            using (var s = new MemoryStream(daten)) e = GebaeudeNeulesen.Lesen(s, "ohne_norden.ifc", quelle, "", false);
            Assert.Null(e.Abbild.NordwinkelWirksamGrad);
        }

        /// <summary>
        /// G5-N (N6, Schritt 199): <b>Neulesen nach den drei Herkünften.</b> Nur ein eingegebener Winkel ersetzt den Dateiwert; ein
        /// gespeicherter Dateiwert weicht dem frisch gelesenen, eine Annahme dem Dateiwert, falls die Datei einen nennt, sonst
        /// bleibt die Annahme.
        /// </summary>
        [Theory]
        [InlineData("ifc4_g5_oeffnungen.ifc", Nordwinkelherkunft.Eingabe, 10.0, 10.0)]
        [InlineData("ifc4_g5_oeffnungen.ifc", Nordwinkelherkunft.Datei, 10.0, null)]       // null = der frische Dateiwert
        [InlineData("ifc4_g5_oeffnungen.ifc", Nordwinkelherkunft.Annahme, null, null)]
        [InlineData("ohne_norden.ifc", Nordwinkelherkunft.Datei, 270.0, null)]
        [InlineData("ohne_norden.ifc", Nordwinkelherkunft.Annahme, null, null)]
        [InlineData("ohne_norden.ifc", Nordwinkelherkunft.Eingabe, 90.0, 90.0)]
        public void Neulesen_folgt_der_Herkunft_des_gespeicherten_Winkels(string name, Nordwinkelherkunft herkunft, double? gespeichert,
                                                                          double? erwartet)
        {
            byte[] daten = name == "ohne_norden.ifc" ? HausOhneNord() : File.ReadAllBytes(Probe(name));
            double? frisch = Lesen(daten, name, null).Abbild.NordwinkelWirksamGrad;
            var quelle = new ImportquelleModel
            {
                ID = 1, Format = GebaeudeQuelle.FORMAT_IFC, Dateiname = name, Hash = Convert.ToHexStringLower(SHA256.HashData(daten)),
                NordwinkelGrad = gespeichert, NordwinkelHerkunft = herkunft,
            };
            Assert.Equal(herkunft == Nordwinkelherkunft.Eingabe ? gespeichert : null, GebaeudeNeulesen.GespeicherterWinkelGilt(quelle));
            NeulesenErgebnis e;
            using (var s = new MemoryStream(daten)) e = GebaeudeNeulesen.Lesen(s, name, quelle, "", false);
            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            double? soll = erwartet ?? frisch;
            Assert.True(GebaeudeImportCtrl.NordwinkelGleich(soll, e.Abbild.NordwinkelWirksamGrad),
                        name + "/" + herkunft + ": " + soll + " ≠ " + e.Abbild.NordwinkelWirksamGrad);
            Assert.Equal(Azimute(Lesen(daten, name, erwartet).Abbild), Azimute(e.Abbild));
            if (name != "ohne_norden.ifc") Assert.NotNull(frisch);                  // die Probe nennt ihren Nordwinkel
            else Assert.Null(frisch);
        }
    }

    /// <summary>
    /// <b>G5-N — nachträglich drehen (N5/N6)</b> gegen die Arbeitskopie der Testdatenbank: alle Bauteile des Gebäudes
    /// (auch von Hand angelegte) um den Unterschied, Azimut NULL bleibt, Neigung bleibt, Quelle und Grundrisse folgen, eine
    /// Transaktion; ohne Quelle benannt abgelehnt; die Herkunftsspalte wird genutzt, sobald sie steht; dazu die Hülle.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AusrichtungAendernTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            GebaeudeImportCtrl.AusrichtungPruefhaken = null;
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int GEBAEUDE = 10614;              // Projekt 1007, ohne Zonen in der Testdatenbank

        /// <summary>Zwei Zonen mit Bauteilen aller Arten der Drehung; liefert die Zonen-IDs.</summary>
        private static (int Wohnen, int Keller) ZonenAnlegen()
        {
            var wohnen = new ZoneModel
            {
                ID = -1, Bezeichner = "Wohnen", Nutzflaeche = 120,
                Bauteile =
                {
                    new BauteilModel { ID = -1, Bezeichner = "Wand Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 20, Neigung = 90, Azimut = 180 },
                    new BauteilModel { ID = -2, Bezeichner = "Fenster Ost", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 4, Neigung = 90, Azimut = 90, U_Wert = 1.1, g_Wert = 0.6 },
                    new BauteilModel { ID = -3, Bezeichner = "Dach geneigt", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 40, Neigung = 35, Azimut = 350 },
                    new BauteilModel { ID = -4, Bezeichner = "Dach flach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 30, Neigung = 0 },
                }
            };
            var keller = new ZoneModel
            {
                ID = -2, Bezeichner = "Keller", IstBeheizt = false, Nutzflaeche = 60,
                Bauteile = { new BauteilModel { ID = -5, Bezeichner = "Tür Nord", Bauteilart = DbWerte.BAUTEILART_TUER, Flaeche = 2, Neigung = 90, Azimut = 0 } }
            };
            GebaeudeZonenCtrl.Ergebnis z = new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { wohnen, keller });
            Assert.True(z.Ok, z.Meldung);
            return (wohnen.ID, keller.ID);
        }

        private static Dictionary<string, (double? Azimut, double? Neigung)> Bauteile()
        {
            var d = new Dictionary<string, (double?, double?)>();
            foreach (System.Data.DataRow r in DataRepository.GetDataTable(
                "SELECT b.\"Bezeichner\", b.\"Azimut\", b.\"Neigung\" FROM \"Tab_Bauteil\" b INNER JOIN \"Tab_Zone\" z ON z.\"ID\" = b.\"ID_Zone\" " +
                "WHERE z.\"ID_Gebaeude\" = ?", new DbParam("@g", GEBAEUDE)).Rows)
                d[Convert.ToString(r[0], CultureInfo.InvariantCulture)] = (BaustoffCtrl.ZahlAus(r, "Azimut"), BaustoffCtrl.ZahlAus(r, "Neigung"));
            return d;
        }

        /// <summary>Schreibt eine Quelle mit Nordwinkel und einem Grundriss an das Gebäude.</summary>
        private static int QuelleSchreiben(double? nordwinkel, int wohnen)
        {
            GebaeudeQuelle q = new GebaeudeQuelle("IFC", "haus.ifc", new string('b', 64), 4711, "IFC4", "2026-10-07T10:00:00+02:00", "1.0", "X4", 0)
            {
                NordwinkelGrad = nordwinkel,
                NordwinkelHerkunft = nordwinkel.HasValue ? Nordwinkelherkunft.Datei : Nordwinkelherkunft.Annahme,
            };
            Koerpergrundriss k = Koerpergrundriss.Ableiten(KoerpergrundrissTests.Koerper(KoerpergrundrissTests.Quader(0, 0, 0, 10, 12, 3)));
            var grundriss = Raumgrundriss.Bilden("space-wohnen", "Wohnen", "EG", 0.0, 120, 360, k.Herleitung.Value, k.Ringe, k.BodenM, k.HoeheM, k.Vermerke, false);
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, q, new List<GebaeudeQuellzuordnung>
            {
                new GebaeudeQuellzuordnung("IfcBuilding", "bldg-1", ImportZiel.Gebaeude),
                new GebaeudeQuellzuordnung("IfcSpace", "space-wohnen", ImportZiel.Zone, wohnen),
            }, null, new[] { grundriss });
            Assert.True(e.Ok, e.Meldung);
            return e.IdImportquelle;
        }

        [Fact]
        public void Alle_Bauteile_drehen_um_den_Unterschied_Quelle_und_Grundriss_folgen()
        {
            if (!_db.Vorhanden) return;
            (int wohnen, _) = ZonenAnlegen();
            int quelle = QuelleSchreiben(30.0, wohnen);
            var ctrl = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.Ausrichtung vorher = ctrl.LesenAusrichtung(GEBAEUDE);
            Assert.Equal(quelle, vorher.IdImportquelle);
            Assert.Equal(30.0, vorher.NordwinkelGrad.Value, 9);
            Assert.Equal(Nordwinkelherkunft.Datei, vorher.Herkunft);                // N6: geschrieben als DATEI
            Assert.Equal(330.0, vorher.PlanoberseiteGrad, 9);
            Assert.Equal(30.0, Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE)).DrehungGrad.Value, 9);
            Dictionary<string, (double? Azimut, double? Neigung)> alt = Bauteile();

            // Planoberseite nach Ost: Nordwinkel 270, Unterschied 240.
            GebaeudeImportCtrl.Ausrichtungsergebnis e = ctrl.AusrichtungAendern(GEBAEUDE, 90.0);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(4, e.GedrehteBauteile);
            Assert.Equal(270.0, e.NordwinkelGrad, 9);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.GEB_AUSRICHTUNG_GEDREHT, 4, "90"), e.Meldung);

            Dictionary<string, (double? Azimut, double? Neigung)> neu = Bauteile();
            Assert.Equal(300.0, neu["Wand Süd"].Azimut.Value, 9);       // 180 − 240
            Assert.Equal(210.0, neu["Fenster Ost"].Azimut.Value, 9);
            Assert.Equal(110.0, neu["Dach geneigt"].Azimut.Value, 9);
            Assert.Equal(120.0, neu["Tür Nord"].Azimut.Value, 9);
            Assert.Null(neu["Dach flach"].Azimut);
            foreach (string k in alt.Keys) Assert.Equal(alt[k].Neigung, neu[k].Neigung);

            GebaeudeImportCtrl.Ausrichtung nachher = ctrl.LesenAusrichtung(GEBAEUDE);
            Assert.Equal(270.0, nachher.NordwinkelGrad.Value, 9);
            Assert.Equal(90.0, nachher.PlanoberseiteGrad, 9);
            Assert.Equal(270.0, ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad.Value, 9);
            Raumgrundriss g = Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE));
            Assert.Equal(270.0, g.DrehungGrad.Value, 9);
            Assert.Equal("0,0;10000,0;10000,12000;0,12000", g.RingeText);            // Modellkoordinaten bleiben

            // Zurück auf Nord: dieselben Azimute wie zu Beginn, über die Annahme 0 gerechnet.
            Assert.True(ctrl.AusrichtungAendern(GEBAEUDE, 330.0).Ok);
            Dictionary<string, (double? Azimut, double? Neigung)> zurueck = Bauteile();
            foreach (string k in alt.Keys)
                Assert.True(alt[k].Azimut.HasValue ? Math.Abs(alt[k].Azimut.Value - zurueck[k].Azimut.Value) < 1e-9 : !zurueck[k].Azimut.HasValue, k);
        }

        [Fact]
        public void Ein_Abbruch_laesst_nichts_halb_gedreht_und_ohne_Quelle_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeImportCtrl();
            (int wohnen, _) = ZonenAnlegen();
            GebaeudeImportCtrl.Ausrichtungsergebnis ohne = ctrl.AusrichtungAendern(GEBAEUDE, 90.0);
            Assert.False(ohne.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_AUSRICHTUNG_KEINE_QUELLE, ohne.Meldung);
            Assert.Null(ctrl.LesenAusrichtung(GEBAEUDE));
            Assert.False(ctrl.AusrichtungAendern(GEBAEUDE, double.NaN).Ok);

            QuelleSchreiben(null, wohnen);
            Assert.Equal(Nordwinkelherkunft.Annahme, ctrl.LesenAusrichtung(GEBAEUDE).Herkunft);
            Dictionary<string, (double? Azimut, double? Neigung)> alt = Bauteile();
            GebaeudeImportCtrl.AusrichtungPruefhaken = () => throw new InvalidOperationException("Abbruchprobe");
            GebaeudeImportCtrl.Ausrichtungsergebnis e = ctrl.AusrichtungAendern(GEBAEUDE, 90.0);
            GebaeudeImportCtrl.AusrichtungPruefhaken = null;
            Assert.False(e.Ok);
            Assert.Contains("Abbruchprobe", e.Meldung);
            Assert.Equal(alt, Bauteile());
            Assert.Null(ctrl.LesenAusrichtung(GEBAEUDE).NordwinkelGrad);
            Assert.True(Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE)).NordwinkelUnbekannt);
        }

        [Fact]
        public void Steht_die_Herkunftsspalte_wird_sie_geschrieben_und_gelesen()
        {
            if (!_db.Vorhanden) return;
            Assert.True(GebaeudeImportCtrl.NordherkunftVorhanden());       // Schritt 199 steht in der Testdatenbank
            var ctrl = new GebaeudeImportCtrl();
            (int wohnen, _) = ZonenAnlegen();
            QuelleSchreiben(null, wohnen);
            Assert.Equal(Nordwinkelherkunft.Annahme, ctrl.LesenAusrichtung(GEBAEUDE).Herkunft);
            Assert.Equal("ANNAHME", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"" + GebaeudeImportCtrl.SPALTE_NORDWINKEL_HERKUNFT + "\" FROM \"Tab_Importquelle\" WHERE \"ID_Gebaeude\" = ?",
                new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture));

            Assert.True(ctrl.AusrichtungAendern(GEBAEUDE, 0.0).Ok);
            GebaeudeImportCtrl.Ausrichtung a = ctrl.LesenAusrichtung(GEBAEUDE);
            Assert.Equal(0.0, a.NordwinkelGrad.Value, 9);
            Assert.Equal(Nordwinkelherkunft.Eingabe, a.Herkunft);
            Assert.Equal(Nordwinkelherkunft.Eingabe, ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelHerkunft);
        }

        [Fact]
        public void Die_Huelle_des_Gebaeudedialogs_nennt_Wert_Herkunft_und_gedrehte_Bauteile()
        {
            if (!_db.Vorhanden) return;
            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungDaten keine = GebaeudeKatalogHuelle.Ausrichtung(GEBAEUDE);
            Assert.False(keine.Aenderbar);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_AUSRICHTUNG_KEINE_QUELLE, keine.Hinweis);
            Assert.Equal(8, keine.Schnellwahl.Count);
            Assert.Equal("Südost", keine.Schnellwahl[3].Name);

            (int wohnen, _) = ZonenAnlegen();
            QuelleSchreiben(null, wohnen);
            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungDaten d = GebaeudeKatalogHuelle.Ausrichtung(GEBAEUDE);
            Assert.True(d.Aenderbar);
            Assert.Equal("ANNAHME", d.Herkunft);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_AUSRICHTUNG_HERKUNFT_ANNAHME, d.HerkunftText);
            Assert.Equal(0.0, d.PlanoberseiteGrad, 9);
            Assert.Equal("Ausrichtung", d.Titel);
            Assert.Equal("Planoberseite zeigt nach", d.Beschriftung);
            Assert.Equal(0, keine.Bauteile);
            Assert.Equal(4, d.Bauteile);                                            // die Rückfrage nennt die Bauteile mit Azimut

            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungErgebnis e = GebaeudeKatalogHuelle.AusrichtungAendern(GEBAEUDE, 180.0);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(4, e.GedrehteBauteile);
            Assert.Equal(e.GedrehteBauteile, d.Bauteile);
            Assert.Equal(180.0, e.PlanoberseiteGrad, 9);
            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungDaten nach = GebaeudeKatalogHuelle.Ausrichtung(GEBAEUDE);
            Assert.Equal(180.0, nach.PlanoberseiteGrad, 9);
            // Schritt 199: die Herkunft steht an der Quelle — nach der Änderung zeigt die Anzeige „eingegeben“.
            Assert.Equal("EINGABE", nach.Herkunft);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_AUSRICHTUNG_HERKUNFT_EINGABE, nach.HerkunftText);
            Assert.Equal(0.0, Bauteile()["Wand Süd"].Azimut.Value, 9);              // Planoberseite Süd: Modell-Süd zeigt nach Nord
        }
    }
}
