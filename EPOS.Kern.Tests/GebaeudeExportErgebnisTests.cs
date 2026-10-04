using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;
using Xunit.Abstractions;
using M = Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7c, Teil 2 — Ergebnisse, weitere Abbildfelder und Beipackzettel im Gebäudeexport</b>
    /// (Datenaustauschkonzept 5.6, 6.3, 6.4; Kühlkonzept 9.2): Der Ablauf trägt die Jahresergebnisse des
    /// letzten Rechenlaufs je Gebäude und Zone in das Abbild; daraus entstehen <c>EPOS_Ergebnis</c> (IFC,
    /// jede Energie in KILOWATTHOUR) und <c>Results</c> (gbXML, mit <c>CoolingLoad</c>). Ohne Rechenlauf
    /// bleibt alles wie zuvor — byte-gleich.
    /// </summary>
    public sealed class GebaeudeExportErgebnisTests : IDisposable
    {
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private static readonly DateTime LAUF = new DateTime(2026, 9, 30, 14, 5, 0);
        private const string WETTER = "Probenregion TMY";
        private const int ANBAU = 602;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _aus;

        public GebaeudeExportErgebnisTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Vorrichtung
        // ==================================================================

        /// <summary>Zwei Zonen (Wohnen gekühlt, Anbau ungekühlt) mit einer Trennfläche.</summary>
        private static GebaeudeExportSatz Zweizonig(bool kuehlbetrieb = true)
        {
            ZoneModel a = ExportSatzProbe.Schichtenhaus();
            a.Nutzflaeche = 80.0;
            var b = new ZoneModel { ID = ANBAU, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Anbau", Nutzflaeche = 40.0,
                                    IstBeheizt = true, Kuehlung_Aktiv = false };
            a.Bauteile.Add(ExportSatzProbe.B(1013, DbWerte.BAUTEILART_INNENWAND, 12.0, DbWerte.RANDBEDINGUNG_ZONE,
                                             ExportSatzProbe.AUFBAU_DECKE, neigung: 90.0, nachbarzone: ANBAU));
            return ExportSatzProbe.Satz(a, weitere: new[] { b }, kuehlbetrieb: kuehlbetrieb);
        }

        /// <summary>Ein gesätes Ergebnis wie aus <c>Tab_ErgebnisGebaeude</c>/<c>Tab_ErgebnisZone</c> (MWh, kW).</summary>
        private static ErgebnisGebaeudeModel Ergebnis(bool zonen = true)
        {
            var e = new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = ExportSatzProbe.GEB, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
                HeizwaermeMwh = 12.5, SpitzeKw = 8.25, MittlereRaumtemperaturC = 20.75, KuehlenergieMwh = 3.5,
            };
            if (zonen)
            {
                e.Zonen.Add(new ErgebnisZoneModel { ID_Zone = ExportSatzProbe.ZONE, Rang = 1, IstBeheizt = true,
                                                    HeizwaermeMwh = 9.0, SpitzeKw = 6.0, MittlereRaumtemperaturC = 21.0, KuehlenergieMwh = 3.5 });
                e.Zonen.Add(new ErgebnisZoneModel { ID_Zone = ANBAU, Rang = 2, IstBeheizt = true,
                                                    HeizwaermeMwh = 3.5, SpitzeKw = 2.5, MittlereRaumtemperaturC = 20.0, KuehlenergieMwh = 1.25 });
            }
            return e;
        }

        private static GebaeudeExportPlan Plan(GebaeudeExportSatz satz, GebaeudeExportProfil profil) => ExportSatzProbe.Plan(satz, profil);

        private static byte[] Datei(GebaeudeExportSatz satz, GebaeudeExportProfil profil, out GebaeudeExportBilanz bilanz)
        {
            GebaeudeExportPlan plan = Plan(satz, profil);
            using (var ziel = new MemoryStream())
            {
                bilanz = new GebaeudeExportAblauf().Schreiben(plan, ziel, profil, System.Threading.CancellationToken.None);
                return ziel.ToArray();
            }
        }

        // ==================================================================
        //  Das Abbild
        // ==================================================================

        [Fact]
        public void Ablauf_traegt_Gebaeude_und_Zonenergebnisse_in_kWh_und_W()
        {
            GebaeudeExportPlan plan = Plan(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), GbxmlExportProbe.Profil());
            AbbildGebaeude g = plan.Abbild.Gebaeude.Single();
            Assert.NotNull(g.Ergebnis);
            Assert.Equal(12500.0, g.Ergebnis.EnergieKWh.Value, 9);
            Assert.Equal(8250.0, g.Ergebnis.HeizlastW.Value, 9);
            Assert.Equal(20.75, g.Ergebnis.MitteltemperaturC);
            Assert.Equal(3500.0, g.Ergebnis.KaeltebedarfKWh.Value, 9);
            Assert.Null(g.Ergebnis.KaeltelastW);             // das Ergebnis speichert keine Kältelast
            Assert.Equal(LAUF, g.Ergebnis.Rechenzeitpunkt);
            Assert.Equal(WETTER, g.Ergebnis.Wetterdatensatz);
            Assert.Equal(new DateTime(2026, 1, 1), g.Ergebnis.Beginn);

            AbbildRaum wohnen = g.Raeume.Single(r => r.Name == "Wohnen");
            AbbildRaum anbau = g.Raeume.Single(r => r.Name == "Anbau");
            Assert.Equal(9000.0, wohnen.Ergebnis.EnergieKWh.Value, 9);
            Assert.Equal(6000.0, wohnen.Ergebnis.HeizlastW.Value, 9);
            Assert.Equal(3500.0, wohnen.Ergebnis.KaeltebedarfKWh.Value, 9);
            Assert.Equal(3500.0, anbau.Ergebnis.EnergieKWh.Value, 9);
            Assert.Null(anbau.Ergebnis.KaeltebedarfKWh);     // ungekühlt: die Kühlenergie wäre Überwärme, kein Kältebedarf
            Assert.All(g.Raeume.Where(r => !r.Beheizt), r => Assert.Null(r.Ergebnis));
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_KAELTE);
        }

        [Fact]
        public void Ohne_Kuehlbetrieb_kein_Kaeltebedarf_und_kein_Hinweis()
        {
            GebaeudeExportPlan plan = Plan(Zweizonig(kuehlbetrieb: false).MitErgebnis(Ergebnis(), LAUF, WETTER), GbxmlExportProbe.Profil());
            AbbildGebaeude g = plan.Abbild.Gebaeude.Single();
            Assert.Null(g.Ergebnis.KaeltebedarfKWh);
            Assert.All(g.Raeume.Where(r => r.Ergebnis != null), r => Assert.Null(r.Ergebnis.KaeltebedarfKWh));
            Assert.DoesNotContain(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_KAELTE);
        }

        [Fact]
        public void Einzonig_traegt_der_beheizte_Raum_die_Gebaeudewerte()
        {
            GebaeudeExportSatz satz = ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()).MitErgebnis(Ergebnis(zonen: false), LAUF, WETTER);
            AbbildGebaeude g = Plan(satz, GbxmlExportProbe.Profil()).Abbild.Gebaeude.Single();
            AbbildRaum wohnen = g.Raeume.Single(r => r.Name == "Wohnen");
            Assert.Equal(12500.0, wohnen.Ergebnis.EnergieKWh.Value, 9);
            Assert.Null(wohnen.Ergebnis.Rechenzeitpunkt);
            Assert.Null(g.Raeume.Single(r => !r.Beheizt).Ergebnis);   // der Platzhalter „unbeheizt"
        }

        [Fact]
        public void Ohne_Rechenlauf_bleibt_das_Abbild_ohne_Ergebnis()
        {
            GebaeudeExportPlan plan = Plan(Zweizonig(), GbxmlExportProbe.Profil());
            Assert.Null(plan.Abbild.Gebaeude[0].Ergebnis);
            Assert.All(plan.Abbild.Gebaeude[0].Raeume, r => Assert.Null(r.Ergebnis));
            Assert.Null(IfcErgebnisse.AusAbbild(plan.Abbild));
            Assert.Null(Zweizonig().MitErgebnis(null, LAUF, WETTER).Rechenzeitpunkt);
        }

        [Fact]
        public void Weitere_Abbildfelder_aus_Gebaeude_Zone_und_Bauteil()
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Bauteile.Single(b => b.ID == 1001).Psi_L = 3.5;
            ProjektGebaeudeModel geb = ExportSatzProbe.Gebaeude();
            geb.Baujahr = 1975;
            geb.Baualtersklasse = " E ";
            geb.Luftwechsel_Nutzer = 0.4;
            GebaeudeExportSatz satz = MitKlimaort(ExportSatzProbe.Satz(z, gebaeude: geb), 51.05, 13.74);
            GebaeudeExportPlan plan = Plan(satz, IfcExportProbe.Profil());
            AbbildGebaeude g = plan.Abbild.Gebaeude.Single();
            Assert.Equal(1975, g.Baujahr);
            Assert.Equal("E", g.Baualtersklasse);
            Assert.Equal(51.05, plan.Abbild.BreiteGrad);
            Assert.Equal(13.74, plan.Abbild.LaengeGrad);
            AbbildRaum wohnen = g.Raeume.Single(r => r.Name == "Wohnen");
            Assert.Equal(2.5, wohnen.HoeheM);
            Assert.True(wohnen.Nachtabsenkung);              // 16 °C nachts unter 20 °C am Tag
            Assert.Equal(0.4, wohnen.LuftwechselNutzerJeH);
            Assert.Null(g.Raeume.Single(r => !r.Beheizt).Nachtabsenkung);
            Assert.Equal(3.5, g.Bauteile.Single(b => b.Kennung.EndsWith("1001", StringComparison.Ordinal)).WaermebrueckeWK);
            Assert.All(g.Bauteile.Where(b => !b.Kennung.EndsWith("1001", StringComparison.Ordinal)), b => Assert.Null(b.WaermebrueckeWK));

            byte[] datei = IfcExportProbe.Schreiben(plan.Abbild, IfcExportProbe.Profil());
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcBuilding b = m.Instances.OfType<IIfcBuilding>().Single();
                Dictionary<string, Dictionary<string, IIfcPropertySingleValue>> e = IfcExportProbe.Eigenschaften(b);
                Assert.Equal("1975", e["Pset_BuildingCommon"]["YearOfConstruction"].NominalValue.ToString());
                Assert.Equal("E", e[IfcSchreiber.EPOS_GEBAEUDE]["Baualtersklasse"].NominalValue.ToString());
                IIfcSite site = m.Instances.OfType<IIfcSite>().Single();
                Assert.NotNull(site.RefLatitude);
                Assert.Equal(51L, ((IEnumerable<long>)site.RefLatitude.Value.Value).First());
                IIfcSpace raum = m.Instances.OfType<IIfcSpace>().Single(s => s.LongName == "Wohnen");
                Dictionary<string, IIfcPropertySingleValue> soll = IfcExportProbe.Eigenschaften(raum)["Pset_SpaceThermalRequirements"];
                Assert.Equal(true, soll["DiscontinuedHeating"].NominalValue.Value);
                Assert.IsType<M.IfcNumericMeasure>(soll["NaturalVentilationRate"].NominalValue);
                Assert.Equal(0.4, IfcExportProbe.Zahl(soll["NaturalVentilationRate"]), 12);
                Assert.Equal(2.5, IfcExportProbe.Mengen(raum, "Qto_SpaceBaseQuantities")["Height"], 12);
                IIfcWall wand = m.Instances.OfType<IIfcWall>().Single(w => w.Tag.ToString().EndsWith("1001", StringComparison.Ordinal));
                IIfcPropertySingleValue ua = IfcExportProbe.Eigenschaften(wand)[IfcSchreiber.EPOS_BAUTEIL]["WaermebrueckeUA"];
                Assert.Equal(3.5, IfcExportProbe.Zahl(ua), 12);
                Assert.Contains("W/K", ua.Description.ToString(), StringComparison.Ordinal);
            }
        }

        /// <summary>Derselbe Satz mit einem Klimaort.</summary>
        private static GebaeudeExportSatz MitKlimaort(GebaeudeExportSatz s, double breite, double laenge) => new GebaeudeExportSatz
        {
            IdProjekt = s.IdProjekt, IdZ = s.IdZ, Gebaeude = s.Gebaeude, Zonen = s.Zonen, Aufbauten = s.Aufbauten, Baustoffe = s.Baustoffe,
            Kuehlbetrieb = s.Kuehlbetrieb, Klimaregion = s.Klimaregion, Plz = s.Plz, BreiteGrad = breite, LaengeGrad = laenge,
        };

        // ==================================================================
        //  IFC: EPOS_Ergebnis am Gebäude und je Zone
        // ==================================================================

        [Fact]
        public void Ifc_EPOS_Ergebnis_am_Gebaeude_und_je_Zone_mit_KILOWATTHOUR_und_Hinweis()
        {
            GebaeudeExportProfil profil = IfcExportProbe.Profil();
            byte[] datei = Datei(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), profil, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.OHNE_ERGEBNIS);
            string hinweis = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_DATEI_KAELTE_SENSIBEL", CultureInfo.GetCultureInfo("de-DE"));
            Assert.Contains("ohne Entfeuchtung", hinweis, StringComparison.Ordinal);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcConversionBasedUnit kwh = m.Instances.OfType<IIfcConversionBasedUnit>().Single(u => u.Name == IfcSchreiber.KILOWATTSTUNDE);
                IIfcBuilding b = m.Instances.OfType<IIfcBuilding>().Single();
                Dictionary<string, IIfcPropertySingleValue> eg = IfcExportProbe.Eigenschaften(b)[IfcSchreiber.EPOS_ERGEBNIS];
                Assert.Equal(12500.0, IfcExportProbe.Zahl(eg["Heizwaermebedarf"]), 9);
                Assert.Equal(8250.0, IfcExportProbe.Zahl(eg["Heizlast"]), 9);
                Assert.Equal(3500.0, IfcExportProbe.Zahl(eg["Kaeltebedarf"]), 9);
                Assert.Equal(hinweis, eg["Kaeltebedarf"].Description.ToString());
                Assert.False(eg.ContainsKey("Kaeltelast"));
                Dictionary<string, IIfcPropertySingleValue> lauf = IfcExportProbe.Eigenschaften(b)[IfcSchreiber.EPOS_RECHENLAUF];
                Assert.Equal("2026-09-30T14:05:00", lauf["Rechenzeitpunkt"].NominalValue.ToString());
                Assert.Equal(WETTER, lauf["Wetterdatensatz"].NominalValue.ToString());

                var erwartet = new Dictionary<string, (double Heiz, double Last, double? Kaelte)>
                {
                    ["Wohnen"] = (9000.0, 6000.0, 3500.0),
                    ["Anbau"] = (3500.0, 2500.0, null),
                };
                foreach (KeyValuePair<string, (double Heiz, double Last, double? Kaelte)> z in erwartet)
                {
                    IIfcSpace raum = m.Instances.OfType<IIfcSpace>().Single(s => s.LongName == z.Key);
                    Dictionary<string, IIfcPropertySingleValue> er = IfcExportProbe.Eigenschaften(raum)[IfcSchreiber.EPOS_ERGEBNIS];
                    Assert.Equal(z.Value.Heiz, IfcExportProbe.Zahl(er["Heizwaermebedarf"]), 9);
                    Assert.Equal(z.Value.Last, IfcExportProbe.Zahl(er["Heizlast"]), 9);
                    Assert.Equal(z.Value.Kaelte.HasValue, er.ContainsKey("Kaeltebedarf"));
                    if (z.Value.Kaelte.HasValue) Assert.Equal(z.Value.Kaelte.Value, IfcExportProbe.Zahl(er["Kaeltebedarf"]), 9);
                }
                // Jede Energie mit KILOWATTHOUR (6.4), jede Leistung ohne Unit (W ist die globale Einheit).
                List<IIfcPropertySingleValue> werte = m.Instances.OfType<IIfcPropertySingleValue>().ToList();
                List<IIfcPropertySingleValue> energie = werte.Where(p => p.NominalValue is M.IfcEnergyMeasure).ToList();
                Assert.Equal(5, energie.Count);   // Gebäude 2, Wohnen 2 (Heizwärme, Kälte), Anbau 1
                Assert.All(energie, p => Assert.Same(kwh, p.Unit));
                Assert.All(werte.Where(p => p.NominalValue is M.IfcPowerMeasure), p => Assert.Null(p.Unit));
            }
        }

        [Fact]
        public void Ohne_Rechenlauf_schreibt_der_Ablauf_IFC_wie_der_Schreiber_ohne_Ergebnisse()
        {
            GebaeudeExportProfil profil = IfcExportProbe.Profil();
            GebaeudeExportSatz satz = Zweizonig();
            byte[] ablauf = Datei(satz, profil, out GebaeudeExportBilanz bilanz);
            byte[] direkt = IfcExportProbe.Schreiben(Plan(satz, profil).Abbild, profil, null);
            Assert.Equal(direkt, ablauf);
            Assert.Contains(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.OHNE_ERGEBNIS);
            Assert.DoesNotContain("'EPOS_Ergebnis'", Encoding.ASCII.GetString(ablauf));
        }

        // ==================================================================
        //  gbXML: Results mit CoolingLoad
        // ==================================================================

        [Fact]
        public void Gbxml_Results_je_Zone_mit_CoolingLoad_in_KilowattHours()
        {
            byte[] datei = Datei(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), GbxmlExportProbe.Profil(), out _);
            XDocument d = XDocument.Load(new MemoryStream(datei));
            List<XElement> results = d.Root.Elements(NS + "Results").ToList();
            Assert.Equal(7, results.Count);   // je Zone Energy, HeatLoad, DryBulbTemperature; CoolingLoad nur am gekühlten Wohnen
            XElement kaelte = Assert.Single(results, r => (string)r.Attribute("resultsType") == "CoolingLoad");
            Assert.Equal("KilowattHours", (string)kaelte.Attribute("unit"));
            Assert.Equal(GbxmlSchreiber.ERGEBNIS_TRAEGER_KAELTE, (string)kaelte.Attribute("resourceType"));
            Assert.Equal("Simulated", (string)kaelte.Attribute("valueType"));
            Assert.Contains("ohne Entfeuchtung", (string)kaelte.Element(NS + "Description"), StringComparison.Ordinal);
            Assert.Equal(3500.0, double.Parse((string)kaelte.Element(NS + "Value"), CultureInfo.InvariantCulture), 9);
            Assert.All(results.Where(r => (string)r.Attribute("resultsType") != "CoolingLoad"),
                       r => Assert.Equal(GbxmlSchreiber.ERGEBNIS_TRAEGER, (string)r.Attribute("resourceType")));
            Assert.Equal(2, results.Count(r => (string)r.Attribute("resultsType") == "Energy" && (string)r.Attribute("unit") == "KilowattHours"));
        }

        [Fact]
        public void Gbxml_Kaeltelast_als_CoolingLoad_in_Watt_wenn_das_Abbild_sie_traegt()
        {
            GebaeudeAbbild abbild = Plan(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), GbxmlExportProbe.Profil()).Abbild;
            abbild.Gebaeude[0].Raeume.Single(r => r.Name == "Wohnen").Ergebnis.KaeltelastW = 4200.0;
            XDocument d = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(abbild)));
            XElement last = Assert.Single(d.Root.Elements(NS + "Results"),
                                          r => (string)r.Attribute("resultsType") == "CoolingLoad" && (string)r.Attribute("unit") == "Watt");
            Assert.Equal("4200", (string)last.Element(NS + "Value"));
        }

        [Fact]
        public void Gbxml_mit_Results_besteht_die_Schemapruefung()
        {
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema == null)
            {
                _aus.WriteLine("Schemaprüfung übersprungen: Referenzlaeufe/Schemakopien/" + GbxmlExportProbe.SCHEMAKOPIE + " liegt nicht bei.");
                return;
            }
            GebaeudeAbbild abbild = Plan(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), GbxmlExportProbe.Profil()).Abbild;
            abbild.Gebaeude[0].Raeume.Single(r => r.Name == "Wohnen").Ergebnis.KaeltelastW = 4200.0;
            string[] befunde = GbxmlExportProbe.Schemapruefung(schema, GbxmlExportProbe.Schreiben(abbild));
            Assert.True(befunde.Length == 0, string.Join("\n", befunde));
        }

        /// <summary>
        /// Ohne Rechenlauf ist die gbXML-Datei byte-gleich mit der Datei desselben Projekts mit Rechenlauf,
        /// aus der die <c>Results</c> gestrichen sind — die Ergebnisse ändern nichts außer ihren eigenen Elementen,
        /// und alle übrigen neuen Abbildfelder schreibt der gbXML-Schreiber nicht.
        /// </summary>
        [Fact]
        public void Gbxml_ohne_Rechenlauf_byte_gleich_bis_auf_die_Results()
        {
            GebaeudeExportProfil profil = GbxmlExportProbe.Profil();
            string ohne = Encoding.UTF8.GetString(Datei(Zweizonig(), profil, out _));
            string mit = Encoding.UTF8.GetString(Datei(Zweizonig().MitErgebnis(Ergebnis(), LAUF, WETTER), profil, out _));
            Assert.Contains("<Results", mit, StringComparison.Ordinal);
            Assert.DoesNotContain("<Results", ohne, StringComparison.Ordinal);
            string gestrichen = Regex.Replace(mit, @"\s*<Results\b.*?</Results>", "", RegexOptions.Singleline);
            Assert.Equal(ohne, gestrichen);
        }

        // ==================================================================
        //  Beipackzettel
        // ==================================================================

        [Fact]
        public void Beipackzettel_steht_in_der_Exportbilanz_beider_Sprachen()
        {
            GebaeudeExportPlan ifc = Plan(Zweizonig(), IfcExportProbe.Profil());
            string[] beipack = { GebaeudeExportAblauf.BEIPACK_RAUMGRENZEN, GebaeudeExportAblauf.BEIPACK_OHNE_MVD, GebaeudeExportAblauf.BEIPACK_IDS };
            foreach (string s in beipack) Assert.Contains(ifc.Meldungen, m => m.Schluessel == s && m.Stufe == PruefStufe.Info);
            Assert.Contains(ifc.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_IDS && m.Werte.Contains(GebaeudeExportAblauf.IDS_DATEI));
            Assert.DoesNotContain(ifc.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_KAELTE);

            GebaeudeExportPlan gbxml = Plan(Zweizonig(), GbxmlExportProbe.Profil());
            foreach (string s in beipack) Assert.DoesNotContain(gbxml.Meldungen, m => m.Schluessel == s);

            foreach (string sprache in new[] { "de-DE", "en-US" })
                foreach (string s in beipack.Append(GebaeudeExportAblauf.BEIPACK_KAELTE).Append("GEXP_DATEI_KAELTE_SENSIBEL"))
                {
                    string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, CultureInfo.GetCultureInfo(sprache));
                    Assert.False(string.IsNullOrWhiteSpace(text), s + " fehlt in " + sprache);
                }
            Assert.Contains("{0}", WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(GebaeudeExportAblauf.BEIPACK_IDS, CultureInfo.GetCultureInfo("en-US")), StringComparison.Ordinal);
            Assert.NotEqual(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(GebaeudeExportAblauf.BEIPACK_OHNE_MVD, CultureInfo.GetCultureInfo("de-DE")),
                            WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(GebaeudeExportAblauf.BEIPACK_OHNE_MVD, CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
