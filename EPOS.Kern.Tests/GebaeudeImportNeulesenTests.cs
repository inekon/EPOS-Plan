using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// HC-4 (HottCAD-Verbund 6.5, E87 F3) — „Datei erneut lesen“ im Kern ohne Datenbank: die passende Datei geht
    /// durch denselben Lese- und Klassifikationsweg wie der Import, jede andere Lage ist ein benannter Zustand ohne
    /// Abbild.
    /// </summary>
    public class GebaeudeImportNeulesenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        internal static string Probe(string name) => Path.Combine(IfcProbenTests.Ordner(), name);

        /// <summary>Eine gespeicherte Quelle zur Datei <paramref name="pfad"/> — Hash wie beim Import.</summary>
        internal static ImportquelleModel QuelleZu(string pfad, string format, string zonenregel = null)
        {
            byte[] b = File.ReadAllBytes(pfad);
            return new ImportquelleModel
            {
                ID = 1, ID_Gebaeude = 1, Format = format, Dateiname = Path.GetFileName(pfad),
                Hash = Convert.ToHexStringLower(SHA256.HashData(b)), Groesse = b.LongLength,
                Zeitpunkt = "2026-10-05T10:00:00+02:00", Zonenregel = zonenregel
            };
        }

        private static NeulesenErgebnis Lesen(string pfad, ImportquelleModel quelle, bool ios = false)
        {
            using FileStream s = File.OpenRead(pfad);
            return GebaeudeNeulesen.Lesen(s, pfad, quelle, "", ios);
        }

        [Fact]
        public void Eine_passende_IFC_liefert_Abbild_Klassifikation_und_Grundriss()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            NeulesenErgebnis e = Lesen(pfad, q);

            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            Assert.Equal(q.Hash, e.HashDatei);
            AbbildGebaeude g = Assert.IsType<AbbildGebaeude>(e.Gebaeude);
            Assert.NotEmpty(g.Raeume);
            Assert.All(g.Raeume, r => Assert.NotNull(r.Koerper));
            Assert.NotNull(g.Flaechengruppen);
            Assert.NotEmpty(g.Flaechengruppen);
            Assert.NotNull(e.Geometrie);
            Assert.NotNull(e.Zonierung);
            Assert.False(string.IsNullOrEmpty(e.Zonenregel));
        }

        [Fact]
        public void Eine_passende_gbXML_liefert_Abbild_und_Grundriss()
        {
            string pfad = Probe("gbxml_haus_si.xml");
            NeulesenErgebnis e = Lesen(pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_GBXML));

            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            Assert.NotNull(e.Gebaeude);
            Assert.NotNull(e.Geometrie);
            Assert.False(e.Hottcad);
        }

        [Fact]
        public void Die_gespeicherte_Zonenregel_gilt_wenn_das_Gebaeude_sie_traegt()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            NeulesenErgebnis vorgabe = Lesen(pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC));
            (var regeln, string _, bool _) = GebaeudeZonierung.Waehlbar(vorgabe.Abbild, vorgabe.Gebaeudeindex);
            foreach (string regel in regeln)
            {
                NeulesenErgebnis e = Lesen(pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC, regel));
                Assert.Equal(regel, e.Zonenregel);
            }
            NeulesenErgebnis fremd = Lesen(pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC, "Z9"));
            Assert.Equal(vorgabe.Zonenregel, fremd.Zonenregel);
        }

        [Fact]
        public void Ein_geaendertes_Byte_ergibt_Hash_abweichend_ohne_Abbild()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            byte[] b = File.ReadAllBytes(pfad);
            b[b.Length / 2] ^= 0x01;

            NeulesenErgebnis e;
            using (var s = new MemoryStream(b))
                e = GebaeudeNeulesen.Lesen(s, pfad, q, "", false);

            Assert.Equal(NeulesenZustand.HashAbweichend, e.Zustand);
            Assert.Null(e.Abbild);
            Assert.Null(e.Geometrie);
            Assert.Equal(q.Hash, e.HashQuelle);
            Assert.Equal(64, e.HashDatei.Length);
            Assert.NotEqual(q.Hash, e.HashDatei);
        }

        [Fact]
        public void Fremde_Endung_und_anderes_Format_sind_Format_unbekannt()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            using (var s = new MemoryStream(File.ReadAllBytes(pfad)))
                Assert.Equal(NeulesenZustand.FormatUnbekannt, GebaeudeNeulesen.Lesen(s, "haus.txt", q, "", false).Zustand);

            string xml = Probe("gbxml_haus_si.xml");
            Assert.Equal(NeulesenZustand.FormatUnbekannt, Lesen(xml, q).Zustand);
        }

        [Fact]
        public void Eine_Datei_ueber_der_Grenze_ist_zu_gross_und_wird_nicht_gelesen()
        {
            ImportquelleModel q = QuelleZu(Probe("ifc4_koerper_bauteile.ifc"), DbWerte.IMPORT_FORMAT_IFC);
            using var s = new Riesenstrom(long.MaxValue / 2);
            NeulesenErgebnis e = GebaeudeNeulesen.Lesen(s, "haus.ifc", q, "", true);
            Assert.Equal(NeulesenZustand.ZuGross, e.Zustand);
            Assert.True(e.Grenze > 0);
            Assert.False(s.Gelesen);
        }

        [Fact]
        public void Ohne_Quelle_und_ohne_Strom_bleibt_es_benannt()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            Assert.Equal(NeulesenZustand.KeineQuelle, GebaeudeNeulesen.Lesen(null, pfad, null, "", false).Zustand);
            Assert.Equal(NeulesenZustand.NichtLesbar,
                         GebaeudeNeulesen.Lesen(null, pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC), "", false).Zustand);
        }

        [Fact]
        public void Die_Kennung_waehlt_das_Gebaeude_einer_Datei_mit_mehreren()
        {
            string pfad = Probe("gbxml_zwei_gebaeude.xml");
            NeulesenErgebnis erst = Lesen(pfad, QuelleZu(pfad, DbWerte.IMPORT_FORMAT_GBXML));
            Assert.Equal(NeulesenZustand.Passend, erst.Zustand);
            Assert.True(erst.Abbild.Gebaeude.Count >= 2);
            string zweite = Quellkennung.Kuerzen(erst.Abbild.Gebaeude[1].Kennung);
            Assert.Equal(1, GebaeudeNeulesen.Gebaeudeindex(erst.Abbild, zweite));
            Assert.Equal(0, GebaeudeNeulesen.Gebaeudeindex(erst.Abbild, "unbekannt"));
        }

        /// <summary>Ein Strom, der eine Länge meldet, aber nie gelesen werden darf.</summary>
        private sealed class Riesenstrom : Stream
        {
            private readonly long _laenge;
            public Riesenstrom(long laenge) => _laenge = laenge;
            public bool Gelesen { get; private set; }
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _laenge;
            public override long Position { get; set; }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) { Gelesen = true; return 0; }
            public override long Seek(long offset, SeekOrigin origin) => Position = offset;
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }

    /// <summary>
    /// HC-4 gegen die Arbeitskopie der Testdatenbank: Die Quelle eines importierten Gebäudes wird über die Zuordnung
    /// gefunden, die Gebäudekennung über die Paarung — und das erneute Lesen schreibt keine Zeile.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportNeulesenDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int GEBAEUDE = 10614;

        private static readonly string[] TABELLEN =
            { "Tab_Importquelle", "Tab_Importzuordnung", "Tab_Raumgrundriss", "Tab_Gebaeude", "Tab_Zone", "Tab_Bauteil", "Tab_Bauteilaufbau", "Tab_Baustoff" };

        private static long[] Zaehlen()
            => TABELLEN.Select(t => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t + "\""),
                                                    CultureInfo.InvariantCulture)).ToArray();

        [Fact]
        public void Quelle_ueber_die_Zuordnung_finden_und_neu_lesen_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportSatz satz = ImportzuordnungSchemaRegelTests.IfcSatz();
            var ctrl = new GebaeudeImportCtrl();
            Assert.True(ctrl.SchreibeHerkunft(GEBAEUDE, satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz)).Ok);
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                                   new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture);

            long[] vorher = Zaehlen();
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellenDerZuordnung(idZ));
            Assert.Equal("ifc4_haus.ifc", q.Dateiname);
            string kennung = ctrl.Gebaeudekennung(q);
            Assert.Equal(satz.Gebaeudekennung, kennung);

            string pfad = GebaeudeImportNeulesenTests.Probe("ifc4_haus.ifc");
            NeulesenErgebnis e;
            using (FileStream s = File.OpenRead(pfad))
                e = GebaeudeNeulesen.Lesen(s, pfad, q, kennung, false);
            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            Assert.NotNull(e.Geometrie);

            byte[] b = File.ReadAllBytes(pfad);
            b[10] ^= 0x01;
            using (var s = new MemoryStream(b))
                Assert.Equal(NeulesenZustand.HashAbweichend, GebaeudeNeulesen.Lesen(s, pfad, q, kennung, false).Zustand);

            Assert.Equal(vorher, Zaehlen());
        }

        [Fact]
        public void Ein_Gebaeude_ohne_Import_hat_keine_Quelle()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeImportCtrl();
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                                   new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture);
            Assert.Empty(ctrl.LesenQuellenDerZuordnung(idZ));
            Assert.Empty(ctrl.LesenQuellenDerZuordnung(0));
            Assert.Equal("", ctrl.Gebaeudekennung(null));
        }
    }
}
