using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Export;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Formatwahl der Exporthülle</b> (<c>EPOS.UI.Daten/Bedarf/GebaeudeExportHuelle.cs</c>, Stufe G7c):
    /// Der Steuerwert der Eingabe setzt das Format des Profils, Dateifilter und Endung folgen dem Profil, ein
    /// abgebrochenes Schreiben (<c>Bytes == 0</c>) fragt keine Dateiwahl, schreibt keine Datei und teilt nichts,
    /// und die Exportzusage (IDS) wird neben der Vorlagendatenbank gesucht und über
    /// <see cref="IDateiDienst.MitSystemOeffnen"/> geöffnet bzw. geteilt.
    /// </summary>
    [Collection("Testdatenbank")]   // die Fälle tauschen Dienste.Datei und Dienste.Pfade — prozessweiter Zustand
    public sealed class GebaeudeExportFormatHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly IPfade _pfadeVorher = Dienste.Pfade;
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-gexpf-" + Guid.NewGuid().ToString("N"));

        public GebaeudeExportFormatHuelleTests()
        {
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            Dienste.Pfade = _pfadeVorher;
            _kultur.Dispose();
            try { Directory.Delete(_ordner, recursive: true); } catch (IOException) { }
        }

        // =====================================================================
        //  Vorrichtung
        // =====================================================================

        private static GebaeudeExportSatz Haus() => ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus(), plz: null);

        private static GebaeudeExportHuelle Huelle(bool ios = false,
            Func<GebaeudeExportPlan, Stream, GebaeudeExportProfil, GebaeudeExportBilanz> schreiber = null)
            => new GebaeudeExportHuelle(1, 1, "Probe Haus", ios, (p, z) => Haus(), () => GbxmlExportProbe.Profil(),
                                        schreiber);

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            internal string OeffnenPfad = "";
            internal string OeffnenFilter = "";
            internal int Oeffnungen;
            internal string Filter = "";
            internal string Vorschlag = "";
            internal int Wahlen;
            internal bool OeffnenAntwort = true;
            internal readonly List<string> Geoeffnet = new();

            public string DateiOeffnen(string titel, string filter, string startOrdner)
            {
                Oeffnungen++;
                OeffnenFilter = filter ?? "";
                return OeffnenPfad;
            }

            public string DateiSpeichern(string titel, string filter, string vorschlag)
            {
                Wahlen++;
                Filter = filter ?? "";
                Vorschlag = vorschlag ?? "";
                return Antwort;
            }

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad)
            {
                Geoeffnet.Add(pfad);
                return OeffnenAntwort;
            }
        }

        private sealed class Pfadprobe : StandardPfade
        {
            private readonly string _vorlage;
            private readonly string _berichte;
            internal Pfadprobe(string vorlage, string berichte = null) { _vorlage = vorlage; _berichte = berichte; }
            public override string Auslieferungsvorlage => _vorlage;
            public override string Berichtsvorlagen => _berichte ?? base.Berichtsvorlagen;
        }

        // =====================================================================
        //  Format, Profil, Filter
        // =====================================================================

        [Fact]
        public void Die_Formate_tragen_die_Persistenzwerte_und_Texte_aus_den_Ressourcen()
        {
            IReadOnlyList<GebaeudeExportFormat> formate = GebaeudeExportHuelle.Formate();
            Assert.Equal(new[] { GebaeudeQuelle.FORMAT_GBXML, GebaeudeQuelle.FORMAT_IFC }, formate.Select(f => f.Wert));
            Assert.Equal(new[] { R.GEXP_FORMAT_GBXML, R.GEXP_FORMAT_IFC }, formate.Select(f => f.Text));
            Assert.Equal(new[] { R.GEXP_STUFE_SCHEMATISCH, R.GEXP_STUFE_DATEN }, formate.Select(f => f.Umfang));
            Assert.Equal(new[] { false, true }, formate.Select(f => f.MitZusage));
        }

        [Fact]
        public void Das_Format_der_Eingabe_setzt_Profil_Filter_und_Endung()
        {
            GebaeudeExportProfil vorbild = GbxmlExportProbe.Profil();
            Assert.Same(vorbild, GebaeudeExportHuelle.MitFormat(vorbild, ""));
            Assert.Same(vorbild, GebaeudeExportHuelle.MitFormat(vorbild, GebaeudeQuelle.FORMAT_GBXML));

            GebaeudeExportProfil ifc = GebaeudeExportHuelle.MitFormat(vorbild, GebaeudeQuelle.FORMAT_IFC);
            Assert.True(ifc.IstIfc);
            Assert.Equal(vorbild.Sprache, ifc.Sprache);
            Assert.Equal(vorbild.Programmversion, ifc.Programmversion);
            Assert.Same(vorbild.Uhr, ifc.Uhr);
            Assert.Throws<ArgumentException>(() => GebaeudeExportHuelle.MitFormat(vorbild, "gbXML (Schemafassung 6.01)"));

            Assert.Equal(R.GEXP_DATEITYP_GBXML + " " + GebaeudeExportProfil.DATEIFILTER, GebaeudeExportHuelle.Dateifilter(vorbild));
            Assert.Equal(R.GEXP_DATEITYP_IFC + " " + GebaeudeExportProfil.DATEIFILTER_IFC, GebaeudeExportHuelle.Dateifilter(ifc));
            Assert.Equal(".xml", GebaeudeExportHuelle.Endung(vorbild));
            Assert.Equal(".ifc", GebaeudeExportHuelle.Endung(ifc));
        }

        [Fact]
        public async Task Die_Vorschau_zeigt_die_Meldungen_je_Format()
        {
            GebaeudeExportHuelle h = Huelle();
            GebaeudeExportAnsicht gbxml = await h.Vorbereiten("", GebaeudeQuelle.FORMAT_GBXML);
            GebaeudeExportAnsicht ifc = await h.Vorbereiten("", GebaeudeQuelle.FORMAT_IFC);
            Assert.Equal(1, h.Lesungen);
            Assert.False(gbxml.Abgelehnt, gbxml.Ablehnung);
            Assert.False(ifc.Abgelehnt, ifc.Ablehnung);
            Assert.NotEmpty(gbxml.Meldungen);
            Assert.NotEmpty(ifc.Meldungen);
            // Welche Meldungen je Format im Plan stehen, entscheidet der Ablauf im Kern
            // (GebaeudeExportAblauf.Vorbereiten); die Hülle reicht nur das Profil des Formats.
        }

        [Fact]
        public async Task Unter_Windows_entsteht_eine_IFC_Datei_mit_Endung_und_Filter_des_Profils()
        {
            string ziel = Path.Combine(_ordner, "haus.ifc");
            var datei = new Dateiprobe { Antwort = ziel };
            Dienste.Datei = datei;

            GebaeudeExportErgebnis e = await Huelle().Speichern("", GebaeudeQuelle.FORMAT_IFC);

            Assert.True(e.Gespeichert, e.Text);
            Assert.Equal("Probe Haus.ifc", datei.Vorschlag);
            Assert.Equal(R.GEXP_DATEITYP_IFC + " " + GebaeudeExportProfil.DATEIFILTER_IFC, datei.Filter);
            Assert.Empty(datei.Geoeffnet);
            Assert.StartsWith("ISO-10303-21;", File.ReadAllText(ziel, Encoding.ASCII), StringComparison.Ordinal);
        }

        [Fact]
        public async Task Auf_iOS_wird_die_IFC_Datei_geteilt_und_traegt_die_Gebaeude_ID()
        {
            string ziel = Path.Combine(_ordner, "haus_ios.ifc");
            var datei = new Dateiprobe { Antwort = ziel };
            Dienste.Datei = datei;

            GebaeudeExportErgebnis e = await Huelle(ios: true).Speichern("", GebaeudeQuelle.FORMAT_IFC);

            Assert.True(e.Gespeichert, e.Text);
            Assert.EndsWith("_" + ExportSatzProbe.GEB + ".ifc", datei.Vorschlag, StringComparison.Ordinal);
            Assert.Equal(ziel, Assert.Single(datei.Geoeffnet));
        }

        [Fact]
        public async Task Ein_abgebrochenes_Schreiben_fragt_keine_Datei_schreibt_nichts_und_teilt_nichts()
        {
            var datei = new Dateiprobe { Antwort = Path.Combine(_ordner, "nie.ifc") };
            Dienste.Datei = datei;
            var abbruch = new List<PruefMeldung>
            {
                new PruefMeldung(PruefStufe.Fehler, IfcSchreiber.ABGEBROCHEN, "3"),
            };
            GebaeudeExportHuelle h = Huelle(ios: true, schreiber: (plan, ziel, p) =>
            {
                Assert.True(p.IstIfc);
                return new GebaeudeExportBilanz(0, 0, 0, 0, plan.Meldungen.Concat(abbruch).ToList(), 0);
            });

            GebaeudeExportErgebnis e = await h.Speichern("", GebaeudeQuelle.FORMAT_IFC);

            Assert.False(e.Gespeichert);
            Assert.False(e.Abgebrochen);
            Assert.Equal(R.GEXP_MSG_NICHTS_GESCHRIEBEN, e.Text);
            Assert.NotNull(e.Meldungen);
            GebaeudeExportMeldung letzte = e.Meldungen!.Last();
            Assert.Equal("GEXP_PROT_IFC_ABGEBROCHEN", letzte.Schluessel);
            Assert.Equal(WarnStufe.Fehler, letzte.Stufe);
            Assert.Contains("3", letzte.Text, StringComparison.Ordinal);
            Assert.Equal(0, datei.Wahlen);
            Assert.Empty(datei.Geoeffnet);
            Assert.Empty(Directory.GetFiles(_ordner));
        }

        // =====================================================================
        //  Exportzusage (IDS)
        // =====================================================================

        [Fact]
        public async Task Die_Exportzusage_wird_neben_der_Vorlage_geoeffnet()
        {
            string vorlage = Path.Combine(_ordner, "Vorlage", "Kenndaten.sqlite");
            Directory.CreateDirectory(Path.GetDirectoryName(vorlage)!);
            string ids = Path.Combine(_ordner, "Vorlage", GebaeudeExportHuelle.ZUSAGE_DATEI);
            File.WriteAllText(ids, "<ids/>");
            Dienste.Pfade = new Pfadprobe(vorlage);
            var datei = new Dateiprobe();
            Dienste.Datei = datei;

            Assert.Null(await Huelle().ZusageOeffnen());
            Assert.Equal(ids, Assert.Single(datei.Geoeffnet));

            datei.OeffnenAntwort = false;
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.GEXP_MSG_ZUSAGE_FEHLER, ids), await Huelle(ios: true).ZusageOeffnen());
        }

        /// <summary>iOS: Die Vorlagendatenbank liegt nicht im Paket; die Zusage steht dort neben dem Ordner der Berichtsvorlagen.</summary>
        [Fact]
        public async Task Die_Exportzusage_wird_ersatzweise_im_Paket_neben_den_Berichtsvorlagen_gefunden()
        {
            string paket = Path.Combine(_ordner, "Paket.app");
            Directory.CreateDirectory(Path.Combine(paket, "Vorlage"));
            string ids = Path.Combine(paket, "Vorlage", GebaeudeExportHuelle.ZUSAGE_DATEI);
            File.WriteAllText(ids, "<ids/>");
            Dienste.Pfade = new Pfadprobe(Path.Combine(_ordner, "anderswo", "Vorlage", "Kenndaten.sqlite"),
                                          Path.Combine(paket, "Vorlagen"));
            var datei = new Dateiprobe();
            Dienste.Datei = datei;

            Assert.Null(await Huelle(ios: true).ZusageOeffnen());
            Assert.Equal(ids, Assert.Single(datei.Geoeffnet));
        }

        [Fact]
        public async Task Eine_fehlende_Exportzusage_wird_benannt_und_nichts_geoeffnet()
        {
            string vorlage = Path.Combine(_ordner, "Vorlage", "Kenndaten.sqlite");
            Dienste.Pfade = new Pfadprobe(vorlage);
            var datei = new Dateiprobe();
            Dienste.Datei = datei;

            string grund = await Huelle().ZusageOeffnen();

            string erwartet = Path.Combine(_ordner, "Vorlage", GebaeudeExportHuelle.ZUSAGE_DATEI);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.GEXP_MSG_ZUSAGE_FEHLT, erwartet), grund);
            Assert.Empty(datei.Geoeffnet);
        }

        [Fact]
        public void Der_Parametersatz_reicht_Formate_und_Zusage()
        {
            IReadOnlyDictionary<string, object> gaben = Huelle(ios: true).Gaben(geaendert: false);
            Assert.IsAssignableFrom<IReadOnlyList<GebaeudeExportFormat>>(gaben["Formate"]);
            Assert.IsType<Func<Task<string>>>(gaben["ZusageOeffnen"]);
            Assert.Equal(R.GEXP_BTN_ZUSAGE_IOS, ((GebaeudeExportTexte)gaben["Texte"]).Zusage);
            Assert.Equal(R.GEXP_BTN_ZUSAGE, GebaeudeExportHuelle.Texte(ios: false).Zusage);
        }

        // =====================================================================
        //  Anreicherung der Originaldatei (Stufe G7d)
        // =====================================================================

        private static GebaeudeExportHuelle AnrHuelle(List<ImportquelleModel> quellen, bool ios = false,
            Func<GebaeudeExportPlan, Stream, ImportquelleModel, IReadOnlyList<ImportzuordnungModel>, Stream,
                 GebaeudeExportProfil, GebaeudeAnreicherungBilanz> anreicherer = null,
            List<int> gefragt = null)
            => new GebaeudeExportHuelle(1, 1, "Probe Haus", ios, (p, z) => Haus(), () => GbxmlExportProbe.Profil(), null,
                                        g => { gefragt?.Add(g); return quellen; },
                                        q => q == 3 ? IfcAnreicherungTests.Zuordnungen() : new List<ImportzuordnungModel>(),
                                        anreicherer);

        private string OriginalAblegen(byte[] inhalt, string name = "haus.ifc")
        {
            string pfad = Path.Combine(_ordner, "quelle", name);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            File.WriteAllBytes(pfad, inhalt);
            return pfad;
        }

        private static GebaeudeAnreicherungBilanz Bilanz(Stream ziel, int bytes = 64)
        {
            ziel.Write(Encoding.ASCII.GetBytes("ISO-10303-21;" + new string('x', bytes - 13)));
            return new GebaeudeAnreicherungBilanz(true, false, 5, 3, 1, 1, new List<PruefMeldung>(), bytes);
        }

        [Fact]
        public async Task Die_Anreicherung_steht_nur_mit_IFC_Importquelle_des_Gebaeudes()
        {
            var gefragt = new List<int>();
            byte[] datei = IfcAnreicherungTests.Fremdhaus();
            var gbxml = new ImportquelleModel { ID = 9, Format = GebaeudeQuelle.FORMAT_GBXML, Zeitpunkt = "2026-09-01T08:00:00" };

            Assert.True(await AnrHuelle(new List<ImportquelleModel> { gbxml, IfcAnreicherungTests.Quelle(datei) }, gefragt: gefragt).AnreicherungMoeglich());
            Assert.Equal(new[] { ExportSatzProbe.GEB }, gefragt);
            Assert.False(await AnrHuelle(new List<ImportquelleModel> { gbxml }).AnreicherungMoeglich());
            Assert.False(await AnrHuelle(new List<ImportquelleModel>()).AnreicherungMoeglich());
            var wirft = new GebaeudeExportHuelle(1, 1, "Probe Haus", false, (p, z) => Haus(), () => GbxmlExportProbe.Profil(), null,
                                                 g => throw new InvalidOperationException("keine Datenbank"));
            Assert.False(await wirft.AnreicherungMoeglich());
        }

        [Fact]
        public async Task Eine_abgebrochene_Dateiwahl_merkt_nichts_und_ohne_Datei_wird_nicht_angereichert()
        {
            var datei = new Dateiprobe { Antwort = Path.Combine(_ordner, "nie.ifc") };
            Dienste.Datei = datei;
            int gerufen = 0;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel>(), anreicherer: (pl, o, q, z, ziel, pr) => { gerufen++; return Bilanz(ziel); });

            Assert.Null(await h.OriginalWaehlen());
            Assert.Equal(1, datei.Oeffnungen);
            Assert.Equal(R.GEXP_DATEITYP_IFC + " " + GebaeudeExportProfil.DATEIFILTER_IFC, datei.OeffnenFilter);

            GebaeudeExportErgebnis e = await h.Anreichern("");
            Assert.False(e.Gespeichert);
            Assert.Equal(R.GEXP_ANR_SPERRE_DATEI, e.Text);
            Assert.Equal(0, gerufen);
            Assert.Equal(0, datei.Wahlen);
        }

        [Fact]
        public async Task Eine_fremde_Datei_wird_benannt_verweigert_und_nie_angereichert()
        {
            byte[] importiert = IfcAnreicherungTests.Fremdhaus();
            byte[] anders = IfcAnreicherungTests.Fremdhaus(geometrie: true);
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(anders, "anders.ifc"), Antwort = Path.Combine(_ordner, "nie.ifc") };
            Dienste.Datei = datei;
            int gerufen = 0;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) },
                                               anreicherer: (pl, o, q, z, ziel, pr) => { gerufen++; return Bilanz(ziel); });

            GebaeudeAnreicherungWahl w = await h.OriginalWaehlen();

            Assert.NotNull(w);
            Assert.True(w.Verweigert);
            Assert.Equal("anders.ifc", w.Dateiname);
            Assert.StartsWith("fremdhaus.ifc, ", w.Quelle, StringComparison.Ordinal);
            Assert.Contains("26.09.2026", w.Quelle, StringComparison.Ordinal);
            GebaeudeExportMeldung grund = w.Meldungen.First(m => m.Stufe == WarnStufe.Fehler);
            Assert.Equal("GEXP_PROT_ANR_HASH", grund.Schluessel);
            Assert.Equal(grund.Text, w.Grund);
            Assert.Contains("fremdhaus.ifc", w.Grund, StringComparison.Ordinal);

            GebaeudeExportErgebnis e = await h.Anreichern("");
            Assert.False(e.Gespeichert);
            Assert.Equal(0, gerufen);
            Assert.Equal(0, datei.Wahlen);
        }

        [Fact]
        public async Task Ohne_IFC_Quelle_nennt_die_Vorschau_die_fehlende_Quelle()
        {
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(IfcAnreicherungTests.Fremdhaus()) };
            Dienste.Datei = datei;

            GebaeudeAnreicherungWahl w = await AnrHuelle(new List<ImportquelleModel>()).OriginalWaehlen();

            Assert.True(w.Verweigert);
            Assert.Equal(R.GEXP_ANR_KEINE_QUELLE, w.Quelle);
            Assert.Equal("GEXP_PROT_ANR_KEINE_QUELLE", Assert.Single(w.Meldungen).Schluessel);
        }

        [Fact]
        public async Task Die_Freigabe_zeigt_den_Beipackzettel_und_speichert_unter_dem_Dateivorschlag()
        {
            byte[] importiert = IfcAnreicherungTests.Fremdhaus();
            string ziel = Path.Combine(_ordner, "haus_EPOS.ifc");
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(importiert), Antwort = ziel };
            Dienste.Datei = datei;
            ImportquelleModel gesehen = null;
            IReadOnlyList<ImportzuordnungModel> zuordnungen = null;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) },
                anreicherer: (pl, o, q, z, aus, pr) =>
                {
                    Assert.True(pr.IstIfc);
                    Assert.False(pl.Abgelehnt);
                    using var kopie = new MemoryStream();
                    o.CopyTo(kopie);
                    Assert.Equal(importiert, kopie.ToArray());
                    gesehen = q;
                    zuordnungen = z;
                    return Bilanz(aus);
                });

            GebaeudeAnreicherungWahl w = await h.OriginalWaehlen();
            Assert.False(w.Verweigert, w.Grund);
            Assert.Null(w.Grund);
            GebaeudeExportMeldung beipack = Assert.Single(w.Meldungen);
            Assert.Equal("GEXP_PROT_ANR_BEIPACK_FREMDDATEI", beipack.Schluessel);
            Assert.Equal(WarnStufe.Warnung, beipack.Stufe);

            GebaeudeExportErgebnis e = await h.Anreichern("01067");

            Assert.True(e.Gespeichert, e.Text);
            Assert.Equal("haus_EPOS.ifc", datei.Vorschlag);
            Assert.NotEqual("haus.ifc", datei.Vorschlag);
            Assert.Equal(R.GEXP_DATEITYP_IFC + " " + GebaeudeExportProfil.DATEIFILTER_IFC, datei.Filter);
            Assert.Equal(3, gesehen.ID);
            Assert.Equal(IfcAnreicherungTests.Zuordnungen().Count, zuordnungen.Count);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.GEXP_ANR_MSG_GESPEICHERT, ziel, 64L, 5, 3, 1, 1), e.Text);
            Assert.Equal(64, new FileInfo(ziel).Length);
            Assert.Empty(datei.Geoeffnet);
        }

        [Fact]
        public async Task Auf_iOS_wird_die_angereicherte_Datei_geteilt()
        {
            byte[] importiert = IfcAnreicherungTests.Fremdhaus();
            string ziel = Path.Combine(_ordner, "haus_EPOS.ifc");
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(importiert), Antwort = ziel };
            Dienste.Datei = datei;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) }, ios: true,
                                               anreicherer: (pl, o, q, z, aus, pr) => Bilanz(aus));

            Assert.False((await h.OriginalWaehlen()).Verweigert);
            GebaeudeExportErgebnis e = await h.Anreichern("");

            Assert.True(e.Gespeichert, e.Text);
            Assert.Equal("haus_EPOS.ifc", datei.Vorschlag);
            Assert.Equal(ziel, Assert.Single(datei.Geoeffnet));
        }

        [Fact]
        public async Task Eine_Verweigerung_beim_Anreichern_fragt_keine_Datei_schreibt_nichts_und_teilt_nichts()
        {
            byte[] importiert = IfcAnreicherungTests.Fremdhaus();
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(importiert), Antwort = Path.Combine(_ordner, "nie.ifc") };
            Dienste.Datei = datei;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) }, ios: true,
                anreicherer: (pl, o, q, z, aus, pr) => GebaeudeAnreicherungBilanz.Verweigerung(new[]
                {
                    new PruefMeldung(PruefStufe.Fehler, "GEXP_PROT_ANR_ENTITAET_FEHLT", "2Fremd0Wand000000MitU0"),
                }));

            Assert.False((await h.OriginalWaehlen()).Verweigert);
            GebaeudeExportErgebnis e = await h.Anreichern("");

            Assert.False(e.Gespeichert);
            Assert.False(e.Abgebrochen);
            Assert.Equal(R.GEXP_ANR_MSG_NICHTS_GESCHRIEBEN, e.Text);
            Assert.Equal("GEXP_PROT_ANR_ENTITAET_FEHLT", Assert.Single(e.Meldungen!).Schluessel);
            Assert.Equal(0, datei.Wahlen);
            Assert.Empty(datei.Geoeffnet);
            Assert.False(File.Exists(Path.Combine(_ordner, "nie.ifc")));

            // Ebenso eine Bilanz ohne Bytes.
            GebaeudeExportHuelle leer = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) },
                anreicherer: (pl, o, q, z, aus, pr) => new GebaeudeAnreicherungBilanz(true, false, 0, 0, 0, 0, null, 0));
            Assert.False((await leer.OriginalWaehlen()).Verweigert);
            Assert.False((await leer.Anreichern("")).Gespeichert);
            Assert.Equal(0, datei.Wahlen);
        }

        [Fact]
        public async Task Der_Kernweg_reichert_die_Originaldatei_an_und_laesst_sie_unveraendert()
        {
            byte[] importiert = IfcAnreicherungTests.Fremdhaus();
            string ziel = Path.Combine(_ordner, "fremdhaus_EPOS.ifc");
            var datei = new Dateiprobe { OeffnenPfad = OriginalAblegen(importiert, "fremdhaus.ifc"), Antwort = ziel };
            Dienste.Datei = datei;
            GebaeudeExportHuelle h = AnrHuelle(new List<ImportquelleModel> { IfcAnreicherungTests.Quelle(importiert) });

            Assert.False((await h.OriginalWaehlen()).Verweigert);
            GebaeudeExportErgebnis e = await h.Anreichern("");

            Assert.True(e.Gespeichert, e.Text);
            Assert.Equal("fremdhaus_EPOS.ifc", datei.Vorschlag);
            Assert.StartsWith("ISO-10303-21;", File.ReadAllText(ziel, Encoding.ASCII), StringComparison.Ordinal);
            Assert.NotEqual(importiert, File.ReadAllBytes(ziel));
            Assert.Equal(importiert, File.ReadAllBytes(datei.OeffnenPfad));   // das Original bleibt unverändert
        }

        [Fact]
        public void Der_Parametersatz_reicht_die_Gaben_der_Anreicherung()
        {
            IReadOnlyDictionary<string, object> gaben = Huelle().Gaben(geaendert: false);
            Assert.IsType<Func<Task<bool>>>(gaben["AnreicherungMoeglich"]);
            Assert.IsType<Func<Task<GebaeudeAnreicherungWahl>>>(gaben["OriginalWaehlen"]);
            Assert.IsType<Func<GebaeudeExportEingabe, Task<GebaeudeExportErgebnis>>>(gaben["Anreichern"]);
            Assert.True(GebaeudeExportHuelle.Formate().Single(f => f.Wert == GebaeudeQuelle.FORMAT_IFC).MitAnreicherung);
        }

        [Fact]
        public void Die_Quelle_steht_mit_Dateiname_und_Importzeitpunkt()
        {
            var q = new ImportquelleModel { Dateiname = "haus.ifc", Zeitpunkt = "2026-09-26T10:00:00" };
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.GEXP_ANR_QUELLE, "haus.ifc",
                                       new DateTime(2026, 9, 26, 10, 0, 0).ToString("g", CultureInfo.CurrentCulture)),
                         GebaeudeExportHuelle.Quelltext(q));
            Assert.Equal(R.GEXP_ANR_KEINE_QUELLE, GebaeudeExportHuelle.Quelltext(null));
            Assert.Contains("unbekannt", GebaeudeExportHuelle.Quelltext(new ImportquelleModel { Dateiname = "a.ifc", Zeitpunkt = "unbekannt" }));
        }
    }
}
