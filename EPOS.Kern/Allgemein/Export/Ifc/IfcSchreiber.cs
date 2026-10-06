using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Common.Step21;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Memory;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der IFC-Schreiber, Stufe S1</b> (Stufe G7c; Datenaustauschkonzept 6.2 bis 6.5; Entscheide E27, E67)
    /// — der semantische Export ohne Geometrie: IFC4 über <c>Xbim.IO.MemoryModel</c> (nie <c>IfcStore</c>,
    /// der zöge Esent), Speichern als STEP (Part 21) in den Zielstrom, ohne Dateisystem.
    ///
    /// <para><b>Was entsteht</b> (Abbildungstabelle 6.3): <c>IfcProject</c> mit der von Hand gebauten
    /// <c>IfcUnitAssignment</c> (Meter, Quadratmeter, Kubikmeter, Joule, Watt, Kelvin, Radiant) und der
    /// <c>IfcConversionBasedUnit</c> KILOWATTHOUR; <c>IfcSite</c> mit Postleitzahl und, soweit das Abbild
    /// sie trägt, <c>RefLatitude</c>/<c>RefLongitude</c>; <c>IfcBuilding</c>; je Raum ein <c>IfcSpace</c>,
    /// bei mehr als einer Zone dazu je Zone eine <c>IfcZone</c>; je Fläche ein <c>IfcWall</c> oder
    /// <c>IfcSlab</c>, je Öffnung ein <c>IfcWindow</c> oder <c>IfcDoor</c> in einem geometrielosen
    /// <c>IfcOpeningElement</c>; je Nachbarraum eine <c>IfcRelSpaceBoundary2ndLevel</c> ohne
    /// <c>ConnectionGeometry</c>; Aufbauten als <c>IfcMaterialLayerSet</c> mit
    /// <c>IfcRelAssociatesMaterial</c>. Struktur über <c>IfcRelAggregates</c> und
    /// <c>IfcRelContainedInSpatialStructure</c>; kein erzwungenes Geschoss.</para>
    ///
    /// <para><b>Stufe S3 — schematische Körper</b> (Stufe G7e; 6.7, 8.4): nach derselben Regel wie gbXML Stufe 2
    /// (<see cref="GbxmlSchreiber.Raumgeometrie"/>). Liefert das Zonengeometrie-Modell schematische Rechtecke, trägt
    /// jeder Raum sein Prisma und jede Fläche und Öffnung ihre Platte (<see cref="IfcKoerper"/>); jedes Produkt
    /// bekommt dann ein <c>ObjectPlacement</c> (Kette Grundstück → Gebäude → Raum bzw. Bauteil), auch das ohne
    /// Körper. Die Kennzeichnung „schematisch“ steht an vier Stellen: <c>IfcProject.Name</c>,
    /// <c>FILE_DESCRIPTION</c>, <c>Description</c> jedes Produkts mit Körper und eine <c>IfcAnnotation</c> am
    /// Gebäude. Zusammengefasste Bauteile (Klassenweg) bleiben ohne Körper. Ohne Rechteck bleibt die Datei S1 ohne
    /// Geometrie und ohne Vermerk; bei widersprüchlicher Anordnung S1 mit Vermerk.</para>
    ///
    /// <para><b>Einheiten:</b> Temperaturen stehen in Kelvin (die globale Einheit), Winkel in Grad mit
    /// ausdrücklichem <c>Unit</c> (<c>IfcConversionBasedUnit</c> DEGREE), jede Energie in kWh mit
    /// ausdrücklichem <c>Unit</c> KILOWATTHOUR — wer <c>IfcEnergyMeasure</c> ohne <c>Unit</c> schreibt,
    /// behauptet Joule (6.4).</para>
    ///
    /// <para><b>Kennungen und Zeitstempel:</b> <c>GlobalId</c> deterministisch (<see cref="IfcExportKennung"/>);
    /// genau eine <c>IfcOwnerHistory</c> an jeder <c>IfcRoot</c>-Instanz. Die Uhr des Profils wird einmal
    /// abgelesen; der Wert steht im Dateikopf (<c>FILE_NAME</c>) und als <c>CreationDate</c> der
    /// <c>IfcOwnerHistory</c> — beide sind Pflicht. Sonst ist die Datei byte-gleich wiederholbar.</para>
    ///
    /// <para><b>Prüfung vor dem Schreiben:</b> <see cref="Validator"/> mit Attribut- und Inversenprüfung
    /// (<see cref="ValidationFlags.All"/>). Jeder Verstoß wird eine Meldung der Stufe Fehler, der Export
    /// bricht benannt ab (<c>GEXP_PROT_IFC_ABGEBROCHEN</c>) und der Zielstrom bleibt unberührt.</para>
    /// </summary>
    internal sealed class IfcSchreiber : IGebaeudeSchreiber
    {
        /// <summary>Höchstzahl einzeln gemeldeter Schemaverstöße; der Rest steht in der Summe.</summary>
        internal const int MELDUNGEN_SCHEMA_MAX = 20;

        /// <summary>Der Name der Umrechnungseinheit kWh (6.4).</summary>
        internal const string KILOWATTSTUNDE = "KILOWATTHOUR";

        /// <summary>Der Name der Umrechnungseinheit Grad.</summary>
        internal const string GRAD = "DEGREE";

        /// <summary>Kelvin bei 0 °C.</summary>
        internal const double NULLPUNKT_K = 273.15;

        /// <summary>Wert von <c>EPOS_Bauteil.Raumgrenze</c>: die Raumgrenzen sind logisch, nicht vermessen (6.3).</summary>
        internal const string RAUMGRENZE_LOGISCH = "logisch";

        /// <summary>Wert von <c>EPOS_Bauteil.Schichtrichtung</c>: die Reihenfolge der Schichten in der Datei.</summary>
        internal const string SCHICHTRICHTUNG_AUSSEN_INNEN = "AussenNachInnen";

        /// <summary>Präfix der Kennungen des Klassenwegs: dessen Bauteile sind Zusammenfassungen (6.3).</summary>
        internal const string PRAEFIX_ZUSAMMENFASSUNG = "epos-klasse-";

        // Die Namen der eigenen Sätze — nie Pset_ oder Qto_ (6.4, Regel PSE001).
        internal const string EPOS_GEBAEUDE = "EPOS_Gebaeude";
        internal const string EPOS_ZONE = "EPOS_Zone";
        internal const string EPOS_BAUTEIL = "EPOS_Bauteil";
        internal const string EPOS_ERGEBNIS = "EPOS_Ergebnis";
        internal const string EPOS_RECHENLAUF = "EPOS_Rechenlauf";
        internal const string EPOS_BAUSTOFF = "EPOS_Baustoff";

        // Die Meldungen.
        internal const string OHNE_ERGEBNIS = GebaeudeExportAblauf.P + "IFC_OHNE_ERGEBNIS";
        internal const string OHNE_KOORDINATEN = GebaeudeExportAblauf.P + "IFC_OHNE_KOORDINATEN";
        internal const string SCHEMA = GebaeudeExportAblauf.P + "IFC_SCHEMA";
        internal const string ABGEBROCHEN = GebaeudeExportAblauf.P + "IFC_ABGEBROCHEN";

        // Die Kennzeichnung der Stufe S3 (8.4).
        internal const string DATEI_STUFE_S1 = "GEXP_IFC_DATEI_STUFE";
        internal const string DATEI_STUFE_S3 = "GEXP_IFC_DATEI_STUFE_S3";
        internal const string DATEI_ABGELEHNT = "GEXP_IFC_DATEI_ABGELEHNT";
        internal const string PROJEKT_SCHEMATISCH = "GEXP_IFC_PROJEKT_SCHEMATISCH";
        internal const string ELEMENT_SCHEMATISCH = "GEXP_IFC_ELEMENT_SCHEMATISCH";
        internal const string KENNZEICHNUNG_SCHEMATISCH = "GEXP_IFC_KENNZEICHNUNG_SCHEMATISCH";
        /// <summary>HC-5: die Stufenzeile, wenn jeder Körper ein Prisma aus dem Grundriss der Importdatei ist.</summary>
        internal const string DATEI_STUFE_S3_GRUNDRISS = "GEXP_IFC_DATEI_STUFE_S3_GRUNDRISS";
        /// <summary>HC-5: die Kennzeichnung eines Raums mit Prismen aus dem Grundriss; {0} = Vermerke (leer = keine).</summary>
        internal const string ELEMENT_GRUNDRISS = "GEXP_IFC_ELEMENT_GRUNDRISS";

        /// <summary>HC-5c: der Vermerk einer Bauteilplatte an einem Grundriss-Prisma (Kantenzuordnung).</summary>
        internal const string ELEMENT_PLATTE = "GEXP_IFC_ELEMENT_PLATTE";
        internal const string GEOMETRIE_ABGELEHNT_TEXT = "GEXP_IFC_GEOMETRIE_ABGELEHNT";

        /// <summary>Der Name der <c>IfcAnnotation</c>, die die Datei als schematisch kennzeichnet.</summary>
        internal const string KENNZEICHNUNG_NAME = "EPOS-Plan";

        private readonly IfcErgebnisse _ergebnisse;

        /// <summary>Legt den Schreiber an.</summary>
        /// <param name="ergebnisse">Die Rechenergebnisse für <c>EPOS_Ergebnis</c>; <c>null</c> = keine (benannt weggelassen).</param>
        internal IfcSchreiber(IfcErgebnisse ergebnisse = null)
        {
            _ergebnisse = ergebnisse;
        }

        /// <summary>
        /// Ein Eingriff in das fertige Modell vor der Prüfung — <b>nur für die Proben</b> (Probe 10: eine
        /// künstlich entfernte <c>GlobalId</c> muss gemeldet werden).
        /// </summary>
        internal Action<IModel> VorDerPruefung { get; set; }

        /// <summary>
        /// Die Vorschau des IFC-Formats: der Beipackzettel (logische Raumgrenzen ohne Anschlussgeometrie, ohne MVD,
        /// die IDS-Datei der Auslieferung), dazu „ohne Ergebnis", wenn der Schreiber keine Ergebnisse trägt, und
        /// „ohne Koordinaten", wenn Breite oder Länge fehlen — dieselben Bedingungen wie beim Schreiben. Dazu die
        /// Meldungen der Raumgeometrie wie bei gbXML (<see cref="GbxmlSchreiber.Geometriemeldungen"/>: schematische
        /// Körper oder benannt abgelehnt), sonst bei einem Gebäude ohne Körper „Daten ohne Geometrie“.
        /// </summary>
        public IReadOnlyList<PruefMeldung> Vorschau(GebaeudeAbbild abbild, GebaeudeExportProfil profil)
        {
            var meldungen = new List<PruefMeldung>
            {
                new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.BEIPACK_RAUMGRENZEN),
                new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.BEIPACK_OHNE_MVD),
                new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.BEIPACK_IDS, GebaeudeExportAblauf.IDS_DATEI),
            };
            if (abbild == null) return meldungen;
            if (_ergebnisse == null && abbild.Gebaeude.Count > 0) meldungen.Add(new PruefMeldung(PruefStufe.Info, OHNE_ERGEBNIS));
            if (!(abbild.BreiteGrad.HasValue && abbild.LaengeGrad.HasValue)) meldungen.Add(new PruefMeldung(PruefStufe.Info, OHNE_KOORDINATEN));
            IReadOnlyList<PruefMeldung> geometrie = GbxmlSchreiber.Geometriemeldungen(abbild);
            meldungen.AddRange(geometrie);
            if (geometrie.Count == 0 && abbild.Gebaeude.Count == 1)
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GbxmlSchreiber.GEOMETRIE_OHNE));
            if (abbild.Gebaeude.Any(g => g.Raeume.Any(r => r.Konditionierung != null)))
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeExportAblauf.BEIPACK_KONDITIONIERUNG));
            return meldungen;
        }

        /// <inheritdoc />
        public GebaeudeExportBilanz Schreiben(GebaeudeAbbild abbild, Stream ziel, GebaeudeExportProfil profil, CancellationToken abbruch)
        {
            if (abbild == null) throw new ArgumentNullException(nameof(abbild));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            abbruch.ThrowIfCancellationRequested();

            using (var modell = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4(), NullLoggerFactory.Instance, 0))
            {
                var lauf = new Lauf(modell, abbild, profil, _ergebnisse, abbruch);
                using (ITransaction t = modell.BeginTransaction("EPOS-Plan IFC-Export"))
                {
                    lauf.Bauen();
                    t.Commit();
                }
                VorDerPruefung?.Invoke(modell);
                abbruch.ThrowIfCancellationRequested();

                List<PruefMeldung> verstoesse = Pruefen(modell);
                if (verstoesse.Count > 0)
                {
                    lauf.Meldungen.AddRange(verstoesse.Take(MELDUNGEN_SCHEMA_MAX));
                    lauf.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, ABGEBROCHEN, verstoesse.Count.ToString(CultureInfo.InvariantCulture)));
                    return new GebaeudeExportBilanz(lauf.Flaechen, lauf.Oeffnungen, lauf.Aufbauten, lauf.Ersatzaufbauten, lauf.Meldungen, 0);
                }

                byte[] bytes = Speichern(modell, lauf.Zeitstempel, profil, lauf.Dateibeschreibung);
                abbruch.ThrowIfCancellationRequested();
                ziel.Write(bytes, 0, bytes.Length);
                ziel.Flush();
                return new GebaeudeExportBilanz(lauf.Flaechen, lauf.Oeffnungen, lauf.Aufbauten, lauf.Ersatzaufbauten, lauf.Meldungen, bytes.Length);
            }
        }

        /// <summary>
        /// Die Schemaprüfung des ganzen Modells mit Attribut- und Inversenprüfung; je Verstoß eine Meldung
        /// der Stufe Fehler (<c>GEXP_PROT_IFC_SCHEMA</c>: Entität und Befund).
        /// </summary>
        internal static List<PruefMeldung> Pruefen(IModel modell)
        {
            var pruefer = new Validator { ValidateLevel = ValidationFlags.All, CreateEntityHierarchy = true };
            var meldungen = new List<PruefMeldung>();
            foreach (ValidationResult r in pruefer.Validate(modell))
                Sammeln(r, meldungen);
            return meldungen;
        }

        private static void Sammeln(ValidationResult r, List<PruefMeldung> meldungen)
        {
            List<ValidationResult> details = r.Details?.ToList() ?? new List<ValidationResult>();
            if (details.Count == 0 || !string.IsNullOrWhiteSpace(r.IssueSource) && r.IssueType != ValidationFlags.None)
            {
                string ort = r.Item is IPersistEntity e
                    ? e.ExpressType.ExpressName + " #" + e.EntityLabel.ToString(CultureInfo.InvariantCulture)
                    : r.Item?.GetType().Name ?? "?";
                string befund = string.Join(" ", new[] { r.IssueSource, r.Message }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (details.Count == 0 || befund.Length > 0) meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SCHEMA, ort, befund));
            }
            foreach (ValidationResult d in details) Sammeln(d, meldungen);
        }

        /// <summary>
        /// Der Dateikopf und die Byteform: keine MVD-Angabe (6.2), Zeitstempel aus der einen Ablesung der
        /// Uhr, keine Anwenderdaten; Zeilenende CRLF unabhängig von der Plattform. <paramref name="beschreibung"/>
        /// sind die Zeilen von <c>FILE_DESCRIPTION</c>; ohne sie steht dort die Stufe S1.
        /// </summary>
        internal static byte[] Speichern(MemoryModel modell, DateTime zeit, GebaeudeExportProfil profil, IReadOnlyList<string> beschreibung = null)
        {
            IStepFileHeader kopf = modell.Header;
            kopf.FileDescription.Description.Clear();
            if (beschreibung == null || beschreibung.Count == 0) kopf.FileDescription.Description.Add(T(profil, DATEI_STUFE_S1));
            else foreach (string zeile in beschreibung) kopf.FileDescription.Description.Add(zeile);
            kopf.FileDescription.ImplementationLevel = "2;1";
            kopf.FileName.Name = profil.Programmname + " IFC";
            kopf.FileName.TimeStamp = Zeitstempel(zeit);
            kopf.FileName.AuthorName.Clear();
            kopf.FileName.AuthorName.Add("");
            kopf.FileName.Organization.Clear();
            kopf.FileName.Organization.Add("");
            kopf.FileName.PreprocessorVersion = profil.Programmname + " " + profil.Programmversion;
            kopf.FileName.OriginatingSystem = profil.Programmname;
            kopf.FileName.AuthorizationName = "";
            using (var text = new StringWriter(CultureInfo.InvariantCulture))
            {
                modell.SaveAsStep21(text, null);
                string inhalt = text.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
                return new UTF8Encoding(false).GetBytes(inhalt);
            }
        }

        /// <summary>Der Zeitstempel des Dateikopfs: ISO 8601 auf Sekunden.</summary>
        internal static string Zeitstempel(DateTime zeit) => GbxmlSchreiber.Zeitstempel(zeit);

        /// <summary>Ein Text in der Sprache des Profils.</summary>
        internal static string T(GebaeudeExportProfil profil, string schluessel)
            => MyResource.Resource.ResourceManager.GetString(schluessel, profil.Sprache) ?? schluessel;

        /// <summary>
        /// Ein Winkel in Grad als <c>IfcCompoundPlaneAngleMeasure</c>: Grad, Minuten, Sekunden,
        /// Millionstelsekunden — <b>alle Glieder mit demselben Vorzeichen</b> (Befund S 1.2).
        /// </summary>
        internal static List<long> GradMinutenSekunden(double grad)
        {
            if (double.IsNaN(grad) || double.IsInfinity(grad)) throw new InvalidOperationException("IFC-Export: Eine Koordinate ist nicht endlich.");
            long vorzeichen = grad < 0.0 ? -1 : 1;
            long mikro = (long)Math.Round(Math.Abs(grad) * 3600.0 * 1e6, MidpointRounding.AwayFromZero);
            long g = mikro / 3_600_000_000L;
            mikro -= g * 3_600_000_000L;
            long m = mikro / 60_000_000L;
            mikro -= m * 60_000_000L;
            long s = mikro / 1_000_000L;
            mikro -= s * 1_000_000L;
            return new List<long> { vorzeichen * g, vorzeichen * m, vorzeichen * s, vorzeichen * mikro };
        }

        // ==================================================================
        //  Ein Schreibdurchgang
        // ==================================================================

        private sealed class Lauf
        {
            private readonly MemoryModel _m;
            private readonly GebaeudeAbbild _abbild;
            private readonly GebaeudeExportProfil _profil;
            private readonly IfcErgebnisse _ergebnisse;
            private readonly CancellationToken _abbruch;
            private readonly IfcExportKennung _kennung = new IfcExportKennung();
            private readonly Dictionary<string, IfcSpace> _raeume = new Dictionary<string, IfcSpace>(StringComparer.Ordinal);
            private readonly List<IfcElement> _elemente = new List<IfcElement>();
            private readonly Dictionary<string, IfcMaterialLayerSet> _saetze = new Dictionary<string, IfcMaterialLayerSet>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<IfcElement>> _satzElemente = new Dictionary<string, List<IfcElement>>(StringComparer.Ordinal);
            private readonly List<string> _satzReihenfolge = new List<string>();
            private readonly Dictionary<string, IfcMaterial> _stoffe = new Dictionary<string, IfcMaterial>(StringComparer.Ordinal);
            private IfcOwnerHistory _geschichte;
            private IfcConversionBasedUnit _kwh;
            private IfcConversionBasedUnit _grad;
            private bool _mehrzonig;
            private Zonengeometrie _geometrie;
            private Zonenkoerper _koerper;
            private IfcKoerper _form;
            private IfcLocalPlacement _siteLage;
            private IfcLocalPlacement _gebaeudeLage;
            private readonly List<string> _ohneKoerper = new List<string>();
            private readonly List<string> _begrenzt = new List<string>();

            internal Lauf(MemoryModel m, GebaeudeAbbild abbild, GebaeudeExportProfil profil, IfcErgebnisse ergebnisse, CancellationToken abbruch)
            {
                _m = m;
                _abbild = abbild;
                _profil = profil;
                _ergebnisse = ergebnisse;
                _abbruch = abbruch;
            }

            internal int Flaechen { get; private set; }
            internal int Oeffnungen { get; private set; }
            internal int Aufbauten => _saetze.Count;
            internal int Ersatzaufbauten { get; private set; }
            internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();
            internal DateTime Zeitstempel { get; private set; }

            /// <summary>Die Zeilen von <c>FILE_DESCRIPTION</c>: die Stufe, bei S3 die Kennzeichnung, bei abgelehnter Anordnung der Vermerk.</summary>
            internal List<string> Dateibeschreibung { get; } = new List<string>();

            /// <summary>Die Zahl der geschriebenen Körper (<c>IfcExtrudedAreaSolid</c>); 0 = Stufe S1.</summary>
            internal int Koerperzahl => _form?.Koerper ?? 0;

            private T Neu<T>(Action<T> belegen = null) where T : IInstantiableEntity
                => _m.Instances.New(belegen ?? (_ => { }));

            /// <summary>Die eine Erzeugungsfunktion für jede <c>IfcRoot</c>-Instanz: <c>GlobalId</c> und <c>OwnerHistory</c>.</summary>
            private T Wurzel<T>(string schluessel, string rolle, Action<T> belegen = null) where T : IfcRoot, IInstantiableEntity
            {
                IfcGloballyUniqueId id = _kennung.Vergeben(schluessel, rolle);
                return Neu<T>(e =>
                {
                    e.GlobalId = id;
                    e.OwnerHistory = _geschichte;
                    belegen?.Invoke(e);
                });
            }

            internal void Bauen()
            {
                if (_abbild.Gebaeude.Count != 1)
                    throw new InvalidOperationException("IFC-Export: Das Abbild trägt " + _abbild.Gebaeude.Count.ToString(CultureInfo.InvariantCulture)
                                                        + " Gebäude; exportiert wird genau eines.");
                AbbildGebaeude g = _abbild.Gebaeude[0];
                string wurzel = string.IsNullOrWhiteSpace(_abbild.CampusKennung) ? g.Kennung : _abbild.CampusKennung;

                Zeitstempel = _profil.Uhr();
                // Die Raumgeometrie nach derselben Regel wie gbXML Stufe 2: Körper nur aus schematischen Rechtecken.
                (_geometrie, _koerper) = GbxmlSchreiber.Raumgeometrie(_abbild);
                Dateibeschreibung.Add(T(_profil, _koerper == null ? DATEI_STUFE_S1 : _koerper.Schematisch ? DATEI_STUFE_S3 : DATEI_STUFE_S3_GRUNDRISS));
                if (_geometrie.AnordnungAbgelehnt) Dateibeschreibung.Add(T(_profil, DATEI_ABGELEHNT));
                Geschichte();
                if (_koerper != null) _form = new IfcKoerper(_m);
                // HC-5c: Stehen alle Körper als Prisma aus dem Grundriss, trägt der Kontext die Nordrichtung der Quelle.
                if (_form != null && GbxmlSchreiber.Exportnordwinkel(_geometrie, _koerper) is double nord) _form.Nordrichtung(nord);
                IfcProject projekt = Projekt(wurzel, g);
                IfcSite site = Grundstueck(wurzel);
                IfcBuilding gebaeude = Gebaeude(g);
                Aggregieren(wurzel + "#Projekt", projekt, new IfcObjectDefinition[] { site });
                Aggregieren(wurzel + "#Site", site, new IfcObjectDefinition[] { gebaeude });

                _mehrzonig = g.Raeume.Where(r => r.ZonenKennung != null).Select(r => r.ZonenKennung).Distinct(StringComparer.Ordinal).Count() > 1;
                foreach (AbbildRaum r in g.Raeume) _raeume[r.Kennung] = Raum(r);
                Aggregieren(g.Kennung, gebaeude, g.Raeume.Select(r => (IfcObjectDefinition)_raeume[r.Kennung]).ToArray());
                if (_mehrzonig) Zonen(g);

                foreach (AbbildBauteil b in g.Bauteile)
                {
                    _abbruch.ThrowIfCancellationRequested();
                    Flaeche(b);
                }
                IfcAnnotation kennzeichnung = Kennzeichnung(g);
                if (_elemente.Count > 0 || kennzeichnung != null)
                    Wurzel<IfcRelContainedInSpatialStructure>(g.Kennung, "RelContained", r =>
                    {
                        r.RelatingStructure = gebaeude;
                        r.RelatedElements.AddRange(_elemente);
                        if (kennzeichnung != null) r.RelatedElements.Add(kennzeichnung);
                    });
                foreach (string kennung in _satzReihenfolge)
                {
                    IfcMaterialLayerSet satz = _saetze[kennung];
                    // Schlüssel ist das Gebäude: derselbe Aufbau an zwei Gebäuden ergibt zwei Kennungen.
                    Wurzel<IfcRelAssociatesMaterial>(g.Kennung, "RelMaterial:" + kennung, r =>
                    {
                        r.RelatingMaterial = satz;
                        r.RelatedObjects.AddRange(_satzElemente[kennung]);
                    });
                }

                if (_ergebnisse == null) Meldungen.Add(new PruefMeldung(PruefStufe.Info, OHNE_ERGEBNIS));
                Sammelmeldung(GbxmlSchreiber.FLAECHE_OHNE_POLYGON, _ohneKoerper);
                if (_koerper != null && GbxmlSchreiber.NordwinkelAngenommen(_geometrie) is string ohneNord)
                    Meldungen.Add(new PruefMeldung(PruefStufe.Info, GbxmlSchreiber.NORDWINKEL_ANGENOMMEN, ohneNord));
                Sammelmeldung(GbxmlSchreiber.OEFFNUNG_BEGRENZT, _begrenzt);
            }

            // --------------------------------------------------------------
            //  Schematische Körper (Stufe S3)
            // --------------------------------------------------------------

            /// <summary>Eine Platzierung im Ursprung relativ zum Gebäude — nur in Stufe S3, sonst <c>null</c>.</summary>
            private IfcLocalPlacement Lage() => _form?.Platzierung(_gebaeudeLage);

            /// <summary>Der Text der Kennzeichnung je Produkt mit Körper.</summary>
            private string Vermerk => T(_profil, ELEMENT_SCHEMATISCH);

            /// <summary>Hängt Platzierung und, soweit vorhanden, Körper samt Vermerk an ein Bauteil (nur Stufe S3).</summary>
            /// <param name="vermerk">Der Vermerk; <c>null</c> = der schematische.</param>
            private void Koerper(IfcProduct e, IfcProductDefinitionShape form, string vermerk = null)
            {
                if (_form == null) return;
                e.ObjectPlacement = Lage();
                if (form == null) return;
                e.Representation = form;
                vermerk ??= Vermerk;
                string bisher = e.Description?.Value?.ToString();
                e.Description = Text(string.IsNullOrWhiteSpace(bisher) ? vermerk : bisher.Trim() + " – " + vermerk);
            }

            /// <summary>HC-5: der Vermerk eines Raums mit Prismen aus dem Grundriss, samt den Vermerken der Grundrisse.</summary>
            private string Grundrissvermerk(string kennung)
            {
                Raumumriss u = _geometrie.Raum(kennung);
                string vermerke = u == null || u.Grundrissvermerke.Count == 0 ? "" : " (" + string.Join(", ", u.Grundrissvermerke.Select(v => v.ToString())) + ")";
                return string.Format(_profil.Sprache, T(_profil, ELEMENT_GRUNDRISS), vermerke).Trim();
            }

            /// <summary>
            /// Die <c>IfcAnnotation</c> der Kennzeichnung am Gebäude (8.4, vierte Stelle): Name, Beschreibung mit dem
            /// Text der Kennzeichnung, Platzierung im Ursprung; ohne Darstellung. Nur in Stufe S3.
            /// </summary>
            private IfcAnnotation Kennzeichnung(AbbildGebaeude g)
            {
                if (_form == null || !_koerper.Schematisch) return null;
                return Wurzel<IfcAnnotation>(g.Kennung, "Kennzeichnung", a =>
                {
                    a.Name = Label(KENNZEICHNUNG_NAME);
                    a.Description = Text(T(_profil, KENNZEICHNUNG_SCHEMATISCH));
                    a.ObjectType = Label(T(_profil, DATEI_STUFE_S3));
                    a.ObjectPlacement = Lage();
                });
            }

            /// <summary>Die Fläche eines Bauteils im Körper seines ersten Nachbarraums; Zusammenfassung und innere Masse haben keine.</summary>
            private Koerperflaeche Koerperflaeche(AbbildBauteil f)
            {
                if (_koerper == null || f.Nachbarn.Count == 0 || InnereMasse(f)
                    || f.Kennung.StartsWith(PRAEFIX_ZUSAMMENFASSUNG, StringComparison.Ordinal)) return null;
                foreach (AbbildNachbar n in f.Nachbarn)
                {
                    Koerperflaeche k = _koerper.Flaeche(n.Kennung, f.Kennung);
                    if (k != null) return k;
                }
                return null;
            }

            /// <summary>
            /// HC-5c: die Platten eines Bauteils, wenn seine Fläche an einem Grundriss-Prisma steht (je Kante eine, die größte zuerst);
            /// <c>null</c> = keine oder ein Rechteckkörper (dort bleibt es bei der einen Fläche).
            /// </summary>
            private IReadOnlyList<Koerperflaeche> Plattenflaechen(AbbildBauteil f, Koerperflaeche wand)
            {
                if (wand == null || _koerper?.Raum(wand.RaumKennung)?.AusGrundriss != true) return null;
                return _koerper.Flaechen(wand.RaumKennung, f.Kennung);
            }

            /// <summary>Innere Masse einer Zone: beide Nachbarn sind derselbe Raum — sie liegt an keiner Kante.</summary>
            private static bool InnereMasse(AbbildBauteil f)
                => f.Nachbarn.Count == 2 && string.Equals(f.Nachbarn[0].Kennung, f.Nachbarn[1].Kennung, StringComparison.Ordinal);

            private void Sammelmeldung(string schluessel, List<string> namen)
            {
                if (namen.Count == 0) return;
                string beispiele = namen.Count <= 5 ? string.Join(", ", namen) : string.Join(", ", namen.Take(5)) + ", …";
                Meldungen.Add(new PruefMeldung(PruefStufe.Info, schluessel, namen.Count.ToString(CultureInfo.InvariantCulture), beispiele));
            }

            // --------------------------------------------------------------
            //  Kopf: Geschichte, Projekt, Einheiten, Grundstück, Gebäude
            // --------------------------------------------------------------

            private void Geschichte()
            {
                IfcOrganization org = Neu<IfcOrganization>(o => o.Name = _profil.Programmname);
                IfcPerson person = Neu<IfcPerson>(p => p.FamilyName = _profil.Programmname);
                IfcPersonAndOrganization nutzer = Neu<IfcPersonAndOrganization>(n =>
                {
                    n.ThePerson = person;
                    n.TheOrganization = org;
                });
                IfcApplication programm = Neu<IfcApplication>(a =>
                {
                    a.ApplicationDeveloper = org;
                    a.Version = _profil.Programmversion;
                    a.ApplicationFullName = _profil.Programmname;
                    a.ApplicationIdentifier = _profil.Programmname;
                });
                long sekunden = (long)Math.Floor((DateTime.SpecifyKind(Zeitstempel, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalSeconds);
                if (Zeitstempel.Kind == DateTimeKind.Local) sekunden = new DateTimeOffset(Zeitstempel).ToUnixTimeSeconds();
                _geschichte = Neu<IfcOwnerHistory>(h =>
                {
                    h.OwningUser = nutzer;
                    h.OwningApplication = programm;
                    // ChangeAction bleibt leer: ohne LastModifiedDate erlaubt die Regel CorrectChangeAction nur leer, NOCHANGE oder NOTDEFINED.
                    h.CreationDate = new IfcTimeStamp(sekunden);
                });
            }

            private IfcProject Projekt(string wurzel, AbbildGebaeude g)
            {
                IfcUnitAssignment einheiten = Neu<IfcUnitAssignment>(u => u.Units.AddRange(new IfcUnit[]
                {
                    Si(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE),
                    Si(IfcUnitEnum.AREAUNIT, IfcSIUnitName.SQUARE_METRE),
                    Si(IfcUnitEnum.VOLUMEUNIT, IfcSIUnitName.CUBIC_METRE),
                    Si(IfcUnitEnum.ENERGYUNIT, IfcSIUnitName.JOULE),
                    Si(IfcUnitEnum.POWERUNIT, IfcSIUnitName.WATT),
                    Si(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT, IfcSIUnitName.KELVIN),
                    Si(IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN),
                }));
                // kWh: einmal je Datei, an jeder Energie-Eigenschaft als Unit (6.4); Grad für Azimut und Neigung.
                _kwh = Umrechnung(IfcUnitEnum.ENERGYUNIT, KILOWATTSTUNDE, Exponenten(2, 1, -2),
                                  new IfcEnergyMeasure(3.6e6), Si(IfcUnitEnum.ENERGYUNIT, IfcSIUnitName.JOULE));
                _grad = Umrechnung(IfcUnitEnum.PLANEANGLEUNIT, GRAD, Exponenten(0, 0, 0),
                                   new IfcPlaneAngleMeasure(Math.PI / 180.0), Si(IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN));
                string name = _form == null || !_koerper.Schematisch || string.IsNullOrWhiteSpace(g.Anzeigename)
                    ? g.Anzeigename
                    : string.Format(_profil.Sprache, T(_profil, PROJEKT_SCHEMATISCH), g.Anzeigename.Trim());
                string beschreibung = T(_profil, _form == null ? DATEI_STUFE_S1 : _koerper.Schematisch ? DATEI_STUFE_S3 : DATEI_STUFE_S3_GRUNDRISS);
                if (_geometrie.AnordnungAbgelehnt)
                    beschreibung += " " + string.Format(_profil.Sprache, T(_profil, GEOMETRIE_ABGELEHNT_TEXT), GbxmlSchreiber.Widersprueche(_geometrie));
                return Wurzel<IfcProject>(wurzel, "Projekt", p =>
                {
                    p.Name = Label(name);
                    p.Description = beschreibung;
                    p.UnitsInContext = einheiten;
                    if (_form != null) p.RepresentationContexts.Add(_form.Kontext);
                });
            }

            private IfcSIUnit Si(IfcUnitEnum art, IfcSIUnitName name) => Neu<IfcSIUnit>(u =>
            {
                u.UnitType = art;
                u.Name = name;
            });

            private IfcDimensionalExponents Exponenten(int laenge, int masse, int zeit) => Neu<IfcDimensionalExponents>(d =>
            {
                d.LengthExponent = laenge;
                d.MassExponent = masse;
                d.TimeExponent = zeit;
            });

            private IfcConversionBasedUnit Umrechnung(IfcUnitEnum art, string name, IfcDimensionalExponents dim, IfcValue faktor, IfcSIUnit basis)
                => Neu<IfcConversionBasedUnit>(u =>
                {
                    u.UnitType = art;
                    u.Name = name;
                    u.Dimensions = dim;
                    u.ConversionFactor = Neu<IfcMeasureWithUnit>(w =>
                    {
                        w.ValueComponent = faktor;
                        w.UnitComponent = basis;
                    });
                });

            private IfcSite Grundstueck(string wurzel)
            {
                bool koordinaten = _abbild.BreiteGrad.HasValue && _abbild.LaengeGrad.HasValue;
                if (!koordinaten) Meldungen.Add(new PruefMeldung(PruefStufe.Info, OHNE_KOORDINATEN));
                IfcPostalAddress adresse = string.IsNullOrWhiteSpace(_abbild.Plz) ? null : Neu<IfcPostalAddress>(a =>
                {
                    a.PostalCode = _abbild.Plz.Trim();
                    if (!string.IsNullOrWhiteSpace(_abbild.Ort)) a.Town = _abbild.Ort.Trim();
                });
                return Wurzel<IfcSite>(wurzel, "Site", s =>
                {
                    s.Name = Label(string.IsNullOrWhiteSpace(_abbild.Ort) ? _abbild.Plz : _abbild.Ort);
                    s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    if (koordinaten)
                    {
                        s.RefLatitude = new IfcCompoundPlaneAngleMeasure(GradMinutenSekunden(_abbild.BreiteGrad.Value));
                        s.RefLongitude = new IfcCompoundPlaneAngleMeasure(GradMinutenSekunden(_abbild.LaengeGrad.Value));
                    }
                    if (adresse != null) s.SiteAddress = adresse;
                    if (_form != null) s.ObjectPlacement = _siteLage = _form.Platzierung(null);
                });
            }

            private IfcBuilding Gebaeude(AbbildGebaeude g)
            {
                IfcBuilding b = Wurzel<IfcBuilding>(g.Kennung, "Objekt", x =>
                {
                    x.Name = Label(g.Anzeigename);
                    x.Description = Text(g.Beschreibung);
                    x.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    if (_form != null) x.ObjectPlacement = _gebaeudeLage = _form.Platzierung(_siteLage);
                });
                string baujahr = !string.IsNullOrWhiteSpace(g.BaujahrText) ? g.BaujahrText.Trim()
                    : g.Baujahr?.ToString(CultureInfo.InvariantCulture);
                Satz(b, g.Kennung, "Pset_BuildingCommon",
                     baujahr == null ? null : Wert("YearOfConstruction", new IfcLabel(baujahr)));
                Satz(b, g.Kennung, EPOS_GEBAEUDE,
                     Wert("Kennung", new IfcIdentifier(g.Kennung)),
                     string.IsNullOrWhiteSpace(g.Art) ? null : Wert("Gebaeudeart", new IfcLabel(g.Art.Trim())),
                     string.IsNullOrWhiteSpace(g.Baualtersklasse) ? null : Wert("Baualtersklasse", new IfcLabel(g.Baualtersklasse.Trim())));
                Ergebnis(b, g.Kennung, null);
                Rechenlauf(b, g.Kennung, mitLauf: true);
                return b;
            }

            // --------------------------------------------------------------
            //  Räume und Zonen
            // --------------------------------------------------------------

            private IfcSpace Raum(AbbildRaum r)
            {
                IfcSpace s = Wurzel<IfcSpace>(r.Kennung, "Objekt", x =>
                {
                    x.Name = Label(r.Name ?? r.Kennung);
                    x.LongName = Label(r.Name);
                    x.Description = Text(r.Beschreibung);
                    x.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    x.PredefinedType = IfcSpaceTypeEnum.SPACE;
                });
                Raumkoerper raumkoerper = _koerper?.Raum(r.Kennung);
                Koerper(s, _form?.Raum(raumkoerper), raumkoerper?.AusGrundriss == true ? Grundrissvermerk(r.Kennung) : null);
                Mengen(s, r.Kennung, "Qto_SpaceBaseQuantities",
                       Flaeche("NetFloorArea", r.FlaecheM2), Laenge("Height", r.HoeheM), Volumen("NetVolume", r.VolumenM3));
                if (r.Beheizt)
                    Satz(s, r.Kennung, "Pset_SpaceThermalRequirements",
                         Temperatur("SpaceTemperature", r.SollHeizenC),
                         Temperatur("SpaceTemperatureSummerMax", r.SollKuehlenC),
                         r.Nachtabsenkung.HasValue ? Wert("DiscontinuedHeating", new IfcBoolean(r.Nachtabsenkung.Value)) : null,
                         r.LuftwechselNutzerJeH.HasValue ? Wert("NaturalVentilationRate", new IfcNumericMeasure(Endlich(r.LuftwechselNutzerJeH.Value))) : null);
                var zone = new List<IfcPropertySingleValue>
                {
                    Wert("IstBeheizt", new IfcBoolean(r.Beheizt)),
                    r.ZonenKennung == null ? null : Wert("Kennung", new IfcIdentifier(r.ZonenKennung)),
                    Zahl("LuftwechselInfiltration", r.LuftwechselJeH, "GEXP_IFC_LUFTWECHSEL"),
                    Zahl("Personen", r.Personen, null),
                    Zahl("GeraeteWm2", r.GeraeteWm2, null),
                    Zahl("LichtWm2", r.LichtWm2, null),
                };
                // Nutzung, Matrixzellen und Kalender der Zone (6.3, 16.3); ohne Konditionierung bleibt die Datei byte-gleich.
                zone.AddRange(IfcKonditionierungssatz.Zonenwerte(r.Konditionierung).Select(Satzwert));
                Satz(s, r.Kennung, EPOS_ZONE, zone.ToArray());
                if (r.Konditionierung != null)
                    foreach (AbbildKalender k in r.Konditionierung.Kalender)
                        Satz(s, r.Kennung, IfcKonditionierungssatz.Satzname(k.Kalender.Groesse),
                             IfcKonditionierungssatz.Kalenderwerte(k).Select(Satzwert).ToArray());
                if (r.Beheizt)
                {
                    Ergebnis(s, r.Kennung, r.FlaecheM2);
                    if (_mehrzonig) Rechenlauf(s, r.Kennung, mitLauf: false);
                }
                return s;
            }

            private void Zonen(AbbildGebaeude g)
            {
                foreach (IGrouping<string, AbbildRaum> gruppe in g.Raeume.Where(r => r.ZonenKennung != null).GroupBy(r => r.ZonenKennung, StringComparer.Ordinal))
                {
                    AbbildRaum erster = gruppe.First();
                    IfcZone zone = Wurzel<IfcZone>(gruppe.Key, "Objekt", z =>
                    {
                        z.Name = Label(erster.ZonenName ?? erster.Name ?? gruppe.Key);
                        z.Description = Text(erster.ZonenBeschreibung);
                    });
                    Wurzel<IfcRelAssignsToGroup>(gruppe.Key, "RelGroup", r =>
                    {
                        r.RelatingGroup = zone;
                        r.RelatedObjects.AddRange(gruppe.Select(x => _raeume[x.Kennung]));
                    });
                }
            }

            /// <summary><c>EPOS_Ergebnis</c> eines Objekts, soweit die Ergebnisse es tragen; Energie in kWh mit Unit.</summary>
            private void Ergebnis(IfcObject ziel, string kennung, double? flaeche)
            {
                if (_ergebnisse == null || !_ergebnisse.JeKennung.TryGetValue(kennung, out IfcErgebnis e) || e == null) return;
                double? bezogen = e.HeizwaermebedarfKwh.HasValue && flaeche > 0.0 ? e.HeizwaermebedarfKwh / flaeche : null;
                Satz(ziel, kennung, EPOS_ERGEBNIS,
                     e.HeizwaermebedarfKwh.HasValue ? Wert("Heizwaermebedarf", new IfcEnergyMeasure(Endlich(e.HeizwaermebedarfKwh.Value)), _kwh) : null,
                     Zahl("HeizwaermebedarfFlaechenbezogen", bezogen, "GEXP_IFC_FLAECHENBEZOGEN"),
                     e.HeizlastW.HasValue ? Wert("Heizlast", new IfcPowerMeasure(Endlich(e.HeizlastW.Value))) : null,
                     Temperatur("RaumtemperaturMittel", e.RaumtemperaturMittelC),
                     Temperatur("RaumtemperaturMax", e.RaumtemperaturMaxC),
                     // Kälte (Kühlkonzept 9.2): die Grenze reist in der Beschreibung der Eigenschaft mit.
                     e.KaeltebedarfKwh.HasValue ? Wert("Kaeltebedarf", new IfcEnergyMeasure(Endlich(e.KaeltebedarfKwh.Value)), _kwh, "GEXP_DATEI_KAELTE_SENSIBEL") : null,
                     e.KaeltelastW.HasValue ? Wert("Kaeltelast", new IfcPowerMeasure(Endlich(e.KaeltelastW.Value)), null, "GEXP_DATEI_KAELTE_SENSIBEL") : null);
            }

            /// <summary><c>EPOS_Rechenlauf</c>: Rechenmodell und Produktausweis (E10), am Gebäude dazu Fassung, Zeitpunkt und Wetter.</summary>
            private void Rechenlauf(IfcObject ziel, string kennung, bool mitLauf)
            {
                string modell = T(_profil, _mehrzonig ? "GEXP_IFC_RECHENMODELL_MEHRZONE" : "GEXP_IFC_RECHENMODELL_EINZONE");
                Satz(ziel, kennung, EPOS_RECHENLAUF,
                     Wert("Rechenmodell", new IfcLabel(modell)),
                     Wert("Validierung", new IfcLabel(T(_profil, nameof(MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007)))),
                     mitLauf ? Wert("Programmfassung", new IfcLabel(_profil.Programmname + " " + _profil.Programmversion)) : null,
                     mitLauf && _ergebnisse?.Rechenzeitpunkt != null
                         ? Wert("Rechenzeitpunkt", new IfcDateTime(_ergebnisse.Rechenzeitpunkt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)))
                         : null,
                     mitLauf && !string.IsNullOrWhiteSpace(_ergebnisse?.Wetterdatensatz) ? Wert("Wetterdatensatz", new IfcLabel(_ergebnisse.Wetterdatensatz.Trim())) : null);
            }

            // --------------------------------------------------------------
            //  Bauteile
            // --------------------------------------------------------------

            private void Flaeche(AbbildBauteil b)
            {
                Randbedingung rand = Rand(b);
                IfcElement e = Element(b, rand, null);
                Flaechen++;
                Koerperflaeche wand = Koerperflaeche(b);
                IReadOnlyList<Koerperflaeche> platten = Plattenflaechen(b, wand);
                if (platten != null) Koerper(e, _form?.Flaechen(platten), T(_profil, ELEMENT_PLATTE));
                else Koerper(e, (_form?.Flaeche(wand)));
                bool zusammen = b.Kennung.StartsWith(PRAEFIX_ZUSAMMENFASSUNG, StringComparison.Ordinal);
                if (_koerper != null && wand == null && !zusammen && !InnereMasse(b)) _ohneKoerper.Add(b.Name ?? b.Kennung);
                for (int i = 0; i < b.Oeffnungen.Count; i++)
                {
                    AbbildBauteil o = b.Oeffnungen[i];
                    IfcElement f = Element(o, rand, b);
                    Oeffnungen++;
                    Koerper(f, (_form?.Oeffnung(Oeffnungsring(o, wand, i, b.Oeffnungen.Count))));
                    // Das Öffnungselement bleibt geometrielos (kein Ausschnitt, 6.7); in Stufe S3 trägt es nur die Platzierung.
                    IfcOpeningElement loch = Wurzel<IfcOpeningElement>(o.Kennung, "Oeffnungselement", x =>
                    {
                        x.Name = Label(o.Name);
                        x.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                        if (_form != null) x.ObjectPlacement = Lage();
                    });
                    Wurzel<IfcRelVoidsElement>(o.Kennung, "RelVoids", r =>
                    {
                        r.RelatingBuildingElement = e;
                        r.RelatedOpeningElement = loch;
                    });
                    Wurzel<IfcRelFillsElement>(o.Kennung, "RelFills", r =>
                    {
                        r.RelatingOpeningElement = loch;
                        r.RelatedBuildingElement = f;
                    });
                }
            }

            /// <summary>Der Ring einer Öffnung in ihrer Wand (dieselbe Regel wie gbXML Stufe 2); <c>null</c> = keiner.</summary>
            private IReadOnlyList<double[]> Oeffnungsring(AbbildBauteil o, Koerperflaeche wand, int stelle, int anzahl)
            {
                if (wand == null || o.Kennung.StartsWith(PRAEFIX_ZUSAMMENFASSUNG, StringComparison.Ordinal)) return null;
                double? flaeche = o.BruttoflaecheM2 ?? (o.BreiteM * o.HoeheM);
                if (!(flaeche > 0.0)) return null;
                IReadOnlyList<double[]> ring = Zonenkoerper.Oeffnung(wand, stelle, anzahl, flaeche.Value, out bool begrenzt);
                if (begrenzt) _begrenzt.Add(o.Name ?? o.Kennung);
                return ring;
            }

            /// <summary>Die Randbedingung einer Fläche: aus der gbXML-Flächenart, sonst aus dem Abbild; zwei Nachbarn sind innen.</summary>
            private static Randbedingung Rand(AbbildBauteil b)
            {
                Randbedingung rand = b.Quellart != null && GbxmlVokabular.Flaechenarten.TryGetValue(b.Quellart, out var art) ? art.Rand : b.Randbedingung;
                if (b.Nachbarn.Count > 1 && (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich)) rand = Randbedingung.Innen;
                return rand;
            }

            private IfcElement Element(AbbildBauteil b, Randbedingung rand, AbbildBauteil wirt)
            {
                bool aussen = rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich;
                bool zusammen = b.Kennung.StartsWith(PRAEFIX_ZUSAMMENFASSUNG, StringComparison.Ordinal);
                string name = zusammen ? T(_profil, "GEXP_IFC_ZUSAMMEN_" + Artkuerzel(b.Art)) : b.Name ?? b.Kennung;
                string beschreibung = zusammen ? b.Name : null;
                double? flaeche = b.BruttoflaecheM2.HasValue ? Endlich(b.BruttoflaecheM2.Value) : (double?)null;
                double? u = b.UWertWm2K.HasValue ? Endlich(b.UWertWm2K.Value) : (double?)null;

                IfcElement e;
                string klasse;
                switch (b.Art)
                {
                    case Bauteilart.Fenster:
                    case Bauteilart.Vorhangfassade:
                        e = Wurzel<IfcWindow>(b.Kennung, "Objekt", x =>
                        {
                            x.PredefinedType = IfcWindowTypeEnum.WINDOW;
                            if (b.BreiteM > 0.0) x.OverallWidth = b.BreiteM.Value;
                            if (b.HoeheM > 0.0) x.OverallHeight = b.HoeheM.Value;
                        });
                        klasse = "Window";
                        break;
                    case Bauteilart.Tuer:
                        e = Wurzel<IfcDoor>(b.Kennung, "Objekt", x =>
                        {
                            x.PredefinedType = IfcDoorTypeEnum.DOOR;
                            if (b.BreiteM > 0.0) x.OverallWidth = b.BreiteM.Value;
                            if (b.HoeheM > 0.0) x.OverallHeight = b.HoeheM.Value;
                        });
                        klasse = "Door";
                        break;
                    case Bauteilart.Dach:
                    case Bauteilart.Bodenplatte:
                    case Bauteilart.Decke:
                        IfcSlabTypeEnum typ = b.Art == Bauteilart.Dach ? IfcSlabTypeEnum.ROOF
                            : b.Art == Bauteilart.Bodenplatte ? IfcSlabTypeEnum.BASESLAB : IfcSlabTypeEnum.FLOOR;
                        e = Wurzel<IfcSlab>(b.Kennung, "Objekt", x => x.PredefinedType = typ);
                        klasse = "Slab";
                        break;
                    default:
                        e = Wurzel<IfcWall>(b.Kennung, "Objekt");
                        klasse = "Wall";
                        break;
                }
                e.Name = Label(name);
                e.Description = Text(beschreibung);
                e.Tag = b.Kennung;
                _elemente.Add(e);

                // Standardsätze nach 6.3.
                Satz(e, b.Kennung, "Pset_" + klasse + "Common",
                     u.HasValue ? Wert("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)) : null,
                     Wert("IsExternal", new IfcBoolean(aussen)));
                if (klasse == "Window" && b.GWert.HasValue)
                    Satz(e, b.Kennung, "Pset_DoorWindowGlazingType",
                         Wert("SolarHeatGainTransmittance", new IfcNormalisedRatioMeasure(Endlich(b.GWert.Value))));
                switch (klasse)
                {
                    case "Wall":
                        Mengen(e, b.Kennung, "Qto_WallBaseQuantities", Flaeche("GrossSideArea", flaeche), Laenge("Width", b.DickeM));
                        break;
                    case "Slab":
                        Mengen(e, b.Kennung, "Qto_SlabBaseQuantities", Flaeche("GrossArea", flaeche), Laenge("Width", b.DickeM));
                        break;
                    default:
                        Mengen(e, b.Kennung, "Qto_" + klasse + "BaseQuantities",
                               Flaeche("Area", flaeche), Laenge("Width", b.BreiteM), Laenge("Height", b.HoeheM));
                        break;
                }

                // Der eigene Satz.
                AbbildAufbau a = b.Aufbau;
                Satz(e, b.Kennung, EPOS_BAUTEIL,
                     Wert("Kennung", new IfcIdentifier(b.Kennung)),
                     Wert("Bauteilart", new IfcLabel(b.Art.ToString())),
                     Wert("Randbedingung", new IfcLabel(rand.ToString())),
                     Winkel("Azimut", b.AzimutGrad, "GEXP_IFC_AZIMUT_BEZUG"),
                     Winkel("Neigung", b.NeigungGrad, "GEXP_IFC_NEIGUNG_BEZUG"),
                     Wert("IstZusammenfassung", new IfcBoolean(zusammen)),
                     zusammen ? null : Wert("AnzahlTeilflaechen", new IfcInteger(1)),
                     Zahl("WaermebrueckeUA", b.WaermebrueckeWK, "GEXP_IFC_WAERMEBRUECKE"),
                     a != null && a.Schichten.Count > 0 ? Wert("Schichtrichtung", new IfcLabel(SCHICHTRICHTUNG_AUSSEN_INNEN), null, "GEXP_IFC_SCHICHTRICHTUNG") : null,
                     a != null ? Wert("Ersatzschichtung", new IfcBoolean(a.IstErsatz)) : null,
                     b.Nachbarn.Count > 0 ? Wert("Raumgrenze", new IfcLabel(RAUMGRENZE_LOGISCH), null, "GEXP_IFC_RAUMGRENZE") : null);

                if (a != null && a.Schichten.Count > 0) Aufbau(a, e);
                Raumgrenzen(b, wirt, e, rand);
                return e;
            }

            /// <summary>Das Kürzel der Ressource einer Zusammenfassung je Bauteilart.</summary>
            private static string Artkuerzel(Bauteilart art)
            {
                switch (art)
                {
                    case Bauteilart.Aussenwand: return "AUSSENWAND";
                    case Bauteilart.Dach: return "DACH";
                    case Bauteilart.Bodenplatte: return "BODENPLATTE";
                    case Bauteilart.Fenster: return "FENSTER";
                    case Bauteilart.Tuer: return "TUER";
                    case Bauteilart.Innenwand: return "INNENWAND";
                    case Bauteilart.Decke: return "DECKE";
                    case Bauteilart.Vorhangfassade: return "VORHANGFASSADE";
                    default: return "SONSTIGES";
                }
            }

            /// <summary>
            /// Die Raumgrenzen 2. Ebene ohne Anschlussgeometrie: ein Nachbar — <c>EXTERNAL</c> bzw.
            /// <c>EXTERNAL_EARTH</c>; zwei Nachbarn — <c>INTERNAL</c>, wechselseitig über
            /// <c>CorrespondingBoundary</c>; ein Nachbar einer Innenfläche ohne Gegenseite — <c>INTERNAL</c>, „2b".
            /// Eine Öffnung trägt die Nachbarn ihrer Wirtsfläche.
            /// </summary>
            private void Raumgrenzen(AbbildBauteil b, AbbildBauteil wirt, IfcElement e, Randbedingung rand)
            {
                List<AbbildNachbar> nachbarn = (b.Nachbarn.Count > 0 ? b.Nachbarn : wirt?.Nachbarn ?? b.Nachbarn).ToList();
                var grenzen = new List<IfcRelSpaceBoundary2ndLevel>();
                for (int i = 0; i < nachbarn.Count; i++)
                {
                    if (!_raeume.TryGetValue(nachbarn[i].Kennung, out IfcSpace raum))
                        throw new InvalidOperationException("IFC-Export: Das Bauteil " + b.Kennung + " nennt den unbekannten Raum " + nachbarn[i].Kennung + ".");
                    IfcInternalOrExternalEnum lage = nachbarn.Count > 1 ? IfcInternalOrExternalEnum.INTERNAL
                        : rand == Randbedingung.Erdreich ? IfcInternalOrExternalEnum.EXTERNAL_EARTH
                        : rand == Randbedingung.Aussenluft ? IfcInternalOrExternalEnum.EXTERNAL
                        : IfcInternalOrExternalEnum.INTERNAL;
                    string art = nachbarn.Count == 1 && lage == IfcInternalOrExternalEnum.INTERNAL ? "2b" : "2a";
                    grenzen.Add(Wurzel<IfcRelSpaceBoundary2ndLevel>(b.Kennung,
                        "SpaceBoundary:" + art + ":" + nachbarn[i].Kennung + ":" + i.ToString(CultureInfo.InvariantCulture), g =>
                        {
                            g.Name = "2ndLevel";
                            g.Description = art;
                            g.RelatingSpace = raum;
                            g.RelatedBuildingElement = e;
                            g.PhysicalOrVirtualBoundary = IfcPhysicalOrVirtualEnum.PHYSICAL;
                            g.InternalOrExternalBoundary = lage;
                        }));
                }
                if (grenzen.Count == 2)
                {
                    grenzen[0].CorrespondingBoundary = grenzen[1];
                    grenzen[1].CorrespondingBoundary = grenzen[0];
                }
            }

            /// <summary>Der Aufbau als <c>IfcMaterialLayerSet</c> (nie <c>…Usage</c>), erste Schicht außen.</summary>
            private void Aufbau(AbbildAufbau a, IfcElement e)
            {
                string kennung = string.IsNullOrWhiteSpace(a.Kennung) ? throw new InvalidOperationException("IFC-Export: Ein Aufbau ohne Kennung.") : a.Kennung;
                if (!_saetze.TryGetValue(kennung, out IfcMaterialLayerSet satz))
                {
                    List<AbbildSchicht> schichten = a.Schichten.ToList();
                    if (a.Richtung == Schichtrichtung.InnenNachAussen) schichten.Reverse();
                    var lagen = schichten.Select(Schicht).ToList();
                    satz = Neu<IfcMaterialLayerSet>(s =>
                    {
                        s.LayerSetName = Label(a.Name ?? kennung);
                        s.Description = Text(a.Beschreibung);
                        s.MaterialLayers.AddRange(lagen);
                    });
                    _saetze[kennung] = satz;
                    _satzElemente[kennung] = new List<IfcElement>();
                    _satzReihenfolge.Add(kennung);
                    if (a.IstErsatz) Ersatzaufbauten++;
                }
                _satzElemente[kennung].Add(e);
            }

            private IfcMaterialLayer Schicht(AbbildSchicht s)
            {
                IfcMaterial stoff = Stoff(s);
                bool luft = s.NurRWert;
                return Neu<IfcMaterialLayer>(l =>
                {
                    l.Material = stoff;
                    l.LayerThickness = s.DickeM > 0.0 ? Endlich(s.DickeM.Value) : 0.0;
                    // IFC4: UNKNOWN = Luftschicht ohne Luftaustausch (ruhend), FALSE = feste Schicht.
                    l.IsVentilated = luft ? new IfcLogical((bool?)null) : new IfcLogical(false);
                    l.Name = Label(s.Name);
                });
            }

            private IfcMaterial Stoff(AbbildSchicht s)
            {
                string kennung = string.IsNullOrWhiteSpace(s.BaustoffKennung) ? s.Kennung : s.BaustoffKennung;
                if (string.IsNullOrWhiteSpace(kennung)) throw new InvalidOperationException("IFC-Export: Eine Schicht ohne Baustoffkennung.");
                if (_stoffe.TryGetValue(kennung, out IfcMaterial m)) return m;
                m = Neu<IfcMaterial>(x =>
                {
                    x.Name = s.Name ?? kennung;
                    x.Description = kennung;
                });
                _stoffe[kennung] = m;
                Stoffsatz(m, "Pset_MaterialThermal",
                          s.LambdaWmK > 0.0 ? Wert("ThermalConductivity", new IfcThermalConductivityMeasure(Endlich(s.LambdaWmK.Value))) : null,
                          s.CpJkgK > 0.0 ? Wert("SpecificHeatCapacity", new IfcSpecificHeatCapacityMeasure(Endlich(s.CpJkgK.Value))) : null);
                Stoffsatz(m, "Pset_MaterialCommon",
                          s.RhoKgM3 > 0.0 ? Wert("MassDensity", new IfcMassDensityMeasure(Endlich(s.RhoKgM3.Value))) : null);
                Stoffsatz(m, EPOS_BAUSTOFF,
                          s.RWertM2KW > 0.0 ? Wert("Waermedurchlasswiderstand", new IfcThermalResistanceMeasure(Endlich(s.RWertM2KW.Value))) : null);
                return m;
            }

            private void Stoffsatz(IfcMaterial m, string name, params IfcPropertySingleValue[] werte)
            {
                List<IfcPropertySingleValue> liste = werte.Where(w => w != null).ToList();
                if (liste.Count == 0) return;
                Neu<IfcMaterialProperties>(p =>
                {
                    p.Name = name;
                    p.Material = m;
                    p.Properties.AddRange(liste);
                });
            }

            // --------------------------------------------------------------
            //  Sätze, Mengen, Werte
            // --------------------------------------------------------------

            private void Satz(IfcObject ziel, string kennung, string name, params IfcPropertySingleValue[] werte)
            {
                List<IfcPropertySingleValue> liste = werte.Where(w => w != null).ToList();
                if (liste.Count == 0) return;
                IfcPropertySet satz = Wurzel<IfcPropertySet>(kennung, "Pset:" + name, p =>
                {
                    p.Name = name;
                    p.HasProperties.AddRange(liste);
                });
                Definieren(ziel, kennung, name, satz);
            }

            private void Mengen(IfcObject ziel, string kennung, string name, params IfcPhysicalSimpleQuantity[] mengen)
            {
                List<IfcPhysicalSimpleQuantity> liste = mengen.Where(w => w != null).ToList();
                if (liste.Count == 0) return;
                IfcElementQuantity satz = Wurzel<IfcElementQuantity>(kennung, "Qto:" + name, q =>
                {
                    q.Name = name;
                    q.Quantities.AddRange(liste);
                });
                Definieren(ziel, kennung, name, satz);
            }

            private void Definieren(IfcObject ziel, string kennung, string name, IfcPropertySetDefinition satz)
                => Wurzel<IfcRelDefinesByProperties>(kennung, "RelProps:" + name, r =>
                {
                    r.RelatingPropertyDefinition = satz;
                    r.RelatedObjects.Add(ziel);
                });

            private void Aggregieren(string kennung, IfcObjectDefinition ganzes, IfcObjectDefinition[] teile)
            {
                if (teile.Length == 0) return;
                Wurzel<IfcRelAggregates>(kennung, "RelAggregates", r =>
                {
                    r.RelatingObject = ganzes;
                    r.RelatedObjects.AddRange(teile);
                });
            }

            private IfcPropertySingleValue Wert(string name, IfcValue wert, IfcUnit einheit = null, string beschreibung = null)
                => Neu<IfcPropertySingleValue>(p =>
                {
                    p.Name = name;
                    p.NominalValue = wert;
                    if (einheit != null) p.Unit = einheit;
                    if (beschreibung != null) p.Description = T(_profil, beschreibung);
                });

            /// <summary>Ein Wert der Konditionierungssätze in seinem IFC-Typ (<see cref="IfcSatzwertart"/>).</summary>
            private IfcPropertySingleValue Satzwert(IfcSatzwert w)
            {
                IfcPropertySingleValue p;
                switch (w.Art)
                {
                    case IfcSatzwertart.Temperatur: p = Temperatur(w.Name, w.Zahl); break;
                    case IfcSatzwertart.Zahl: p = Zahl(w.Name, w.Zahl, w.Beschreibung); break;
                    case IfcSatzwertart.Leistung: p = w.Zahl.HasValue ? Wert(w.Name, new IfcPowerMeasure(Endlich(w.Zahl.Value))) : null; break;
                    case IfcSatzwertart.Kennwort: p = Wert(w.Name, new IfcLabel(w.Text ?? ""), null, w.Beschreibung); break;
                    case IfcSatzwertart.Wahrheit: p = Wert(w.Name, new IfcBoolean(w.Wahr == true), null, w.Beschreibung); break;
                    default: p = Wert(w.Name, new IfcText(w.Text ?? ""), null, w.Beschreibung); break;
                }
                if (p != null && w.BeschreibungWoertlich != null) p.Description = new IfcText(w.BeschreibungWoertlich);
                return p;
            }

            private IfcPropertySingleValue Zahl(string name, double? wert, string beschreibung)
                => wert.HasValue ? Wert(name, new IfcReal(Endlich(wert.Value)), null, beschreibung) : null;

            private IfcPropertySingleValue Temperatur(string name, double? celsius)
                => celsius.HasValue ? Wert(name, new IfcThermodynamicTemperatureMeasure(Endlich(celsius.Value) + NULLPUNKT_K)) : null;

            private IfcPropertySingleValue Winkel(string name, double? grad, string beschreibung)
                => grad.HasValue ? Wert(name, new IfcPlaneAngleMeasure(Endlich(grad.Value)), _grad, beschreibung) : null;

            private IfcQuantityArea Flaeche(string name, double? wert)
                => wert.HasValue ? Neu<IfcQuantityArea>(q => { q.Name = name; q.AreaValue = Endlich(wert.Value); }) : null;

            private IfcQuantityLength Laenge(string name, double? wert)
                => wert.HasValue ? Neu<IfcQuantityLength>(q => { q.Name = name; q.LengthValue = Endlich(wert.Value); }) : null;

            private IfcQuantityVolume Volumen(string name, double? wert)
                => wert.HasValue ? Neu<IfcQuantityVolume>(q => { q.Name = name; q.VolumeValue = Endlich(wert.Value); }) : null;

            private static IfcLabel? Label(string text) => string.IsNullOrWhiteSpace(text) ? (IfcLabel?)null : new IfcLabel(text.Trim());

            private static IfcText? Text(string text) => string.IsNullOrWhiteSpace(text) ? (IfcText?)null : new IfcText(text.Trim());

            private static double Endlich(double wert)
            {
                if (double.IsNaN(wert) || double.IsInfinity(wert)) throw new InvalidOperationException("IFC-Export: Eine Zahl ist nicht endlich.");
                return wert;
            }
        }
    }
}
