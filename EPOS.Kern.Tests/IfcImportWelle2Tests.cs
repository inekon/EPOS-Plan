using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der IFC-Import, Stufe G4a Welle 2</b> (Umsetzungskonzept 3.4): die Vorgabe-Rückfälle
    /// (Raumhöhe, Dach-, Grund- und sonstige Fläche), das Volumen als Prüfgröße, die Bauart aus den
    /// Schichten samt der Schichtfolge aus der <c>IfcMaterialLayerSetUsage</c>, Stoffwerte ≤ 0 als
    /// Fehlstelle, die in IFC2X3 benannt nicht gelesenen Stoffwerte und der iOS-Dateifilter.
    ///
    /// <para><b>Die Proben</b> (<see cref="IfcProbenErzeuger"/>): <c>ifc4_schichten.ifc</c> — ein Raum
    /// (80 m²), vier Außenwände (85 m² netto), Dach- und Bodenplatte je 80 m², alles ohne U-Wert, aber mit
    /// Schichten; raumseitig bis 10 cm tragen die Wände 123 000 J/(m²K) (Putz 1,5 cm, Mauerwerk 8,5 cm),
    /// Dach und Bodenplatte je 240 000 J/(m²K) (Beton 10 cm) — flächengewichtet 55,39 Wh/(m²K), also
    /// „schwer". Mit der Annahme „erste Schicht außen" lägen bei Süd- und Nordwand und beim Dach die
    /// Dämmung raumseitig (4 500 J/(m²K)), und es käme 27,98 Wh/(m²K) heraus, „leicht".
    /// <c>ifc4_rueckfaelle.ifc</c> — zwei Geschosse, Räume 60 und 50 m² ohne Höhe und Volumen, Dach- und
    /// Bodenplatte ohne Mengen, das Obergeschoss mit 70 m² Bruttogrundfläche.</para>
    /// </summary>
    public sealed class IfcImportWelle2Tests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string G = "IMP_GEB_PROT_";

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // U-Werte der Schichtenprobe von Hand: R_si 0,13 / 0,10 / 0,17, R_se 0,04 bzw. 0 gegen Erdreich.
        private static readonly double UWand = 1.0 / (0.13 + 0.015 / 0.7 + 0.24 / 0.5 + 0.1 / 0.04 + 0.04);
        private static readonly double UDach = 1.0 / (0.10 + 0.2 / 2.0 + 0.2 / 0.04 + 0.04);
        private static readonly double UBoden = 1.0 / (0.17 + 0.1 / 0.04 + 0.2 / 2.0);

        // ==================================================================
        //  Bauart aus den Schichten, Schichtfolge
        // ==================================================================

        [Fact]
        public void Schichtenhaus_Bauart_aus_den_Schichten_mit_der_Schichtfolge_aus_der_Nutzung()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_schichten.ifc");
            Assert.DoesNotContain(a.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            foreach (string k in new[] { "STOFFWERT_NULL", "STOFFWERTE_NICHT_GELESEN", "SCHICHTFOLGE_ANGENOMMEN" })
                Assert.False(Hat(a.Meldungen, P + k), k);

            // Die Schichtfolge folgt der Nutzung: Süd innen zuerst gegen die y-Achse, Ost außen zuerst längs
            // der Achse, Dach von unten (innen) nach oben, Bodenplatte von unten (außen) nach oben.
            AbbildBauteil sued = Bauteil(a.Abbild, "Süd");
            Assert.Equal(Schichtrichtung.InnenNachAussen, sued.Aufbau.Richtung);
            Assert.False(sued.Aufbau.RichtungAngenommen);
            Assert.Equal(Schichtrichtung.AussenNachInnen, Bauteil(a.Abbild, "Ost").Aufbau.Richtung);
            Assert.Equal(Schichtrichtung.InnenNachAussen, Bauteil(a.Abbild, "Dach").Aufbau.Richtung);
            Assert.Equal(Schichtrichtung.AussenNachInnen, Bauteil(a.Abbild, "Bodenplatte").Aufbau.Richtung);

            // Die Stoffwerte aus Pset_MaterialThermal und Pset_MaterialCommon, die Dicke in Metern.
            AbbildSchicht putz = sued.Aufbau.Schichten[0];
            Assert.Equal("Putz", putz.Name);
            Nah(0.015, putz.DickeM, 1e-12);
            Nah(0.7, putz.LambdaWmK);
            Nah(1400.0, putz.RhoKgM3);
            Nah(1000.0, putz.CpJkgK);
            Assert.Equal(Aufbaustatus.Vollstaendig, sued.Aufbau.Status);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, bauart.Textwert);
            Assert.Equal(Importherkunft.Ifc, bauart.Herkunft);
            Assert.Equal("GIMP_BELEG_BAUART_SCHICHTEN", bauart.Beleg.Schluessel);
            Assert.Equal(new[] { "55.39", "6", "245" }, bauart.Beleg.Werte);
            Assert.Null(bauart.Markierung);

            // U-Werte aus den Schichten über Bauteilreduktion, Flächen aus den Mengen.
            Nah(85.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(UWand, Wert(s, GebaeudeZielfelder.U_AUSSENWAND), 1e-9);
            Assert.Equal(Importherkunft.Ifc, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Nah(80.0, Wert(s, GebaeudeZielfelder.FLAECHE_DACH));
            Nah(UDach, Wert(s, GebaeudeZielfelder.U_DACH), 1e-9);
            Nah(80.0, Wert(s, GebaeudeZielfelder.FLAECHE_GRUND));
            Nah(UBoden, Wert(s, GebaeudeZielfelder.U_GRUND), 1e-9);
            Assert.Equal(DbWerte.GRUND_ERDREICH, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
            Nah(5.0, Wert(s, GebaeudeZielfelder.FENSTER_SUED));

            // Keine Tür, keine Vorhangfassade: die sonstigen Flächen fallen auf 0 als Vorgabe.
            GebaeudeFeldzeile sonstige = s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE);
            Nah(0.0, sonstige.Wert);
            Assert.Equal(Importherkunft.Vorgabe, sonstige.Herkunft);
            Assert.Equal("GIMP_BELEG_SONSTIGE_VORGABE", sonstige.Beleg.Schluessel);
            Assert.Equal(new[] { "0" }, sonstige.Beleg.Werte);
        }

        [Fact]
        public void Mit_der_Annahme_erste_Schicht_aussen_kaeme_leicht_heraus()
        {
            // Gegenprobe zur Schichtfolge: dieselben Aufbauten, alle „erste Schicht außen" gelesen.
            GebaeudeImportAblauf a = Lesen("ifc4_schichten.ifc");
            foreach (AbbildBauteil b in a.Abbild.Gebaeude[0].Bauteile.Where(b => b.Aufbau != null))
                b.Aufbau.Richtung = Schichtrichtung.AussenNachInnen;
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a.Abbild, 0, 'E', a.Quelle, a.Profil);
            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_LEICHT, bauart.Textwert);
            Assert.Equal("27.98", bauart.Beleg.Werte[0]);
        }

        [Fact]
        public void Ohne_Nutzung_bleibt_die_Annahme_und_wird_benannt()
        {
            // Die Zuordnungen zeigen auf den Schichtsatz selbst statt auf seine Nutzung.
            string text = File.ReadAllText(Probe("ifc4_schichten.ifc"));
            Dictionary<string, string> satzDerNutzung = Regex.Matches(text, @"#(\d+)=IFCMATERIALLAYERSETUSAGE\(#(\d+),")
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            Assert.Equal(6, satzDerNutzung.Count);
            string ohne = Regex.Replace(text, @"(IFCRELASSOCIATESMATERIAL\([^;]*\),)#(\d+)\);",
                m => m.Groups[1].Value + "#" + satzDerNutzung[m.Groups[2].Value] + ");");
            Assert.NotEqual(text, ohne);

            GebaeudeImportAblauf a = LesenBytes(Encoding.ASCII.GetBytes(ohne), "ohne_nutzung.ifc");
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "SCHICHTFOLGE_ANGENOMMEN");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal("6", m.Werte[0]);
            Assert.True(Bauteil(a.Abbild, "Süd").Aufbau.RichtungAngenommen);
            Assert.Equal(GebaeudeZielfelder.BAUART_LEICHT, a.Zuordnen(0, 'E').Zeile(GebaeudeZielfelder.BAUART).Textwert);
        }

        [Fact]
        public void U15_Die_Schichtfolge_folgt_Schichtungsrichtung_und_Aussenseite()
        {
            // Die erste Schicht liegt gegen die Schichtungsrichtung; liegt außen in derselben Richtung, ist sie innen.
            Assert.Equal(Schichtrichtung.InnenNachAussen, IfcAbbildBauer.Schichtfolge(+1, +1));
            Assert.Equal(Schichtrichtung.InnenNachAussen, IfcAbbildBauer.Schichtfolge(-1, -1));
            Assert.Equal(Schichtrichtung.AussenNachInnen, IfcAbbildBauer.Schichtfolge(+1, -1));
            Assert.Equal(Schichtrichtung.AussenNachInnen, IfcAbbildBauer.Schichtfolge(-1, +1));
        }

        [Fact]
        public void Stoffwerte_null_sind_Fehlstellen_und_lassen_die_Bauart_bei_der_Vorgabe()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_schichten_nullwerte.ifc");
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "STOFFWERT_NULL");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1", "Dämmung" }, m.Werte);

            AbbildBauteil sued = Bauteil(a.Abbild, "Süd");
            AbbildSchicht daemmung = sued.Aufbau.Schichten.Single(x => x.Name == "Dämmung");
            Nah(0.04, daemmung.LambdaWmK);
            Assert.Null(daemmung.RhoKgM3);
            Assert.Null(daemmung.CpJkgK);
            Assert.Equal(Aufbaustatus.Masselos, sued.Aufbau.Status);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, bauart.Textwert);
            Assert.Equal(Importherkunft.Vorgabe, bauart.Herkunft);
            Assert.Equal("GIMP_BELEG_BAUART_VORGABE", bauart.Beleg.Schluessel);
            Assert.Equal(new[] { "0", "6" }, bauart.Beleg.Werte);
            // Der U-Wert bleibt aus den Schichten bestimmbar — die Masse fehlt, der Widerstand nicht.
            Nah(UWand, Wert(s, GebaeudeZielfelder.U_AUSSENWAND), 1e-9);
            Assert.Equal(Importherkunft.Ifc, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
        }

        [Fact]
        public void IFC2X3_Stoffwerte_werden_benannt_nicht_gelesen()
        {
            GebaeudeImportAblauf a = Lesen("ifc2x3_schichten.ifc");
            Assert.False(Hat(a.Meldungen, P + "LESEFEHLER"), string.Join(" | ", a.Meldungen));
            Assert.DoesNotContain(a.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "STOFFWERTE_NICHT_GELESEN");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "IFC2X3", "4", "Beton, Dämmung, Mauerwerk, Putz" }, m.Werte);

            AbbildBauteil sued = Bauteil(a.Abbild, "Süd");
            Assert.All(sued.Aufbau.Schichten, x => Assert.Null(x.LambdaWmK));
            Assert.Equal(Aufbaustatus.Unvollstaendig, sued.Aufbau.Status);
            Assert.False(sued.Aufbau.RichtungAngenommen);   // die Nutzung ist auch in IFC2X3 lesbar
            // Aus einem Aufbau ohne λ ist kein U-Wert zu rechnen — die Gruppe gilt als „ohne U-Wert".
            Assert.Contains(a.Meldungen, x => x.Schluessel == P + "KEIN_UWERT" && x.Werte[0] == GebaeudeZielfelder.U_AUSSENWAND);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.BAUART).Herkunft);
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Nah(85.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(80.0, Wert(s, GebaeudeZielfelder.FLAECHE_DACH));
        }

        [Fact]
        public void Befund_xBIM_bildet_die_Stoffwerte_von_IFC2X3_nicht_ueber_die_Schnittstelle_ab()
        {
            // Hält den gemessenen Stand von xBIM 6.1.605 fest: Ändert er sich, lässt sich die dritte
            // Verzweigung des Lesers (IfcAbbildBauer.Stoffwerte) überprüfen.
            using (var m = new MemoryModel(MemoryModel.GetFactory(XbimSchemaVersion.Ifc2X3), NullLoggerFactory.Instance, 0))
            {
                using (FileStream f = File.OpenRead(Probe("ifc2x3_schichten.ifc")))
                    m.LoadStep21(f, f.Length, null, null);
                List<IIfcMaterialProperties> saetze = m.Instances.OfType<IIfcMaterial>().SelectMany(x => x.HasProperties).ToList();
                Assert.Equal(9, saetze.Count);
                foreach (IIfcMaterialProperties satz in saetze)
                {
                    if (satz.ExpressType.ExpressName == "IfcExtendedMaterialProperties")
                        Assert.ThrowsAny<MissingMethodException>(() => satz.Properties.ToList());
                    else
                        Assert.Empty(satz.Properties);
                }
            }
        }

        // ==================================================================
        //  Vorgabe-Rückfälle
        // ==================================================================

        [Fact]
        public void Rueckfallhaus_Raumhoehe_Dach_Grund_und_Sonstige_fallen_auf_ihre_Vorgaben()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_rueckfaelle.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, g.Geschosse.Select(x => x.Name));
            Nah(70.0, g.Geschosse[1].GrundflaecheM2);
            Nah(2.8, g.Geschosse[1].LageM, 1e-12);
            Assert.Null(g.Geschosse[0].GrundflaecheM2);
            Assert.Equal("2", a.Meldungen.Single(x => x.Schluessel == P + "KEINE_MENGEN").Werte[0]);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Nah(110.0, Wert(s, GebaeudeZielfelder.NUTZFLAECHE));
            Assert.Null(Wert(s, GebaeudeZielfelder.VOLUMEN));

            GebaeudeFeldzeile hoehe = s.Zeile(GebaeudeZielfelder.RAUMHOEHE);
            Nah(2.5, hoehe.Wert);
            Assert.Equal(Importherkunft.Vorgabe, hoehe.Herkunft);
            Assert.Equal("GIMP_BELEG_RAUMHOEHE_VORGABE", hoehe.Beleg.Schluessel);
            Assert.Equal(new[] { "2.5" }, hoehe.Beleg.Werte);
            Nah(2.5, hoehe.VorgabeWert);

            GebaeudeFeldzeile dach = s.Zeile(GebaeudeZielfelder.FLAECHE_DACH);
            Nah(70.0, dach.Wert);
            Assert.Equal(Importherkunft.Vorgabe, dach.Herkunft);
            Assert.Equal("GIMP_BELEG_DACH_VORGABE_GESCHOSS", dach.Beleg.Schluessel);
            Assert.Equal(new[] { "Obergeschoss", "70" }, dach.Beleg.Werte);
            Assert.Equal(PruefStufe.Warnung, dach.Markierung);   // die Mengen fehlen — gelb bleibt
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.U_DACH).Herkunft);
            Nah(GebaeudeVorgaben.Wert('E', GebaeudeZielfelder.U_DACH).Value, Wert(s, GebaeudeZielfelder.U_DACH));

            GebaeudeFeldzeile grund = s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND);
            Nah(55.0, grund.Wert);
            Assert.Equal(Importherkunft.Vorgabe, grund.Herkunft);
            Assert.Equal("GIMP_BELEG_GRUND_VORGABE", grund.Beleg.Schluessel);
            Assert.Equal(new[] { "110", "2" }, grund.Beleg.Werte);
            // Die Bodenplatte ist da (nur ohne Mengen): ihre Randbedingung gilt, keine Vorgabe.
            GebaeudeFeldzeile rand = s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG);
            Assert.Equal(DbWerte.GRUND_ERDREICH, rand.Textwert);
            Assert.Equal(Importherkunft.Ifc, rand.Herkunft);

            Nah(0.0, Wert(s, GebaeudeZielfelder.FLAECHE_SONSTIGE));
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Herkunft);
            Nah(50.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(0.3, Wert(s, GebaeudeZielfelder.U_AUSSENWAND));

            foreach (string f in new[] { GebaeudeZielfelder.RAUMHOEHE, GebaeudeZielfelder.FLAECHE_DACH,
                                         GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.FLAECHE_SONSTIGE })
                Assert.True(s.Zeile(f).Uebernehmen, f);
            Assert.DoesNotContain(GebaeudeImportAblauf.Pruefen(s), x => x.Stufe == PruefStufe.Fehler);
        }

        [Fact]
        public void Das_Dach_nimmt_ohne_Geschossmenge_die_beheizten_Raeume_des_obersten_Geschosses()
        {
            IfcGebaeudeAbbild a = Synthetisch(new IfcGebaeudeAbbild(), lage: true);
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil());

            GebaeudeFeldzeile dach = s.Zeile(GebaeudeZielfelder.FLAECHE_DACH);
            Nah(40.0, dach.Wert);   // nur der beheizte Raum im OG, nicht der unbeheizte
            Assert.Equal(Importherkunft.Vorgabe, dach.Herkunft);
            Assert.Equal("GIMP_BELEG_DACH_VORGABE_RAEUME", dach.Beleg.Schluessel);
            Assert.Equal(new[] { "OG", "1", "40" }, dach.Beleg.Werte);

            // Ohne jedes Bodenbauteil: Grundfläche und Randbedingung sind Vorgaben.
            Nah(50.0, Wert(s, GebaeudeZielfelder.FLAECHE_GRUND));
            GebaeudeFeldzeile rand = s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG);
            Assert.Equal(DbWerte.GRUND_ERDREICH, rand.Textwert);
            Assert.Equal(Importherkunft.Vorgabe, rand.Herkunft);
            Assert.Equal("GIMP_BELEG_RANDBEDINGUNG_VORGABE", rand.Beleg.Schluessel);
            Assert.True(rand.Uebernehmen);
        }

        [Fact]
        public void Eine_Teilsumme_bleibt_stehen_und_ohne_Geschosslage_greift_kein_Rueckfall()
        {
            IfcGebaeudeAbbild a = Synthetisch(new IfcGebaeudeAbbild(), lage: true);
            a.Gebaeude[0].Bauteile.Add(Platte("D1", Bauteilart.Dach, 30.0));
            a.Gebaeude[0].Bauteile.Add(Platte("D2", Bauteilart.Dach, null));
            GebaeudeFeldzeile dach = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil()).Zeile(GebaeudeZielfelder.FLAECHE_DACH);
            Nah(30.0, dach.Wert);
            Assert.Equal(Importherkunft.Ifc, dach.Herkunft);
            Assert.Equal(PruefStufe.Warnung, dach.Markierung);

            // Zwei Geschosse ohne Lage: welches oben liegt, ist offen — die Zeile bleibt, wie sie ist.
            IfcGebaeudeAbbild b = Synthetisch(new IfcGebaeudeAbbild(), lage: false);
            GebaeudeImportSatz sb = GebaeudeAggregation.Bilden(b, 0, 'E', null, new IfcImportProfil());
            Nah(0.0, Wert(sb, GebaeudeZielfelder.FLAECHE_DACH));
            Assert.Equal(Importherkunft.Ifc, sb.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Herkunft);
            Nah(50.0, Wert(sb, GebaeudeZielfelder.FLAECHE_GRUND));   // die Geschosszahl genügt
        }

        [Fact]
        public void Die_Rueckfaelle_gelten_nicht_fuer_gbXML()
        {
            // Dasselbe Gebäude, aber ohne Höhe und Volumen — einmal als IFC, einmal als gbXML.
            IfcGebaeudeAbbild ifc = Synthetisch(new IfcGebaeudeAbbild(), lage: true, hoehe: false);
            GebaeudeImportSatz si = GebaeudeAggregation.Bilden(ifc, 0, 'E', null, new IfcImportProfil());
            Assert.Equal(Importherkunft.Vorgabe, si.Zeile(GebaeudeZielfelder.RAUMHOEHE).Herkunft);
            Nah(2.5, Wert(si, GebaeudeZielfelder.RAUMHOEHE));

            GbxmlAbbild gbxml = Synthetisch(new GbxmlAbbild(), lage: true, hoehe: false);
            GebaeudeImportSatz sg = GebaeudeAggregation.Bilden(gbxml, 0, 'E', null, new GbxmlImportProfil());
            Assert.Null(Wert(sg, GebaeudeZielfelder.RAUMHOEHE));   // NULL = Wert des Gebäudes (Datenaustauschkonzept 3.4)
            Assert.Equal(Importherkunft.Leer, sg.Zeile(GebaeudeZielfelder.RAUMHOEHE).Herkunft);
            foreach (string f in new[] { GebaeudeZielfelder.FLAECHE_DACH, GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.FLAECHE_SONSTIGE })
            {
                Nah(0.0, Wert(sg, f));
                Assert.Equal(Importherkunft.GbXml, sg.Zeile(f).Herkunft);
            }
            Assert.Null(sg.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
        }

        [Fact]
        public void Die_angegebene_Raumhoehe_wird_gegen_das_Volumen_gehalten()
        {
            IfcGebaeudeAbbild a = Synthetisch(new IfcGebaeudeAbbild(), lage: true);
            foreach (AbbildRaum r in a.Gebaeude[0].Raeume) r.VolumenM3 = r.FlaecheM2 * 4.0;   // Höhe 2,5 m angegeben
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil());
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "VOLUMEN_ABWEICHUNG");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "400", "250" }, m.Werte);
            Assert.Equal(PruefStufe.Warnung, s.Zeile(GebaeudeZielfelder.RAUMHOEHE).Markierung);

            // Innerhalb von 20 % bleibt es still; ebenso beim Probenhaus.
            foreach (AbbildRaum r in a.Gebaeude[0].Raeume) r.VolumenM3 = r.FlaecheM2 * 2.9;
            Assert.DoesNotContain(GebaeudeAggregation.Bilden(a, 0, 'E', null, new IfcImportProfil()).Meldungen,
                x => x.Schluessel == G + "VOLUMEN_ABWEICHUNG");
            GebaeudeImportSatz haus = Lesen("ifc4_haus.ifc").Zuordnen(0, null);
            Assert.DoesNotContain(haus.Meldungen, x => x.Schluessel == G + "VOLUMEN_ABWEICHUNG");
            foreach (string f in new[] { GebaeudeZielfelder.RAUMHOEHE, GebaeudeZielfelder.FLAECHE_DACH,
                                         GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.FLAECHE_SONSTIGE })
                Assert.Equal(Importherkunft.Ifc, haus.Zeile(f).Herkunft);
        }

        // ==================================================================
        //  iOS-Dateifilter (Umsetzungskonzept 3.6, Datenaustauschkonzept 2.5)
        // ==================================================================

        [Fact]
        public void Der_iOS_Dateifilter_kennt_die_Endungen_der_Gebaeudeimporte()
        {
            // Die iOS-Schale baut nur auf macOS; gelesen wird deshalb die Tabelle im Quelltext.
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.iOS", "Dienste", "Dateifilter.cs"));
            string code = Regex.Replace(text, @"//[^\r\n]*", "");
            Dictionary<string, string> tabelle = Regex.Matches(code, @"\[""(\.[a-z0-9]+)""\]\s*=\s*""([^""]+)""")
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

            Assert.Equal("public.xml", tabelle[".ifcxml"]);
            Assert.Equal("public.zip-archive", tabelle[".ifczip"]);
            Assert.Equal("public.xml", tabelle[".gbxml"]);
            Assert.Equal("public.xml", tabelle[".xml"]);
            // Für .ifc gibt es keine registrierte Typkennung: public.data bleibt — mit Kommentar.
            Assert.False(tabelle.ContainsKey(".ifc"));
            Assert.Contains("\".ifc\"", text);

            foreach (string filter in new[] { IfcImportProfil.DATEIFILTER, GbxmlImportProfil.DATEIFILTER })
                foreach (Match m in Regex.Matches(filter.Split('|')[1], @"\*(\.[a-z0-9]+)"))
                    Assert.True(m.Groups[1].Value == ".ifc" || tabelle.ContainsKey(m.Groups[1].Value), m.Groups[1].Value);
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static string Probe(string name) => Path.Combine(IfcProbenTests.Ordner(), name);

        private static GebaeudeImportAblauf Lesen(string name)
        {
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(Probe(name)))
                a.Lesen(s, Probe(name), new IfcImportProfil());
            Assert.True(a.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a;
        }

        private static GebaeudeImportAblauf LesenBytes(byte[] daten, string name)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(daten))
                a.Lesen(s, name, new IfcImportProfil());
            Assert.True(a.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a;
        }

        private static double? Wert(GebaeudeImportSatz s, string feld) => s.Zeile(feld).Wert;

        private static bool Hat(IEnumerable<PruefMeldung> meldungen, string schluessel)
            => meldungen.Any(m => m.Schluessel == schluessel);

        private static void Nah(double erwartet, double? ist, double toleranz = 1e-9)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= toleranz, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        private static AbbildBauteil Bauteil(GebaeudeAbbild a, string name)
            => a.Gebaeude.SelectMany(g => g.Bauteile).Single(b => b.Name == name);

        /// <summary>
        /// Ein Abbild ohne Datei: zwei Geschosse (EG, OG — mit oder ohne Lage), im EG ein beheizter Raum
        /// (60 m²), im OG ein beheizter (40 m²) und ein unbeheizter (10 m²), Höhe 2,5 m, dazu eine Außenwand
        /// ohne Raumgrenze (50 m², U 0,3); kein Dach, kein Boden, keine Tür.
        /// </summary>
        private static T Synthetisch<T>(T a, bool lage, bool hoehe = true) where T : GebaeudeAbbild
        {
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Synthetisch", Quelltyp = "IfcBuilding" };
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "S0", Name = "EG", LageM = lage ? 0.0 : null });
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "S1", Name = "OG", LageM = lage ? 3.0 : null });
            foreach ((string k, string s, double f, bool b) in new[] { ("R1", "S0", 60.0, true), ("R2", "S1", 40.0, true), ("R3", "S1", 10.0, false) })
                g.Raeume.Add(new AbbildRaum
                {
                    Kennung = k, Quelltyp = "IfcSpace", Name = k, GeschossKennung = s, FlaecheM2 = f,
                    HoeheM = hoehe ? 2.5 : null, Beheizt = b, BeheiztQuelle = b ? BeheiztQuelle.Annahme : BeheiztQuelle.Name,
                });
            g.Bauteile.Add(new AbbildBauteil
            {
                Kennung = "W1", Name = "W1", Quelltyp = "IfcWall", Art = Bauteilart.Aussenwand, Randbedingung = Randbedingung.Aussenluft,
                BruttoflaecheM2 = 50.0, UWertWm2K = 0.3, NeigungGrad = 90, AzimutGrad = 180, HuelleOhneNachbar = true,
            });
            a.Gebaeude.Add(g);
            return a;
        }

        private static AbbildBauteil Platte(string name, Bauteilart art, double? flaeche) => new AbbildBauteil
        {
            Kennung = name, Name = name, Quelltyp = "IfcSlab", Art = art, Randbedingung = Randbedingung.Aussenluft,
            BruttoflaecheM2 = flaeche, UWertWm2K = 0.2, NeigungGrad = 0, HuelleOhneNachbar = true,
        };

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
