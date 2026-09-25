using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Memory;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die synthetischen Großfälle der Importmessung</b> (Umsetzungskonzept Gebäudesimulation 3.6,
    /// G4-8): je Format eine Datei aus N gleichartigen Räumen, im Speicher erzeugt — keine Datei im
    /// Repositorium, keine Hersteller- oder Produktdaten, neutrale Namen und runde Werte.
    ///
    /// <para><b>Wozu.</b> Die iOS-Größengrenzen (IFC 20 MB, gbXML 25 MB nach E42, Softwarearchitektur 1.5
    /// Regel 2) werden hier gemessen. Gemessen werden kann nur an Dateien, die groß genug sind, dass der
    /// Speicherbedarf des Lesers die Grundlast der Anwendung übersteigt — und die liegen nicht vor.
    /// Die Erzeugung ist deshalb Teil des Kerns: Der Prüfmodus der iOS-Schale und die Tests unter
    /// Windows fahren denselben Weg (<see cref="Importmessung"/>).</para>
    ///
    /// <para><b>Deterministisch:</b> Derselbe Aufruf liefert dieselben Bytes — auch plattformübergreifend,
    /// weil die Zeilenenden auf LF gezogen werden (die Bibliothek schreibt die der Plattform). IFC
    /// schreibt xBIM (<c>MemoryModel</c> im Schreibmodus, Schema IFC4) mit festen <c>GlobalId</c>s aus
    /// einem Zähler und festem Kopf, nach dem Muster des Probenerzeugers der Tests.</para>
    ///
    /// <para><b>Ein Raum</b> ist 20 m² groß, 3 m hoch (60 m³), beheizt, und hat eine Außenwand von
    /// 4 m × 3 m mit einem Fenster von 1,8 m²; die Wände drehen reihum nach Süd, Ost, Nord und West.
    /// Je <see cref="RAEUME_JE_GESCHOSS"/> Räume bilden ein Geschoss. gbXML legt zusätzlich eine
    /// Innenwand zwischen zwei aufeinanderfolgenden Räumen an.</para>
    /// </summary>
    internal static class ImportmessungProben
    {
        /// <summary>Räume je Geschoss.</summary>
        public const int RAEUME_JE_GESCHOSS = 50;

        /// <summary>Nutzfläche eines Raums [m²].</summary>
        public const double RAUMFLAECHE_M2 = 20.0;

        /// <summary>Der feste Zeitstempel im IFC-Kopf.</summary>
        public const string ZEITSTEMPEL = "2026-09-25T00:00:00";

        // ==================================================================
        //  Zielgröße
        // ==================================================================

        /// <summary>
        /// Erzeugt eine Datei von ungefähr <paramref name="zielBytes"/> Bytes: Zwei Vorläufe (64 und
        /// 128 Räume) bestimmen die Bytes je Raum und den festen Anteil, daraus folgt die Raumzahl.
        /// Deterministisch — derselbe Aufruf ergibt dieselbe Raumzahl.
        /// </summary>
        public static (byte[] Daten, int Raeume) ZuGroesse(long zielBytes, Func<int, byte[]> erzeugen)
        {
            if (erzeugen == null) throw new ArgumentNullException(nameof(erzeugen));
            const int klein = 64, gross = 128;
            long a = erzeugen(klein).LongLength;
            long b = erzeugen(gross).LongLength;
            double jeRaum = Math.Max(1.0, (b - a) / (double)(gross - klein));
            double fest = a - klein * jeRaum;
            int raeume = (int)Math.Max(1.0, Math.Round((zielBytes - fest) / jeRaum));
            return (erzeugen(raeume), raeume);
        }

        // ==================================================================
        //  gbXML
        // ==================================================================

        /// <summary>Eine gbXML-Datei (Fassung 6.01, SI) mit <paramref name="raeume"/> Räumen, UTF-8 ohne BOM, LF.</summary>
        public static byte[] Gbxml(int raeume)
        {
            if (raeume < 1) throw new ArgumentOutOfRangeException(nameof(raeume));
            var sb = new StringBuilder(4096 + raeume * 1400);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<!-- EPOS-Plan Messfall (synthetisch, im Speicher erzeugt): ").Append(Ganz(raeume))
              .Append(" gleichartige Räume. -->\n");
            sb.Append("<gbXML xmlns=\"http://www.gbxml.org/schema\" version=\"6.01\" temperatureUnit=\"C\" lengthUnit=\"Meters\"")
              .Append(" areaUnit=\"SquareMeters\" volumeUnit=\"CubicMeters\" useSIUnitsForResults=\"true\">\n");
            sb.Append("  <Campus id=\"campus-1\">\n");
            sb.Append("    <Location>\n      <Name>Messort</Name>\n      <Latitude>50</Latitude>\n      <Longitude>10</Longitude>\n")
              .Append("      <CADModelAzimuth>0</CADModelAzimuth>\n    </Location>\n");
            sb.Append("    <Building id=\"geb-1\" buildingType=\"Office\">\n      <Name>Messgebäude</Name>\n");

            int geschosse = (raeume + RAEUME_JE_GESCHOSS - 1) / RAEUME_JE_GESCHOSS;
            for (int g = 0; g < geschosse; g++)
                sb.Append("      <BuildingStorey id=\"gs-").Append(Ganz(g)).Append("\"><Name>Geschoss ").Append(Ganz(g))
                  .Append("</Name><Level>").Append(Ganz(3 * g)).Append("</Level></BuildingStorey>\n");

            for (int i = 1; i <= raeume; i++)
            {
                sb.Append("      <Space id=\"").Append(Raum(i)).Append("\" conditionType=\"Heated\" zoneIdRef=\"zone-1\" buildingStoreyIdRef=\"gs-")
                  .Append(Ganz((i - 1) / RAEUME_JE_GESCHOSS)).Append("\">\n");
                sb.Append("        <Name>Raum ").Append(Ganz(i)).Append("</Name>\n");
                sb.Append("        <Area>20</Area>\n        <Volume>60</Volume>\n        <AirChangesPerHour>0.5</AirChangesPerHour>\n");
                sb.Append("        <PeopleNumber unit=\"NumberOfPeople\">1</PeopleNumber>\n");
                sb.Append("        <LightPowerPerArea unit=\"WattPerSquareMeter\">5</LightPowerPerArea>\n");
                sb.Append("        <EquipPowerPerArea unit=\"WattPerSquareMeter\">5</EquipPowerPerArea>\n");
                sb.Append("      </Space>\n");
            }
            sb.Append("    </Building>\n");

            for (int i = 1; i <= raeume; i++)
            {
                string nr = Ganz6(i);
                sb.Append("    <Surface id=\"aw-").Append(nr).Append("\" surfaceType=\"ExteriorWall\" constructionIdRef=\"kon-aw\">\n");
                sb.Append("      <AdjacentSpaceId spaceIdRef=\"").Append(Raum(i)).Append("\"/>\n");
                sb.Append("      <RectangularGeometry>\n        <Azimuth>").Append(Ganz(90 * (i % 4)))
                  .Append("</Azimuth>\n        <Tilt>90</Tilt>\n        <Width>4</Width>\n        <Height>3</Height>\n      </RectangularGeometry>\n");
                sb.Append("      <Opening id=\"fenster-").Append(nr).Append("\" openingType=\"FixedWindow\" windowTypeIdRef=\"fenster-typ-1\">\n");
                sb.Append("        <RectangularGeometry>\n          <Width>1.5</Width>\n          <Height>1.2</Height>\n        </RectangularGeometry>\n");
                sb.Append("      </Opening>\n    </Surface>\n");
                if (i > 1)
                {
                    sb.Append("    <Surface id=\"iw-").Append(nr).Append("\" surfaceType=\"InteriorWall\" constructionIdRef=\"kon-iw\">\n");
                    sb.Append("      <AdjacentSpaceId spaceIdRef=\"").Append(Raum(i - 1)).Append("\"/>\n");
                    sb.Append("      <AdjacentSpaceId spaceIdRef=\"").Append(Raum(i)).Append("\"/>\n");
                    sb.Append("      <RectangularGeometry>\n        <Azimuth>90</Azimuth>\n        <Tilt>90</Tilt>\n        <Width>5</Width>\n")
                      .Append("        <Height>3</Height>\n      </RectangularGeometry>\n    </Surface>\n");
                }
            }
            sb.Append("  </Campus>\n");

            sb.Append("  <Construction id=\"kon-aw\">\n    <Name>Außenwand</Name>\n    <U-value unit=\"WPerSquareMeterK\">0.3</U-value>\n  </Construction>\n");
            sb.Append("  <Construction id=\"kon-iw\">\n    <Name>Innenwand</Name>\n    <U-value unit=\"WPerSquareMeterK\">1.5</U-value>\n  </Construction>\n");
            sb.Append("  <WindowType id=\"fenster-typ-1\">\n    <Name>Fenster</Name>\n    <U-value unit=\"WPerSquareMeterK\">1.1</U-value>\n")
              .Append("    <SolarHeatGainCoeff unit=\"Fraction\" solarIncidentAngle=\"0\">0.6</SolarHeatGainCoeff>\n  </WindowType>\n");
            sb.Append("  <Zone id=\"zone-1\">\n    <Name>Zone</Name>\n    <DesignHeatT>20</DesignHeatT>\n    <DesignCoolT>26</DesignCoolT>\n  </Zone>\n");
            sb.Append("</gbXML>\n");
            return new UTF8Encoding(false).GetBytes(sb.ToString());
        }

        private static string Raum(int i) => "raum-" + Ganz6(i);

        // ==================================================================
        //  IFC
        // ==================================================================

        /// <summary>Eine IFC4-Datei (STEP) mit <paramref name="raeume"/> Räumen, geschrieben von xBIM, LF.</summary>
        public static byte[] Ifc(int raeume)
        {
            if (raeume < 1) throw new ArgumentOutOfRangeException(nameof(raeume));
            using (var b = new IfcBau())
            {
                b.Anfang();
                IIfcBuilding haus = b.Gebaeude("Messgebäude");
                IIfcWallType typ = b.Wandtyp("Außenwand", 0.3);
                IIfcBuildingStorey geschoss = null;
                for (int i = 1; i <= raeume; i++)
                {
                    int g = (i - 1) / RAEUME_JE_GESCHOSS;
                    if ((i - 1) % RAEUME_JE_GESCHOSS == 0)
                        geschoss = b.Geschoss(haus, "Geschoss " + Ganz(g), 3000.0 * g);
                    b.Raum(geschoss, typ, i);
                }
                return b.Speichern("messfall_" + Ganz(raeume) + ".ifc");
            }
        }

        /// <summary>Der Bauhelfer, verkürzt aus dem Probenerzeuger der Tests (IFC4, Längen in mm).</summary>
        private sealed class IfcBau : IDisposable
        {
            private static readonly (double Rx, double Ry)[] Richtungen = { (1, 0), (0, 1), (-1, 0), (0, -1) };

            private readonly MemoryModel _m;
            private readonly ITransaction _t;
            private int _guid;
            private IIfcProject _projekt;
            private IIfcSite _site;
            private IIfcRelAggregates _siteZerlegung;
            private readonly Dictionary<int, IIfcRelAggregates> _zerlegung = new Dictionary<int, IIfcRelAggregates>();
            private readonly Dictionary<int, IIfcRelContainedInSpatialStructure> _enthalten = new Dictionary<int, IIfcRelContainedInSpatialStructure>();
            private readonly Dictionary<int, IIfcRelDefinesByType> _typisiert = new Dictionary<int, IIfcRelDefinesByType>();

            public IfcBau()
            {
                _m = new MemoryModel(MemoryModel.GetFactory(XbimSchemaVersion.Ifc4), NullLoggerFactory.Instance, 0);
                _t = _m.BeginTransaction("Messfall");
            }

            public void Dispose()
            {
                _t.Dispose();
                _m.Dispose();
            }

            private T N<T>(string typ) where T : class, IPersistEntity
                => (T)_m.Instances.New(_m.Metadata.ExpressType(typ.ToUpperInvariant()).Type);

            private T Wurzel<T>(string typ, string name) where T : class, IIfcRoot
            {
                T e = N<T>(typ);
                _guid++;
                e.GlobalId = new IfcGloballyUniqueId(IfcGloballyUniqueId.ConvertToBase64(
                    new Guid("00000000-0000-4000-8000-" + _guid.ToString("D12", CultureInfo.InvariantCulture))));
                if (name != null) e.Name = new IfcLabel(name);
                return e;
            }

            private IIfcCartesianPoint Punkt(double x, double y, double z)
            {
                IIfcCartesianPoint p = N<IIfcCartesianPoint>("IfcCartesianPoint");
                p.Coordinates.Add(new IfcLengthMeasure(x));
                p.Coordinates.Add(new IfcLengthMeasure(y));
                p.Coordinates.Add(new IfcLengthMeasure(z));
                return p;
            }

            private IIfcDirection Richtung(double x, double y, double z)
            {
                IIfcDirection d = N<IIfcDirection>("IfcDirection");
                d.DirectionRatios.Add(new IfcReal(x));
                d.DirectionRatios.Add(new IfcReal(y));
                d.DirectionRatios.Add(new IfcReal(z));
                return d;
            }

            private IIfcLocalPlacement Platzierung(IIfcObjectPlacement relTo, double x, double y, double z, double rx = 1, double ry = 0)
            {
                IIfcAxis2Placement3D a = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                a.Location = Punkt(x, y, z);
                if (rx != 1 || ry != 0)
                {
                    a.Axis = Richtung(0, 0, 1);
                    a.RefDirection = Richtung(rx, ry, 0);
                }
                IIfcLocalPlacement p = N<IIfcLocalPlacement>("IfcLocalPlacement");
                p.PlacementRelTo = relTo;
                p.RelativePlacement = a;
                return p;
            }

            private IIfcSIUnit Einheit(IfcUnitEnum art, IfcSIUnitName name, IfcSIPrefix? prefix)
            {
                IIfcSIUnit u = N<IIfcSIUnit>("IfcSIUnit");
                u.UnitType = art;
                u.Name = name;
                u.Prefix = prefix;
                return u;
            }

            /// <summary>Projekt, Einheiten (mm, m², m³), Modellkontext mit Nord = +y, Grundstück.</summary>
            public void Anfang()
            {
                _projekt = Wurzel<IIfcProject>("IfcProject", "Messfall");
                IIfcUnitAssignment e = N<IIfcUnitAssignment>("IfcUnitAssignment");
                e.Units.Add(Einheit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, IfcSIPrefix.MILLI));
                e.Units.Add(Einheit(IfcUnitEnum.AREAUNIT, IfcSIUnitName.SQUARE_METRE, null));
                e.Units.Add(Einheit(IfcUnitEnum.VOLUMEUNIT, IfcSIUnitName.CUBIC_METRE, null));
                e.Units.Add(Einheit(IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN, null));
                e.Units.Add(Einheit(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT, IfcSIUnitName.DEGREE_CELSIUS, null));
                _projekt.UnitsInContext = e;

                IIfcGeometricRepresentationContext kontext = N<IIfcGeometricRepresentationContext>("IfcGeometricRepresentationContext");
                kontext.ContextType = new IfcLabel("Model");
                kontext.CoordinateSpaceDimension = new Xbim.Ifc4.GeometryResource.IfcDimensionCount(3);
                kontext.Precision = new IfcReal(1e-5);
                IIfcAxis2Placement3D wcs = N<IIfcAxis2Placement3D>("IfcAxis2Placement3D");
                wcs.Location = Punkt(0, 0, 0);
                kontext.WorldCoordinateSystem = wcs;
                kontext.TrueNorth = Richtung(0, 1, 0);
                _projekt.RepresentationContexts.Add(kontext);

                _site = Wurzel<IIfcSite>("IfcSite", "Grundstück");
                _site.CompositionType = IfcElementCompositionEnum.ELEMENT;
                _site.ObjectPlacement = Platzierung(null, 0, 0, 0);
                IIfcRelAggregates projektZerlegung = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                projektZerlegung.RelatingObject = _projekt;
                projektZerlegung.RelatedObjects.Add(_site);
            }

            public IIfcBuilding Gebaeude(string name)
            {
                IIfcBuilding g = Wurzel<IIfcBuilding>("IfcBuilding", name);
                g.CompositionType = IfcElementCompositionEnum.ELEMENT;
                g.ObjectPlacement = Platzierung(_site.ObjectPlacement, 0, 0, 0);
                _siteZerlegung = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                _siteZerlegung.RelatingObject = _site;
                _siteZerlegung.RelatedObjects.Add(g);
                return g;
            }

            public IIfcBuildingStorey Geschoss(IIfcBuilding g, string name, double hoeheMm)
            {
                IIfcBuildingStorey s = Wurzel<IIfcBuildingStorey>("IfcBuildingStorey", name);
                s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                s.Elevation = new IfcLengthMeasure(hoeheMm);
                s.ObjectPlacement = Platzierung(g.ObjectPlacement, 0, 0, hoeheMm);
                Zerlegen(g, s);
                return s;
            }

            public IIfcWallType Wandtyp(string name, double u)
            {
                IIfcWallType t = Wurzel<IIfcWallType>("IfcWallType", name);
                t.PredefinedType = IfcWallTypeEnum.STANDARD;
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", "Pset_WallCommon");
                ps.HasProperties.Add(Einzel("IsExternal", new IfcBoolean(true)));
                ps.HasProperties.Add(Einzel("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u)));
                t.HasPropertySets.Add(ps);
                return t;
            }

            /// <summary>Ein beheizter Raum mit seiner Außenwand und deren Fenster.</summary>
            public void Raum(IIfcBuildingStorey s, IIfcWallType typ, int i)
            {
                int platz = (i - 1) % RAEUME_JE_GESCHOSS;
                double x = 5000.0 * (platz % 10), y = 4000.0 * (platz / 10);

                IIfcSpace r = Wurzel<IIfcSpace>("IfcSpace", Ganz(i));
                r.LongName = new IfcLabel("Raum " + Ganz(i));
                r.CompositionType = IfcElementCompositionEnum.ELEMENT;
                r.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0);
                Zerlegen(s, r);
                Mengen(r, "BaseQuantities", Flaeche("NetFloorArea", RAUMFLAECHE_M2), Laenge("Height", 3000), Volumen("NetVolume", 60));
                Satz(r, "Pset_SpaceCommon", ("IsExternal", new IfcBoolean(false)));
                IIfcPropertyBoundedValue band = N<IIfcPropertyBoundedValue>("IfcPropertyBoundedValue");
                band.Name = new IfcIdentifier("SpaceTemperature");
                band.LowerBoundValue = new IfcThermodynamicTemperatureMeasure(18);
                band.UpperBoundValue = new IfcThermodynamicTemperatureMeasure(26);
                band.SetPointValue = new IfcThermodynamicTemperatureMeasure(20);
                IIfcPropertySet anforderung = Wurzel<IIfcPropertySet>("IfcPropertySet", "Pset_SpaceThermalRequirements");
                anforderung.HasProperties.Add(band);
                Definieren(r, anforderung);

                (double rx, double ry) = Richtungen[i % 4];
                IIfcWall w = Wurzel<IIfcWall>("IfcWall", "Außenwand " + Ganz(i));
                w.PredefinedType = IfcWallTypeEnum.STANDARD;
                w.ObjectPlacement = Platzierung(s.ObjectPlacement, x, y, 0, rx, ry);
                Enthalten(s, w);
                if (!_typisiert.TryGetValue(s.EntityLabel, out IIfcRelDefinesByType rel))
                {
                    rel = Wurzel<IIfcRelDefinesByType>("IfcRelDefinesByType", null);
                    rel.RelatingType = typ;
                    _typisiert[s.EntityLabel] = rel;
                }
                rel.RelatedObjects.Add(w);
                Mengen(w, "BaseQuantities", Flaeche("GrossSideArea", 12.0), Flaeche("NetSideArea", 10.2));

                IIfcRelSpaceBoundary grenze = Wurzel<IIfcRelSpaceBoundary>("IfcRelSpaceBoundary", "2ndLevel");
                grenze.Description = new IfcText("2a");
                grenze.RelatingSpace = r;
                grenze.RelatedBuildingElement = w;
                grenze.PhysicalOrVirtualBoundary = IfcPhysicalOrVirtualEnum.PHYSICAL;
                grenze.InternalOrExternalBoundary = IfcInternalOrExternalEnum.EXTERNAL;

                IIfcOpeningElement o = Wurzel<IIfcOpeningElement>("IfcOpeningElement", "Öffnung " + Ganz(i));
                IIfcRelVoidsElement v = Wurzel<IIfcRelVoidsElement>("IfcRelVoidsElement", null);
                v.RelatingBuildingElement = w;
                v.RelatedOpeningElement = o;
                IIfcWindow f = Wurzel<IIfcWindow>("IfcWindow", "Fenster " + Ganz(i));
                Enthalten(s, f);
                IIfcRelFillsElement fuellung = Wurzel<IIfcRelFillsElement>("IfcRelFillsElement", null);
                fuellung.RelatingOpeningElement = o;
                fuellung.RelatedBuildingElement = f;
                Mengen(f, "Qto_WindowBaseQuantities", Flaeche("Area", 1.8));
                Satz(f, "Pset_WindowCommon", ("IsExternal", new IfcBoolean(true)), ("ThermalTransmittance", new IfcThermalTransmittanceMeasure(1.1)));
            }

            private void Zerlegen(IIfcObjectDefinition ganzes, IIfcObjectDefinition teil)
            {
                if (!_zerlegung.TryGetValue(ganzes.EntityLabel, out IIfcRelAggregates r))
                {
                    r = Wurzel<IIfcRelAggregates>("IfcRelAggregates", null);
                    r.RelatingObject = ganzes;
                    _zerlegung[ganzes.EntityLabel] = r;
                }
                r.RelatedObjects.Add(teil);
            }

            private void Enthalten(IIfcSpatialElement ort, IIfcProduct teil)
            {
                if (!_enthalten.TryGetValue(ort.EntityLabel, out IIfcRelContainedInSpatialStructure r))
                {
                    r = Wurzel<IIfcRelContainedInSpatialStructure>("IfcRelContainedInSpatialStructure", null);
                    r.RelatingStructure = ort;
                    _enthalten[ort.EntityLabel] = r;
                }
                r.RelatedElements.Add(teil);
            }

            private IIfcPropertySingleValue Einzel(string name, IIfcValue wert)
            {
                IIfcPropertySingleValue p = N<IIfcPropertySingleValue>("IfcPropertySingleValue");
                p.Name = new IfcIdentifier(name);
                p.NominalValue = wert;
                return p;
            }

            private void Satz(IIfcObject o, string name, params (string Name, IIfcValue Wert)[] werte)
            {
                IIfcPropertySet ps = Wurzel<IIfcPropertySet>("IfcPropertySet", name);
                foreach ((string n, IIfcValue w) in werte) ps.HasProperties.Add(Einzel(n, w));
                Definieren(o, ps);
            }

            private void Mengen(IIfcObject o, string name, params IIfcPhysicalQuantity[] mengen)
            {
                IIfcElementQuantity q = Wurzel<IIfcElementQuantity>("IfcElementQuantity", name);
                foreach (IIfcPhysicalQuantity m in mengen) q.Quantities.Add(m);
                Definieren(o, q);
            }

            private void Definieren(IIfcObject o, IIfcPropertySetDefinition d)
            {
                IIfcRelDefinesByProperties rel = Wurzel<IIfcRelDefinesByProperties>("IfcRelDefinesByProperties", null);
                rel.RelatedObjects.Add(o);
                rel.RelatingPropertyDefinition = d;
            }

            private IIfcQuantityArea Flaeche(string name, double wert)
            {
                IIfcQuantityArea q = N<IIfcQuantityArea>("IfcQuantityArea");
                q.Name = new IfcLabel(name);
                q.AreaValue = new IfcAreaMeasure(wert);
                return q;
            }

            private IIfcQuantityLength Laenge(string name, double wert)
            {
                IIfcQuantityLength q = N<IIfcQuantityLength>("IfcQuantityLength");
                q.Name = new IfcLabel(name);
                q.LengthValue = new IfcLengthMeasure(wert);
                return q;
            }

            private IIfcQuantityVolume Volumen(string name, double wert)
            {
                IIfcQuantityVolume q = N<IIfcQuantityVolume>("IfcQuantityVolume");
                q.Name = new IfcLabel(name);
                q.VolumeValue = new IfcVolumeMeasure(wert);
                return q;
            }

            /// <summary>Schließt ab und schreibt STEP mit festem Kopf; Zeilenenden LF.</summary>
            public byte[] Speichern(string dateiname)
            {
                _t.Commit();
                IStepFileHeader kopf = _m.Header;
                kopf.FileDescription.Description.Clear();
                kopf.FileDescription.Description.Add("ViewDefinition [ReferenceView]");
                kopf.FileName.Name = dateiname;
                kopf.FileName.TimeStamp = ZEITSTEMPEL;
                kopf.FileName.AuthorName.Clear();
                kopf.FileName.AuthorName.Add("");
                kopf.FileName.Organization.Clear();
                kopf.FileName.Organization.Add("");
                kopf.FileName.PreprocessorVersion = "EPOS-Plan Messfall (EPOS.Kern)";
                kopf.FileName.OriginatingSystem = "EPOS-Plan Messfall, synthetisch";
                kopf.FileName.AuthorizationName = "";
                var ziel = new MemoryStream();
                _m.SaveAsStep21(ziel, null, true);
                return OhneCr(ziel.ToArray());
            }
        }

        /// <summary>Entfernt jedes CR in place — die Zeilenenden der Plattform werden zu LF.</summary>
        private static byte[] OhneCr(byte[] daten)
        {
            int n = 0;
            for (int i = 0; i < daten.Length; i++)
                if (daten[i] != (byte)'\r') daten[n++] = daten[i];
            if (n == daten.Length) return daten;
            Array.Resize(ref daten, n);
            return daten;
        }

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);

        private static string Ganz6(int w) => w.ToString("D6", CultureInfo.InvariantCulture);
    }
}
