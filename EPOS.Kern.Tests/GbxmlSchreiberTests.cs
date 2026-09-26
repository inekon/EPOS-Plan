using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7a, Welle 1 — der gbXML-Schreiber</b> (Umsetzungsauftrag G7a, Abschnitt 3 W1):
    /// Rundlauf Abbild → Schreiber → Leser → Abbild (Probe 1a), die Namensraum-Wache, Uhr und Sprache
    /// (Probe 12), die Byteform, die Zahlenform, die Prüfungen des Schreibers und — nur lokal, wenn die
    /// Schemakopie beiliegt — die Schemaprüfung des Probenabbilds als Vorstufe von Probe 3.
    /// </summary>
    public sealed class GbxmlSchreiberTests
    {
        private const double Genau = 1e-6;
        private readonly ITestOutputHelper _ausgabe;

        public GbxmlSchreiberTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        // ==================================================================
        //  Probe 1a — Abbild → Schreiber → Leser → Abbild
        // ==================================================================

        [Fact]
        public void Probe1a_das_Abbild_kehrt_ueber_den_eigenen_Leser_zurueck()
        {
            GebaeudeAbbild quelle = GbxmlExportProbe.Abbild();
            byte[] datei = GbxmlExportProbe.Schreiben(quelle);
            GbxmlAbbild zurueck = GbxmlExportProbe.Lesen(datei);

            // Keine Lese- und keine Verweisfehler; der Platzhalter „unbeheizt" ergibt den Zonenvorschlag X1.
            string[] schlecht = { "LESEFEHLER", "KEIN_CAMPUS", "VERWEIS_LEER", "EINHEIT_UNBEKANNT", "ZAHL_UNLESBAR",
                                  "TYP_UNBEKANNT", "NACHBAR_UNBEKANNT", "OHNE_AUFBAU", "KEIN_NORDEN", "NORDDREHUNG",
                                  "VERSION_UNBEKANNT", "STOFFWERT_FEHLSTELLE", "GEOMETRIE_FEHLT" };
            List<PruefMeldung> alle = zurueck.Meldungen.Concat(zurueck.Gebaeude.SelectMany(x => x.Meldungen))
                                             .Concat(zurueck.Gebaeude.SelectMany(x => x.Bauteile).SelectMany(b => b.Meldungen)).ToList();
            Assert.DoesNotContain(alle, m => schlecht.Any(s => m.Schluessel == "IMP_GBXML_PROT_" + s));
            Assert.Equal("6.01", zurueck.Version);
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, zurueck.Format);

            AbbildGebaeude gq = quelle.Gebaeude[0];
            AbbildGebaeude gz = Assert.Single(zurueck.Gebaeude);
            Assert.Equal(gq.Kennung, gz.Kennung);
            Assert.Equal(gq.Name, gz.Name);
            Assert.Equal(gq.Art, gz.Art);
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, gz.Zonenvorschlag);
            Assert.Contains(gz.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_ZONENVORSCHLAG");

            // Ort: die PLZ, Nordwinkel 0.
            Assert.Equal(quelle.Plz, zurueck.Ort);
            Assert.Equal(0.0, zurueck.NordwinkelGrad);

            Assert.Equal(gq.Raeume.Count, gz.Raeume.Count);
            for (int i = 0; i < gq.Raeume.Count; i++) RaumGleich(gq.Raeume[i], gz.Raeume[i]);

            Assert.Equal(gq.Bauteile.Count, gz.Bauteile.Count);
            for (int i = 0; i < gq.Bauteile.Count; i++) BauteilGleich(gq.Bauteile[i], gz.Bauteile[i], true);
        }

        [Fact]
        public void Probe1a_die_unsymmetrische_Schichtfolge_wird_umgekehrt_geschrieben_und_kehrt_umgekehrt_zurueck()
        {
            GebaeudeAbbild quelle = GbxmlExportProbe.Abbild();
            GbxmlAbbild zurueck = GbxmlExportProbe.Lesen(GbxmlExportProbe.Schreiben(quelle));

            AbbildAufbau aq = quelle.Gebaeude[0].Bauteile[0].Aufbau;
            AbbildAufbau az = zurueck.Gebaeude[0].Bauteile[0].Aufbau;
            Assert.Equal(Schichtrichtung.InnenNachAussen, aq.Richtung);
            Assert.Equal(Schichtrichtung.AussenNachInnen, az.Richtung);
            // Innendämmung: raumseitig Gipskarton, außen Putz — die Datei beginnt außen.
            Assert.Equal("Kalkzementputz", az.Schichten[0].Name);
            Assert.Equal("Gipskarton", az.Schichten[az.Schichten.Count - 1].Name);
            Assert.Equal(aq.Schichten.Select(s => s.BaustoffKennung).Reverse(), az.Schichten.Select(s => s.BaustoffKennung));
            Assert.Equal(Aufbaustatus.Vollstaendig, az.Status);
        }

        private static void RaumGleich(AbbildRaum q, AbbildRaum z)
        {
            Assert.Equal(q.Kennung, z.Kennung);
            Assert.Equal(q.Name, z.Name);
            Nah(q.FlaecheM2, z.FlaecheM2);
            Nah(q.VolumenM3, z.VolumenM3);
            Assert.Equal(q.Beheizt, z.Beheizt);
            Assert.Equal(q.Zustandsangabe, z.Zustandsangabe);
            Nah(q.LuftwechselJeH, z.LuftwechselJeH);
            Nah(q.Personen, z.Personen);
            Nah(q.FlaecheJePersonM2, z.FlaecheJePersonM2);
            Nah(q.LichtWm2, z.LichtWm2);
            Nah(q.GeraeteWm2, z.GeraeteWm2);
            Nah(q.SollHeizenC, z.SollHeizenC);
            Nah(q.SollKuehlenC, z.SollKuehlenC);
            Assert.Equal(q.ZonenKennung, z.ZonenKennung);
        }

        private static void BauteilGleich(AbbildBauteil q, AbbildBauteil z, bool flaeche)
        {
            string wo = q.Kennung;
            Assert.Equal(q.Kennung, z.Kennung);
            Assert.Equal(q.Name, z.Name);
            Assert.Equal(q.Quellart, z.Quellart);
            Assert.Equal(q.Art, z.Art);
            if (flaeche) Assert.Equal(q.Randbedingung, z.Randbedingung);
            Assert.Equal(q.Nachbarn.Select(n => n.Kennung + "|" + n.Sicht), z.Nachbarn.Select(n => n.Kennung + "|" + n.Sicht));
            Nah(q.BruttoflaecheM2, z.BruttoflaecheM2, wo);
            Nah(q.AzimutGrad, z.AzimutGrad, wo);
            Nah(q.NeigungGrad, z.NeigungGrad, wo);
            Nah(q.UWertWm2K, z.UWertWm2K, wo);
            Nah(q.GWert, z.GWert, wo);
            AufbauGleich(q.Aufbau, z.Aufbau);
            Assert.Equal(q.Oeffnungen.Count, z.Oeffnungen.Count);
            for (int i = 0; i < q.Oeffnungen.Count; i++) BauteilGleich(q.Oeffnungen[i], z.Oeffnungen[i], false);
        }

        private static void AufbauGleich(AbbildAufbau q, AbbildAufbau z)
        {
            if (q == null) { Assert.Null(z); return; }
            Assert.NotNull(z);
            Assert.Equal(q.Kennung, z.Kennung);
            Assert.Equal(q.Name, z.Name);
            Nah(q.UWertWm2K, z.UWertWm2K, q.Kennung);
            List<AbbildSchicht> erwartet = q.Richtung == Schichtrichtung.InnenNachAussen
                ? Enumerable.Reverse(q.Schichten).ToList() : q.Schichten.ToList();
            Assert.Equal(erwartet.Count, z.Schichten.Count);
            for (int i = 0; i < erwartet.Count; i++)
            {
                AbbildSchicht s = erwartet[i], t = z.Schichten[i];
                Assert.Equal(s.BaustoffKennung, t.BaustoffKennung);
                Assert.Equal(s.Name, t.Name);
                Nah(s.DickeM, t.DickeM, s.BaustoffKennung);
                Nah(s.LambdaWmK, t.LambdaWmK, s.BaustoffKennung);
                Nah(s.RhoKgM3, t.RhoKgM3, s.BaustoffKennung);
                Nah(s.CpJkgK, t.CpJkgK, s.BaustoffKennung);
                Nah(s.RWertM2KW, t.RWertM2KW, s.BaustoffKennung);
            }
        }

        private static void Nah(double? erwartet, double? ist, string wo = "")
        {
            Assert.True(erwartet.HasValue == ist.HasValue, wo + ": erwartet " + erwartet + ", ist " + ist);
            if (erwartet.HasValue)
                Assert.True(Math.Abs(erwartet.Value - ist.Value) <= Genau * Math.Max(1.0, Math.Abs(erwartet.Value)),
                            wo + ": erwartet " + erwartet.Value.ToString("R", CultureInfo.InvariantCulture)
                            + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        // ==================================================================
        //  Namensraum-Wache — der Leser prüft nur lokale Namen
        // ==================================================================

        [Fact]
        public void Namensraum_jedes_Element_im_gbXML_Namensraum_und_die_Wurzelattribute_exakt()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild());
            string text = Encoding.UTF8.GetString(datei);
            XDocument d = XDocument.Load(new MemoryStream(datei));

            XNamespace ns = GbxmlLeser.NAMENSRAUM;
            Assert.All(d.Descendants(), e => Assert.Equal(ns, e.Name.Namespace));
            Assert.DoesNotContain("xmlns=\"\"", text, StringComparison.Ordinal);
            Assert.Equal(1, Vorkommen(text, "xmlns"));
            Assert.All(d.Descendants().Attributes().Where(a => !a.IsNamespaceDeclaration),
                       a => Assert.Equal(XNamespace.None, a.Name.Namespace));

            Assert.Equal("gbXML", d.Root.Name.LocalName);
            Assert.Equal(new[]
            {
                "xmlns=http://www.gbxml.org/schema", "version=6.01", "temperatureUnit=C", "lengthUnit=Meters",
                "areaUnit=SquareMeters", "volumeUnit=CubicMeters", "useSIUnitsForResults=true",
            }, d.Root.Attributes().Select(a => a.Name.LocalName + "=" + a.Value));
            Assert.Contains("<gbXML xmlns=\"http://www.gbxml.org/schema\" version=\"6.01\" temperatureUnit=\"C\" lengthUnit=\"Meters\" "
                            + "areaUnit=\"SquareMeters\" volumeUnit=\"CubicMeters\" useSIUnitsForResults=\"true\">", text, StringComparison.Ordinal);

            // Aufbau: Campus zuerst, dann die Kataloge der Wurzel, zuletzt DocumentHistory.
            Assert.Equal(new[] { "Campus", "Construction", "Layer", "Material", "WindowType", "Zone", "DocumentHistory" },
                         d.Root.Elements().Select(e => e.Name.LocalName).Distinct());
            XElement campus = d.Root.Element(ns + "Campus");
            Assert.Equal(new[] { "Name", "Description", "Location", "Building", "Surface" },
                         campus.Elements().Select(e => e.Name.LocalName).Distinct());
        }

        private static int Vorkommen(string text, string teil)
        {
            int n = 0;
            for (int i = text.IndexOf(teil, StringComparison.Ordinal); i >= 0; i = text.IndexOf(teil, i + 1, StringComparison.Ordinal)) n++;
            return n;
        }

        // ==================================================================
        //  Probe 12 — Uhr, Sprache, Byteform
        // ==================================================================

        [Fact]
        public void Probe12_feste_Uhr_ergibt_dieselben_Bytes()
        {
            byte[] a = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild());
            byte[] b = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild());
            Assert.Equal(a, b);
        }

        [Fact]
        public void Probe12_zwei_Uhren_unterscheiden_sich_an_genau_einer_Stelle()
        {
            byte[] a = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(new DateTime(2026, 9, 26, 12, 0, 0)));
            byte[] b = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(new DateTime(2027, 1, 2, 3, 4, 5)));
            string[] za = Encoding.UTF8.GetString(a).Split('\n'), zb = Encoding.UTF8.GetString(b).Split('\n');
            Assert.Equal(za.Length, zb.Length);
            int[] verschieden = Enumerable.Range(0, za.Length).Where(i => za[i] != zb[i]).ToArray();
            int zeile = Assert.Single(verschieden);
            Assert.Contains("<CreatedBy ", za[zeile], StringComparison.Ordinal);
            Assert.Contains("date=\"2026-09-26T12:00:00\"", za[zeile], StringComparison.Ordinal);
            Assert.Contains("date=\"2027-01-02T03:04:05\"", zb[zeile], StringComparison.Ordinal);
            Assert.Equal(1, Vorkommen(za[zeile], "2026"));
        }

        [Fact]
        public void Probe12_deutsch_und_englisch_ergeben_dieselben_Zahlen_und_je_Sprache_dieselben_Bytes()
        {
            byte[] de1, de2, en1, en2;
            using (new Kulturvorrichtung("de-DE"))
            {
                de1 = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(sprache: "de-DE"));
                de2 = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(sprache: "de-DE"));
            }
            using (new Kulturvorrichtung("en-US"))
            {
                en1 = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(sprache: "en-US"));
                en2 = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), GbxmlExportProbe.Profil(sprache: "en-US"));
            }
            Assert.Equal(de1, de2);
            Assert.Equal(en1, en2);
            string[] zahlen = GbxmlExportProbe.Zahlen(de1);
            Assert.Contains("Thickness=0.0125", zahlen);
            Assert.Contains("DesignHeatT=20", zahlen);
            Assert.Equal(zahlen, GbxmlExportProbe.Zahlen(en1));
            Assert.DoesNotContain(zahlen, z => z.Contains(','));
        }

        [Fact]
        public void Byteform_UTF8_ohne_BOM_Zeilenende_LF_zwei_Leerzeichen_Einzug()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild());
            Assert.False(datei.Length >= 3 && datei[0] == 0xEF && datei[1] == 0xBB && datei[2] == 0xBF, "BOM geschrieben");
            string text = Encoding.UTF8.GetString(datei);
            Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<gbXML ", text, StringComparison.Ordinal);
            Assert.Contains("\n  <Campus id=\"epos-campus-7\">\n    <Name>Probehaus</Name>", text, StringComparison.Ordinal);
            // Die Umlaute stehen als UTF-8, ohne Entität.
            Assert.Contains("<Name>Außenwand Innendämmung</Name>", text, StringComparison.Ordinal);
        }

        [Fact]
        public void DocumentHistory_traegt_nur_Programmname_Version_und_den_Zeitstempel()
        {
            XDocument d = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild())));
            XNamespace ns = GbxmlLeser.NAMENSRAUM;
            XElement h = Assert.Single(d.Root.Elements(ns + "DocumentHistory"));
            Assert.Equal("EPOS-Plan", h.Element(ns + "ProgramInfo").Element(ns + "ProductName").Value);
            Assert.Equal(GbxmlExportProbe.VERSION, h.Element(ns + "ProgramInfo").Element(ns + "Version").Value);
            Assert.Equal(new[] { "FirstName=EPOS-Plan", "LastName=" + GbxmlExportProbe.VERSION },
                         h.Element(ns + "PersonInfo").Elements().Select(e => e.Name.LocalName + "=" + e.Value));
            XElement c = h.Element(ns + "CreatedBy");
            Assert.Equal("epos-person", (string)c.Attribute("personId"));
            Assert.Equal("epos-programm", (string)c.Attribute("programId"));
            Assert.Equal("2026-09-26T12:00:00", (string)c.Attribute("date"));
        }

        [Fact]
        public void Kataloge_je_Kennung_einmal_Schichten_der_Uebergangsfaelle_geteilt()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(), null, out GebaeudeExportBilanz bilanz);
            XDocument d = XDocument.Load(new MemoryStream(datei));
            XNamespace ns = GbxmlLeser.NAMENSRAUM;

            // Zwei Übergangsfälle der Außenwand, Dach, Bodenplatte, Ersatz, Innenwand.
            Assert.Equal(6, d.Root.Elements(ns + "Construction").Count());
            Assert.Equal(4 + 3 + 2 + 1 + 1, d.Root.Elements(ns + "Layer").Count());
            Assert.Equal(4 + 3 + 2 + 1 + 1, d.Root.Elements(ns + "Material").Count());
            Assert.Single(d.Root.Elements(ns + "WindowType"));
            Assert.Single(d.Root.Elements(ns + "Zone"));

            List<string> ids = d.Descendants().Attributes("id").Select(a => a.Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            var verweise = d.Descendants().Attributes()
                            .Where(a => a.Name.LocalName.EndsWith("IdRef", StringComparison.Ordinal) || a.Name.LocalName.EndsWith("Id", StringComparison.Ordinal)
                                        && a.Name.LocalName != "id").Select(a => a.Value).ToList();
            Assert.All(verweise, v => Assert.Contains(v, ids));

            Assert.Equal(9, bilanz.Flaechen);
            Assert.Equal(2, bilanz.Oeffnungen);
            Assert.Equal(6, bilanz.Aufbauten);
            Assert.Equal(1, bilanz.Ersatzaufbauten);
            Assert.Equal(datei.Length, bilanz.Bytes);
            Assert.Empty(bilanz.Meldungen);
        }

        [Fact]
        public void Ohne_PLZ_entfaellt_Location_ganz_und_der_Leser_meldet_keinen_Norden()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(plz: null));
            XDocument d = XDocument.Load(new MemoryStream(datei));
            Assert.Empty(d.Descendants(XName.Get("Location", GbxmlLeser.NAMENSRAUM)));
            GbxmlAbbild zurueck = GbxmlExportProbe.Lesen(datei);
            Assert.Null(zurueck.Ort);
            Assert.Contains(zurueck.Meldungen, m => m.Schluessel == "IMP_GBXML_PROT_KEIN_NORDEN");
        }

        // ==================================================================
        //  Zahlen
        // ==================================================================

        [Theory]
        [InlineData(0.1, "0.1")]
        [InlineData(20.0, "20")]
        [InlineData(0.0125, "0.0125")]
        [InlineData(1e-5, "0.00001")]
        [InlineData(-2.5e-7, "-0.00000025")]
        [InlineData(1.2345e-10, "0.00000000012345")]
        [InlineData(1.5e20, "150000000000000000000")]
        [InlineData(1e15, "1000000000000000")]
        [InlineData(123456789.25, "123456789.25")]
        public void Zahl_kulturfrei_rundlaufend_ohne_Exponenten(double wert, string erwartet)
        {
            string text = GbxmlSchreiber.Zahl(wert);
            Assert.Equal(erwartet, text);
            Assert.Equal(wert, double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
            Assert.Equal(wert, (double)decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture), 15);
        }

        [Fact]
        public void Zahl_nicht_endlich_ist_ein_Fehler()
        {
            Assert.Throws<InvalidOperationException>(() => GbxmlSchreiber.Zahl(double.NaN));
            Assert.Throws<InvalidOperationException>(() => GbxmlSchreiber.Zahl(double.PositiveInfinity));
        }

        [Fact]
        public void Zeitstempel_ohne_Zone_mit_Z_bei_UTC()
        {
            Assert.Equal("2026-09-26T12:00:00", GbxmlSchreiber.Zeitstempel(new DateTime(2026, 9, 26, 12, 0, 0, 500)));
            Assert.Equal("2026-09-26T12:00:00Z", GbxmlSchreiber.Zeitstempel(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc)));
        }

        // ==================================================================
        //  Die Prüfungen des Schreibers — ganz oder gar nicht
        // ==================================================================

        [Fact]
        public void Weniger_als_vier_Flaechen_wirft_und_schreibt_nichts()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile.RemoveRange(3, a.Gebaeude[0].Bauteile.Count - 3);
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Flaeche_ohne_Aufbau_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[1].Aufbau = null;
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Doppelte_Kennung_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[1].Kennung = a.Gebaeude[0].Bauteile[0].Kennung;
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Kennung_kein_NCName_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[1].Kennung = "7-bauteil";
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Baustoffkennung_mit_zwei_Stoffwerten_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[4].Aufbau.Schichten[0] = GbxmlExportProbe.Schicht(201, 1, "Gipskarton", 0.015, 0.25, 900.0, 1000.0);
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Nachbar_ausserhalb_des_Gebaeudes_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[1].Nachbarn.Add(GbxmlExportProbe.N("epos-raum-999"));
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Breite_mal_Hoehe_ungleich_Bruttoflaeche_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Bauteile[1].BruttoflaecheM2 = 26.0;
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Unbekannte_Gebaeudeart_wirft()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Art = "Einfamilienhaus";
            AssertNichtsGeschrieben(a);
        }

        [Fact]
        public void Abbruch_schreibt_nichts()
        {
            using (var ziel = new MemoryStream())
            using (var quelle = new CancellationTokenSource())
            {
                quelle.Cancel();
                Assert.ThrowsAny<OperationCanceledException>(() =>
                    new GbxmlSchreiber().Schreiben(GbxmlExportProbe.Abbild(), ziel, GbxmlExportProbe.Profil(), quelle.Token));
                Assert.Equal(0, ziel.Length);
            }
        }

        [Fact]
        public void Steuerzeichen_im_Text_fallen_weg()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            a.Gebaeude[0].Name = "Probe\u0001haus";
            string text = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(a));
            Assert.Contains("<Name>Probehaus</Name>", text, StringComparison.Ordinal);
        }

        private static void AssertNichtsGeschrieben(GebaeudeAbbild a)
        {
            using (var ziel = new MemoryStream())
            {
                Assert.Throws<InvalidOperationException>(() =>
                    new GbxmlSchreiber().Schreiben(a, ziel, GbxmlExportProbe.Profil(), CancellationToken.None));
                Assert.Equal(0, ziel.Length);
            }
        }

        // ==================================================================
        //  Vorstufe von Probe 3 — Schemaprüfung, nur lokal (D17)
        // ==================================================================

        [Fact]
        public void Probe3_Vorstufe_das_Probenabbild_ist_schemagueltig_mit_und_ohne_PLZ()
        {
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema == null)
            {
                _ausgabe.WriteLine("Schemaprüfung übersprungen: Referenzlaeufe/Schemakopien/" + GbxmlExportProbe.SCHEMAKOPIE
                                   + " liegt nicht bei (D17, lokal beizustellen).");
                return;
            }
            foreach (string plz in new[] { "01067", null })
            {
                string[] befunde = GbxmlExportProbe.Schemapruefung(schema, GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild(plz)));
                Assert.True(befunde.Length == 0, "PLZ " + (plz ?? "ohne") + ":\n" + string.Join("\n", befunde));
            }

            // Gegenproben: Der Prüfer beißt — version 8.01 und eine Gebäudeart außerhalb von buildingTypeEnum.
            string gueltig = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(GbxmlExportProbe.Abbild()));
            foreach ((string alt, string neu) in new[] { ("version=\"6.01\"", "version=\"8.01\""), ("buildingType=\"SingleFamily\"", "buildingType=\"Einfamilienhaus\"") })
            {
                Assert.Contains(alt, gueltig, StringComparison.Ordinal);
                string[] befunde = GbxmlExportProbe.Schemapruefung(schema, Encoding.UTF8.GetBytes(gueltig.Replace(alt, neu)));
                Assert.True(befunde.Length > 0, "Die Gegenprobe " + neu + " ist nicht aufgefallen.");
            }
            _ausgabe.WriteLine("Schemaprüfung gegen " + schema + ": gültig mit und ohne PLZ; Gegenproben fallen auf.");
        }
    }
}
