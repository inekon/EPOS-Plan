using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 3 des gbXML-Exports</b> (Stufe G7a, Welle W4; Datenaustauschkonzept Kapitel 9, D17): jede
    /// Probenausgabe gegen die Schemakopie <c>Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd</c>,
    /// dazu die Messung des Inhaltsmodells, auf das sich der Schreiber verlässt — ob <c>Location</c>
    /// wegbleiben darf, die Pflichtkinder von <c>RectangularGeometry</c>, ob die Kinder der geschriebenen
    /// Elemente einer Reihenfolge folgen, die Werte von <c>buildingTypeEnum</c>, <c>Opening/U-value</c>
    /// und der Ort von <c>DocumentHistory</c>.
    ///
    /// <para><b>Nur lokal.</b> Die Schemakopie liegt nach D17 nicht im Repositorium und damit nicht in der
    /// CI; fehlt sie, schreibt die Probe das über <see cref="ITestOutputHelper"/> und kehrt zurück (xunit
    /// 2.9.3 kennt keinen dynamischen Skip, Muster <c>Normzahlen.cs</c>). Die CI decken die
    /// Namensraum-Wache und die eigene Verweisprüfung (Probe 11) ab.</para>
    /// </summary>
    public sealed class GbxmlProbe3Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public GbxmlProbe3Tests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        public void Dispose() => _kultur.Dispose();

        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;

        private string Schemakopie()
        {
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema == null)
                _ausgabe.WriteLine("Probe 3 übersprungen: Referenzlaeufe/Schemakopien/" + GbxmlExportProbe.SCHEMAKOPIE
                                   + " liegt nicht bei (D17, lokal beizustellen).");
            return schema;
        }

        // ==================================================================
        //  Die Probenausgaben
        // ==================================================================

        /// <summary>Die Probenausgaben: Bauteilweg und Klassenweg, mit und ohne PLZ, mit Tür, zwei Zonen, Testlizenz englisch.</summary>
        private static IEnumerable<(string Name, byte[] Datei)> Proben()
        {
            GebaeudeExportProfil de = GbxmlExportProbe.Profil();
            GebaeudeExportProfil enTest = GbxmlExportProbe.Profil(sprache: "en-US", testlizenz: true);

            yield return ("Bauteilweg Schichten, mit PLZ, mit Tür", Datei(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), de));
            yield return ("Bauteilweg Schichten, ohne PLZ, mit Tür", Datei(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus(), plz: null), de));
            yield return ("Bauteilweg U-Werte (Ersatzschichtung), mit PLZ", Datei(ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus()), de));
            yield return ("Bauteilweg U-Werte (Ersatzschichtung), ohne PLZ", Datei(ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus(), plz: null), de));
            yield return ("Klassenweg, mit PLZ", Datei(GebaeudeExportAblaufTests.Klassenweg(true), de));
            yield return ("Klassenweg, ohne PLZ", Datei(GebaeudeExportAblaufTests.Klassenweg(true).MitPlz(null), de));
            yield return ("Zwei Zonen mit Trennfläche", Datei(ZweiZonen(), de));
            yield return ("Testlizenz, englisch", Datei(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), enTest));
        }

        private static byte[] Datei(GebaeudeExportSatz satz, GebaeudeExportProfil profil)
            => ExportSatzProbe.Datei(ExportSatzProbe.Plan(satz, profil), profil);

        private static GebaeudeExportSatz ZweiZonen()
        {
            ZoneModel a = ExportSatzProbe.Schichtenhaus();
            a.Nutzflaeche = 80.0;
            var b = new ZoneModel { ID = 602, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Anbau", Nutzflaeche = 40.0, IstBeheizt = true };
            a.Bauteile.Add(ExportSatzProbe.B(1013, DbWerte.BAUTEILART_INNENWAND, 12.0, DbWerte.RANDBEDINGUNG_ZONE,
                                             ExportSatzProbe.AUFBAU_DECKE, neigung: 90.0, nachbarzone: 602));
            return ExportSatzProbe.Satz(a, weitere: new[] { b });
        }

        /// <summary>
        /// <b>Probe 3, erster Teil:</b> jede Probenausgabe ist gültig gegen die Schemakopie — ohne Fehler und
        /// ohne Warnung des Prüfers. Die Zahlen je Probe gehen ins Protokoll.
        /// </summary>
        [Fact]
        public void Probe3_jede_Probenausgabe_ist_schemagueltig()
        {
            string schema = Schemakopie();
            if (schema == null) return;

            foreach ((string name, byte[] datei) in Proben())
            {
                string[] befunde = GbxmlExportProbe.Schemapruefung(schema, datei);
                Assert.True(befunde.Length == 0, name + ":\n" + string.Join("\n", befunde));

                XDocument d = XDocument.Load(new MemoryStream(datei));
                int Zahl(string element) => d.Descendants(NS + element).Count();
                _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "gültig: {0} — {1} Byte, {2} Space, {3} Surface, {4} Opening, {5} Construction, {6} Layer, {7} Material, {8} WindowType, {9} Zone, Location {10}",
                    name, datei.Length, Zahl("Space"), Zahl("Surface"), Zahl("Opening"), Zahl("Construction"), Zahl("Layer"),
                    Zahl("Material"), Zahl("WindowType"), Zahl("Zone"), Zahl("Location") > 0 ? "ja" : "nein"));
            }
        }

        // ==================================================================
        //  Das Inhaltsmodell der Schemakopie
        // ==================================================================

        /// <summary>Ein Kind im Inhaltsmodell: Name, ob es stehen MUSS, und die Gruppe, in der es steht.</summary>
        private readonly record struct Kind(string Name, bool Pflicht, string Gruppe);

        private static XmlSchemaSet Laden(string schemakopie)
        {
            var satz = new XmlSchemaSet { XmlResolver = null };
            var einstellungen = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (XmlReader r = XmlReader.Create(schemakopie, einstellungen))
                satz.Add(null, r);
            satz.Compile();
            return satz;
        }

        private static XmlSchemaElement Element(XmlSchemaSet satz, string name)
            => (XmlSchemaElement)satz.GlobalElements[new XmlQualifiedName(name, GbxmlLeser.NAMENSRAUM)];

        /// <summary>
        /// Die Kinder eines globalen Elements. Pflicht ist ein Kind, wenn es selbst mindestens einmal stehen
        /// muss und jede umgebende Gruppe das erzwingt: eine Folge oder Alle-Gruppe mit Mindestzahl ≥ 1, eine
        /// Auswahl nur, wenn sie genau dieses eine Kind anbietet.
        /// </summary>
        private static List<Kind> Kinder(XmlSchemaSet satz, string element)
        {
            var typ = (XmlSchemaComplexType)Element(satz, element).ElementSchemaType;
            var kinder = new List<Kind>();
            Sammeln(typ.ContentTypeParticle, true, "", kinder);
            return kinder;
        }

        private static void Sammeln(XmlSchemaParticle p, bool erzwungen, string gruppe, List<Kind> kinder)
        {
            switch (p)
            {
                case XmlSchemaElement e:
                    kinder.Add(new Kind(e.QualifiedName.Name, erzwungen && e.MinOccurs >= 1, gruppe));
                    break;
                case XmlSchemaChoice c:
                    string auswahl = "choice[" + Grenzen(c) + "]";
                    foreach (XmlSchemaObject o in c.Items)
                        Sammeln((XmlSchemaParticle)o, erzwungen && c.MinOccurs >= 1 && c.Items.Count == 1, auswahl, kinder);
                    break;
                case XmlSchemaGroupBase g:   // sequence, all
                    string folge = (g is XmlSchemaSequence ? "sequence" : "all") + "[" + Grenzen(g) + "]";
                    foreach (XmlSchemaObject o in g.Items)
                        Sammeln((XmlSchemaParticle)o, erzwungen && g.MinOccurs >= 1, folge, kinder);
                    break;
            }
        }

        private static string Grenzen(XmlSchemaParticle p)
            => p.MinOccurs.ToString(CultureInfo.InvariantCulture) + ".."
               + (p.MaxOccursString == "unbounded" || p.MaxOccurs == decimal.MaxValue ? "unbounded" : p.MaxOccurs.ToString(CultureInfo.InvariantCulture));

        /// <summary>Die Aufzählungswerte eines benannten einfachen Typs.</summary>
        private static List<string> Aufzaehlung(XmlSchemaSet satz, string typ)
        {
            var t = (XmlSchemaSimpleType)satz.GlobalTypes[new XmlQualifiedName(typ, GbxmlLeser.NAMENSRAUM)];
            var r = (XmlSchemaSimpleTypeRestriction)t.Content;
            return r.Facets.OfType<XmlSchemaEnumerationFacet>().Select(f => f.Value).ToList();
        }

        /// <summary>
        /// <b>Probe 3, zweiter Teil — die Messung.</b> Was der Schreiber aus der Schemakopie übernimmt, steht
        /// hier als Befund: Die Kinder aller geschriebenen Elemente stehen in einer unbegrenzten Auswahl, also
        /// ohne Reihenfolge und ohne Pflicht; <c>Location</c> darf fehlen, wird es geschrieben, trägt es die
        /// Postleitzahl; <c>RectangularGeometry</c> braucht kein <c>CartesianPoint</c>; <c>Opening</c> nimmt
        /// einen <c>U-value</c>; <c>DocumentHistory</c> steht an der Wurzel und braucht zwei Kinder.
        /// </summary>
        [Fact]
        public void Probe3_misst_das_Inhaltsmodell_auf_das_sich_der_Schreiber_verlaesst()
        {
            string schema = Schemakopie();
            if (schema == null) return;
            XmlSchemaSet satz = Laden(schema);

            // Die geschriebenen Elemente: Reihenfolge und Pflichtkinder.
            string[] geschrieben =
            {
                "gbXML", "Campus", "Building", "Space", "Surface", "AdjacentSpaceId", "Opening", "RectangularGeometry",
                "Construction", "LayerId", "Layer", "MaterialId", "Material", "WindowType", "Zone", "DocumentHistory",
                "ProgramInfo", "PersonInfo", "CreatedBy"
            };
            foreach (string e in geschrieben)
            {
                if (!(Element(satz, e)?.ElementSchemaType is XmlSchemaComplexType { ContentTypeParticle: XmlSchemaGroupBase or XmlSchemaElement }))
                {
                    _ausgabe.WriteLine(e + ": ohne Kindelemente");
                    continue;
                }
                List<Kind> kinder = Kinder(satz, e);
                string[] gruppen = kinder.Select(k => k.Gruppe).Distinct().ToArray();
                string[] pflicht = kinder.Where(k => k.Pflicht).Select(k => k.Name).ToArray();
                _ausgabe.WriteLine(e + ": " + string.Join(", ", gruppen) + "; Pflichtkinder: "
                                   + (pflicht.Length == 0 ? "keine" : string.Join(", ", pflicht)));
            }

            // Location darf fehlen (die Kinder von Campus stehen in einer Auswahl) — ohne PLZ schreibt der
            // Schreiber es nicht; steht es, verlangt es die Postleitzahl.
            List<Kind> campus = Kinder(satz, "Campus");
            Assert.Contains(campus, k => k.Name == "Location");
            Assert.False(campus.Single(k => k.Name == "Location").Pflicht);
            Assert.Equal(new[] { "ZipcodeOrPostalCode" }, Kinder(satz, "Location").Where(k => k.Pflicht).Select(k => k.Name));

            // RectangularGeometry: keine Pflichtkinder — kein Rückfall CartesianPoint im Ursprung nötig.
            List<Kind> rechteck = Kinder(satz, "RectangularGeometry");
            Assert.Contains(rechteck, k => k.Name == "CartesianPoint");
            Assert.DoesNotContain(rechteck, k => k.Pflicht);

            // Opening nimmt einen U-value.
            Assert.Contains(Kinder(satz, "Opening"), k => k.Name == "U-value");

            // DocumentHistory: ein Kind der Wurzel; seine Auswahl verlangt mindestens zwei Kinder, der
            // Schreiber setzt ProgramInfo, PersonInfo (Programmname und Fassung, keine Anwenderdaten) und
            // CreatedBy mit dem einzigen Zeitstempel.
            Assert.Contains(Kinder(satz, "gbXML"), k => k.Name == "DocumentHistory");
            var historie = (XmlSchemaComplexType)Element(satz, "DocumentHistory").ElementSchemaType;
            Assert.True(historie.ContentTypeParticle.MinOccurs >= 2);
            XDocument probe = XDocument.Load(new MemoryStream(Proben().First().Datei));
            XElement dh = Assert.Single(probe.Root!.Elements(NS + "DocumentHistory"));
            Assert.Equal(new[] { "ProgramInfo", "PersonInfo", "CreatedBy" }, dh.Elements().Select(x => x.Name.LocalName));

            // buildingTypeEnum: jede Regel der Profiltabelle und „Unknown" sind gültige Werte; das Vokabular
            // führt genau die Werte der Schemakopie.
            List<string> arten = Aufzaehlung(satz, "buildingTypeEnum");
            Assert.All(GebaeudeExportProfil.StandardGebaeudetypen, r => Assert.Contains(r.Wert, arten));
            Assert.Contains(GbxmlVokabular.Unknown, arten);
            Assert.Equal(arten.OrderBy(x => x, StringComparer.Ordinal), GbxmlVokabular.Gebaeudearten.OrderBy(x => x, StringComparer.Ordinal));
            _ausgabe.WriteLine("buildingTypeEnum: " + arten.Count.ToString(CultureInfo.InvariantCulture) + " Werte, davon "
                               + GebaeudeExportProfil.StandardGebaeudetypen.Select(r => r.Wert).Distinct().Count().ToString(CultureInfo.InvariantCulture)
                               + " in der Profiltabelle.");

            // Die Wurzelfassung: versionEnum führt 6.01 als höchsten Wert, nicht 8.01.
            List<string> fassungen = Aufzaehlung(satz, "versionEnum");
            Assert.Contains(GbxmlSchreiber.VERSION, fassungen);
            Assert.DoesNotContain("8.01", fassungen);
        }
    }
}
