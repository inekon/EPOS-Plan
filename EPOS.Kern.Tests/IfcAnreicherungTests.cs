using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7d — die Round-Trip-Anreicherung</b> (Datenaustauschkonzept 6.6, Probe 13): die drei Sperren
    /// je benannt mit Angebot, Ergänzen statt Doppeln, Kennungen (vorhandene unverändert, neue deterministisch),
    /// Kennung der Datei, Baustoffe nur ohne Zuordnung, Validator, Rundlauf über den eigenen Leser und die
    /// unveränderte Geometrie. Die Testdatei baut <see cref="Fremdhaus"/> selbst (8.3); Importquelle und
    /// Zuordnungen sind gesät, wie sie <see cref="GebaeudeImportCtrl"/> speichert.
    /// </summary>
    public sealed class IfcAnreicherungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Vorrichtung: die Fremddatei
        // ==================================================================

        internal const string G_GEBAEUDE = "2Fremd0Gebaeude000001$";
        internal const string G_WOHNEN = "2Fremd0Raum00000Wohnen";
        internal const string G_KUECHE = "2Fremd0Raum00000Kueche";
        internal const string G_WAND_U = "2Fremd0Wand000000MitU0";
        internal const string G_WAND_OHNE_U = "2Fremd0Wand00000OhneU0";
        internal const string G_WAND_MATERIAL = "2Fremd0Wand00Material0";
        internal const string G_FENSTER = "2Fremd0Fenster00000001";
        internal const string ORIGINATING = "Fremd-CAD Probe 1.0";
        internal const string PREPROCESSOR = "Fremd-CAD Schreiber 7";

        private const int GEB = 7, ZONE_W = 11, ZONE_K = 12, WAND_U = 101, WAND_OHNE_U = 102, WAND_MAT = 103, FENSTER = 104;

        /// <summary>Geometrietypen, deren Instanzen die Anreicherung nie anfassen darf.</summary>
        private static readonly string[] Geometrietypen =
        {
            "IFCEXTRUDEDAREASOLID", "IFCRECTANGLEPROFILEDEF", "IFCSHAPEREPRESENTATION", "IFCPRODUCTDEFINITIONSHAPE",
            "IFCLOCALPLACEMENT", "IFCAXIS2PLACEMENT3D", "IFCAXIS2PLACEMENT2D", "IFCCARTESIANPOINT", "IFCDIRECTION",
            "IFCGEOMETRICREPRESENTATIONCONTEXT",
        };

        /// <summary>
        /// <b>Das Fremdhaus</b> (IFC4, Längen in mm, Geschichte „Fremd-CAD"): Site, Building, Geschoss, zwei Räume;
        /// Wand mit U (<c>Pset_WallCommon</c> mit ThermalTransmittance 1,5 und IsExternal, Mengensatz 20 m²),
        /// Wand ohne U (<c>Pset_WallCommon</c> nur mit IsExternal), Wand mit Materialzuordnung, ein Fenster ohne
        /// Sätze; Raumgrenzen 1. Ebene. Mit <paramref name="geometrie"/> trägt die Wand mit U einen Quader
        /// (<c>IfcExtrudedAreaSolid</c>) samt Platzierung. Mit <paramref name="geteilt"/> teilen beide Wände ohne
        /// Materialzuordnung einen <c>Pset_WallCommon</c> (eine Beziehung, zwei Objekte).
        /// </summary>
        internal static byte[] Fremdhaus(bool geometrie = false, string schema = null, bool geteilt = false)
        {
            using (var m = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4(), NullLoggerFactory.Instance, 0))
            {
                using (ITransaction t = m.BeginTransaction("Fremdhaus"))
                {
                    var org = m.Instances.New<IfcOrganization>(o => o.Name = "Fremd-CAD GmbH");
                    var app = m.Instances.New<IfcApplication>(a =>
                    {
                        a.ApplicationDeveloper = org;
                        a.Version = "1.0";
                        a.ApplicationFullName = "Fremd-CAD";
                        a.ApplicationIdentifier = "FremdCAD";
                    });
                    var person = m.Instances.New<IfcPerson>(p => p.FamilyName = "Architektin");
                    var nutzer = m.Instances.New<IfcPersonAndOrganization>(n => { n.ThePerson = person; n.TheOrganization = org; });
                    var geschichte = m.Instances.New<IfcOwnerHistory>(h =>
                    {
                        h.OwningUser = nutzer;
                        h.OwningApplication = app;
                        h.CreationDate = new IfcTimeStamp(1758758400);
                    });
                    int lauf = 0;
                    T W<T>(string id, string name) where T : IfcRoot, IInstantiableEntity
                        => m.Instances.New<T>(x =>
                        {
                            x.GlobalId = new IfcGloballyUniqueId(id ?? IfcGloballyUniqueId.ConvertToBase64(
                                new Guid("00000000-0000-4000-8000-" + (++lauf).ToString("D12", CultureInfo.InvariantCulture))));
                            x.OwnerHistory = geschichte;
                            x.Name = name;
                        });

                    var einheiten = m.Instances.New<IfcUnitAssignment>(u => u.Units.AddRange(new IfcUnit[]
                    {
                        m.Instances.New<IfcSIUnit>(e => { e.UnitType = IfcUnitEnum.LENGTHUNIT; e.Name = IfcSIUnitName.METRE; e.Prefix = IfcSIPrefix.MILLI; }),
                        m.Instances.New<IfcSIUnit>(e => { e.UnitType = IfcUnitEnum.AREAUNIT; e.Name = IfcSIUnitName.SQUARE_METRE; }),
                        m.Instances.New<IfcSIUnit>(e => { e.UnitType = IfcUnitEnum.VOLUMEUNIT; e.Name = IfcSIUnitName.CUBIC_METRE; }),
                    }));
                    IfcGeometricRepresentationContext kontext = m.Instances.New<IfcGeometricRepresentationContext>(k =>
                    {
                        k.ContextType = "Model";
                        k.CoordinateSpaceDimension = 3;
                        k.Precision = 1e-5;
                        k.WorldCoordinateSystem = m.Instances.New<IfcAxis2Placement3D>(a => a.Location = Punkt(m, 0, 0, 0));
                        k.TrueNorth = m.Instances.New<IfcDirection>(d => d.SetXY(0, 1));
                    });
                    var projekt = W<IfcProject>("2Fremd0Projekt00000001", "Fremdprojekt");
                    projekt.UnitsInContext = einheiten;
                    projekt.RepresentationContexts.Add(kontext);
                    var site = W<IfcSite>(null, "Grundstück");
                    var gebaeude = W<IfcBuilding>(G_GEBAEUDE, "Fremdhaus");
                    var geschoss = W<IfcBuildingStorey>(null, "Erdgeschoss");
                    geschoss.Elevation = 0.0;
                    var wohnen = W<IfcSpace>(G_WOHNEN, "0.01");
                    wohnen.LongName = "Wohnen";
                    var kueche = W<IfcSpace>(G_KUECHE, "0.02");
                    kueche.LongName = "Küche";
                    void Teile(IfcObjectDefinition ganzes, params IfcObjectDefinition[] teile)
                    {
                        var r = W<IfcRelAggregates>(null, null);
                        r.RelatingObject = ganzes;
                        r.RelatedObjects.AddRange(teile);
                    }
                    Teile(projekt, site);
                    Teile(site, gebaeude);
                    Teile(gebaeude, geschoss);
                    Teile(geschoss, wohnen, kueche);

                    var wandU = W<IfcWall>(G_WAND_U, "Außenwand Süd");
                    var wandOhneU = W<IfcWall>(G_WAND_OHNE_U, "Außenwand Ost");
                    var wandMat = W<IfcWall>(G_WAND_MATERIAL, "Außenwand Nord");
                    var fenster = W<IfcWindow>(G_FENSTER, "Fenster Süd");
                    var enthalten = W<IfcRelContainedInSpatialStructure>(null, null);
                    enthalten.RelatingStructure = geschoss;
                    enthalten.RelatedElements.AddRange(new IfcProduct[] { wandU, wandOhneU, wandMat, fenster });

                    IfcPropertySet Satz(IfcObject o, string name, params IfcProperty[] p)
                    {
                        var s = W<IfcPropertySet>(null, name);
                        s.HasProperties.AddRange(p);
                        var r = W<IfcRelDefinesByProperties>(null, null);
                        r.RelatingPropertyDefinition = s;
                        r.RelatedObjects.Add(o);
                        return s;
                    }
                    IfcPropertySingleValue Wert(string name, IfcValue v) => m.Instances.New<IfcPropertySingleValue>(p => { p.Name = name; p.NominalValue = v; });
                    IfcPropertySet wc = Satz(wandU, "Pset_WallCommon", Wert("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.5)),
                                             Wert("IsExternal", new IfcBoolean(true)), Wert("Reference", new IfcIdentifier("AW-1")));
                    if (geteilt) wc.DefinesOccurrence.Single().RelatedObjects.Add(wandOhneU);
                    else Satz(wandOhneU, "Pset_WallCommon", Wert("IsExternal", new IfcBoolean(true)));
                    var qto = W<IfcElementQuantity>(null, "Qto_WallBaseQuantities");
                    qto.Quantities.Add(m.Instances.New<IfcQuantityArea>(q => { q.Name = "GrossSideArea"; q.AreaValue = 20.0; }));
                    var qrel = W<IfcRelDefinesByProperties>(null, null);
                    qrel.RelatingPropertyDefinition = qto;
                    qrel.RelatedObjects.Add(wandU);

                    var mauerwerk = m.Instances.New<IfcMaterial>(x => x.Name = "Mauerwerk der Architektin");
                    var mrel = W<IfcRelAssociatesMaterial>(null, null);
                    mrel.RelatingMaterial = mauerwerk;
                    mrel.RelatedObjects.Add(wandMat);

                    foreach (var (el, raum) in new (IfcElement, IfcSpace)[] { (wandU, wohnen), (wandOhneU, kueche), (wandMat, wohnen), (fenster, wohnen) })
                    {
                        var g = W<IfcRelSpaceBoundary>(null, "1stLevel");
                        g.RelatingSpace = raum;
                        g.RelatedBuildingElement = el;
                        g.PhysicalOrVirtualBoundary = IfcPhysicalOrVirtualEnum.PHYSICAL;
                        g.InternalOrExternalBoundary = IfcInternalOrExternalEnum.EXTERNAL;
                    }

                    if (geometrie)
                    {
                        wandU.ObjectPlacement = m.Instances.New<IfcLocalPlacement>(p =>
                            p.RelativePlacement = m.Instances.New<IfcAxis2Placement3D>(a => a.Location = Punkt(m, 0, 0, 0)));
                        var profil = m.Instances.New<IfcRectangleProfileDef>(r =>
                        {
                            r.ProfileType = IfcProfileTypeEnum.AREA;
                            r.XDim = 8000;
                            r.YDim = 300;
                            r.Position = m.Instances.New<IfcAxis2Placement2D>(a => a.Location = m.Instances.New<IfcCartesianPoint>(c => c.SetXY(4000, 150)));
                        });
                        var koerper = m.Instances.New<IfcExtrudedAreaSolid>(e =>
                        {
                            e.SweptArea = profil;
                            e.Position = m.Instances.New<IfcAxis2Placement3D>(a => a.Location = Punkt(m, 0, 0, 0));
                            e.ExtrudedDirection = m.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                            e.Depth = 2500;
                        });
                        var darstellung = m.Instances.New<IfcShapeRepresentation>(s =>
                        {
                            s.ContextOfItems = kontext;
                            s.RepresentationIdentifier = "Body";
                            s.RepresentationType = "SweptSolid";
                            s.Items.Add(koerper);
                        });
                        wandU.Representation = m.Instances.New<IfcProductDefinitionShape>(p => p.Representations.Add(darstellung));
                    }
                    t.Commit();
                }
                IStepFileHeader kopf = m.Header;
                kopf.FileDescription.Description.Clear();
                kopf.FileDescription.Description.Add("ViewDefinition [ReferenceView]");
                kopf.FileName.Name = "fremdhaus.ifc";
                kopf.FileName.TimeStamp = "2026-09-25T00:00:00";
                kopf.FileName.PreprocessorVersion = PREPROCESSOR;
                kopf.FileName.OriginatingSystem = ORIGINATING;
                kopf.FileName.AuthorizationName = "";
                using (var text = new StringWriter(CultureInfo.InvariantCulture))
                {
                    m.SaveAsStep21(text, null);
                    string inhalt = text.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
                    if (schema != null) inhalt = inhalt.Replace("FILE_SCHEMA (('IFC4'))", "FILE_SCHEMA (('" + schema + "'))");
                    return new UTF8Encoding(false).GetBytes(inhalt);
                }
            }
        }

        private static IfcCartesianPoint Punkt(IModel m, double x, double y, double z) => m.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(x, y, z));

        /// <summary>Die Importquelle wie nach dem Import der Datei (gesät).</summary>
        internal static ImportquelleModel Quelle(byte[] datei, string schemastand = "IFC4", int fehlende = 0) => new ImportquelleModel
        {
            ID = 3,
            ID_Gebaeude = GEB,
            Format = GebaeudeQuelle.FORMAT_IFC,
            Dateiname = "fremdhaus.ifc",
            Hash = IfcAnreicherung.Hash(datei),
            Groesse = datei.Length,
            Schemastand = schemastand,
            Zeitpunkt = "2026-09-26T10:00:00",
            FehlendeEntitaeten = fehlende,
        };

        /// <summary>Die Zuordnungen wie nach dem Import (gesät): Gebäude, zwei Zonen, drei Wände, ein Fenster, eine verlorene Entität, ein Aufbau.</summary>
        internal static List<ImportzuordnungModel> Zuordnungen()
        {
            int id = 0;
            ImportzuordnungModel Z(string kennung, string typ, int? geb = null, int? zone = null, int? bauteil = null, int? aufbau = null)
                => new ImportzuordnungModel { ID = ++id, ID_Importquelle = 3, ID_Gebaeude = geb, ID_Zone = zone, ID_Bauteil = bauteil, ID_Aufbau = aufbau, Quellkennung = kennung, Quelltyp = typ };
            return new List<ImportzuordnungModel>
            {
                Z(G_GEBAEUDE, "IfcBuilding", geb: GEB),
                Z(G_WOHNEN, "IfcSpace", zone: ZONE_W),
                Z(G_KUECHE, "IfcSpace", zone: ZONE_K),
                Z(G_WAND_U, "IfcWall", bauteil: WAND_U),
                Z(G_WAND_OHNE_U, "IfcWall", bauteil: WAND_OHNE_U),
                Z(G_WAND_MATERIAL, "IfcWall", bauteil: WAND_MAT),
                Z(G_FENSTER, "IfcWindow", bauteil: FENSTER),
                Z("3Verloren0000000000000", "IfcWall", bauteil: 199),
                Z(G_WAND_U, "IfcWall", aufbau: 55),
            };
        }

        /// <summary>Das Abbild wie beim IFC-Export (Zonenweg, mit Ergebnissen).</summary>
        internal static GbxmlAbbild Abbild()
        {
            var abbild = new GbxmlAbbild { CampusKennung = GebaeudeExportKennung.Campus(GEB) };
            var g = new AbbildGebaeude
            {
                Kennung = GebaeudeExportKennung.Gebaeude(GEB),
                Name = "Fremdhaus",
                Baujahr = 1972,
                Ergebnis = new AbbildErgebnis { EnergieKWh = 12000.0, HeizlastW = 8000.0, Rechenzeitpunkt = new DateTime(2026, 9, 30, 8, 0, 0), Wetterdatensatz = "TRY 2015 Probe" },
            };
            abbild.Gebaeude.Add(g);
            AbbildRaum Raum(int zone, string name, double flaeche) => new AbbildRaum
            {
                Kennung = GebaeudeExportKennung.Raum(zone),
                Name = name,
                FlaecheM2 = flaeche,
                HoeheM = 2.5,
                VolumenM3 = flaeche * 2.5,
                SollHeizenC = 21.0,
                ZonenKennung = GebaeudeExportKennung.Zone(zone),
                LuftwechselJeH = 0.5,
                Ergebnis = new AbbildErgebnis { EnergieKWh = flaeche * 100.0, HeizlastW = flaeche * 50.0, MitteltemperaturC = 20.5 },
            };
            g.Raeume.Add(Raum(ZONE_W, "Wohnen", 30.0));
            g.Raeume.Add(Raum(ZONE_K, "Küche", 12.0));
            AbbildBauteil Wand(int id, double? u, string raum, AbbildAufbau aufbau = null)
            {
                var b = new AbbildBauteil
                {
                    Kennung = GebaeudeExportKennung.Bauteil(id),
                    Name = "Wand " + id.ToString(CultureInfo.InvariantCulture),
                    Art = Bauteilart.Aussenwand,
                    Randbedingung = Randbedingung.Aussenluft,
                    UWertWm2K = u,
                    BruttoflaecheM2 = 20.0,
                    DickeM = 0.3,
                    AzimutGrad = 180.0,
                    NeigungGrad = 90.0,
                    Aufbau = aufbau,
                };
                b.Nachbarn.Add(new AbbildNachbar(GebaeudeExportKennung.Raum(raum == "W" ? ZONE_W : ZONE_K), null));
                return b;
            }
            AbbildAufbau Aufbau(string kennung) => new AbbildAufbau
            {
                Kennung = kennung,
                Name = "EPOS-Aufbau " + kennung,
                Schichten =
                {
                    new AbbildSchicht { Kennung = kennung + "-s1", BaustoffKennung = kennung + "-putz", Name = "Putz", DickeM = 0.015, LambdaWmK = 0.87, RhoKgM3 = 1800, CpJkgK = 1000 },
                    new AbbildSchicht { Kennung = kennung + "-s2", BaustoffKennung = kennung + "-ziegel", Name = "Ziegel", DickeM = 0.285, LambdaWmK = 0.12, RhoKgM3 = 700, CpJkgK = 1000 },
                },
            };
            AbbildBauteil wandU = Wand(WAND_U, 0.28, "W", Aufbau("epos-aufbau-a"));
            wandU.Oeffnungen.Add(new AbbildBauteil
            {
                Kennung = GebaeudeExportKennung.Oeffnung(FENSTER),
                Name = "Fenster",
                Art = Bauteilart.Fenster,
                Randbedingung = Randbedingung.Aussenluft,
                UWertWm2K = 1.1,
                GWert = 0.6,
                BruttoflaecheM2 = 2.0,
                BreiteM = 1.0,
                HoeheM = 2.0,
            });
            g.Bauteile.Add(wandU);
            g.Bauteile.Add(Wand(WAND_OHNE_U, 0.30, "K"));
            g.Bauteile.Add(Wand(WAND_MAT, 0.25, "W", Aufbau("epos-aufbau-b")));
            return abbild;
        }

        private static GebaeudeExportProfil Profil(DateTime? zeit = null) => IfcExportProbe.Profil(zeit);

        /// <summary>Reichert an und liefert die Bytes und die Bilanz.</summary>
        private static byte[] Anreichern(byte[] datei, out GebaeudeAnreicherungBilanz bilanz, ImportquelleModel quelle = null,
                                         GebaeudeExportProfil profil = null, Action<IModel> eingriff = null)
        {
            GbxmlAbbild abbild = Abbild();
            using (var ziel = new MemoryStream())
            {
                var a = new IfcAnreicherung(IfcErgebnisse.AusAbbild(abbild)) { VorDerPruefung = eingriff };
                bilanz = a.Anreichern(datei, quelle ?? Quelle(datei), Zuordnungen(), abbild, ziel, profil ?? Profil(), CancellationToken.None);
                return ziel.ToArray();
            }
        }

        private static IIfcRoot Objekt(MemoryModel m, string id) => m.Instances.OfType<IIfcRoot>().Single(r => r.GlobalId == id);

        private static List<IIfcPropertySet> Saetze(IIfcObject o, string name)
            => o.IsDefinedBy.Select(r => r.RelatingPropertyDefinition).OfType<IIfcPropertySet>().Where(s => s.Name == name).ToList();

        private static string Text(byte[] b) => Encoding.UTF8.GetString(b);

        // ==================================================================
        //  Probe 13: die drei Sperren
        // ==================================================================

        [Fact]
        public void Probe13_Hash_ungleich_verweigert_benannt_mit_Angebot()
        {
            byte[] datei = Fremdhaus();
            byte[] andere = Fremdhaus(geometrie: true);
            byte[] aus = Anreichern(andere, out GebaeudeAnreicherungBilanz b, Quelle(datei));
            Assert.Empty(aus);
            Assert.True(b.Verweigert);
            Assert.False(b.Geschrieben);
            Assert.Equal(IfcAnreicherung.HASH, b.Grund.Schluessel);
            Assert.Equal(new[] { IfcAnreicherung.HASH }, IfcAnreicherung.Sperren(andere, Quelle(datei)).Select(m => m.Schluessel));
            AngebotImText(IfcAnreicherung.HASH);
        }

        [Fact]
        public void Probe13_Schemastand_IFC2X3_verweigert_benannt_mit_Angebot()
        {
            byte[] datei = Fremdhaus();
            byte[] aus = Anreichern(datei, out GebaeudeAnreicherungBilanz b, Quelle(datei, schemastand: "IFC2X3"));
            Assert.Empty(aus);
            Assert.True(b.Verweigert);
            Assert.Equal(IfcAnreicherung.SCHEMA, b.Grund.Schluessel);
            Assert.Equal(new[] { "IFC2X3" }, b.Grund.Werte);
            AngebotImText(IfcAnreicherung.SCHEMA);
            Assert.StartsWith("Rückgabe nur für IFC4", WindowsFormsApplication1.MyResource.Resource.GEXP_PROT_ANR_SCHEMA);

            // Auch ein Kopf, der ein anderes Schema nennt, sperrt beim erneuten Laden (Schemastand der Quelle stimmt).
            byte[] x3 = Fremdhaus(schema: "IFC4X3_ADD2");
            IReadOnlyList<PruefMeldung> s = IfcAnreicherung.Sperren(x3, Quelle(x3));
            Assert.Equal(IfcAnreicherung.SCHEMA, Assert.Single(s).Schluessel);
        }

        [Fact]
        public void Probe13_FehlendeEntitaeten_verweigert_benannt_mit_Angebot()
        {
            byte[] datei = Fremdhaus();
            byte[] aus = Anreichern(datei, out GebaeudeAnreicherungBilanz b, Quelle(datei, fehlende: 2));
            Assert.Empty(aus);
            Assert.True(b.Verweigert);
            Assert.Equal(IfcAnreicherung.VERLUST, b.Grund.Schluessel);
            Assert.Equal(new[] { "2" }, b.Grund.Werte);
            AngebotImText(IfcAnreicherung.VERLUST);
        }

        [Fact]
        public void Probe13_Verlust_beim_erneuten_Laden_beide_Kanaele_verweigert()
        {
            // Ein Verweis ins Leere (#999999) — der zweite Verlustkanal; der Hash der Quelle passt zur Datei.
            string text = Text(Fremdhaus());
            int i = text.IndexOf("IFCRELASSOCIATESMATERIAL(", StringComparison.Ordinal);
            int a = text.IndexOf("(#", i + 25, StringComparison.Ordinal);
            int e = text.IndexOf(')', a);
            byte[] kaputt = Encoding.UTF8.GetBytes(text.Substring(0, a) + "(#999999" + text.Substring(e));
            IReadOnlyList<PruefMeldung> s = IfcAnreicherung.Sperren(kaputt, Quelle(kaputt));
            PruefMeldung m = Assert.Single(s);
            Assert.Equal(IfcAnreicherung.VERLUST_LADEN, m.Schluessel);
            Assert.NotEqual("0", m.Werte[0]);
            AngebotImText(IfcAnreicherung.VERLUST_LADEN);

            Anreichern(kaputt, out GebaeudeAnreicherungBilanz b, Quelle(kaputt));
            Assert.True(b.Verweigert);
            Assert.Equal(IfcAnreicherung.VERLUST_LADEN, b.Grund.Schluessel);
        }

        [Fact]
        public void Probe13_alle_Sperren_der_Quelle_in_der_Vorschau_und_frei_nur_mit_Beipackzettel()
        {
            byte[] datei = Fremdhaus();
            var q = Quelle(datei, schemastand: "IFC2X3", fehlende: 1);
            q.Hash = new string('0', 64);
            Assert.Equal(new[] { IfcAnreicherung.HASH, IfcAnreicherung.SCHEMA, IfcAnreicherung.VERLUST },
                         IfcAnreicherung.Sperren(datei, q).Select(m => m.Schluessel));
            Assert.All(IfcAnreicherung.Sperren(datei, q), m => Assert.Equal(PruefStufe.Fehler, m.Stufe));
            Assert.Equal(IfcAnreicherung.KEINE_QUELLE, Assert.Single(IfcAnreicherung.Sperren(datei, null)).Schluessel);
            Assert.Empty(IfcAnreicherung.Sperren(datei, Quelle(datei)));

            var ablauf = new GebaeudeExportAblauf();
            IReadOnlyList<PruefMeldung> frei = ablauf.AnreicherungVorschau(new MemoryStream(datei), Quelle(datei));
            PruefMeldung beipack = Assert.Single(frei);
            Assert.Equal(IfcAnreicherung.BEIPACK_FREMDDATEI, beipack.Schluessel);
            Assert.Equal(PruefStufe.Warnung, beipack.Stufe);
            Assert.Contains("fremde Datei verändert weiter", WindowsFormsApplication1.MyResource.Resource.GEXP_PROT_ANR_BEIPACK_FREMDDATEI);
        }

        [Fact]
        public void Nur_STEP_wird_angereichert()
        {
            byte[] zip = IfcProbenErzeuger.Zip("fremdhaus.ifc", Fremdhaus());
            Assert.Equal(IfcAnreicherung.NUR_STEP, Assert.Single(IfcAnreicherung.Sperren(zip, Quelle(zip))).Schluessel);
        }

        private static void AngebotImText(string schluessel)
        {
            foreach (string sprache in new[] { "de-DE", "en-US" })
            {
                string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.GetCultureInfo(sprache));
                Assert.False(string.IsNullOrWhiteSpace(text), schluessel + " fehlt in " + sprache);
                Assert.Contains(sprache == "de-DE" ? "eigene IFC-Datei" : "separate IFC file", text);
            }
        }

        // ==================================================================
        //  Ergänzen statt doppeln
        // ==================================================================

        [Fact]
        public void Ergaenzen_statt_doppeln_genau_ein_Pset_WallCommon_je_Wand_Wert_ersetzt()
        {
            byte[] aus = Anreichern(Fremdhaus(), out GebaeudeAnreicherungBilanz b);
            Assert.True(b.Geschrieben, string.Join("\n", b.Meldungen.Select(m => m.Schluessel + " " + string.Join("|", m.Werte))));
            using (MemoryModel m = IfcExportProbe.Modell(aus))
            {
                var wandU = (IIfcWall)Objekt(m, G_WAND_U);
                var wandOhneU = (IIfcWall)Objekt(m, G_WAND_OHNE_U);
                var wandMat = (IIfcWall)Objekt(m, G_WAND_MATERIAL);
                foreach (IIfcWall w in new[] { wandU, wandOhneU, wandMat })
                    Assert.Single(Saetze(w, "Pset_WallCommon"));

                var werte = IfcExportProbe.Eigenschaften(wandU)["Pset_WallCommon"];
                Assert.Equal(0.28, IfcExportProbe.Zahl(werte["ThermalTransmittance"]), 12);
                Assert.Equal("AW-1", werte["Reference"].NominalValue.ToString());   // fremde Eigenschaft bleibt
                Assert.Equal(3, werte.Count);                                     // kein zweites IsExternal
                Assert.Equal(0.30, IfcExportProbe.Zahl(IfcExportProbe.Eigenschaften(wandOhneU)["Pset_WallCommon"]["ThermalTransmittance"]), 12);
                Assert.Equal(0.25, IfcExportProbe.Zahl(IfcExportProbe.Eigenschaften(wandMat)["Pset_WallCommon"]["ThermalTransmittance"]), 12);

                // Vorhandene Mengen bleiben; der Satz wird nicht gedoppelt. Ohne Satz kommt er hinzu (in Projekteinheiten: mm).
                Assert.Single(wandU.IsDefinedBy.Select(r => r.RelatingPropertyDefinition).OfType<IIfcElementQuantity>(), q => q.Name == "Qto_WallBaseQuantities");
                Assert.Equal(new Dictionary<string, double> { ["GrossSideArea"] = 20.0 }, IfcExportProbe.Mengen(wandU, "Qto_WallBaseQuantities"));
                Assert.Equal(300.0, IfcExportProbe.Mengen(wandOhneU, "Qto_WallBaseQuantities")["Width"], 9);

                var fenster = (IIfcWindow)Objekt(m, G_FENSTER);
                var f = IfcExportProbe.Eigenschaften(fenster);
                Assert.Equal(1.1, IfcExportProbe.Zahl(f["Pset_WindowCommon"]["ThermalTransmittance"]), 12);
                Assert.Equal(0.6, IfcExportProbe.Zahl(f["Pset_DoorWindowGlazingType"]["SolarHeatGainTransmittance"]), 12);
                Assert.Equal("epos-oeffnung-104", f[IfcSchreiber.EPOS_BAUTEIL]["Kennung"].NominalValue.ToString());

                var raum = (IIfcSpace)Objekt(m, G_WOHNEN);
                var r = IfcExportProbe.Eigenschaften(raum);
                Assert.Equal(21.0 + IfcSchreiber.NULLPUNKT_K, IfcExportProbe.Zahl(r["Pset_SpaceThermalRequirements"]["SpaceTemperature"]), 9);
                Assert.Equal(3000.0, IfcExportProbe.Zahl(r[IfcSchreiber.EPOS_ERGEBNIS]["Heizwaermebedarf"]), 9);
                Assert.True(r.ContainsKey(IfcSchreiber.EPOS_ZONE));

                var geb = (IIfcBuilding)Objekt(m, G_GEBAEUDE);
                var g = IfcExportProbe.Eigenschaften(geb);
                Assert.Equal(12000.0, IfcExportProbe.Zahl(g[IfcSchreiber.EPOS_ERGEBNIS]["Heizwaermebedarf"]), 9);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, g[IfcSchreiber.EPOS_RECHENLAUF]["Validierung"].NominalValue.ToString());
                Assert.Equal("1972", g["Pset_BuildingCommon"]["YearOfConstruction"].NominalValue.ToString());
            }
            Assert.Contains(b.Meldungen, x => x.Schluessel == IfcAnreicherung.ENTITAET_FEHLT && x.Werte[0] == "3Verloren0000000000000");
            Assert.Contains(b.Meldungen, x => x.Schluessel == IfcAnreicherung.BEIPACK_FREMDDATEI);
            Assert.Equal(7, b.Objekte);
            Assert.Equal(1, b.Uebersprungen);
            Assert.Equal(1, b.Ersetzt);   // ThermalTransmittance der Wand mit U; alles andere kommt hinzu
            Assert.True(b.Ergaenzt > 0);
        }

        [Fact]
        public void Geteilter_Satz_wird_je_Wand_abgespalten_fremde_Eigenschaften_bleiben()
        {
            byte[] aus = Anreichern(Fremdhaus(geteilt: true), out GebaeudeAnreicherungBilanz b);
            Assert.True(b.Geschrieben, string.Join("\n", b.Meldungen.Select(m => m.Schluessel + " " + string.Join("|", m.Werte))));
            using (MemoryModel m = IfcExportProbe.Modell(aus))
            {
                var wandU = (IIfcWall)Objekt(m, G_WAND_U);
                var wandOhneU = (IIfcWall)Objekt(m, G_WAND_OHNE_U);
                IIfcPropertySet a = Assert.Single(Saetze(wandU, "Pset_WallCommon"));
                IIfcPropertySet c = Assert.Single(Saetze(wandOhneU, "Pset_WallCommon"));
                Assert.NotSame(a, c);
                Assert.Equal(0.28, IfcExportProbe.Zahl(IfcExportProbe.Eigenschaften(wandU)["Pset_WallCommon"]["ThermalTransmittance"]), 12);
                Assert.Equal(0.30, IfcExportProbe.Zahl(IfcExportProbe.Eigenschaften(wandOhneU)["Pset_WallCommon"]["ThermalTransmittance"]), 12);
                foreach (IIfcWall w in new[] { wandU, wandOhneU })
                    Assert.Equal("AW-1", IfcExportProbe.Eigenschaften(w)["Pset_WallCommon"]["Reference"].NominalValue.ToString());
                Assert.Empty(IfcSchreiber.Pruefen(m));
            }
            Assert.Equal(2, b.Ersetzt);
        }

        [Fact]
        public void Zweiter_Durchlauf_auf_der_angereicherten_Datei_ersetzt_die_EPOS_Saetze()
        {
            byte[] erste = Anreichern(Fremdhaus(), out _);
            byte[] zweite = Anreichern(erste, out GebaeudeAnreicherungBilanz b);
            Assert.True(b.Geschrieben, string.Join("\n", b.Meldungen.Select(m => m.Schluessel + " " + string.Join("|", m.Werte))));
            using (MemoryModel m = IfcExportProbe.Modell(zweite))
            {
                foreach (string id in new[] { G_WAND_U, G_WAND_OHNE_U, G_WAND_MATERIAL, G_FENSTER })
                {
                    var o = (IIfcObject)Objekt(m, id);
                    Assert.Single(Saetze(o, IfcSchreiber.EPOS_BAUTEIL));
                    Assert.Single(Saetze(o, "Pset_" + (id == G_FENSTER ? "Window" : "Wall") + "Common"));
                }
                Assert.Single(Saetze((IIfcObject)Objekt(m, G_GEBAEUDE), IfcSchreiber.EPOS_RECHENLAUF));
                Assert.Single(m.Instances.OfType<IIfcMaterialLayerSet>());
                Assert.Equal(m.Instances.OfType<IIfcRoot>().Count(), m.Instances.OfType<IIfcRoot>().Select(r => (string)r.GlobalId).Distinct().Count());
            }
        }

        // ==================================================================
        //  Kennungen, Kennung der Datei, Baustoffe
        // ==================================================================

        [Fact]
        public void Vorhandene_GlobalIds_unveraendert_neue_deterministisch_zwei_Laeufe_bytegleich()
        {
            byte[] datei = Fremdhaus();
            byte[] a = Anreichern(datei, out _);
            byte[] b = Anreichern(datei, out _);
            Assert.Equal(a, b);

            // Andere Uhr: gleich bis auf den Zeitstempel (Vermerk und Geschichte).
            DateTime spaeter = GbxmlExportProbe.Zeitpunkt.AddHours(3);
            byte[] c = Anreichern(datei, out _, profil: Profil(spaeter));
            Assert.NotEqual(a, c);
            string ta = Text(a), tc = Text(c);
            long s1 = new DateTimeOffset(DateTime.SpecifyKind(GbxmlExportProbe.Zeitpunkt, DateTimeKind.Utc)).ToUnixTimeSeconds();
            long s2 = new DateTimeOffset(DateTime.SpecifyKind(spaeter, DateTimeKind.Utc)).ToUnixTimeSeconds();
            tc = tc.Replace(IfcSchreiber.Zeitstempel(spaeter), IfcSchreiber.Zeitstempel(GbxmlExportProbe.Zeitpunkt))
                   .Replace(s2.ToString(CultureInfo.InvariantCulture), s1.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(ta, tc);

            using (MemoryModel vorher = IfcExportProbe.Modell(datei))
            using (MemoryModel nachher = IfcExportProbe.Modell(a))
            {
                var alt = vorher.Instances.OfType<IIfcRoot>().ToDictionary(r => r.EntityLabel, r => (string)r.GlobalId);
                foreach (var (label, id) in alt)
                    Assert.Equal(id, ((IIfcRoot)nachher.Instances[label]).GlobalId.ToString());
                var neu = nachher.Instances.OfType<IIfcRoot>().Where(r => !alt.ContainsKey(r.EntityLabel)).ToList();
                Assert.NotEmpty(neu);
                Assert.All(neu, r => Assert.Equal("EPOS-Plan", r.OwnerHistory.OwningApplication.ApplicationIdentifier.ToString()));
                // Die Kennung folgt der Ableitung: GlobalId der Quellentität + Rollenglied.
                IIfcPropertySet bauteil = Saetze((IIfcObject)nachher.Instances.OfType<IIfcRoot>().Single(r => r.GlobalId == G_WAND_U), IfcSchreiber.EPOS_BAUTEIL).Single();
                Assert.Equal(IfcGloballyUniqueId.ConvertToBase64(IfcExportKennung.NamensUuid(IfcExportKennung.NAMENSRAUM, G_WAND_U + "#Anreicherung:Pset:EPOS_Bauteil")),
                             bauteil.GlobalId.ToString());
            }
        }

        [Fact]
        public void Kennung_der_Datei_Vermerk_eigene_Anwendung_Herkunft_und_Schema_bleiben()
        {
            byte[] datei = Fremdhaus();
            byte[] aus = Anreichern(datei, out _);
            using (MemoryModel m = IfcExportProbe.Modell(aus))
            {
                IStepFileHeader kopf = m.Header;
                Assert.Equal(ORIGINATING, kopf.FileName.OriginatingSystem);
                Assert.Equal(PREPROCESSOR, kopf.FileName.PreprocessorVersion);
                Assert.Equal("fremdhaus.ifc", kopf.FileName.Name);
                Assert.Equal(new[] { "IFC4" }, kopf.FileSchema.Schemas);
                Assert.Equal("ViewDefinition [ReferenceView]", kopf.FileDescription.Description[0]);
                string vermerk = kopf.FileDescription.Description.Last();
                Assert.Equal(IfcAnreicherung.Vermerk(GbxmlExportProbe.Zeitpunkt, Profil()), vermerk);
                Assert.Contains("Geometrie unverändert", vermerk);
                Assert.Contains("EPOS-Plan " + GbxmlExportProbe.VERSION, vermerk);

                List<IIfcApplication> programme = m.Instances.OfType<IIfcApplication>().ToList();
                Assert.Equal(2, programme.Count);
                Assert.Contains(programme, p => p.ApplicationIdentifier == "FremdCAD");
                IIfcApplication epos = programme.Single(p => p.ApplicationIdentifier == "EPOS-Plan");
                IIfcOwnerHistory h = m.Instances.OfType<IIfcOwnerHistory>().Single(x => x.OwningApplication == epos);
                Assert.Equal(IfcChangeActionEnum.ADDED, h.ChangeAction);
                // Die Bestandsobjekte behalten ihre Geschichte.
                Assert.Equal("FremdCAD", Objekt(m, G_WAND_U).OwnerHistory.OwningApplication.ApplicationIdentifier.ToString());
            }
        }

        [Fact]
        public void Baustoffe_nur_ohne_Materialzuordnung()
        {
            byte[] aus = Anreichern(Fremdhaus(), out _);
            using (MemoryModel m = IfcExportProbe.Modell(aus))
            {
                var wandU = (IIfcWall)Objekt(m, G_WAND_U);
                var wandMat = (IIfcWall)Objekt(m, G_WAND_MATERIAL);
                IIfcRelAssociatesMaterial neu = wandU.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Single();
                var satz = Assert.IsAssignableFrom<IIfcMaterialLayerSet>(neu.RelatingMaterial);
                Assert.Equal(2, satz.MaterialLayers.Count);
                Assert.Equal(15.0, satz.MaterialLayers[0].LayerThickness, 9);   // Projekteinheit mm
                Assert.Equal(IfcSchreiber.SCHICHTRICHTUNG_AUSSEN_INNEN, IfcExportProbe.Eigenschaften(wandU)[IfcSchreiber.EPOS_BAUTEIL]["Schichtrichtung"].NominalValue.ToString());
                Assert.Equal("EPOS-Plan", IfcExportProbe.Eigenschaften(wandU)[IfcSchreiber.EPOS_BAUTEIL]["Materialherkunft"].NominalValue.ToString());

                IIfcRelAssociatesMaterial alt = wandMat.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Single();
                Assert.Equal("Mauerwerk der Architektin", ((IIfcMaterial)alt.RelatingMaterial).Name.ToString());
                var e = IfcExportProbe.Eigenschaften(wandMat)[IfcSchreiber.EPOS_BAUTEIL];
                Assert.Equal("Datei", e["Materialherkunft"].NominalValue.ToString());
                Assert.True(e.ContainsKey("Schichtrichtung"));
                Assert.Single(m.Instances.OfType<IIfcMaterialLayerSet>());
            }
        }

        // ==================================================================
        //  Validator, Rundlauf, Geometrie
        // ==================================================================

        [Fact]
        public void Validator_gruen_und_neuer_Verstoss_bricht_ab_ohne_zu_schreiben()
        {
            byte[] datei = Fremdhaus();
            byte[] aus = Anreichern(datei, out GebaeudeAnreicherungBilanz gut);
            Assert.True(gut.Geschrieben);
            using (MemoryModel m = IfcExportProbe.Modell(aus))
                Assert.Empty(IfcSchreiber.Pruefen(m));
            Assert.DoesNotContain(gut.Meldungen, x => x.Schluessel == IfcAnreicherung.VERSTOESSE_VORHER);

            byte[] nichts = Anreichern(datei, out GebaeudeAnreicherungBilanz schlecht, eingriff: mo =>
            {
                using (ITransaction t = mo.BeginTransaction("Probe"))
                {
                    IfcPropertySet s = mo.Instances.OfType<IfcPropertySet>().First(p => p.Name == IfcSchreiber.EPOS_BAUTEIL);
                    s.GlobalId = new IfcGloballyUniqueId();
                    t.Commit();
                }
            });
            Assert.Empty(nichts);
            Assert.False(schlecht.Geschrieben);
            Assert.False(schlecht.Verweigert);
            Assert.Equal(IfcSchreiber.ABGEBROCHEN, schlecht.Meldungen.Last().Schluessel);
        }

        [Fact]
        public void Rundlauf_der_Leser_findet_dieselben_Raeume_und_die_neuen_U_Werte()
        {
            byte[] datei = Fremdhaus();
            byte[] aus = Anreichern(datei, out _);
            GebaeudeAbbild vorher = IfcExportProbe.Lesen(datei);
            GebaeudeAbbild nachher = IfcExportProbe.Lesen(aus);
            Assert.Equal(0, nachher.FehlendeEntitaeten);
            Assert.Equal(vorher.Gebaeude.Select(g => g.Kennung), nachher.Gebaeude.Select(g => g.Kennung));
            Assert.Equal(vorher.Gebaeude[0].Raeume.Select(r => r.Kennung + "|" + r.Name).OrderBy(x => x),
                         nachher.Gebaeude[0].Raeume.Select(r => r.Kennung + "|" + r.Name).OrderBy(x => x));
            var u = nachher.Gebaeude[0].Bauteile.Concat(nachher.Gebaeude[0].Bauteile.SelectMany(b => b.Oeffnungen))
                .Where(b => b.UWertWm2K.HasValue).ToDictionary(b => b.Kennung, b => b.UWertWm2K.Value);
            Assert.Equal(0.28, u[G_WAND_U], 9);
            Assert.Equal(0.30, u[G_WAND_OHNE_U], 9);
            Assert.Equal(0.25, u[G_WAND_MATERIAL], 9);
        }

        [Fact]
        public void Geometrie_bleibt_bytegleich()
        {
            byte[] datei = Fremdhaus(geometrie: true);
            byte[] aus = Anreichern(datei, out GebaeudeAnreicherungBilanz b);
            Assert.True(b.Geschrieben);
            Dictionary<int, string> vorher = Zeilen(datei), nachher = Zeilen(aus);
            foreach (string typ in Geometrietypen)
            {
                var alt = vorher.Where(z => z.Value.StartsWith(typ + "(", StringComparison.Ordinal)).ToList();
                Assert.Equal(alt.Count, nachher.Count(z => z.Value.StartsWith(typ + "(", StringComparison.Ordinal)));
                foreach (var z in alt) Assert.Equal(z.Value, nachher[z.Key]);
            }
            Assert.Contains(vorher.Values, z => z.StartsWith("IFCEXTRUDEDAREASOLID(", StringComparison.Ordinal));
            // Jede Entität der Eingangsdatei steht mit derselben Nummer wieder da (Bestand ergänzt, nicht neu geschrieben).
            Assert.All(vorher.Keys, k => Assert.True(nachher.ContainsKey(k), "#" + k + " fehlt"));
        }

        /// <summary>Die Datenzeilen einer STEP-Datei: Nummer → Text nach dem Gleichheitszeichen.</summary>
        private static Dictionary<int, string> Zeilen(byte[] datei)
        {
            var r = new Dictionary<int, string>();
            foreach (string zeile in Text(datei).Split("\r\n"))
            {
                if (!zeile.StartsWith("#", StringComparison.Ordinal)) continue;
                int gleich = zeile.IndexOf('=');
                r[int.Parse(zeile.Substring(1, gleich - 1), CultureInfo.InvariantCulture)] = zeile.Substring(gleich + 1);
            }
            return r;
        }

        // ==================================================================
        //  Ablauf und Dateivorschlag
        // ==================================================================

        [Fact]
        public void Ablauf_Anreichern_neben_Schreiben_mit_Quelle_zur_Datei()
        {
            byte[] datei = Fremdhaus();
            var fremd = Quelle(Fremdhaus(geometrie: true));
            fremd.ID = 2;
            fremd.Zeitpunkt = "2026-09-27T10:00:00";
            ImportquelleModel q = GebaeudeExportAblauf.AnreicherungsQuelle(new[] { fremd, Quelle(datei) }, datei);
            Assert.Equal(3, q.ID);   // die mit gleichem Hash, nicht die jüngste
            Assert.Equal(2, GebaeudeExportAblauf.AnreicherungsQuelle(new[] { fremd }, datei).ID);
            Assert.Null(GebaeudeExportAblauf.AnreicherungsQuelle(Array.Empty<ImportquelleModel>(), datei));

            var plan = new GebaeudeExportPlan(Abbild(), new List<PruefMeldung>
            {
                new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.BEIPACK_RAUMGRENZEN),
                new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.OHNE_ORT),
            }, null);
            using (var ziel = new MemoryStream())
            {
                GebaeudeAnreicherungBilanz b = new GebaeudeExportAblauf().Anreichern(plan, new MemoryStream(datei), q, Zuordnungen(), ziel,
                                                                                    Profil(), CancellationToken.None);
                Assert.True(b.Geschrieben);
                Assert.Equal(b.Bytes, ziel.Length);
                Assert.Equal(GebaeudeExportAblauf.OHNE_ORT, b.Meldungen[0].Schluessel);
                Assert.DoesNotContain(b.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_RAUMGRENZEN);
                Assert.Contains(b.Meldungen, m => m.Schluessel == IfcAnreicherung.BILANZ);
            }
            Assert.Throws<ArgumentException>(() => new GebaeudeExportAblauf().Anreichern(plan, new MemoryStream(datei), q, Zuordnungen(),
                new MemoryStream(), GbxmlExportProbe.Profil(), CancellationToken.None));
        }

        [Theory]
        [InlineData("fremdhaus.ifc", "fremdhaus_EPOS.ifc")]
        [InlineData(@"C:\Planung\Haus A.IFC", "Haus A_EPOS.ifc")]
        [InlineData("/var/mobile/haus.ifczip", "haus_EPOS.ifc")]
        [InlineData("", "EPOS-Plan_EPOS.ifc")]
        [InlineData(null, "EPOS-Plan_EPOS.ifc")]
        public void Dateivorschlag_immer_neuer_Name(string original, string vorschlag)
            => Assert.Equal(vorschlag, GebaeudeExportAblauf.Dateivorschlag(original));

        [Fact]
        public void Jeder_Anreicherungsschluessel_steht_in_beiden_Sprachen()
        {
            var schluessel = typeof(IfcAnreicherung).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue())
                .Where(s => s.StartsWith(IfcAnreicherung.P, StringComparison.Ordinal) && s != IfcAnreicherung.P).ToList();
            schluessel.Add(GebaeudeExportAblauf.ANREICHERUNG_OHNE_XBIM);
            schluessel.Add("GEXP_IFC_ANR_VERMERK");
            schluessel.Add("GEXP_IFC_ANR_MATERIALHERKUNFT");
            Assert.True(schluessel.Count >= 18);
            foreach (string k in schluessel)
                foreach (string sprache in new[] { "de-DE", "en-US" })
                {
                    string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(k, CultureInfo.GetCultureInfo(sprache));
                    Assert.False(string.IsNullOrWhiteSpace(text), k + " fehlt in " + sprache);
                }
            Assert.NotEqual(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_PROT_ANR_HASH", CultureInfo.GetCultureInfo("de-DE")),
                            WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_PROT_ANR_HASH", CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
