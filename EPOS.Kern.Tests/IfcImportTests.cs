using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der IFC-Import, Stufe G4a Welle 1</b> (Umsetzungskonzept 3.2–3.7): Leser und gemeinsame
    /// Zuordnung gegen die selbst erzeugten Proben unter <c>Referenzlaeufe/Importproben/ifc*</c>
    /// (<see cref="IfcProbenErzeuger"/>, Quellenvermerk in <c>LIESMICH_Importproben.md</c>), dazu die
    /// Unit-Tests ohne Datei aus 3.7 und die Quelltextwache.
    ///
    /// <para><b>Das Probenhaus</b> (<c>ifc4_haus.ifc</c>, gleiche Zahlen in <c>ifc2x3_haus.ifc</c>): vier
    /// beheizte Räume (130 m², 325 m³; Höhen 2,6 m im EG und 2,4 m im OG, flächengewichtet 2,5 m) über
    /// einem unbeheizten Keller; Außenwände brutto 201,6 m², Fenster 20 m², Haustür 2 m² (U14 → netto
    /// 179,6 m²); das Modell ist um TrueNorth [−2, 1, 0] = 296,57° gedreht, sodass Modell-Ost nach Süd
    /// (153,43°), Modell-Nord nach Ost (63,43°), Modell-Süd nach West (243,43°) und Modell-West nach Nord
    /// (333,43°) zeigt; Fenster Süd 10, Ost 5, West 4, Nord 1 m². Längen in mm, Flächen in m² ohne Prefix.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche.</b> Die Texttests nehmen die Kulturvorrichtung.</para>
    /// </summary>
    public sealed class IfcImportTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string G = "IMP_GEB_PROT_";
        private const double Genau = 1e-9;

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Zugang
        // ==================================================================

        private static string Probe(string name) => Path.Combine(IfcProbenTests.Ordner(), name);

        private static GebaeudeImportAblauf Lesen(string name, GebaeudeImportProfil profil = null)
        {
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(Probe(name)))
                a.Lesen(s, Probe(name), profil ?? new IfcImportProfil());
            return a;
        }

        private static GebaeudeImportAblauf LesenBytes(byte[] daten, string name, GebaeudeImportProfil profil = null)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(daten))
                a.Lesen(s, name, profil ?? new IfcImportProfil());
            return a;
        }

        private static GebaeudeImportSatz Satz(string name, char? klasse = null)
        {
            GebaeudeImportAblauf a = Lesen(name);
            Assert.True(a.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a.Zuordnen(0, klasse);
        }

        private static double? Wert(GebaeudeImportSatz s, string feld) => s.Zeile(feld).Wert;

        private static bool Hat(IEnumerable<PruefMeldung> meldungen, string schluessel, PruefStufe? stufe = null)
            => meldungen.Any(m => m.Schluessel == schluessel && (!stufe.HasValue || m.Stufe == stufe.Value));

        private static void Nah(double erwartet, double? ist, double toleranz = Genau)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= toleranz, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        private static AbbildBauteil Bauteil(GebaeudeAbbild a, string name)
            => a.Gebaeude.SelectMany(g => g.Bauteile).Single(b => b.Name == name);

        /// <summary>Die Drehung des Probenhauses: der Modellazimut von TrueNorth [−2, 1].</summary>
        private static readonly double Drehung = IfcPlatzierung.Normieren(Math.Atan2(-2.0, 1.0) * 180.0 / Math.PI);

        // ==================================================================
        //  Das Probenhaus (IFC4)
        // ==================================================================

        [Fact]
        public void Probenhaus_IFC4_liest_Schema_Gebaeude_Raeume_Raumgrenzen_und_Einheiten()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifc");
            Assert.False(Hat(a.Meldungen, P + "LESEFEHLER"), string.Join(" | ", a.Meldungen));
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);

            Assert.Equal(IfcSchemaStand.Ifc4, abbild.SchemaStand);
            Assert.Equal("IFC4", a.Quelle.Schemastand);
            Assert.Equal(GebaeudeQuelle.FORMAT_IFC, a.Quelle.Format);
            Assert.Equal("ifc4_haus.ifc", a.Quelle.Dateiname);
            Assert.Equal(new FileInfo(Probe("ifc4_haus.ifc")).Length, a.Quelle.Groesse);
            Assert.Matches("^[0-9a-f]{64}$", a.Quelle.Hash);
            Assert.Equal(0, a.Quelle.FehlendeEntitaeten);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, a.Quelle.Zonenregel);

            // Ein Gebäude, fünf Räume, der Keller nach seinem Namen unbeheizt.
            Assert.Equal(new[] { "Probenhaus" }, a.Gebaeude);
            AbbildGebaeude g = abbild.Gebaeude[0];
            Assert.Equal(5, g.Raeume.Count);
            AbbildRaum keller = g.Raeume.Single(r => r.Name == "Keller");
            Assert.False(keller.Beheizt);
            Assert.Equal(BeheiztQuelle.Name, keller.BeheiztQuelle);
            Assert.Equal(4, g.Raeume.Count(r => r.Beheizt));
            Assert.Equal("IfcBuilding", g.Quelltyp);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, g.Zonenvorschlag);   // drei Geschosse tragen Räume
            Assert.Equal(1965, g.Baujahr);
            Assert.True(Hat(g.Meldungen, P + "BAUJAHR_TEXT", PruefStufe.Info));

            // Einheiten: MILLI METRE und SQUARE_METRE ohne Prefix — die Fläche NICHT quadriert (10⁻⁶).
            Assert.Equal(0.001, abbild.LaengenFaktorNachMeter, 12);
            Assert.Equal(1.0, abbild.FlaechenFaktorNachM2, 12);
            Assert.Equal(1.0, abbild.VolumenFaktorNachM3, 12);
            Nah(40.0, g.Raeume.Single(r => r.Name == "Wohnen").FlaecheM2);
            Nah(2.6, g.Raeume.Single(r => r.Name == "Wohnen").HoeheM);   // 2600 mm

            // Raumgrenzen: alle 28 als BASISKLASSE mit Name '2ndLevel' — über den Namen gefunden, nicht über den Typ.
            Assert.Equal(28, abbild.ZahlRaumgrenzen);
            Assert.Equal(28, abbild.ZahlRaumgrenzenZweiteEbene);
            Assert.Equal(28, abbild.ZahlRaumgrenzenNachName);

            // Nordrichtung: TrueNorth gedreht, keine Koordinatenumrechnung.
            Nah(Drehung, abbild.TrueNorthGrad, 1e-9);
            Assert.False(abbild.MapConversionVorhanden);
            Nah(Drehung, abbild.NordwinkelGrad, 1e-9);
            Assert.True(Hat(a.Meldungen, P + "NORDDREHUNG", PruefStufe.Info));
            Assert.False(Hat(a.Meldungen, P + "KEIN_NORDEN"));
            Assert.False(Hat(a.Meldungen, P + "ENTITAETEN_VERLOREN"));
            Assert.DoesNotContain(a.Meldungen, m => m.Stufe == PruefStufe.Fehler);

            // Zähler der Importprobe.
            Assert.Equal(4, abbild.ZahlSollwertsaetze);
            Assert.True(abbild.ZahlUWerte >= 14, "U-Werte gelesen: " + abbild.ZahlUWerte);
        }

        [Fact]
        public void Probenhaus_Azimut_mit_TrueNorth_und_U_Wert_Vorkommnis_vor_Typ()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifc");
            GebaeudeAbbild abbild = a.Abbild;

            // Modell-Ost → Süd, Modell-Nord → Ost, Modell-Süd → West, Modell-West → Nord.
            Nah(IfcPlatzierung.Normieren(90.0 - Drehung), Bauteil(abbild, "EG Ost").AzimutGrad, 1e-9);
            Nah(IfcPlatzierung.Normieren(0.0 - Drehung), Bauteil(abbild, "EG Nord").AzimutGrad, 1e-9);
            Nah(IfcPlatzierung.Normieren(180.0 - Drehung), Bauteil(abbild, "EG Süd").AzimutGrad, 1e-9);
            Nah(IfcPlatzierung.Normieren(270.0 - Drehung), Bauteil(abbild, "OG West").AzimutGrad, 1e-9);
            Assert.Equal(2, GebaeudeAggregation.Sektor(Bauteil(abbild, "EG Ost").AzimutGrad.Value));   // Süd

            // Das Vorkommnis (0,25) schlägt den Typ (0,4); ohne Vorkommniswert gilt der Typ.
            AbbildBauteil ogOst = Bauteil(abbild, "OG Ost");
            Nah(0.25, ogOst.UWertWm2K);
            Assert.Equal("Pset_WallCommon", ogOst.UWertQuelle);
            AbbildBauteil egOst = Bauteil(abbild, "EG Ost");
            Nah(0.4, egOst.UWertWm2K);
            Assert.Equal("Pset_WallCommon (Typ)", egOst.UWertQuelle);
            Assert.Equal(Randbedingung.Aussenluft, egOst.Randbedingung);   // IsExternal am Typ
            Assert.Equal(Bauteilart.Aussenwand, egOst.Art);

            // Länge × Höhe in mm als Rückfall der Bruttofläche (10 m × 2,8 m).
            Nah(28.0, Bauteil(abbild, "OG Süd").BruttoflaecheM2);
            // Die Haustür über OverallWidth × OverallHeight (1000 mm × 2000 mm).
            Nah(2.0, Bauteil(abbild, "EG West").Oeffnungen.Single().BruttoflaecheM2);
            Assert.Equal(Bauteilart.Tuer, Bauteil(abbild, "EG West").Oeffnungen.Single().Art);
            // Die Kellerdecke: beheizter Raum oben, Keller unten.
            AbbildBauteil decke = Bauteil(abbild, "Kellerdecke");
            Assert.Equal(2, decke.Nachbarn.Count);
            Assert.Equal(GebaeudeAggregation.SICHT_BODEN, decke.Nachbarn[0].Sicht);
        }

        [Fact]
        public void Probenhaus_IFC4_liefert_die_Zielfelder_des_Einzonenwegs()
        {
            GebaeudeImportSatz s = Satz("ifc4_haus.ifc");

            Nah(130.0, Wert(s, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(2.5, Wert(s, GebaeudeZielfelder.RAUMHOEHE));
            Nah(325.0, Wert(s, GebaeudeZielfelder.VOLUMEN));
            Assert.Equal("GIMP_BELEG_RAUMHOEHE_GEWICHTET", s.Zeile(GebaeudeZielfelder.RAUMHOEHE).Beleg.Schluessel);

            // Außenwand: 201,6 m² brutto − 20 m² Fenster − 2 m² Haustür = 179,6 m² (U14).
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Nah(179.6, wand.Wert, 1e-9);
            Nah(201.6, wand.Bruttowert, 1e-9);
            Nah((39.8 * 0.25 + 139.8 * 0.4) / 179.6, Wert(s, GebaeudeZielfelder.U_AUSSENWAND), 1e-9);

            Nah(10.0, Wert(s, GebaeudeZielfelder.FENSTER_SUED));
            Nah(5.0, Wert(s, GebaeudeZielfelder.FENSTER_OST));
            Nah(4.0, Wert(s, GebaeudeZielfelder.FENSTER_WEST));
            Nah(1.0, Wert(s, GebaeudeZielfelder.FENSTER_NORD));
            Nah(20.0, Wert(s, GebaeudeZielfelder.FENSTER_GESAMT));
            Nah(1.1, Wert(s, GebaeudeZielfelder.U_FENSTER));
            Nah(0.6, Wert(s, GebaeudeZielfelder.G_WERT));

            Nah(80.0, Wert(s, GebaeudeZielfelder.FLAECHE_DACH));
            Nah(0.2, Wert(s, GebaeudeZielfelder.U_DACH));
            Nah(80.0, Wert(s, GebaeudeZielfelder.FLAECHE_GRUND));   // Kellerdecke, nicht die Bodenplatte
            Nah(0.35, Wert(s, GebaeudeZielfelder.U_GRUND));
            Assert.Equal(DbWerte.GRUND_KELLER, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
            Nah(2.0, Wert(s, GebaeudeZielfelder.FLAECHE_SONSTIGE));
            Nah(1.8, Wert(s, GebaeudeZielfelder.U_SONSTIGE));
            Nah(20.0, Wert(s, GebaeudeZielfelder.SOLL_TAG));   // SetPointValue des Bereichs

            // Baualtersklasse aus dem Baujahr „ca. 1965" → D (1958–1968), Herkunft IFC.
            GebaeudeFeldzeile klasse = s.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE);
            Assert.Equal("D", klasse.Textwert);
            Assert.Equal(Importherkunft.Ifc, klasse.Herkunft);
            Assert.Equal("GIMP_BELEG_KLASSE_BAUJAHR", klasse.Beleg.Schluessel);
            Assert.Equal(new[] { "1965", "ca. 1965" }, klasse.Beleg.Werte);
            Assert.Equal('D', s.Baualtersklasse);
            Assert.Equal(1965, s.Baujahr);

            // Herkunft je Zeile (3.7): Nutzfläche, Raumhöhe, Außenwand und U-Werte aus IFC; Wärmebrücken
            // und Luftwechsel Vorgabe; Anschlusslängen leer.
            foreach (string f in new[] { GebaeudeZielfelder.NUTZFLAECHE, GebaeudeZielfelder.RAUMHOEHE,
                                         GebaeudeZielfelder.FLAECHE_AUSSENWAND, GebaeudeZielfelder.U_AUSSENWAND,
                                         GebaeudeZielfelder.U_FENSTER, GebaeudeZielfelder.U_DACH, GebaeudeZielfelder.U_GRUND,
                                         GebaeudeZielfelder.U_SONSTIGE })
                Assert.True(s.Zeile(f).Herkunft == Importherkunft.Ifc, f + ": " + s.Zeile(f).Herkunft);
            foreach (string f in new[] { GebaeudeZielfelder.PSI_FENSTER_WAND, GebaeudeZielfelder.PSI_WAND_DACH,
                                         GebaeudeZielfelder.PSI_AUSSENWAND_KELLER, GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION,
                                         GebaeudeZielfelder.LUFTWECHSEL_NUTZER })
                Assert.True(s.Zeile(f).Herkunft == Importherkunft.Vorgabe, f + ": " + s.Zeile(f).Herkunft);
            Nah(0.3, Wert(s, GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION));
            Nah(0.4, Wert(s, GebaeudeZielfelder.LUFTWECHSEL_NUTZER));
            Nah(0.110, Wert(s, GebaeudeZielfelder.PSI_FENSTER_WAND));   // Vorgabe der Klasse D
            foreach (string f in new[] { GebaeudeZielfelder.LAENGE_FENSTER_WAND, GebaeudeZielfelder.LAENGE_WAND_DACH,
                                         GebaeudeZielfelder.LAENGE_AUSSENWAND_KELLER })
                Assert.Equal(Importherkunft.Leer, s.Zeile(f).Herkunft);

            // Quellzuordnungen tragen die IFC-Typen.
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "IfcBuilding");
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "IfcSpace");
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "IfcWindow");
            Assert.Empty(GebaeudeImportAblauf.Pruefen(s).Where(m => m.Stufe == PruefStufe.Fehler));
        }

        [Fact]
        public void Probenhaus_IFC2X3_liefert_dieselben_Zahlen()
        {
            GebaeudeImportAblauf a = Lesen("ifc2x3_haus.ifc");
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);
            Assert.Equal(IfcSchemaStand.Ifc2x3, abbild.SchemaStand);
            Assert.Equal("IFC2X3", a.Quelle.Schemastand);
            Assert.Equal(28, abbild.ZahlRaumgrenzenNachName);

            GebaeudeImportSatz vier = Satz("ifc4_haus.ifc");
            GebaeudeImportSatz zwei = a.Zuordnen(0, null);
            foreach (GebaeudeFeldzeile z4 in vier.Zeilen)
            {
                GebaeudeFeldzeile z2 = zwei.Zeile(z4.Zielfeld);
                Assert.True(z4.Herkunft == z2.Herkunft, z4.Zielfeld + ": " + z4.Herkunft + " / " + z2.Herkunft);
                Assert.True(z4.Textwert == z2.Textwert, z4.Zielfeld + ": " + z4.Textwert + " / " + z2.Textwert);
                if (z4.Wert.HasValue) Nah(z4.Wert.Value, z2.Wert, 1e-9);
                else Assert.Null(z2.Wert);
            }
        }

        [Fact]
        public void IfcXml_liefert_dieselben_Zahlen_wie_STEP()
        {
            // Die ifcXML-Fassung entsteht zur Laufzeit aus der STEP-Probe (xBIM schreibt ifcXML für IFC4).
            byte[] xml;
            using (var m = new MemoryModel(MemoryModel.GetFactory(XbimSchemaVersion.Ifc4), NullLoggerFactory.Instance, 0))
            {
                using (FileStream s = File.OpenRead(Probe("ifc4_haus.ifc")))
                    m.LoadStep21(s, s.Length, null, null);
                var ziel = new MemoryStream();
                m.SaveAsXml(ziel, new System.Xml.XmlWriterSettings { Indent = false }, null, null, null);
                xml = ziel.ToArray();
            }
            GebaeudeImportAblauf a = LesenBytes(xml, "ifc4_haus.ifcxml");
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);
            Assert.Equal(IfcLeser.INHALT_XML, abbild.Inhaltsart);
            Assert.Equal(IfcSchemaStand.Ifc4, abbild.SchemaStand);

            GebaeudeImportSatz step = Satz("ifc4_haus.ifc");
            GebaeudeImportSatz ausXml = a.Zuordnen(0, null);
            foreach (string f in new[] { GebaeudeZielfelder.NUTZFLAECHE, GebaeudeZielfelder.RAUMHOEHE, GebaeudeZielfelder.FLAECHE_AUSSENWAND,
                                         GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.FENSTER_SUED, GebaeudeZielfelder.FENSTER_OST,
                                         GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.SOLL_TAG })
                Nah(step.Zeile(f).Wert.Value, ausXml.Zeile(f).Wert, 1e-9);
        }

        // ==================================================================
        //  Behälter, Schema, Fehlerbilder
        // ==================================================================

        [Fact]
        public void Ifczip_wird_gegen_die_entpackte_Groesse_geprueft()
        {
            long zip = new FileInfo(Probe("ifc4_haus.ifczip")).Length;
            long entpackt = new FileInfo(Probe("ifc4_haus.ifc")).Length;
            Assert.True(zip < entpackt);

            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifczip");
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);
            Assert.Equal("ifc4_haus.ifc", abbild.Behaeltereintrag);
            Assert.Equal(entpackt, abbild.EntpackteGroesse);
            Nah(130.0, a.Zuordnen(0, null).Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);

            // Grenze zwischen Behälter- und entpackter Größe: benannt abgelehnt, ohne zu entpacken.
            GebaeudeImportAblauf b = Lesen("ifc4_haus.ifczip", new IfcImportProfil(zip + 100));
            Assert.Empty(b.Gebaeude);
            PruefMeldung m = Assert.Single(b.Meldungen);
            Assert.Equal(P + "ZU_GROSS_ENTPACKT", m.Schluessel);
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Equal(entpackt.ToString(), m.Werte[0]);
        }

        [Fact]
        public void Behaelter_ohne_IFC_Eintrag_und_unlesbarer_Behaelter_sind_eigene_Ablehnungen()
        {
            byte[] ohne = IfcProbenErzeuger.Zip("notiz.txt", Encoding.ASCII.GetBytes("keine IFC-Datei"));
            GebaeudeImportAblauf a = LesenBytes(ohne, "probe.ifczip");
            Assert.Equal(P + "BEHAELTER_OHNE_IFC", Assert.Single(a.Meldungen).Schluessel);

            byte[] kaputt = new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5, 6, 7, 8 };
            GebaeudeImportAblauf b = LesenBytes(kaputt, "kaputt.ifczip");
            Assert.Equal(P + "BEHAELTER_UNLESBAR", Assert.Single(b.Meldungen).Schluessel);

            GebaeudeImportAblauf c = LesenBytes(Encoding.ASCII.GetBytes("Hallo"), "text.ifc");
            Assert.Equal(P + "FORMAT_UNBEKANNT", Assert.Single(c.Meldungen).Schluessel);
        }

        [Fact]
        public void Schema_IFC4X1_wird_benannt_abgelehnt()
        {
            GebaeudeImportAblauf a = Lesen("ifc4x1_kopf.ifc");
            Assert.Empty(a.Gebaeude);
            PruefMeldung m = Assert.Single(a.Meldungen);
            Assert.Equal(P + "SCHEMA_UNBEKANNT", m.Schluessel);
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Equal("IFC4X1", m.Werte[0]);
        }

        [Fact]
        public void Zwei_Gebaeude_eines_je_Lauf_U13()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_zwei_gebaeude.ifc");
            Assert.Equal(new[] { "Gebäude A", "Gebäude B" }, a.Gebaeude);

            GebaeudeImportSatz s0 = a.Zuordnen(0, 'E');
            Nah(50.0, Wert(s0, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(5.0, Wert(s0, GebaeudeZielfelder.FENSTER_SUED));   // TrueNorth [0, 1]: Modell-Süd ist Süd
            Nah(25.0, Wert(s0, GebaeudeZielfelder.FLAECHE_AUSSENWAND));

            GebaeudeImportSatz s1 = a.Zuordnen(1, 'E');
            Nah(80.0, Wert(s1, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(8.0, Wert(s1, GebaeudeZielfelder.FENSTER_SUED));
            Nah(22.0, Wert(s1, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Assert.Equal("Gebäude B", s1.Gebaeudename);
        }

        [Fact]
        public void Ohne_Mengen_wird_gemeldet_und_nicht_aus_Geometrie_abgeleitet()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_ohne_mengen.ifc");
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "KEINE_MENGEN");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal("4", m.Werte[0]);
            Assert.True(Hat(a.Abbild.Gebaeude[0].Meldungen, P + "KEINE_RAEUME", PruefStufe.Warnung));

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Assert.Null(Wert(s, GebaeudeZielfelder.NUTZFLAECHE));
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Assert.Equal(0.0, wand.Wert);
            Assert.Equal(PruefStufe.Warnung, wand.Markierung);
            Assert.Contains(GebaeudeImportAblauf.Pruefen(s), x => x.Schluessel == G + "PFLICHT_FEHLT");
        }

        [Fact]
        public void MapConversion_dreht_ohne_TrueNorth_zu_addieren()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_mapconversion.ifc");
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);
            Assert.True(abbild.MapConversionVorhanden);
            Nah(90.0, abbild.MapConversionGrad, 1e-9);
            Nah(Drehung, abbild.TrueNorthGrad, 1e-9);
            Nah(90.0, abbild.NordwinkelGrad, 1e-9);
            Assert.True(Hat(a.Meldungen, P + "MAPCONVERSION", PruefStufe.Info));

            // Modell-Nord (0°) − 90° = 270° West — mit TrueNorth wäre es Ost (63,43°).
            Nah(270.0, Bauteil(abbild, "EG Nord").AzimutGrad, 1e-9);
            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Nah(4.0, Wert(s, GebaeudeZielfelder.FENSTER_WEST));
            Nah(0.0, Wert(s, GebaeudeZielfelder.FENSTER_OST));
        }

        [Fact]
        public void Ohne_TrueNorth_wird_die_Schemavorgabe_gemeldet()
        {
            string text = File.ReadAllText(Probe("ifc4_ohne_mengen.ifc"));
            string ohneNord = Regex.Replace(text, @"(IFCGEOMETRICREPRESENTATIONCONTEXT\([^;]*),#\d+\);", "$1,$);");
            Assert.NotEqual(text, ohneNord);
            GebaeudeImportAblauf a = LesenBytes(Encoding.ASCII.GetBytes(ohneNord), "ohne_norden.ifc");
            Assert.True(Hat(a.Meldungen, P + "KEIN_NORDEN", PruefStufe.Warnung));
            Assert.Null(a.Abbild.NordwinkelGrad);
        }

        [Fact]
        public void Entitaetenverlust_wird_in_beiden_Kanaelen_gezaehlt()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_verlust.ifc");
            var abbild = Assert.IsType<IfcGebaeudeAbbild>(a.Abbild);
            Assert.Equal(1, abbild.VerlusteNichtAngelegt);   // #40 IFCWALLXYZ: unbekannter Typ
            Assert.Equal(2, abbild.VerlusteVerweise);        // #40 aus der Enthaltensrelation, #999 aus #50
            Assert.Equal(3, abbild.FehlendeEntitaeten);
            Assert.Equal(3, a.Quelle.FehlendeEntitaeten);
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "ENTITAETEN_VERLOREN");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "3", "1", "2" }, m.Werte.Take(3));
            Assert.Equal(new[] { "Probengebaeude" }, a.Gebaeude);   // der Rest wird gelesen
        }

        // ==================================================================
        //  Quelltextwache
        // ==================================================================

        [Fact]
        public void Quelltextwache_kein_Typuebergehen_kein_IfcStore_kein_Esent_keine_globale_Senke()
        {
            var funde = new List<string>();
            foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.Kern"), "*.cs", SearchOption.AllDirectories))
            {
                if (datei.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    || datei.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
                string text = Code(datei);
                foreach (string muster in new[] { @"\bignoreTypes\b", @"\bSkipTypes\b", @"\bIfcStore\b", @"\bXbim\.Ifc\.",
                                                   @"\bXbim\.IO\.Esent\b", @"\bEsentModel\b", @"\bXbimServices\b",
                                                   @"\bAddXbimToolkit\b", @"\bOpenReadStep21\b" })
                    if (Regex.IsMatch(text, muster)) funde.Add(Path.GetFileName(datei) + ": " + muster);
            }
            Assert.True(funde.Count == 0, "Verbotene xBIM-Wege im Kern (Datenaustauschkonzept 4, ADR-003):\n" + string.Join("\n", funde));

            // Der Leser arbeitet über die IIfc*-Schnittstellen, nicht über schemagebundene Klassen.
            foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Import", "Ifc"), "*.cs"))
                Assert.False(Regex.IsMatch(Code(datei), @"\bXbim\.Ifc(2x3|4x3|4)\.(?!Interfaces\b)[A-Z]"),
                    Path.GetFileName(datei) + " nennt eine schemagebundene xBIM-Klasse.");
        }

        [Fact]
        public void Jeder_IFC_Schluessel_steht_in_beiden_Sprachen()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            var schluessel = new List<string>
            {
                // Die dreizehn aus Befund N 4.5 und die vier aus Umsetzungskonzept 3.2.
                "ZU_GROSS", "LESEFEHLER", "SCHEMA_UNBEKANNT", "KEIN_GEBAEUDE", "KEINE_RAEUME", "KEINE_MENGEN",
                "KEIN_UWERT", "KEIN_NORDEN", "MAPCONVERSION", "EINHEIT", "MEHRSCHALIG", "BAUJAHR_TEXT", "GELESEN",
                "SEITE_UNBESTIMMT", "PLATZIERUNGSART", "EIGENSCHAFTSART", "FLAECHENART_GEMISCHT",
                // Die, die der gemeinsame Ablauf und die Zuordnung mit dem Präfix des Formats bilden.
                "TRENNFLAECHE_UNGLEICH", "NETTOFLAECHE_NEGATIV", "ZU_VIELE_ZONEN",
            };
            foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Import", "Ifc"), "*.cs"))
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), "P \\+ \"([A-Z_]+)\""))
                    schluessel.Add(m.Groups[1].Value);
            var funde = new List<string>();
            foreach (string k in schluessel.Distinct().Select(n => P + n))
            {
                if (!de.TryGetValue(k, out string d) || d.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.resx");
                if (!en.TryGetValue(k, out string e) || e.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.en-US.resx");
            }
            Assert.True(schluessel.Distinct().Count() >= 30, "Nur " + schluessel.Distinct().Count() + " Schlüssel gesammelt.");
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        [Fact]
        public void Meldungstexte_und_Schemaanzeige_kommen_aus_dem_Katalog()
        {
            string text = GebaeudeZuordnungsModell.MeldungText(new PruefMeldung(PruefStufe.Fehler, P + "ZU_GROSS_ENTPACKT", "300", "100"));
            Assert.Contains("entpackt 300 Byte", text);
            Assert.Equal("IFC-Schema IFC4", GebaeudeZuordnungsModell.SchemaText(new IfcImportProfil(), "IFC4"));
            Assert.Equal("Ifc", Importherkunft.Ifc.ToString());
        }

        // ==================================================================
        //  Die vierzehn Unit-Tests ohne Datei (Umsetzungskonzept 3.7, Befund N 4.6)
        // ==================================================================

        [Fact]
        public void U01_Die_Sektorzuordnung_trifft_die_vier_Mitten()
        {
            Assert.Equal(0, GebaeudeAggregation.Sektor(0));
            Assert.Equal(1, GebaeudeAggregation.Sektor(90));
            Assert.Equal(2, GebaeudeAggregation.Sektor(180));
            Assert.Equal(3, GebaeudeAggregation.Sektor(270));
            Assert.Equal(0, GebaeudeAggregation.Sektor(359));
            Assert.Equal(3, GebaeudeAggregation.Sektor(-90));
            Assert.Equal(1, GebaeudeAggregation.Sektor(450));
        }

        [Fact]
        public void U02_Die_Sektorgrenze_gehoert_aufsteigend_zum_groesseren_Sektor()
        {
            Assert.Equal(1, GebaeudeAggregation.Sektor(45));
            Assert.Equal(2, GebaeudeAggregation.Sektor(135));
            Assert.Equal(3, GebaeudeAggregation.Sektor(225));
            Assert.Equal(0, GebaeudeAggregation.Sektor(315));
            Assert.Equal(0, GebaeudeAggregation.Sektor(44.999));
        }

        [Fact]
        public void U03_Der_Azimut_dreht_mit_TrueNorth()
        {
            // Richtung [1, 0] (Modell-Ost): TrueNorth [0, 1] → Ost; [1, 0] → Nord; [−1, 0] → Süd; [0, −1] → West.
            Assert.Equal(90.0, IfcPlatzierung.Azimut(1, 0, IfcPlatzierung.DrehungAusTrueNorth(0, 1)), 9);
            Assert.Equal(0.0, IfcPlatzierung.Azimut(1, 0, IfcPlatzierung.DrehungAusTrueNorth(1, 0)), 9);
            Assert.Equal(180.0, IfcPlatzierung.Azimut(1, 0, IfcPlatzierung.DrehungAusTrueNorth(-1, 0)), 9);
            Assert.Equal(270.0, IfcPlatzierung.Azimut(1, 0, IfcPlatzierung.DrehungAusTrueNorth(0, -1)), 9);
            // Nicht normierte Richtungsverhältnisse drehen genauso.
            Assert.Equal(0.0, IfcPlatzierung.Azimut(5, 0, IfcPlatzierung.DrehungAusTrueNorth(3, 0)), 9);
        }

        [Fact]
        public void U04_Bei_MapConversion_wird_TrueNorth_nicht_addiert()
        {
            double tn = IfcPlatzierung.DrehungAusTrueNorth(-2, 1);
            double mc = IfcPlatzierung.DrehungAusMapConversion(0, 1);
            Assert.Equal(90.0, mc, 9);
            Assert.Equal(90.0, IfcPlatzierung.Drehung(tn, mc), 9);          // nicht tn + mc
            Assert.Equal(tn, IfcPlatzierung.Drehung(tn, null), 9);          // ohne Umrechnung gilt TrueNorth
            Assert.Equal(0.0, IfcPlatzierung.Drehung(null, null), 9);       // Vorgabe [0, 1]
            Assert.Equal(270.0, IfcPlatzierung.Azimut(0, 1, IfcPlatzierung.Drehung(tn, mc)), 9);
            // Die lokale x-Achse um 30° gegen den Uhrzeigersinn aus Ost gedreht zeigt auf 60°.
            double d30 = IfcPlatzierung.DrehungAusMapConversion(Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
            Assert.Equal(60.0, IfcPlatzierung.Azimut(1, 0, d30), 9);
        }

        [Fact]
        public void U05_Der_U_Wert_einer_Gruppe_ist_flaechengewichtet()
        {
            IfcGebaeudeAbbild a = Synthetisch(("W1", 10.0, 0.5, null), ("W2", 30.0, 1.5, null));
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil());
            Nah(1.25, Wert(s, GebaeudeZielfelder.U_AUSSENWAND));   // nicht 1,0
            Assert.Equal(Importherkunft.Ifc, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Nah(40.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
        }

        [Fact]
        public void U06_Eine_Gruppe_ohne_genug_U_Werte_faellt_auf_die_Vorgabe()
        {
            IfcGebaeudeAbbild a = Synthetisch(("W1", 60.0, 0.5, null), ("W2", 40.0, null, null));
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil());
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Nah(GebaeudeVorgaben.Wert('E', GebaeudeZielfelder.U_AUSSENWAND).Value, Wert(s, GebaeudeZielfelder.U_AUSSENWAND));
        }

        [Fact]
        public void U07_Das_Baujahr_wird_aus_Text_gelesen()
        {
            foreach (string t in new[] { "1965", "ca. 1965", "erbaut 1965/66", "um 1965", "Baujahr: 1965 (Umbau 1990)" })
                Assert.Equal(1965, Baujahrregel.Jahr(t));
            foreach (string t in new[] { "Altbau", "", null, "19650", "1499", "2101", "ca. 65" })
                Assert.Null(Baujahrregel.Jahr(t));
        }

        [Fact]
        public void U08_Die_Baualtersklasse_folgt_dem_Baujahr()
        {
            Assert.Equal('A', Baujahrregel.Klasse(1918));
            Assert.Equal('B', Baujahrregel.Klasse(1919));
            Assert.Equal('B', Baujahrregel.Klasse(1948));
            Assert.Equal('C', Baujahrregel.Klasse(1949));
            Assert.Equal('D', Baujahrregel.Klasse(1965));
            Assert.Equal('H', Baujahrregel.Klasse(2000));
            Assert.Null(Baujahrregel.Klasse(2005));   // ab 2001 ein Standard, kein Jahr

            // In der Zuordnung: ohne gewählte Klasse die aus dem Baujahr, eine gewählte hat Vorrang.
            IfcGebaeudeAbbild a = Synthetisch(("W1", 10.0, 0.5, null));
            a.Gebaeude[0].Baujahr = 1975;
            a.Gebaeude[0].BaujahrText = "1975";
            Assert.Equal('E', GebaeudeAggregation.Bilden(a, 0, null, null, new IfcImportProfil()).Baualtersklasse);
            GebaeudeImportSatz gewaehlt = GebaeudeAggregation.Bilden(a, 0, 'G', null, new IfcImportProfil());
            Assert.Equal('G', gewaehlt.Baualtersklasse);
            Assert.Equal(Importherkunft.Manuell, gewaehlt.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE).Herkunft);
        }

        [Fact]
        public void U09_Die_Einheiten_werden_je_Groessenart_mit_eigenem_Prefix_umgerechnet()
        {
            Assert.Equal(0.001, IfcEinheiten.PrefixFaktor(IfcSIPrefix.MILLI), 15);
            Assert.Equal(0.01, IfcEinheiten.PrefixFaktor(IfcSIPrefix.CENTI), 15);
            Assert.Equal(1.0, IfcEinheiten.PrefixFaktor(null), 15);

            using (var m = new MemoryModel(MemoryModel.GetFactory(XbimSchemaVersion.Ifc4), NullLoggerFactory.Instance, 0))
            using (ITransaction t = m.BeginTransaction("Einheiten"))
            {
                IIfcProject p = Neu<IIfcProject>(m, "IfcProject");
                IIfcUnitAssignment e = Neu<IIfcUnitAssignment>(m, "IfcUnitAssignment");
                e.Units.Add(Si(m, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, IfcSIPrefix.MILLI));
                p.UnitsInContext = e;
                var meldungen = new List<PruefMeldung>();
                IfcEinheiten nurLaenge = IfcEinheiten.Lesen(p, meldungen);
                Assert.Equal(0.001, nurLaenge.Laenge, 15);
                Nah(1e-6, nurLaenge.Flaeche, 1e-18);   // fehlt → aus der Länge abgeleitet, gemeldet
                Nah(1e-9, nurLaenge.Volumen, 1e-21);
                Assert.Equal(2, meldungen.Count(x => x.Schluessel == P + "EINHEIT_ABGELEITET"));

                e.Units.Add(Si(m, IfcUnitEnum.AREAUNIT, IfcSIUnitName.SQUARE_METRE, null));
                e.Units.Add(Si(m, IfcUnitEnum.VOLUMEUNIT, IfcSIUnitName.CUBIC_METRE, IfcSIPrefix.CENTI));
                meldungen.Clear();
                IfcEinheiten gemischt = IfcEinheiten.Lesen(p, meldungen);
                Assert.Equal(1.0, gemischt.Flaeche, 15);     // SQUARE_METRE ohne Prefix bleibt 1, nicht 10⁻⁶
                Nah(1e-6, gemischt.Volumen, 1e-18);    // CENTI CUBIC_METRE = cm³
                Assert.Empty(meldungen);

                // Eine umgerechnete Einheit (Fuß) wird benannt gemeldet, nicht still als 1,0 genommen.
                IIfcConversionBasedUnit fuss = Neu<IIfcConversionBasedUnit>(m, "IfcConversionBasedUnit");
                fuss.UnitType = IfcUnitEnum.LENGTHUNIT;
                fuss.Name = new IfcLabel("FOOT");
                IIfcMeasureWithUnit mw = Neu<IIfcMeasureWithUnit>(m, "IfcMeasureWithUnit");
                mw.ValueComponent = new IfcLengthMeasure(0.3048);
                mw.UnitComponent = Si(m, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null);
                fuss.ConversionFactor = mw;
                var fussMeldungen = new List<PruefMeldung>();
                Assert.Equal(0.3048, IfcEinheiten.Faktor(fuss, 1, "m", fussMeldungen).Value, 12);
                PruefMeldung f = Assert.Single(fussMeldungen);
                Assert.Equal(P + "EINHEIT", f.Schluessel);
                Assert.Equal("FOOT", f.Werte[0]);
                t.RollBack();
            }
        }

        [Fact]
        public void U10_Die_Fensterflaeche_wird_von_der_Wandflaeche_abgezogen()
        {
            IfcGebaeudeAbbild a = Synthetisch(("W1", 100.0, 0.5, null));
            AbbildBauteil w = a.Gebaeude[0].Bauteile[0];
            w.Oeffnungen.Add(new AbbildBauteil { Kennung = "F1", Quelltyp = "IfcWindow", Art = Bauteilart.Fenster, BruttoflaecheM2 = 15.0 });
            w.Oeffnungen.Add(new AbbildBauteil { Kennung = "T1", Quelltyp = "IfcDoor", Art = Bauteilart.Tuer, BruttoflaecheM2 = 2.0 });
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil());
            Nah(83.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(100.0, s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Bruttowert);

            // Überzug mit NetSideArea der Datei → deren Nettofläche (U14), als Hinweis.
            IfcGebaeudeAbbild b = Synthetisch(("W1", 10.0, 0.5, 8.0));
            b.Gebaeude[0].Bauteile[0].Oeffnungen.Add(new AbbildBauteil { Kennung = "F1", Art = Bauteilart.Fenster, BruttoflaecheM2 = 15.0 });
            GebaeudeImportSatz sb = GebaeudeAggregation.Bilden(b, 0, 'E', null, new IfcImportProfil());
            Nah(8.0, Wert(sb, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Assert.Contains(sb.Meldungen, m => m.Schluessel == G + "NETTOFLAECHE_RUECKFALL" && m.Stufe == PruefStufe.Info);

            // Überzug ohne Nettoangabe → 0 und benannter Fehler mit dem IFC-Präfix.
            IfcGebaeudeAbbild c = Synthetisch(("W1", 10.0, 0.5, null));
            c.Gebaeude[0].Bauteile[0].Oeffnungen.Add(new AbbildBauteil { Kennung = "F1", Art = Bauteilart.Fenster, BruttoflaecheM2 = 15.0 });
            GebaeudeImportSatz sc = GebaeudeAggregation.Bilden(c, 0, 'E', null, new IfcImportProfil());
            Nah(0.0, Wert(sc, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Assert.Contains(sc.Meldungen, m => m.Schluessel == P + "NETTOFLAECHE_NEGATIV" && m.Stufe == PruefStufe.Fehler);
        }

        [Fact]
        public void U11_Das_Groessenlimit_greift_vor_dem_Lesen()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifc", new IfcImportProfil(1000));
            PruefMeldung m = Assert.Single(a.Meldungen);
            Assert.Equal(P + "ZU_GROSS", m.Schluessel);
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Empty(a.Gebaeude);
            Assert.Null(a.Abbild);
            Assert.Equal(50L * 1024 * 1024, new IfcImportProfil().MaxBytes);
            Assert.Equal(20L * 1024 * 1024, IfcImportProfil.MAX_BYTES_IOS);
        }

        [Fact]
        public void U12_Ein_Abbruch_wirft_OperationCanceledException()
        {
            var vorher = new CancellationTokenSource();
            vorher.Cancel();
            using (FileStream s = File.OpenRead(Probe("ifc4_haus.ifc")))
                Assert.ThrowsAny<OperationCanceledException>(() =>
                    new GebaeudeImportAblauf().Lesen(s, "ifc4_haus.ifc", new IfcImportProfil(), null, vorher.Token));

            // Mitten im Parsen: Die Bibliothek wickelt den Abbruch in eine Parserausnahme — der Leser
            // gibt ihn als OperationCanceledException weiter, nicht als Lesefehler.
            var mitten = new CancellationTokenSource();
            var melder = new Melder(f => { if (f.Schluessel == P + "FORTSCHRITT") mitten.Cancel(); });
            byte[] gross = Encoding.ASCII.GetBytes(GrosseDatei());
            var a = new GebaeudeImportAblauf();
            Assert.ThrowsAny<OperationCanceledException>(() =>
                a.Lesen(new MemoryStream(gross), "gross.ifc", new IfcImportProfil(), melder, mitten.Token));
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel == P + "LESEFEHLER");
        }

        [Fact]
        public void U13_Der_Azimut_wechselt_verlustfrei_zwischen_Nord_und_Sued_Konvention()
        {
            Assert.Equal(180.0, IfcPlatzierung.NachSuedkonvention(0), 9);
            Assert.Equal(-90.0, IfcPlatzierung.NachSuedkonvention(90), 9);
            Assert.Equal(0.0, IfcPlatzierung.NachSuedkonvention(180), 9);
            Assert.Equal(90.0, IfcPlatzierung.NachSuedkonvention(270), 9);
            Assert.Equal(0.0, IfcPlatzierung.AusSuedkonvention(180), 9);
            Assert.Equal(90.0, IfcPlatzierung.AusSuedkonvention(-90), 9);
            for (double az = 0; az < 360; az += 7.5)
            {
                double sued = IfcPlatzierung.NachSuedkonvention(az);
                Assert.InRange(sued, -180.0 + 1e-12, 180.0);
                Assert.Equal(az, IfcPlatzierung.AusSuedkonvention(sued), 9);
            }
        }

        [Fact]
        public void U14_Die_Wandseite_folgt_der_Raumgrenze_und_die_Platzierungskette_verkettet()
        {
            // Geschoss 3 m hoch, Wand darin um 90° gedreht (Achse nach +y): lokale y-Achse zeigt nach −x.
            IfcRahmen geschoss = IfcPlatzierung.Verketten(IfcRahmen.Welt,
                IfcPlatzierung.Achsen3D(new[] { 0.0, 0.0, 3000.0 }, null, null));
            IfcRahmen wand = IfcPlatzierung.Verketten(geschoss,
                IfcPlatzierung.Achsen3D(new[] { 10000.0, 0.0, 0.0 }, new[] { 0.0, 0.0, 1.0 }, new[] { 0.0, 1.0, 0.0 }));
            Assert.Equal(3000.0, wand.Ursprung[2], 9);
            Assert.Equal(-1.0, wand.Y[0], 12);

            // Raum innen (x < 10 m) → Außenseite +x → Ost; Raum außen → West; beide Seiten → unbestimmt.
            Assert.Equal(90.0, IfcPlatzierung.Wandazimut(wand, new[] { new[] { 6150.0, 150.0, 3000.0 } }, 0).Value, 9);
            Assert.Equal(270.0, IfcPlatzierung.Wandazimut(wand, new[] { new[] { 12000.0, 150.0, 3000.0 } }, 0).Value, 9);
            Assert.Null(IfcPlatzierung.Wandazimut(wand, new[] { new[] { 6150.0, 0.0, 0.0 }, new[] { 12000.0, 0.0, 0.0 } }, 0));
            Assert.Null(IfcPlatzierung.Wandazimut(wand, Array.Empty<double[]>(), 0));
            Assert.Null(IfcPlatzierung.Wandazimut(wand, new[] { new[] { 10000.0, 500.0, 0.0 } }, 0));   // auf der Achse
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>
        /// Ein IFC-Abbild ohne Datei: ein beheizter Raum (100 m², 250 m³) und Außenwände ohne Raumgrenze
        /// (<see cref="AbbildBauteil.HuelleOhneNachbar"/>), je (Name, Brutto, U, NetSideArea).
        /// </summary>
        private static IfcGebaeudeAbbild Synthetisch(params (string Name, double Brutto, double? U, double? Netto)[] waende)
        {
            var a = new IfcGebaeudeAbbild();
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Synthetisch", Quelltyp = "IfcBuilding" };
            g.Raeume.Add(new AbbildRaum { Kennung = "R1", Quelltyp = "IfcSpace", Name = "Raum", FlaecheM2 = 100, VolumenM3 = 250 });
            foreach ((string name, double brutto, double? u, double? netto) in waende)
                g.Bauteile.Add(new AbbildBauteil
                {
                    Kennung = name, Name = name, Quelltyp = "IfcWall", Art = Bauteilart.Aussenwand,
                    Randbedingung = Randbedingung.Aussenluft, BruttoflaecheM2 = brutto, NettoflaecheM2 = netto,
                    UWertWm2K = u, NeigungGrad = 90, AzimutGrad = 180, HuelleOhneNachbar = true,
                });
            a.Gebaeude.Add(g);
            return a;
        }

        /// <summary>Der Code einer Quelldatei ohne Kommentare — geprüft wird der Code, nicht die Erklärung.</summary>
        private static string Code(string datei)
            => Regex.Replace(File.ReadAllText(datei), @"//[^\r\n]*|/\*.*?\*/", "", RegexOptions.Singleline);

        private static T Neu<T>(IModel m, string typ) where T : class => (T)m.Instances.New(m.Metadata.ExpressType(typ.ToUpperInvariant()).Type);

        private static IIfcSIUnit Si(IModel m, IfcUnitEnum art, IfcSIUnitName name, IfcSIPrefix? prefix)
        {
            IIfcSIUnit u = Neu<IIfcSIUnit>(m, "IfcSIUnit");
            u.UnitType = art;
            u.Name = name;
            u.Prefix = prefix;
            return u;
        }

        /// <summary>Eine STEP-Datei mit vielen Wänden, damit der Parser mehrfach Fortschritt meldet.</summary>
        private static string GrosseDatei()
        {
            var sb = new StringBuilder("ISO-10303-21;\r\nHEADER;\r\nFILE_DESCRIPTION((''),'2;1');\r\n" +
                                       "FILE_NAME('gross.ifc','2026-09-25T00:00:00',(''),(''),'','','');\r\nFILE_SCHEMA(('IFC4'));\r\nENDSEC;\r\nDATA;\r\n");
            for (int i = 1; i <= 60000; i++)
                sb.Append('#').Append(i).Append("=IFCWALL('").Append(i.ToString("D22")).Append("',$,'W',$,$,$,$,$,$);\r\n");
            sb.Append("ENDSEC;\r\nEND-ISO-10303-21;\r\n");
            return sb.ToString();
        }

        private sealed class Melder : IProgress<ImportFortschritt>
        {
            private readonly Action<ImportFortschritt> _tun;
            public Melder(Action<ImportFortschritt> tun) { _tun = tun; }
            public void Report(ImportFortschritt value) => _tun(value);
        }

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
