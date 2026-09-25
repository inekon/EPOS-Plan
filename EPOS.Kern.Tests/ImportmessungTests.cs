using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importmessung</b> (G4-8, Umsetzungskonzept Gebäudesimulation 3.6/3.8): Der Weg, den der
    /// Prüfmodus der iOS-Schale im einen iOS-Lauf fährt, läuft hier unter Windows — gegen die Proben unter
    /// <c>Referenzlaeufe/Importproben/</c> und die synthetischen Großfälle. Geprüft wird die Funktion
    /// (Ergebnis, Raumzahl) und das Format der Protokollzeilen, nicht die Speicherzahlen selbst — die
    /// gelten nur für die Plattform, auf der sie gemessen werden.
    /// </summary>
    public sealed class ImportmessungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new("de-DE");

        public void Dispose() => _kultur.Dispose();

        private static readonly Regex ProbeMuster = new(
            @"^IMPORTPROBE fall=\S+ format=(IFC|GBXML|gbXML|\S+) schema=\S+ ergebnis=(OK|LESEFEHLER|RAEUME_FEHLEN|AUSNAHME|FORMAT_UNBEKANNT|ZEITGRENZE)"
            + @" gebaeude=\d+ raeume=\d+( erwartet=\d+)? nutzflaeche=(\d+(\.\d+)?|nv) meldungen=F\d+/W\d+/I\d+ pruefung=F\d+/W\d+"
            + @" fehlend=\d+ bytes=\d+( entpackt=\d+)? grenze_ios=\d+( erste=""[^""]*"")?( ausnahme=""[^""]*"")?$");

        private static readonly Regex MessMuster = new(
            @"^IMPORTMESSUNG fall=\S+ bytes=\d+ basis_mb=\d+\.\d dauer_lesen_ms=\d+ dauer_ms=\d+ verwaltet_vor_mb=\d+\.\d"
            + @" verwaltet_spitze_mb=\d+\.\d zuwachs_mb=\d+\.\d gehalten_mb=\d+\.\d alloziert_mb=(\d+\.\d|nv) faktor=(\d+\.\d|nv)"
            + @" prozess=\S+ prozess_vor_mb=(\d+\.\d|nv) prozess_spitze_mb=(\d+\.\d|nv) faktor_prozess=(\d+\.\d|nv)"
            + @" prozess_lebensspitze_mb=(\d+\.\d|nv) gc=\d+/\d+/\d+ proben=\d+$");

        private static string Probe(string name) => Path.Combine(IfcProbenTests.Ordner(), name);

        // ==================================================================
        //  Die Proben aus dem Paket der iOS-Schale
        // ==================================================================

        /// <summary>Die vier Proben, die die iOS-Schale mitbringt (EPOS.iOS.csproj, Importproben=true).</summary>
        public static IEnumerable<object[]> Paketproben() => new[]
        {
            new object[] { "gbxml_haus_si.xml", 1 },
            new object[] { "ifc4_haus.ifc", 1 },
            new object[] { "ifc4_haus.ifczip", 1 },
            new object[] { "ifc2x3_haus.ifc", 1 },
        };

        [Theory]
        [MemberData(nameof(Paketproben))]
        public void Die_Proben_der_Schale_werden_gelesen_und_im_Zeilenformat_gemeldet(string name, int gebaeude)
        {
            Importmessergebnis e = Importmessung.Messen(File.ReadAllBytes(Probe(name)), name);

            Assert.True(e.Ergebnis == Importmessergebnis.OK, e.ProbeZeile());
            Assert.Equal(gebaeude, e.Gebaeude);
            Assert.True(e.Raeume > 0, e.ProbeZeile());
            Assert.True(e.Nutzflaeche > 0, e.ProbeZeile());
            Assert.Equal(0, e.FehlendeEntitaeten);
            Assert.Matches(ProbeMuster, e.ProbeZeile());
            Assert.Matches(MessMuster, e.MessZeile());
            Assert.True(e.VerwaltetSpitze >= e.VerwaltetVorher);
            Assert.True(e.Proben >= 2, e.MessZeile());
        }

        [Fact]
        public void Bei_ifczip_ist_der_entpackte_Inhalt_die_Bezugsgroesse()
        {
            Importmessergebnis e = Importmessung.Messen(File.ReadAllBytes(Probe("ifc4_haus.ifczip")), "ifc4_haus.ifczip");

            Assert.Equal(new FileInfo(Probe("ifc4_haus.ifc")).Length, e.EntpackteBytes);
            Assert.Equal(e.EntpackteBytes.Value, e.Basisbytes);
            Assert.Contains(" entpackt=", e.ProbeZeile());
        }

        [Fact]
        public void Die_Zeilen_sind_invariant_auch_unter_de_DE()
        {
            Importmessergebnis e = Importmessung.Messen(File.ReadAllBytes(Probe("gbxml_haus_si.xml")), "gbxml_haus_si.xml");

            // Unter de-DE fiele ein ungeschütztes ToString() auf das Komma.
            Assert.DoesNotMatch(new Regex(@"=\d+,\d"), e.ProbeZeile());
            Assert.DoesNotMatch(new Regex(@"=\d+,\d"), e.MessZeile());
        }

        [Fact]
        public void Unbekannte_Endung_und_kaputte_Datei_werden_Zeilen_keine_Ausnahmen()
        {
            Importmessergebnis unbekannt = Importmessung.Messen(new byte[] { 1, 2, 3 }, "probe.txt");
            Assert.Equal(Importmessergebnis.FORMAT_UNBEKANNT, unbekannt.Ergebnis);

            Importmessergebnis kaputt = Importmessung.Messen(new byte[] { (byte)'<', (byte)'x' }, "kaputt.xml");
            Assert.Equal(Importmessergebnis.LESEFEHLER, kaputt.Ergebnis);
            Assert.Matches(ProbeMuster, kaputt.ProbeZeile());
            Assert.Contains(" erste=\"IMP_GBXML_PROT_LESEFEHLER", kaputt.ProbeZeile());
        }

        // ==================================================================
        //  Die synthetischen Großfälle
        // ==================================================================

        [Fact]
        public void Der_synthetische_gbXML_Grossfall_wird_vollstaendig_gelesen()
        {
            (byte[] daten, int raeume) = ImportmessungProben.ZuGroesse(2 * Importmessung.MB, ImportmessungProben.Gbxml);

            Assert.InRange(daten.LongLength, (long)(1.9 * Importmessung.MB), (long)(2.1 * Importmessung.MB));
            Importmessergebnis e = Importmessung.Messen(daten, "synth_gbxml_2mb.xml", erwarteteRaeume: raeume);

            Assert.True(e.Ergebnis == Importmessergebnis.OK, e.ProbeZeile());
            Assert.Equal(raeume, e.Raeume);
            Assert.Equal(raeume * ImportmessungProben.RAUMFLAECHE_M2, e.Nutzflaeche.Value, 6);
            Assert.Matches(ProbeMuster, e.ProbeZeile());
            Assert.Matches(MessMuster, e.MessZeile());
        }

        [Fact]
        public void Der_synthetische_IFC_Grossfall_wird_vollstaendig_gelesen()
        {
            (byte[] daten, int raeume) = ImportmessungProben.ZuGroesse(2 * Importmessung.MB, ImportmessungProben.Ifc);

            Assert.InRange(daten.LongLength, (long)(1.9 * Importmessung.MB), (long)(2.1 * Importmessung.MB));
            Importmessergebnis e = Importmessung.Messen(daten, "synth_ifc_2mb.ifc", erwarteteRaeume: raeume);

            Assert.True(e.Ergebnis == Importmessergebnis.OK, e.ProbeZeile());
            Assert.Equal(raeume, e.Raeume);
            Assert.Equal(0, e.FehlendeEntitaeten);
            Assert.Equal(raeume * ImportmessungProben.RAUMFLAECHE_M2, e.Nutzflaeche.Value, 6);
            Assert.Matches(ProbeMuster, e.ProbeZeile());
            Assert.Matches(MessMuster, e.MessZeile());
        }

        [Fact]
        public void Die_Erzeugung_ist_deterministisch_und_ohne_CR()
        {
            Assert.Equal(ImportmessungProben.Gbxml(120), ImportmessungProben.Gbxml(120));
            byte[] ifc = ImportmessungProben.Ifc(60);
            Assert.Equal(ifc, ImportmessungProben.Ifc(60));
            Assert.DoesNotContain((byte)'\r', ifc);
            Assert.DoesNotContain((byte)'\r', ImportmessungProben.Gbxml(3));
            Assert.Equal(ImportmessungProben.ZuGroesse(300_000, ImportmessungProben.Gbxml).Raeume,
                         ImportmessungProben.ZuGroesse(300_000, ImportmessungProben.Gbxml).Raeume);
        }

        [Fact]
        public void Die_synthetischen_Faelle_des_Pruefmodus_stehen_klein_vor_gross()
        {
            IReadOnlyList<Importmessfall> faelle = Importmessung.SynthetischeFaelle();

            Assert.Equal(6, faelle.Count);
            Assert.Equal(3, faelle.Count(f => f.Name.EndsWith(".xml", StringComparison.Ordinal)));
            Assert.Equal(3, faelle.Count(f => f.Name.EndsWith(".ifc", StringComparison.Ordinal)));
            Assert.All(faelle, f => Assert.NotNull(GebaeudeImportProfil.FuerDatei(f.Name)));
        }

        // ==================================================================
        //  Diagnose und Probelauf
        // ==================================================================

        [Fact]
        public void Die_Metadaten_der_drei_Schemata_sind_vollstaendig_und_die_Erwartung_stimmt()
        {
            var abweichungen = new List<string>();
            foreach (XbimSchemaVersion schema in new[] { XbimSchemaVersion.Ifc2X3, XbimSchemaVersion.Ifc4, XbimSchemaVersion.Ifc4x3 })
            {
                int ist = ExpressMetaData.GetMetadata(MemoryModel.GetFactory(schema)).Types().Count();
                if (Importmessung.ErwarteteTypen[schema] != ist)
                    abweichungen.Add(schema + ": erwartet " + Importmessung.ErwarteteTypen[schema] + ", ungetrimmt gezählt " + ist);
            }
            Assert.True(abweichungen.Count == 0, string.Join("; ", abweichungen));

            IReadOnlyList<string> zeilen = Importmessung.XbimDiagnose();
            Assert.Equal(3, zeilen.Count);
            Assert.All(zeilen, z => Assert.Matches(
                new Regex(@"^IMPORTDIAGNOSE xbim schema=\S+ typen=\d+ erwartet=\d+ eigenschaften=\d+ schluessel_fehlen=- ergebnis=OK$"), z));
        }

        [Fact]
        public void Die_Diagnose_faehrt_den_IFC_Weg_Schritt_fuer_Schritt()
        {
            IReadOnlyList<string> zeilen = Importmessung.Diagnose(File.ReadAllBytes(Probe("ifc4_haus.ifczip")), "ifc4_haus.ifczip");

            Assert.Contains(zeilen, z => z.Contains(" schritt=behaelter ergebnis=OK"));
            Assert.Contains(zeilen, z => z.Contains(" schritt=metadaten ergebnis=OK schema=Ifc4"));
            Assert.Contains(zeilen, z => z.Contains(" schritt=laden ergebnis=OK") && z.Contains(" raeume=5 ") && z.Contains(" nicht_angelegt=0 "));
            Assert.Contains(zeilen, z => z.Contains(" schritt=abbild ergebnis=OK gebaeude=1"));
            Assert.DoesNotContain(zeilen, z => z.Contains("AUSNAHME"));

            IReadOnlyList<string> xml = Importmessung.Diagnose(File.ReadAllBytes(Probe("gbxml_haus_si.xml")), "gbxml_haus_si.xml");
            Assert.Single(xml);
            Assert.Contains(" schritt=xml ergebnis=OK", xml[0]);
        }

        [Fact]
        public void Der_Ausnahmetext_traegt_Typ_Nachricht_innere_Ausnahme_und_hoechstens_die_ersten_Stapelzeilen()
        {
            Exception gefangen;
            try { Werfen(); throw new InvalidOperationException("nicht erreicht"); }
            catch (Exception ex) { gefangen = ex; }

            string text = Importmessung.Ausnahmetext(gefangen, 1);
            Assert.StartsWith("System.InvalidOperationException: außen", text);
            Assert.Contains("innen: System.MissingMethodException: Typ fehlt", text);
            Assert.Equal(4, text.Split('\n').Length);   // außen + 1 Stapelzeile, innen + 1 Stapelzeile
            Assert.DoesNotContain("\n", Importmessung.Einzeilig(text));
        }

        private static void Werfen()
        {
            try { throw new MissingMethodException("Typ fehlt"); }
            catch (Exception ex) { throw new InvalidOperationException("außen", ex); }
        }

        [Fact]
        public void Der_Probelauf_schreibt_Kopf_Metadaten_Faelle_Diagnose_und_Schluss()
        {
            var zeilen = new List<string>();
            var dateien = new List<(string, Func<Stream>)>
            {
                ("gbxml_haus_si.xml", () => File.OpenRead(Probe("gbxml_haus_si.xml"))),
                ("ifc4_haus.ifczip", () => File.OpenRead(Probe("ifc4_haus.ifczip"))),
                ("fehlt.ifc", () => null),
            };
            var faelle = new[]
            {
                Importmessung.Fall("synth_gbxml_klein.xml", 200_000, ImportmessungProben.Gbxml),
                Importmessung.Fall("synth_ifc_klein.ifc", 200_000, ImportmessungProben.Ifc),
            };
            int gezaehlteProzessabfragen = 0;
            Func<Prozessspeicherstand> prozess = () =>
            {
                gezaehlteProzessabfragen++;
                return new Prozessspeicherstand(100L * Importmessung.MB + gezaehlteProzessabfragen, 500L * Importmessung.MB, "attrappe");
            };

            int gut = Importmessung.Probelauf(dateien, zeilen.Add, prozess, faelle);

            Assert.Equal(4, gut);
            Assert.StartsWith("IMPORTPROBE start laufzeit=", zeilen[0]);
            Assert.EndsWith("prozess=attrappe", zeilen[0]);
            Assert.Equal(3, zeilen.Count(z => z.StartsWith("IMPORTDIAGNOSE xbim ", StringComparison.Ordinal)));
            Assert.Contains("IMPORTPROBE fall=fehlt.ifc ergebnis=DATEI_FEHLT", zeilen);
            Assert.Contains(zeilen, z => z.StartsWith("IMPORTDIAGNOSE fall=ifc4_haus.ifczip schritt=laden ergebnis=OK", StringComparison.Ordinal));
            Assert.Contains(zeilen, z => z.StartsWith("IMPORTPROBE fall=synth_ifc_klein.ifc erzeugt bytes=", StringComparison.Ordinal));
            Assert.Equal(4, zeilen.Count(z => z.StartsWith("IMPORTMESSUNG ", StringComparison.Ordinal)));
            Assert.All(zeilen.Where(z => z.StartsWith("IMPORTMESSUNG ", StringComparison.Ordinal)), z =>
            {
                Assert.Matches(MessMuster, z);
                Assert.Contains(" prozess=attrappe ", z);
                Assert.Contains(" prozess_lebensspitze_mb=500.0 ", z);
            });
            Assert.All(zeilen.Where(z => z.StartsWith("IMPORTPROBE fall=", StringComparison.Ordinal) && z.Contains(" format=")),
                       z => Assert.Matches(ProbeMuster, z));
            Assert.Equal("IMPORTPROBE ende faelle=5 ok=4", string.Join(" ", zeilen[^1].Split(' ').Take(4)));
        }

        [Fact]
        public void Ist_der_Zeitrahmen_verbraucht_wird_kein_synthetischer_Fall_mehr_begonnen()
        {
            var zeilen = new List<string>();
            bool erzeugt = false;
            var faelle = new[] { new Importmessfall("synth_gbxml_nie.xml", () => { erzeugt = true; return (ImportmessungProben.Gbxml(1), 1); }) };

            int gut = Importmessung.Probelauf(Array.Empty<(string, Func<Stream>)>(), zeilen.Add, null, faelle, TimeSpan.Zero);

            Assert.Equal(0, gut);
            Assert.False(erzeugt);
            Assert.Contains("IMPORTPROBE fall=synth_gbxml_nie.xml ergebnis=UEBERSPRUNGEN grund=zeitrahmen_0min", zeilen);
        }

        [Fact]
        public void Ein_Abbruch_ueber_die_Fallzeit_ist_eine_Zeile_ZEITGRENZE()
        {
            using var abgelaufen = new System.Threading.CancellationTokenSource();
            abgelaufen.Cancel();

            Importmessergebnis e = Importmessung.Messen(File.ReadAllBytes(Probe("ifc4_haus.ifc")), "ifc4_haus.ifc", abbruch: abgelaufen.Token);

            Assert.Equal(Importmessergebnis.ZEITGRENZE, e.Ergebnis);
            Assert.Matches(ProbeMuster, e.ProbeZeile());
        }
    }
}
