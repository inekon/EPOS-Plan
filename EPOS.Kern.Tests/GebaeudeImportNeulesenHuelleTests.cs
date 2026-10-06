using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// HC-4 — die Hülle „Datei erneut lesen“ (<see cref="GebaeudeNeulesenHuelle"/>): passender und abweichender
    /// SHA-256, Abbruch der Dateiwahl, ein Gebäude ohne Importquelle, Filter je Plattform — und dass nichts
    /// geschrieben wird.
    /// </summary>
    [Collection("Testdatenbank")]   // die Fälle tauschen Dienste.Datei — prozessweiter Zustand
    public class GebaeudeImportNeulesenHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly List<string> _temp = new();

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            foreach (string t in _temp)
                try { File.Delete(t); } catch (IOException) { }
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int GEBAEUDE = 10614;

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Pfad = "";
            internal string Titel = "";
            internal string Filter = "";
            internal int Wahlen;
            public string DateiOeffnen(string titel, string filter, string startOrdner)
            {
                Wahlen++;
                Titel = titel ?? "";
                Filter = filter ?? "";
                return Pfad;
            }
            public string DateiSpeichern(string titel, string filter, string vorschlag) => "";
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => false;
        }

        private static string Probe(string name) => GebaeudeImportNeulesenTests.Probe(name);

        private string Geaendert(string pfad)
        {
            byte[] b = File.ReadAllBytes(pfad);
            b[b.Length / 2] ^= 0x01;
            string ziel = Path.Combine(Path.GetTempPath(), "epos-neulesen-" + Guid.NewGuid().ToString("N") + Path.GetExtension(pfad));
            File.WriteAllBytes(ziel, b);
            _temp.Add(ziel);
            return ziel;
        }

        // ---------------------------------------------------------------- ohne Datenbank

        [Fact]
        public async Task Passender_Hash_baut_die_Ansicht_mit_Randgruppen()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = GebaeudeImportNeulesenTests.QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            GebaeudeNeulesestand s = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(pfad, q, "");

            Assert.Equal(GebaeudeNeulesezustand.Passend, s.Zustand);
            Assert.NotNull(s.Ansicht);
            Assert.NotEmpty(s.Ansicht!.Geschosse);
            Assert.True(s.Ansicht.HatRandgruppen, "Die Ansicht trägt keine Gruppen nach Randbedingung.");
            Assert.False(s.Ansicht.Umhaengbar);
            Assert.Contains("ifc4_koerper_bauteile.ifc", s.Hinweis);
            Assert.Contains("Zuordnungen von Hand", s.Zonenhinweis);
            Assert.False(s.AndereDateiAnbieten);
        }

        [Fact]
        public async Task Passende_gbXML_baut_die_Ansicht()
        {
            string pfad = Probe("gbxml_haus_si.xml");
            ImportquelleModel q = GebaeudeImportNeulesenTests.QuelleZu(pfad, DbWerte.IMPORT_FORMAT_GBXML);
            GebaeudeNeulesestand s = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(pfad, q, "");
            Assert.Equal(GebaeudeNeulesezustand.Passend, s.Zustand);
            Assert.NotNull(s.Ansicht);
        }

        [Fact]
        public async Task Abweichender_Hash_zeigt_keine_Ansicht_und_nennt_beide_Hashes()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = GebaeudeImportNeulesenTests.QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            string anders = Geaendert(pfad);
            GebaeudeNeulesestand s = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(anders, q, "");

            Assert.Equal(GebaeudeNeulesezustand.HashAbweichend, s.Zustand);
            Assert.Null(s.Ansicht);
            Assert.True(s.AndereDateiAnbieten);
            Assert.Contains(q.Hash.Substring(0, GebaeudeNeulesenHuelle.HASH_KURZ), s.Hinweis);
            Assert.DoesNotContain(q.Hash, s.Hinweis);
            Assert.Contains("nicht die Datei des Imports", s.Hinweis);
            Assert.Contains(File.GetLastWriteTime(anders).ToString("g", CultureInfo.CurrentCulture), s.Hinweis);
        }

        [Fact]
        public async Task Fehlende_Datei_und_fremdes_Format_sind_benannt()
        {
            string pfad = Probe("ifc4_koerper_bauteile.ifc");
            ImportquelleModel q = GebaeudeImportNeulesenTests.QuelleZu(pfad, DbWerte.IMPORT_FORMAT_IFC);
            var h = new GebaeudeNeulesenHuelle(ios: false);

            GebaeudeNeulesestand fehlt = await h.LesenAsync(Path.Combine(Path.GetTempPath(), "fehlt-" + Guid.NewGuid().ToString("N") + ".ifc"), q, "");
            Assert.Equal(GebaeudeNeulesezustand.NichtLesbar, fehlt.Zustand);
            Assert.Null(fehlt.Ansicht);
            Assert.True(fehlt.AndereDateiAnbieten);

            GebaeudeNeulesestand fremd = await h.LesenAsync(Probe("gbxml_haus_si.xml"), q, "");
            Assert.Equal(GebaeudeNeulesezustand.FormatUnbekannt, fremd.Zustand);
            Assert.Contains("IFC", fremd.Hinweis);
        }

        [Fact]
        public void Der_Filter_nennt_den_Dateinamen_unter_Windows_und_nicht_auf_iOS()
        {
            string win = new GebaeudeNeulesenHuelle(ios: false).Filter("haus.ifc");
            Assert.StartsWith("haus.ifc|haus.ifc|", win, StringComparison.Ordinal);
            Assert.EndsWith(GebaeudeImportProfil.DATEIFILTER_ALLE, win, StringComparison.Ordinal);
            Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, new GebaeudeNeulesenHuelle(ios: true).Filter("haus.ifc"));
            Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, new GebaeudeNeulesenHuelle(ios: false).Filter("a|b.ifc"));
        }

        [Fact]
        public async Task Eine_Zeile_ohne_Projektkopie_hat_keine_Quelle()
        {
            var zeile = new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX + 1, HatProjektkopie = false };
            Assert.Null(GebaeudeNeulesenHuelle.Angabe(zeile));
            var probe = new Dateiprobe { Pfad = Probe("ifc4_haus.ifc") };
            Dienste.Datei = probe;
            GebaeudeNeulesestand? s = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(zeile);
            Assert.Equal(GebaeudeNeulesezustand.KeineQuelle, s!.Zustand);
            Assert.Equal(0, probe.Wahlen);
        }

        // ---------------------------------------------------------------- mit Datenbank

        private (GebaeudeProjektZeile Zeile, ImportquelleModel Quelle) Importiert()
        {
            GebaeudeImportSatz satz = ImportzuordnungSchemaRegelTests.IfcSatz();
            Assert.True(new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz)).Ok);
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                                   new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture);
            var zeile = new GebaeudeProjektZeile { IdZ = idZ, IdGebaeude = GEBAEUDE, HatProjektkopie = true, Name = "Haus" };
            return (zeile, new GebaeudeImportCtrl().LesenQuellenDerZuordnung(idZ).Single());
        }

        private static long[] Zaehlen()
            => new[] { "Tab_Importquelle", "Tab_Importzuordnung", "Tab_Gebaeude", "Tab_Zone", "Tab_Bauteil" }
               .Select(t => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t + "\""), CultureInfo.InvariantCulture))
               .ToArray();

        [Fact]
        public async Task Mit_Quelle_zeigt_die_Angabe_Name_und_Zeitpunkt_und_der_Lauf_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            (GebaeudeProjektZeile zeile, ImportquelleModel q) = Importiert();

            GebaeudeImportquelleAngabe a = Assert.IsType<GebaeudeImportquelleAngabe>(GebaeudeNeulesenHuelle.Angabe(zeile));
            Assert.Equal("ifc4_haus.ifc", a.Dateiname);
            Assert.Contains("ifc4_haus.ifc", a.Quellzeile);
            Assert.Contains(GebaeudeImportHuelle.Zeitpunkttext(q.Zeitpunkt), a.Quellzeile);

            long[] vorher = Zaehlen();
            var probe = new Dateiprobe { Pfad = Probe("ifc4_haus.ifc") };
            Dienste.Datei = probe;
            GebaeudeNeulesestand? passend = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(zeile);
            Assert.Equal(GebaeudeNeulesezustand.Passend, passend!.Zustand);
            Assert.NotNull(passend.Ansicht);
            Assert.Contains("ifc4_haus.ifc", probe.Titel);
            Assert.StartsWith("ifc4_haus.ifc|ifc4_haus.ifc|", probe.Filter, StringComparison.Ordinal);

            probe.Pfad = Geaendert(Probe("ifc4_haus.ifc"));
            GebaeudeNeulesestand? anders = await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(zeile);
            Assert.Equal(GebaeudeNeulesezustand.HashAbweichend, anders!.Zustand);
            Assert.Null(anders.Ansicht);

            Assert.Equal(vorher, Zaehlen());
        }

        [Fact]
        public async Task Abbruch_der_Dateiwahl_ist_kein_Fehler()
        {
            if (!_db.Vorhanden) return;
            (GebaeudeProjektZeile zeile, ImportquelleModel _) = Importiert();
            var probe = new Dateiprobe { Pfad = "" };
            Dienste.Datei = probe;
            Assert.Null(await new GebaeudeNeulesenHuelle(ios: false).LesenAsync(zeile));
            Assert.Equal(1, probe.Wahlen);

            Dienste.Datei = new KeineDateiwahl();
            Assert.Null(await new GebaeudeNeulesenHuelle(ios: true).LesenAsync(zeile));
        }

        [Fact]
        public void Ein_nicht_importiertes_Gebaeude_hat_keinen_Knopf()
        {
            if (!_db.Vorhanden) return;
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                                   new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture);
            Assert.Null(GebaeudeNeulesenHuelle.Angabe(new GebaeudeProjektZeile { IdZ = idZ, HatProjektkopie = true }));
        }
    }
}
