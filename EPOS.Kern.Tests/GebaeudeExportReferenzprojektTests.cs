using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using WindowsFormsApplication1;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Gebäudeexport des Referenzprojekts 1052</b> (drei Zonen; Stufe G7c, Teil 2): Der Exportsatz liest
    /// Gebäude, Zonen und den Klimaort aus der Testdatenbank; die Testdatenbank führt keinen gespeicherten
    /// Rechenlauf, deshalb wird das Ergebnis über dieselbe Fassade wie der Lauf gerechnet
    /// (<c>GebaeudeBedarfCtrl.Rechnen</c>, schreibt nichts) und in den Satz gelegt. Gehalten werden
    /// <c>EPOS_Ergebnis</c> am Gebäude und je beheizter Zone (IFC) und die <c>Results</c> je Zone (gbXML).
    /// <para><b>Umgebungsvariable <c>EPOS_EXPORT_BEISPIELORDNER</c></b> (<see cref="BEISPIELORDNER_VARIABLE"/>):
    /// Ist sie gesetzt (ein Ordnerpfad), legt der Fall beide Dateien dort ab
    /// (<c>Referenzprojekt_1052.ifc</c>, <c>Referenzprojekt_1052.xml</c>) — zum Ansehen in einem Zielwerkzeug.
    /// Ohne Variable (nicht gesetzt oder leer) schreibt der Test nichts; das hält
    /// <see cref="Ohne_Beispielordner_wird_nichts_geschrieben"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeExportReferenzprojektTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private const int PROJEKT = 1052;

        /// <summary>Die Umgebungsvariable mit dem Ordner für die Beispieldateien; ohne sie wird nichts geschrieben.</summary>
        internal const string BEISPIELORDNER_VARIABLE = "EPOS_EXPORT_BEISPIELORDNER";
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public GebaeudeExportReferenzprojektTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Referenzprojekt_1052_exportiert_EPOS_Ergebnis_und_Results_je_Zone()
        {
            if (!_db.Vorhanden) return;
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ? ORDER BY ID LIMIT 1",
                                                                   new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            GebaeudeExportSatz satz = GebaeudeExportSatz.Lesen(PROJEKT, idZ, "80331");
            Assert.Equal(3, satz.Zonen.Count);
            // Der Klimaort aus Tab_Klimaregion des Projekts (München).
            Assert.Equal(48.137, satz.BreiteGrad.Value, 3);
            Assert.Equal(11.575, satz.LaengeGrad.Value, 3);
            long gespeichert = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ErgebnisGebaeude g JOIN Tab_Ergebnis e ON e.ID = g.ID_Ergebnis WHERE e.ID_Projekt = ?",
                new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            if (gespeichert == 0) Assert.Null(satz.Ergebnis);   // ohne gespeicherten Lauf: nichts erfunden

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            GebaeudeBedarfErgebnis lauf = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, idZ);
            Assert.True(lauf.Erfolgreich, lauf.Befund);
            ErgebnisGebaeudeModel e = lauf.Ergebniszeile;
            Assert.Equal(3, e.Zonen.Count);
            GebaeudeExportSatz mit = satz.MitErgebnis(e, new DateTime(2026, 10, 4, 9, 0, 0), satz.Klimaregion);

            // IFC
            GebaeudeExportProfil ifc = IfcExportProbe.Profil();
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(mit, ifc);
            byte[] ifcDatei = ExportSatzProbe.Datei(plan, ifc);
            using (MemoryModel m = IfcExportProbe.Modell(ifcDatei))
            {
                IIfcBuilding b = m.Instances.OfType<IIfcBuilding>().Single();
                Dictionary<string, IIfcPropertySingleValue> eg = IfcExportProbe.Eigenschaften(b)[IfcSchreiber.EPOS_ERGEBNIS];
                Assert.Equal(e.HeizwaermeMwh * 1000.0, IfcExportProbe.Zahl(eg["Heizwaermebedarf"]), 6);
                Assert.Equal(IfcSchreiber.KILOWATTSTUNDE, ((IIfcConversionBasedUnit)eg["Heizwaermebedarf"].Unit).Name.ToString());
                Assert.Equal(e.SpitzeKw * 1000.0, IfcExportProbe.Zahl(eg["Heizlast"]), 6);
                int mitErgebnis = 0;
                foreach (ErgebnisZoneModel ez in e.Zonen.Where(z => z.IstBeheizt))
                {
                    ZoneModel zone = satz.Zonen.Single(z => z.ID == ez.ID_Zone);
                    IIfcSpace raum = m.Instances.OfType<IIfcSpace>().Single(s => s.LongName == zone.Bezeichner.Trim());
                    Dictionary<string, IIfcPropertySingleValue> er = IfcExportProbe.Eigenschaften(raum)[IfcSchreiber.EPOS_ERGEBNIS];
                    Assert.Equal(ez.HeizwaermeMwh.Value * 1000.0, IfcExportProbe.Zahl(er["Heizwaermebedarf"]), 6);
                    mitErgebnis++;
                }
                Assert.True(mitErgebnis >= 2, "nur " + mitErgebnis + " beheizte Zonen");
                Assert.Equal(48L, ((IEnumerable<long>)m.Instances.OfType<IIfcSite>().Single().RefLatitude.Value.Value).First());
            }

            // gbXML
            GebaeudeExportProfil gbxml = GbxmlExportProbe.Profil();
            byte[] xmlDatei = ExportSatzProbe.Datei(ExportSatzProbe.Plan(mit, gbxml), gbxml);
            XDocument d = XDocument.Load(new MemoryStream(xmlDatei));
            List<XElement> results = d.Root.Elements(NS + "Results").ToList();
            Assert.Equal(e.Zonen.Count(z => z.IstBeheizt), results.Count(r => (string)r.Attribute("resultsType") == "Energy"));
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema != null)
            {
                string[] befunde = GbxmlExportProbe.Schemapruefung(schema, xmlDatei);
                Assert.True(befunde.Length == 0, string.Join("\n", befunde));
            }

            string ordner = Environment.GetEnvironmentVariable(BEISPIELORDNER_VARIABLE);
            if (BeispieleAblegen(ordner, ifcDatei, xmlDatei).Count > 0) _aus.WriteLine("Beispieldateien in " + ordner);
            foreach (var meldung in plan.Meldungen) _aus.WriteLine(meldung.Stufe + " " + meldung.Schluessel);
        }

        /// <summary>
        /// Legt die Beispieldateien im Ordner ab — nur, wenn ein Ordner genannt ist; sonst wird nichts geschrieben.
        /// </summary>
        /// <returns>Die geschriebenen Pfade; leer ohne Ordner.</returns>
        internal static IReadOnlyList<string> BeispieleAblegen(string ordner, byte[] ifcDatei, byte[] xmlDatei)
        {
            if (string.IsNullOrWhiteSpace(ordner)) return Array.Empty<string>();
            Directory.CreateDirectory(ordner);
            string ifc = Path.Combine(ordner, "Referenzprojekt_1052.ifc");
            string xml = Path.Combine(ordner, "Referenzprojekt_1052.xml");
            File.WriteAllBytes(ifc, ifcDatei);
            File.WriteAllBytes(xml, xmlDatei);
            return new[] { ifc, xml };
        }

        [Fact]
        public void Ohne_Beispielordner_wird_nichts_geschrieben()
        {
            string arbeit = Path.Combine(Path.GetTempPath(), "epos_beispielordner_" + Guid.NewGuid().ToString("N"));
            string vorher = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(arbeit);
            try
            {
                Directory.SetCurrentDirectory(arbeit);
                Assert.Empty(BeispieleAblegen(null, new byte[] { 1 }, new byte[] { 2 }));
                Assert.Empty(BeispieleAblegen("", new byte[] { 1 }, new byte[] { 2 }));
                Assert.Empty(BeispieleAblegen("   ", new byte[] { 1 }, new byte[] { 2 }));
                Assert.Empty(Directory.EnumerateFileSystemEntries(arbeit));

                // Gegenprobe: mit Ordner liegen genau die zwei Dateien darin.
                string ziel = Path.Combine(arbeit, "beispiele");
                Assert.Equal(2, BeispieleAblegen(ziel, new byte[] { 1 }, new byte[] { 2 }).Count);
                Assert.Equal(2, Directory.GetFiles(ziel).Length);
            }
            finally
            {
                Directory.SetCurrentDirectory(vorher);
                Directory.Delete(arbeit, true);
            }
        }
    }
}
